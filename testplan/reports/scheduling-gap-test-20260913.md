# 调度差距补全与测试报告

- Time: 2026-09-13
- Scope: `doc/keypoint/10` · `doc/keypoint/11` · cases · DotNet

## Docs

- `10-四向车调度完整逻辑.md` §9：已落地表 + G-FW-01~10 差距矩阵 + 决策速查
- `11-立库调度完整逻辑.md` §8：已落地表 + G-STK-01~10；**G-STK-08 Feedback NG 已修**

## Code fix

- `StackerDestinationService.HandleSegmentFeedbackAsync`：FeedbackCode≠OK（且非空）时不完成段、不推进

## Cases

- `testplan/cases/fourway-scheduling-singapore.md`：ExistingTest / Gap 对齐；新增 TC-FW-SG-042
- `testplan/cases/stacker-scheduling-srm-demo.md`：新增 TC-SRM-DEMO-005 / 023；030→P0

## New / extended tests

- `FourWaySingaporeMapTests`（001 Dijkstra / 002 占边 / 003 选图）
- `StackerPackTests.SelectAisle_ShouldSkip_WhenHeightExceedsMaxHeight`（030）
- `StackerPackTests.HandleSegmentFeedback_FeedbackCodeNg_ShouldNotAdvance`（005）

## DotNet result

```
filter: FourWay* + Stacker* scheduling related
Passed: 60  Failed: 0  Skipped: 0
```

Command:

```powershell
cd Seven.Net8
dotnet test Seven.Tests\Seven.Tests.csproj -o ..\testplan\reports\testbin `
  --filter "FullyQualifiedName~FourWay|FullyQualifiedName~Stacker" `
  --results-directory ..\testplan\reports\dotnet-sched
```
