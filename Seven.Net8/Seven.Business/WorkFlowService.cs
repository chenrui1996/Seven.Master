using System.Data;
using System.Data.Common;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Seven.Application.Interfaces;
using Seven.Business.WorkFlow;
using Seven.Domain.Common;
using Seven.Domain.Entities.Flow;
using Seven.Domain.Enums;
using Seven.Infrastructure.Mail;
using Seven.Infrastructure.Persistence;
using Seven.Infrastructure.Services;
namespace Seven.Business;

/// <summary>工作流引擎：定义、提交、多步审批、驳回</summary>
public class WorkFlowService : IWorkFlowService
{
    private static readonly System.Text.RegularExpressions.Regex SqlIdentifierRegex =
        new(@"^[A-Za-z_][A-Za-z0-9_]*$", System.Text.RegularExpressions.RegexOptions.Compiled);

    private readonly SevenDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IEmailService _email;
    private readonly ILogger<WorkFlowService> _logger;

    public WorkFlowService(
        SevenDbContext db,
        ICurrentUserService currentUser,
        IEmailService email,
        ILogger<WorkFlowService> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _email = email;
        _logger = logger;
    }

    public async Task<WebResponseContent> ListDefinitionsAsync(CancellationToken cancellationToken = default)
    {
        var list = await _db.Sys_WorkFlows.AsNoTracking()
            .Where(f => !f.IsDeleted)
            .OrderByDescending(f => f.CreateDate)
            .Select(f => new
            {
                f.WorkFlow_Id,
                f.WorkName,
                f.WorkTable,
                f.WorkTableKey,
                f.Enable,
                f.CreateDate,
                StepCount = f.Steps.Count(s => !s.IsDeleted),
            })
            .ToListAsync(cancellationToken);
        return WebResponseContent.Ok(data: list);
    }

    public async Task<WebResponseContent> GetDefinitionAsync(int workFlowId, CancellationToken cancellationToken = default)
    {
        var flow = await _db.Sys_WorkFlows.AsNoTracking()
            .Include(f => f.Steps.Where(s => !s.IsDeleted))
            .FirstOrDefaultAsync(f => f.WorkFlow_Id == workFlowId && !f.IsDeleted, cancellationToken);
        if (flow == null) return WebResponseContent.Error("流程不存在");
        flow.Steps = flow.Steps.OrderBy(s => s.StepOrder).ToList();
        return WebResponseContent.Ok(data: flow);
    }

    public async Task<WebResponseContent> SaveDefinitionAsync(WorkFlowDefinitionRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.WorkName))
            return WebResponseContent.Error("流程名称不能为空");
        if (string.IsNullOrWhiteSpace(request.WorkTable))
            return WebResponseContent.Error("业务表名不能为空");

        Sys_WorkFlow flow;
        if (request.WorkFlow_Id > 0)
        {
            flow = await _db.Sys_WorkFlows.Include(f => f.Steps)
                .FirstOrDefaultAsync(f => f.WorkFlow_Id == request.WorkFlow_Id && !f.IsDeleted, cancellationToken)
                ?? throw new InvalidOperationException("流程不存在");
            flow.WorkName = request.WorkName.Trim();
            flow.WorkTable = request.WorkTable.Trim();
            flow.WorkTableName = request.WorkTableName?.Trim();
            flow.WorkTableKey = string.IsNullOrWhiteSpace(request.WorkTableKey) ? "Id" : request.WorkTableKey.Trim();
            flow.Weight = request.Weight;
            flow.Enable = request.Enable ?? 1;
            flow.NodeConfig = request.NodeConfig;
            flow.LineConfig = request.LineConfig;
            flow.Remark = request.Remark;
            flow.AuditingEdit = request.AuditingEdit;
            flow.ModifyDate = DateTime.Now;
            flow.Modifier = _currentUser.UserName;

            foreach (var old in flow.Steps)
                old.IsDeleted = true;
        }
        else
        {
            flow = new Sys_WorkFlow
            {
                WorkName = request.WorkName.Trim(),
                WorkTable = request.WorkTable.Trim(),
                WorkTableName = request.WorkTableName?.Trim(),
                WorkTableKey = string.IsNullOrWhiteSpace(request.WorkTableKey) ? "Id" : request.WorkTableKey.Trim(),
                Weight = request.Weight,
                Enable = request.Enable ?? 1,
                NodeConfig = request.NodeConfig,
                LineConfig = request.LineConfig,
                Remark = request.Remark,
                AuditingEdit = request.AuditingEdit,
                CreateDate = DateTime.Now,
                Creator = _currentUser.UserName,
                CreateId = _currentUser.UserId,
            };
            _db.Sys_WorkFlows.Add(flow);
        }

        await _db.SaveChangesAsync(cancellationToken);

        var order = 1;
        var orderedSteps = request.Steps.OrderBy(s => s.StepOrder).ToList();
        foreach (var step in orderedSteps)
        {
            var stepId = string.IsNullOrWhiteSpace(step.StepId) ? Guid.NewGuid().ToString("N")[..8] : step.StepId.Trim();
            _db.Sys_WorkFlowSteps.Add(new Sys_WorkFlowStep
            {
                WorkFlow_Id = flow.WorkFlow_Id,
                StepId = stepId,
                StepName = string.IsNullOrWhiteSpace(step.StepName) ? $"步骤{order}" : step.StepName.Trim(),
                StepOrder = order,
                StepType = step.StepType ?? (int)WorkFlowStepType.Role,
                StepValue = step.StepValue,
                StepAttrType = string.IsNullOrWhiteSpace(step.StepAttrType) ? "node" : step.StepAttrType.Trim(),
                NextStepIds = step.NextStepIds,
                ParentId = step.ParentId,
                Weight = step.Weight,
                Filters = step.Filters,
                AuditRefuse = step.AuditRefuse,
                AuditBack = step.AuditBack,
                AuditMethod = step.AuditMethod,
                SendMail = step.SendMail,
                Remark = step.Remark,
                CreateDate = DateTime.Now,
                Creator = _currentUser.UserName,
            });
            order++;
        }

        // 线性默认连线：若未传 LineConfig，按顺序生成
        if (string.IsNullOrWhiteSpace(flow.LineConfig) && orderedSteps.Count > 0)
        {
            var ids = _db.Sys_WorkFlowSteps.Local
                .Where(s => s.WorkFlow_Id == flow.WorkFlow_Id && !s.IsDeleted)
                .OrderBy(s => s.StepOrder)
                .Select(s => s.StepId)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .ToList();
            var lines = new List<object>();
            for (var i = 0; i < ids.Count - 1; i++)
                lines.Add(new { from = ids[i], to = ids[i + 1] });
            flow.LineConfig = JsonSerializer.Serialize(new { lines });
        }

        await _db.SaveChangesAsync(cancellationToken);
        return WebResponseContent.Ok("保存成功", new { flow.WorkFlow_Id });
    }

    public async Task<WebResponseContent> DeleteDefinitionsAsync(int[] ids, CancellationToken cancellationToken = default)
    {
        var flows = await _db.Sys_WorkFlows.Where(f => ids.Contains(f.WorkFlow_Id) && !f.IsDeleted).ToListAsync(cancellationToken);
        foreach (var f in flows)
        {
            f.IsDeleted = true;
            f.ModifyDate = DateTime.Now;
        }
        await _db.SaveChangesAsync(cancellationToken);
        return WebResponseContent.Ok("删除成功");
    }

    public async Task<WebResponseContent> SubmitAsync(string tableName, string tableKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tableName) || string.IsNullOrWhiteSpace(tableKey))
            return WebResponseContent.Error("业务表或主键不能为空");

        var flow = await _db.Sys_WorkFlows
            .Include(f => f.Steps.Where(s => !s.IsDeleted))
            .Where(f => f.WorkTable == tableName && f.Enable == 1 && !f.IsDeleted)
            .OrderByDescending(f => f.Weight ?? 0)
            .ThenByDescending(f => f.WorkFlow_Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (flow == null) return WebResponseContent.Error("未配置工作流");

        var nodeSteps = flow.Steps.Where(s => !s.IsDeleted && (s.StepAttrType == null || s.StepAttrType == "node"))
            .OrderBy(s => s.StepOrder)
            .ThenByDescending(s => s.Weight ?? 0)
            .ToList();
        if (nodeSteps.Count == 0)
            nodeSteps = flow.Steps.Where(s => !s.IsDeleted).OrderBy(s => s.StepOrder).ToList();
        if (nodeSteps.Count == 0) return WebResponseContent.Error("流程未配置步骤");

        var exists = await _db.Sys_WorkFlowTables.AnyAsync(t =>
            t.WorkTable == tableName
            && t.WorkTableKey == tableKey
            && (t.AuditStatus == (int)AuditStatus.Pending || t.AuditStatus == (int)AuditStatus.InProgress)
            && !t.IsDeleted, cancellationToken);
        if (exists) return WebResponseContent.Error("该单据已在审批中");

        var bizData = await TryLoadBusinessRowAsync(tableName, tableKey, flow.WorkTableKey, cancellationToken);
        var applicable = nodeSteps
            .Where(s => WorkFlowFilterEvaluator.Matches(s.Filters, bizData))
            .OrderBy(s => s.StepOrder)
            .ThenByDescending(s => s.Weight ?? 0)
            .ToList();
        if (applicable.Count == 0)
            return WebResponseContent.Error("业务数据未匹配任何审批步骤条件");

        var first = applicable[0];
        var instance = new Sys_WorkFlowTable
        {
            WorkFlow_Id = flow.WorkFlow_Id,
            WorkTable = tableName,
            WorkTableKey = tableKey,
            AuditStatus = (int)AuditStatus.InProgress,
            CurrentStepId = first.WorkStepFlow_Id,
            CreateDate = DateTime.Now,
            Creator = _currentUser.UserName,
            CreateId = _currentUser.UserId,
        };
        _db.Sys_WorkFlowTables.Add(instance);
        await _db.SaveChangesAsync(cancellationToken);

        // 实例步骤：全部节点（含未命中条件的，便于进度展示）；当前从首个命中开始
        foreach (var step in nodeSteps)
        {
            _db.Sys_WorkFlowTableSteps.Add(new Sys_WorkFlowTableStep
            {
                WorkFlowTable_Id = instance.WorkFlowTable_Id,
                WorkStepFlow_Id = step.WorkStepFlow_Id,
                StepName = step.StepName,
                AuditStatus = (int)AuditStatus.Pending,
                CreateDate = DateTime.Now,
            });
        }

        await TryWriteBusinessAuditStatusAsync(tableName, tableKey, (int)AuditStatus.InProgress, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        await TryNotifyWorkFlowEmailAsync(instance.CreateId, "工作流已提交",
            $"您提交的业务单据已进入审批流程（表 {tableName}，主键 {tableKey}）。", cancellationToken);
        return WebResponseContent.Ok("提交审批成功", instance);
    }

    public async Task<WebResponseContent> AuditAsync(int workFlowTableId, int auditStatus, string? remark, CancellationToken cancellationToken = default)
    {
        if (auditStatus is not ((int)AuditStatus.Approved or (int)AuditStatus.Returned or (int)AuditStatus.Rejected))
            return WebResponseContent.Error("无效的审批结果");

        var instance = await _db.Sys_WorkFlowTables
            .FirstOrDefaultAsync(t => t.WorkFlowTable_Id == workFlowTableId && !t.IsDeleted, cancellationToken);
        if (instance == null) return WebResponseContent.Error("流程实例不存在");
        if (instance.AuditStatus is (int)AuditStatus.Approved or (int)AuditStatus.Rejected)
            return WebResponseContent.Error("流程已结束");

        var currentStepId = instance.CurrentStepId;
        if (currentStepId == null) return WebResponseContent.Error("当前步骤无效");

        if (!await CanCurrentUserAuditAsync(currentStepId.Value, cancellationToken))
            return WebResponseContent.Error("无权审批当前步骤");

        var currentDef = await _db.Sys_WorkFlowSteps.AsNoTracking()
            .FirstOrDefaultAsync(s => s.WorkStepFlow_Id == currentStepId.Value && !s.IsDeleted, cancellationToken);

        // 会签：同一用户不可重复通过
        if (auditStatus == (int)AuditStatus.Approved && currentDef?.AuditMethod == 1 && _currentUser.UserId is int me)
        {
            var already = await _db.Sys_WorkFlowTableAuditLogs.AsNoTracking()
                .AnyAsync(l => l.WorkFlowTable_Id == workFlowTableId
                    && l.WorkStepFlow_Id == currentStepId.Value
                    && l.AuditStatus == (int)AuditStatus.Approved
                    && l.AuditUser == _currentUser.UserName
                    && !l.IsDeleted, cancellationToken);
            if (already) return WebResponseContent.Error("您已完成会签，请等待其他审批人");
        }

        var tableStep = await _db.Sys_WorkFlowTableSteps
            .FirstOrDefaultAsync(s => s.WorkFlowTable_Id == workFlowTableId
                && s.WorkStepFlow_Id == currentStepId && !s.IsDeleted, cancellationToken);

        _db.Sys_WorkFlowTableAuditLogs.Add(new Sys_WorkFlowTableAuditLog
        {
            WorkFlowTable_Id = workFlowTableId,
            WorkStepFlow_Id = currentStepId.Value,
            AuditStatus = auditStatus,
            Remark = remark,
            AuditUser = _currentUser.UserName,
            CreateDate = DateTime.Now,
        });

        // 拒绝 / 驳回：立即结束或回退（会签中途拒绝也结束）
        var flowSteps = await _db.Sys_WorkFlowSteps.AsNoTracking()
            .Where(s => s.WorkFlow_Id == instance.WorkFlow_Id && !s.IsDeleted)
            .OrderBy(s => s.StepOrder)
            .ToListAsync(cancellationToken);
        var idx = flowSteps.FindIndex(s => s.WorkStepFlow_Id == currentStepId.Value);

        if (auditStatus == (int)AuditStatus.Rejected)
        {
            if (tableStep != null)
            {
                tableStep.AuditStatus = auditStatus;
                tableStep.AuditUserId = _currentUser.UserId;
                tableStep.Auditor = _currentUser.UserName;
                tableStep.Remark = remark;
                tableStep.AuditDate = DateTime.Now;
            }
            instance.AuditStatus = auditStatus;
            instance.CurrentStepId = null;
            instance.ModifyDate = DateTime.Now;
            await TryWriteBusinessAuditStatusAsync(instance.WorkTable, instance.WorkTableKey, auditStatus, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
            await TryNotifyWorkFlowEmailAsync(instance.CreateId, "工作流审批结果：已拒绝",
                $"您的单据审批已被拒绝。意见：{remark ?? "无"}", cancellationToken);
            return WebResponseContent.Ok("已拒绝");
        }

        if (auditStatus == (int)AuditStatus.Returned)
        {
            if (tableStep != null)
            {
                tableStep.AuditStatus = auditStatus;
                tableStep.AuditUserId = _currentUser.UserId;
                tableStep.Auditor = _currentUser.UserName;
                tableStep.Remark = remark;
                tableStep.AuditDate = DateTime.Now;
            }

            if (idx > 0)
            {
                var prevStepId = flowSteps[idx - 1].WorkStepFlow_Id;
                instance.CurrentStepId = prevStepId;
                instance.AuditStatus = (int)AuditStatus.InProgress;
                instance.ModifyDate = DateTime.Now;

                var prevTableStep = await _db.Sys_WorkFlowTableSteps
                    .FirstOrDefaultAsync(s => s.WorkFlowTable_Id == workFlowTableId
                        && s.WorkStepFlow_Id == prevStepId && !s.IsDeleted, cancellationToken);
                ResetTableStepToPending(prevTableStep);
                ResetTableStepToPending(tableStep);

                await TryWriteBusinessAuditStatusAsync(instance.WorkTable, instance.WorkTableKey, (int)AuditStatus.InProgress, cancellationToken);
                await _db.SaveChangesAsync(cancellationToken);
                await TryNotifyWorkFlowEmailAsync(instance.CreateId, "工作流审批：已驳回",
                    $"您的单据已被驳回至上一步。意见：{remark ?? "无"}", cancellationToken);
                return WebResponseContent.Ok("已驳回至上一步");
            }

            instance.AuditStatus = auditStatus;
            instance.CurrentStepId = null;
            instance.ModifyDate = DateTime.Now;
            await TryWriteBusinessAuditStatusAsync(instance.WorkTable, instance.WorkTableKey, auditStatus, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
            await TryNotifyWorkFlowEmailAsync(instance.CreateId, "工作流审批：已驳回",
                $"您的单据已被驳回。意见：{remark ?? "无"}", cancellationToken);
            return WebResponseContent.Ok("已驳回");
        }

        // 通过：会签需全部审批人完成
        if (currentDef?.AuditMethod == 1)
        {
            await _db.SaveChangesAsync(cancellationToken);
            var pending = await GetCountersignPendingAsync(currentDef, workFlowTableId, currentStepId.Value, cancellationToken);
            if (pending.Count > 0)
            {
                if (tableStep != null)
                {
                    tableStep.AuditStatus = (int)AuditStatus.InProgress;
                    tableStep.ModifyDate = DateTime.Now;
                    tableStep.Remark = $"会签进行中，待：{string.Join(',', pending)}";
                }
                await _db.SaveChangesAsync(cancellationToken);
                return WebResponseContent.Ok($"会签已记录，仍待 {pending.Count} 人审批");
            }
        }

        if (tableStep != null)
        {
            tableStep.AuditStatus = (int)AuditStatus.Approved;
            tableStep.AuditUserId = _currentUser.UserId;
            tableStep.Auditor = _currentUser.UserName;
            tableStep.Remark = remark;
            tableStep.AuditDate = DateTime.Now;
            tableStep.ModifyDate = DateTime.Now;
        }

        // 通过 → 条件匹配的下一步或结束
        var bizData = await TryLoadBusinessRowAsync(instance.WorkTable, instance.WorkTableKey, null, cancellationToken);
        var next = ResolveNextStep(flowSteps, currentDef, currentStepId.Value, bizData);
        if (next == null)
        {
            instance.AuditStatus = (int)AuditStatus.Approved;
            instance.CurrentStepId = null;
            instance.ModifyDate = DateTime.Now;
            await TryWriteBusinessAuditStatusAsync(instance.WorkTable, instance.WorkTableKey, (int)AuditStatus.Approved, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
            await TryNotifyWorkFlowEmailAsync(instance.CreateId, "工作流审批完成",
                "您的单据已全部审批通过。", cancellationToken);
            return WebResponseContent.Ok("审批完成（全部通过）");
        }

        instance.CurrentStepId = next.WorkStepFlow_Id;
        instance.AuditStatus = (int)AuditStatus.InProgress;
        instance.ModifyDate = DateTime.Now;

        var nextTableStep = await _db.Sys_WorkFlowTableSteps
            .FirstOrDefaultAsync(s => s.WorkFlowTable_Id == workFlowTableId
                && s.WorkStepFlow_Id == next.WorkStepFlow_Id && !s.IsDeleted, cancellationToken);
        if (nextTableStep != null && nextTableStep.AuditStatus != (int)AuditStatus.Pending)
            ResetTableStepToPending(nextTableStep);

        await _db.SaveChangesAsync(cancellationToken);
        await TryNotifyWorkFlowEmailAsync(instance.CreateId, "工作流审批：已通过",
            "您的单据当前步骤已通过，已进入下一步审批。", cancellationToken);
        return WebResponseContent.Ok("已通过，进入下一步");
    }

    static Sys_WorkFlowStep? ResolveNextStep(
        List<Sys_WorkFlowStep> flowSteps,
        Sys_WorkFlowStep? current,
        int currentStepId,
        IReadOnlyDictionary<string, object?>? bizData)
    {
        var nodeSteps = flowSteps
            .Where(s => s.StepAttrType == null || s.StepAttrType == "node" || s.StepAttrType == "start")
            .OrderBy(s => s.StepOrder)
            .ToList();

        // NextStepIds 显式下一跳（可多选，按条件筛选）
        if (!string.IsNullOrWhiteSpace(current?.NextStepIds))
        {
            var ids = current.NextStepIds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var candidates = nodeSteps
                .Where(s => ids.Contains(s.StepId, StringComparer.OrdinalIgnoreCase)
                            || ids.Contains(s.WorkStepFlow_Id.ToString(), StringComparer.Ordinal))
                .Where(s => WorkFlowFilterEvaluator.Matches(s.Filters, bizData))
                .OrderBy(s => s.StepOrder)
                .ThenByDescending(s => s.Weight ?? 0)
                .ToList();
            if (candidates.Count > 0) return candidates[0];
            // 指向 end
            if (ids.Any(x => string.Equals(x, "end", StringComparison.OrdinalIgnoreCase)))
                return null;
        }

        var idx = nodeSteps.FindIndex(s => s.WorkStepFlow_Id == currentStepId);
        if (idx < 0) return null;
        for (var i = idx + 1; i < nodeSteps.Count; i++)
        {
            if (WorkFlowFilterEvaluator.Matches(nodeSteps[i].Filters, bizData))
                return nodeSteps[i];
        }

        return null;
    }

    async Task<List<string>> GetCountersignPendingAsync(
        Sys_WorkFlowStep step,
        int workFlowTableId,
        int stepId,
        CancellationToken ct)
    {
        // 会签仅对「按用户」严格校验；角色/部门按 StepValue 中的用户 Id 列表解读
        var requiredIds = (step.StepValue ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(v => int.TryParse(v, out var id) ? id : 0)
            .Where(id => id > 0)
            .Distinct()
            .ToList();
        if (requiredIds.Count == 0) return [];

        var approvedUsers = await _db.Sys_WorkFlowTableAuditLogs.AsNoTracking()
            .Where(l => l.WorkFlowTable_Id == workFlowTableId
                && l.WorkStepFlow_Id == stepId
                && l.AuditStatus == (int)AuditStatus.Approved
                && !l.IsDeleted)
            .Select(l => l.AuditUser)
            .ToListAsync(ct);

        var userNames = await _db.Sys_Users.AsNoTracking()
            .Where(u => requiredIds.Contains(u.User_Id) && !u.IsDeleted)
            .Select(u => new { u.User_Id, u.UserName, u.UserTrueName })
            .ToListAsync(ct);

        var pending = new List<string>();
        foreach (var u in userNames)
        {
            if (approvedUsers.Any(a =>
                    string.Equals(a, u.UserName, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(a, u.UserTrueName, StringComparison.OrdinalIgnoreCase)))
                continue;
            pending.Add(u.UserTrueName ?? u.UserName);
        }

        return pending;
    }

    async Task<Dictionary<string, object?>?> TryLoadBusinessRowAsync(
        string? tableName,
        string? tableKey,
        string? keyColHint,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(tableName) || string.IsNullOrWhiteSpace(tableKey)) return null;
        if (!SqlIdentifierRegex.IsMatch(tableName)) return null;

        var keyCol = string.IsNullOrWhiteSpace(keyColHint)
            ? await _db.Sys_WorkFlows.AsNoTracking()
                .Where(f => f.WorkTable == tableName && !f.IsDeleted)
                .Select(f => f.WorkTableKey)
                .FirstOrDefaultAsync(ct) ?? "Id"
            : keyColHint;
        keyCol = string.IsNullOrWhiteSpace(keyCol) ? "Id" : keyCol.Trim();
        if (!SqlIdentifierRegex.IsMatch(keyCol)) return null;

        try
        {
            var conn = _db.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open)
                await conn.OpenAsync(ct);

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT * FROM `{tableName}` WHERE CAST(`{keyCol}` AS CHAR) = @key LIMIT 1";
            var p = cmd.CreateParameter();
            p.ParameterName = "@key";
            p.Value = tableKey;
            cmd.Parameters.Add(p);

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            if (!await reader.ReadAsync(ct)) return null;

            var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < reader.FieldCount; i++)
            {
                var name = reader.GetName(i);
                dict[name] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            }

            return dict;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "加载业务行失败 Table={Table} Key={Key}", tableName, tableKey);
            return null;
        }
    }

    static void ResetTableStepToPending(Sys_WorkFlowTableStep? step)
    {
        if (step == null) return;
        step.AuditStatus = (int)AuditStatus.Pending;
        step.AuditUserId = null;
        step.Auditor = null;
        step.Remark = null;
        step.AuditDate = null;
        step.ModifyDate = DateTime.Now;
    }

    public async Task<WebResponseContent> GetStepsAsync(
        string tableName,
        IReadOnlyList<string> ids,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tableName) || ids == null || ids.Count == 0)
            return WebResponseContent.Error("参数无效");

        var keyList = ids.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
        var flows = await _db.Sys_WorkFlowTables.AsNoTracking()
            .Where(x => x.WorkTable == tableName && keyList.Contains(x.WorkTableKey!) && !x.IsDeleted)
            .OrderByDescending(x => x.CreateDate)
            .ToListAsync(cancellationToken);

        if (flows.Count == 0)
            return WebResponseContent.Ok(data: new { hasFlow = false });

        if (flows.Count > 1 && keyList.Count > 1)
            return WebResponseContent.Error("只能选择一条数据进行审核");

        var flow = flows[0];
        var tableSteps = await _db.Sys_WorkFlowTableSteps.AsNoTracking()
            .Where(s => s.WorkFlowTable_Id == flow.WorkFlowTable_Id && !s.IsDeleted)
            .ToListAsync(cancellationToken);
        var defSteps = await _db.Sys_WorkFlowSteps.AsNoTracking()
            .Where(s => s.WorkFlow_Id == flow.WorkFlow_Id && !s.IsDeleted)
            .OrderBy(s => s.StepOrder)
            .ToListAsync(cancellationToken);
        var logs = await _db.Sys_WorkFlowTableAuditLogs.AsNoTracking()
            .Where(l => l.WorkFlowTable_Id == flow.WorkFlowTable_Id && !l.IsDeleted)
            .OrderBy(l => l.CreateDate)
            .ToListAsync(cancellationToken);

        var list = defSteps.Select(def =>
        {
            var ts = tableSteps.FirstOrDefault(t => t.WorkStepFlow_Id == def.WorkStepFlow_Id);
            var isCurrent = flow.CurrentStepId == def.WorkStepFlow_Id
                && flow.AuditStatus is (int)AuditStatus.Pending or (int)AuditStatus.InProgress;
            return new
            {
                def.WorkStepFlow_Id,
                def.StepId,
                StepName = ts?.StepName ?? def.StepName,
                def.StepOrder,
                def.StepType,
                def.StepValue,
                def.StepAttrType,
                AuditStatus = ts?.AuditStatus,
                Auditor = ts?.Auditor,
                AuditDate = ts?.AuditDate,
                Remark = ts?.Remark,
                isCurrent,
                isCurrentUser = isCurrent, // 前端结合权限再判；服务端在 audit 时校验
            };
        }).OrderBy(x => x.StepOrder).ToList();

        return WebResponseContent.Ok(data: new
        {
            hasFlow = true,
            workFlowTableId = flow.WorkFlowTable_Id,
            currentStepId = flow.CurrentStepId,
            auditStatus = flow.AuditStatus,
            list,
            log = logs.Select(l => new
            {
                l.Id,
                l.WorkStepFlow_Id,
                l.AuditUser,
                l.AuditStatus,
                l.Remark,
                l.CreateDate,
            }),
        });
    }

    public async Task<WebResponseContent> GetNodeDicAsync(CancellationToken cancellationToken = default)
    {
        var users = await _db.Sys_Users.AsNoTracking()
            .Where(u => !u.IsDeleted)
            .OrderBy(u => u.User_Id)
            .Take(5000)
            .Select(u => new { key = u.User_Id, value = u.UserTrueName ?? u.UserName })
            .ToListAsync(cancellationToken);
        var roles = await _db.Sys_Roles.AsNoTracking()
            .Where(r => !r.IsDeleted)
            .Select(r => new { key = r.Role_Id, value = r.RoleName })
            .ToListAsync(cancellationToken);
        var dept = await _db.Sys_Departments.AsNoTracking()
            .Where(d => !d.IsDeleted)
            .Select(d => new { key = d.DepartmentId, value = d.DepartmentName })
            .ToListAsync(cancellationToken);
        return WebResponseContent.Ok(data: new { users, roles, dept });
    }

    public async Task<PageGridData<Sys_WorkFlowTable>> GetPageDataAsync(PageDataOptions options, CancellationToken cancellationToken = default)
    {
        var query = _db.Sys_WorkFlowTables.AsNoTracking().Where(t => !t.IsDeleted);
        var filter = ReadWhereValue(options.Wheres, "scope") ?? ReadWhereValue(options.Wheres, "filter");

        if (string.Equals(filter, "todo", StringComparison.OrdinalIgnoreCase) && _currentUser.UserId is int uid)
        {
            var stepIds = await GetAuditableStepIdsAsync(uid, _currentUser.RoleId, cancellationToken);
            query = query.Where(t =>
                (t.AuditStatus == (int)AuditStatus.Pending || t.AuditStatus == (int)AuditStatus.InProgress)
                && t.CurrentStepId != null
                && stepIds.Contains(t.CurrentStepId.Value));
        }
        else if (string.Equals(filter, "done", StringComparison.OrdinalIgnoreCase) && _currentUser.UserId is int uid2)
        {
            var doneIds = await _db.Sys_WorkFlowTableAuditLogs.AsNoTracking()
                .Where(l => l.AuditUser == _currentUser.UserName)
                .Select(l => l.WorkFlowTable_Id)
                .Distinct()
                .ToListAsync(cancellationToken);
            query = query.Where(t => doneIds.Contains(t.WorkFlowTable_Id));
        }

        query = query.OrderByDescending(w => w.CreateDate);
        return await CrudHelper.PaginateAsync(query, options, cancellationToken);
    }

    async Task<bool> CanCurrentUserAuditAsync(int stepId, CancellationToken ct)
    {
        if (_currentUser.UserId is not int uid) return false;
        var step = await _db.Sys_WorkFlowSteps.AsNoTracking()
            .FirstOrDefaultAsync(s => s.WorkStepFlow_Id == stepId && !s.IsDeleted, ct);
        if (step == null) return false;

        // 超级管理员（角色名或 RoleId=1）放行
        if (_currentUser.RoleId == 1) return true;

        var type = (WorkFlowStepType)(step.StepType ?? (int)WorkFlowStepType.Role);
        var value = step.StepValue ?? string.Empty;
        return type switch
        {
            WorkFlowStepType.User => value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Any(v => int.TryParse(v, out var id) && id == uid),
            WorkFlowStepType.Role => _currentUser.RoleId is int rid
                && value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Any(v => int.TryParse(v, out var id) && id == rid),
            WorkFlowStepType.Department => await UserInDepartmentsAsync(uid, value, ct),
            _ => false,
        };
    }

    async Task<List<int>> GetAuditableStepIdsAsync(int userId, int? roleId, CancellationToken ct)
    {
        var steps = await _db.Sys_WorkFlowSteps.AsNoTracking().Where(s => !s.IsDeleted).ToListAsync(ct);
        var result = new List<int>();
        foreach (var step in steps)
        {
            var type = (WorkFlowStepType)(step.StepType ?? (int)WorkFlowStepType.Role);
            var value = step.StepValue ?? string.Empty;
            var ok = type switch
            {
                WorkFlowStepType.User => value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Any(v => int.TryParse(v, out var id) && id == userId),
                WorkFlowStepType.Role => roleId is int rid
                    && value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .Any(v => int.TryParse(v, out var id) && id == rid),
                WorkFlowStepType.Department => await UserInDepartmentsAsync(userId, value, ct),
                _ => false,
            };
            if (ok || roleId == 1) result.Add(step.WorkStepFlow_Id);
        }
        return result.Distinct().ToList();
    }

    async Task<bool> UserInDepartmentsAsync(int userId, string deptCsv, CancellationToken ct)
    {
        var deptIds = deptCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(v => int.TryParse(v, out var id) ? id : 0).Where(id => id > 0).ToHashSet();
        if (deptIds.Count == 0) return false;

        var user = await _db.Sys_Users.AsNoTracking().FirstOrDefaultAsync(u => u.User_Id == userId, ct);
        if (user?.DeptIds != null)
        {
            var userDepts = user.DeptIds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(v => int.TryParse(v, out var id) ? id : 0).Where(id => id > 0);
            if (userDepts.Any(deptIds.Contains)) return true;
        }

        return await _db.Sys_UserDepartments.AsNoTracking()
            .AnyAsync(ud => ud.UserId == userId && deptIds.Contains(ud.DepartmentId) && !ud.IsDeleted, ct);
    }

    static string? ReadWhereValue(string? wheresJson, string name)
    {
        if (string.IsNullOrWhiteSpace(wheresJson)) return null;
        try
        {
            using var doc = JsonDocument.Parse(wheresJson);
            if (doc.RootElement.ValueKind != JsonValueKind.Array) return null;
            foreach (var item in doc.RootElement.EnumerateArray())
            {
                if (!item.TryGetProperty("name", out var n)) continue;
                if (!string.Equals(n.GetString(), name, StringComparison.OrdinalIgnoreCase)) continue;
                if (!item.TryGetProperty("value", out var v)) return null;
                return v.ValueKind == JsonValueKind.String ? v.GetString() : v.ToString();
            }
        }
        catch { /* ignore */ }
        return null;
    }

    /// <summary>若业务表存在 AuditStatus 列则回写（原生 SQL，忽略失败）</summary>
    async Task TryWriteBusinessAuditStatusAsync(string? tableName, string? tableKey, int status, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(tableName) || string.IsNullOrWhiteSpace(tableKey)) return;
        if (!SqlIdentifierRegex.IsMatch(tableName)) return;

        var keyCol = await _db.Sys_WorkFlows.AsNoTracking()
            .Where(f => f.WorkTable == tableName && !f.IsDeleted)
            .Select(f => f.WorkTableKey)
            .FirstOrDefaultAsync(ct) ?? "Id";
        keyCol = string.IsNullOrWhiteSpace(keyCol) ? "Id" : keyCol.Trim();
        if (!SqlIdentifierRegex.IsMatch(keyCol)) return;

        try
        {
            await _db.Database.ExecuteSqlRawAsync(
                $"UPDATE `{tableName}` SET `AuditStatus` = {{0}} WHERE CAST(`{keyCol}` AS CHAR) = {{1}}",
                [status, tableKey],
                ct);
        }
        catch
        {
            // 业务表可能没有 AuditStatus / 主键列，忽略
        }
    }

    async Task TryNotifyWorkFlowEmailAsync(int? creatorUserId, string subject, string htmlBody, CancellationToken ct)
    {
        if (creatorUserId is not int uid || uid <= 0) return;

        try
        {
            var email = await _db.Sys_Users.AsNoTracking()
                .Where(u => u.User_Id == uid)
                .Select(u => u.Email)
                .FirstOrDefaultAsync(ct);
            if (string.IsNullOrWhiteSpace(email)) return;

            var result = await _email.SendAsync(email, subject, $"<p>{htmlBody}</p>", ct);
            if (!result.Status)
                _logger.LogDebug("工作流邮件未发送 UserId={UserId} Reason={Reason}", uid, result.Message);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "工作流邮件发送异常 UserId={UserId}", creatorUserId);
        }
    }
}
