# Prometheus 指标与监控栈 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** API 暴露 `/metrics`，Compose 提供 Prometheus + Grafana + cAdvisor + node-exporter，用于查看进程/HTTP 与容器/主机资源时间序列。

**Architecture:** `prometheus-net.AspNetCore` 提供 HttpMetrics 与 `/metrics`；`deploy/monitoring` 存放 Prometheus/Grafana 配置并由 `docker-compose.yml` 挂载；与 `/health`/`/health-ui` 分工不变。

**Tech Stack:** .NET 8、prometheus-net.AspNetCore 8.2.1、Prometheus、Grafana、cAdvisor、node-exporter、Docker Compose

## Global Constraints

- 规格：`docs/superpowers/specs/2026-08-25-prometheus-monitoring-design.md`
- 包：`prometheus-net.AspNetCore` **8.2.1**
- 端点：`/metrics` 匿名；宿主机端口 Grafana `3000`、Prometheus `9090`、cAdvisor `8081`、node-exporter `9100`
- Grafana 默认：`admin` / `admin`
- 不改 HealthChecks 行为；不加 `/metrics` 鉴权；不加业务自定义 Counter
- 提交：仅在用户明确要求时 `git commit`
- Working directory：`Seven.Master`（仓库根）

---

## File Structure

| 文件 | 职责 |
|------|------|
| `Seven.Net8/Seven.WebApi/Seven.WebApi.csproj` | NuGet |
| `Seven.Net8/Seven.WebApi/MetricsExtensions.cs` | `UseSevenMetrics` / `MapSevenMetrics` |
| `Seven.Net8/Seven.WebApi/Program.cs` | 接线 |
| `Seven.Net8/Seven.Tests/Integration/MetricsApiTests.cs` | `/metrics` 集成测试 |
| `docker-compose.yml` | 监控四服务 |
| `deploy/monitoring/prometheus.yml` | scrape 配置 |
| `deploy/monitoring/grafana/provisioning/datasources/datasource.yml` | Prometheus 数据源 |
| `deploy/monitoring/grafana/provisioning/dashboards/dashboards.yml` | dashboard provider |
| `deploy/monitoring/grafana/dashboards/seven-overview.json` | 基础看板 |
| `doc/18-指标与监控.md` | 使用说明 |
| `doc/README.md`、`doc/07-部署指南.md`、`doc/17-健康检查看板.md` | 交叉链接 |

---

### Task 1: API `/metrics` + 集成测试

**Files:**
- Modify: `Seven.Net8/Seven.WebApi/Seven.WebApi.csproj`
- Create: `Seven.Net8/Seven.WebApi/MetricsExtensions.cs`
- Modify: `Seven.Net8/Seven.WebApi/Program.cs`
- Create: `Seven.Net8/Seven.Tests/Integration/MetricsApiTests.cs`

**Interfaces:**
- Consumes: `Prometheus` ASP.NET Core 扩展
- Produces: `UseSevenMetrics(this WebApplication app)`、`MapSevenMetrics(this WebApplication app)`；端点 `/metrics`

- [ ] **Step 1: 写失败的集成测试**

创建 `Seven.Net8/Seven.Tests/Integration/MetricsApiTests.cs`：

```csharp
using System.Net;
using FluentAssertions;

namespace Seven.Tests.Integration;

public class MetricsApiTests : IClassFixture<SevenWebApplicationFactory>
{
    private readonly HttpClient _client;

    public MetricsApiTests(SevenWebApplicationFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Metrics_ShouldReturnPrometheusText()
    {
        var response = await _client.GetAsync("/metrics");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Match(b =>
            b.Contains("process_") || b.Contains("http_") || b.Contains("dotnet_"));
    }
}
```

- [ ] **Step 2: 跑测试确认失败**

```powershell
dotnet test Seven.Net8/Seven.Tests/Seven.Tests.csproj --filter "FullyQualifiedName~MetricsApiTests"
```

Expected: 失败（404 或连接失败，因尚未 MapMetrics）

- [ ] **Step 3: 安装包**

```powershell
dotnet add Seven.Net8/Seven.WebApi/Seven.WebApi.csproj package prometheus-net.AspNetCore --version 8.2.1
```

- [ ] **Step 4: 实现 `MetricsExtensions.cs`**

```csharp
using Prometheus;

namespace Seven.WebApi;

public static class MetricsExtensions
{
    /// <summary>
    /// 须尽可能靠前注册，以便在响应离开管道时记录最终状态码
    /// （异常中间件应位于其后，见 prometheus-net 文档）。
    /// </summary>
    public static WebApplication UseSevenMetrics(this WebApplication app)
    {
        app.UseHttpMetrics(options => options.ReduceStatusCodeCardinality());
        return app;
    }

    public static WebApplication MapSevenMetrics(this WebApplication app)
    {
        app.MapMetrics("/metrics");
        return app;
    }
}
```

- [ ] **Step 5: 接线 `Program.cs`**

在 `var app = builder.Build();` 之后、现有中间件之前插入：

```csharp
app.UseSevenMetrics();
```

在 `app.MapSevenHealthChecks();` 附近增加：

```csharp
app.MapSevenMetrics();
```

- [ ] **Step 6: 跑测试确认通过**

```powershell
dotnet test Seven.Net8/Seven.Tests/Seven.Tests.csproj --filter "FullyQualifiedName~MetricsApiTests"
```

Expected: 1 passed

- [ ] **Step 7: 确认健康检查未回归**

```powershell
dotnet test Seven.Net8/Seven.Tests/Seven.Tests.csproj --filter "FullyQualifiedName~HealthApiTests"
```

Expected: 3 passed

---

### Task 2: Prometheus / Grafana / cAdvisor / node-exporter Compose

**Files:**
- Create: `deploy/monitoring/prometheus.yml`
- Create: `deploy/monitoring/grafana/provisioning/datasources/datasource.yml`
- Create: `deploy/monitoring/grafana/provisioning/dashboards/dashboards.yml`
- Create: `deploy/monitoring/grafana/dashboards/seven-overview.json`
- Modify: `docker-compose.yml`

**Interfaces:**
- Consumes: Compose 网络内 `seven-api:8080`、`cadvisor:8080`、`node-exporter:9100`
- Produces: 宿主机 `9090` / `3000` / `8081` / `9100`

- [ ] **Step 1: 写 `deploy/monitoring/prometheus.yml`**

```yaml
global:
  scrape_interval: 15s
  evaluation_interval: 15s

scrape_configs:
  - job_name: seven-api
    metrics_path: /metrics
    static_configs:
      - targets: ["seven-api:8080"]

  - job_name: cadvisor
    static_configs:
      - targets: ["cadvisor:8080"]

  - job_name: node-exporter
    static_configs:
      - targets: ["node-exporter:9100"]
```

- [ ] **Step 2: Grafana 数据源与 dashboard provider**

`deploy/monitoring/grafana/provisioning/datasources/datasource.yml`：

```yaml
apiVersion: 1
datasources:
  - name: Prometheus
    type: prometheus
    access: proxy
    url: http://prometheus:9090
    isDefault: true
    editable: false
```

`deploy/monitoring/grafana/provisioning/dashboards/dashboards.yml`：

```yaml
apiVersion: 1
providers:
  - name: seven
    orgId: 1
    folder: Seven
    type: file
    disableDeletion: false
    updateIntervalSeconds: 30
    options:
      path: /var/lib/grafana/dashboards
```

- [ ] **Step 3: 基础看板 JSON**

创建 `deploy/monitoring/grafana/dashboards/seven-overview.json`，至少含：

- 标题：`Seven Overview`
- `uid`: `seven-overview`
- 面板（Prometheus 查询，datasource 名 `Prometheus`）：
  1. API HTTP 请求速率：`sum(rate(http_request_duration_seconds_count[1m]))`（若指标名随 prometheus-net 版本略有差异，以 `/metrics` 实际名为准，常见为 `http_request_duration_seconds_count`）
  2. 进程内存：`process_working_set_bytes{job="seven-api"}` 或 `process_private_memory_bytes`
  3. 容器 CPU：`sum(rate(container_cpu_usage_seconds_total{name=~\".*seven.*\"}[1m]))`（无匹配时可用 `rate(container_cpu_usage_seconds_total[1m])` 总览）
  4. 容器内存：`container_memory_usage_bytes{name=~\".*seven.*\"}`
  5. 磁盘 IO：`rate(container_fs_reads_bytes_total[1m])` 与 `rate(container_fs_writes_bytes_total[1m])`（或 node `node_disk_read_bytes_total`）

看板 JSON 须为合法 Grafana 8+/9+ schema（`schemaVersion` ≥ 38 即可）；可用最小结构：`{"annotations":{"list":[]},"editable":true,"panels":[...],"schemaVersion":39,"tags":["seven"],"timezone":"browser","title":"Seven Overview","uid":"seven-overview","version":1}`。

若实现时指标名与上述 PromQL 不一致：打开 `http://localhost:5000/metrics` 与 Prometheus Graph 校对后改 JSON，勿编造指标。

- [ ] **Step 4: 扩展 `docker-compose.yml`**

在 `services:` 下追加（保持现有服务不变），并给 `seven-api` 增加 `depends_on` 非必须（监控可后启）：

```yaml
  prometheus:
    image: prom/prometheus:v2.55.1
    volumes:
      - ./deploy/monitoring/prometheus.yml:/etc/prometheus/prometheus.yml:ro
    ports:
      - "9090:9090"
    depends_on:
      - seven-api
      - cadvisor
      - node-exporter

  grafana:
    image: grafana/grafana:11.3.1
    environment:
      GF_SECURITY_ADMIN_USER: admin
      GF_SECURITY_ADMIN_PASSWORD: admin
      GF_USERS_ALLOW_SIGN_UP: "false"
    volumes:
      - ./deploy/monitoring/grafana/provisioning:/etc/grafana/provisioning:ro
      - ./deploy/monitoring/grafana/dashboards:/var/lib/grafana/dashboards:ro
    ports:
      - "3000:3000"
    depends_on:
      - prometheus

  cadvisor:
    image: gcr.io/cadvisor/cadvisor:v0.49.1
    privileged: true
    devices:
      - /dev/kmsg:/dev/kmsg
    volumes:
      - /:/rootfs:ro
      - /var/run:/var/run:ro
      - /sys:/sys:ro
      - /var/lib/docker/:/var/lib/docker:ro
    ports:
      - "8081:8080"

  node-exporter:
    image: prom/node-exporter:v1.8.2
    pid: host
    volumes:
      - /proc:/host/proc:ro
      - /sys:/host/sys:ro
      - /:/rootfs:ro
    command:
      - --path.procfs=/host/proc
      - --path.sysfs=/host/sys
      - --path.rootfs=/rootfs
      - --collector.filesystem.mount-points-exclude=^/(sys|proc|dev|host|etc)($$|/)
    ports:
      - "9100:9100"
```

说明：Windows Docker Desktop 上 cAdvisor/node-exporter 可能部分指标不全；文档注明推荐 Linux 或 WSL2 后端。镜像 tag 若拉取失败，可改为同系列更新的稳定 tag，保持端口与卷路径不变。

- [ ] **Step 5: 语法检查（有 Docker 时）**

```powershell
docker compose config
```

Expected: 打印合并后的 Compose，无错误。无 Docker 则跳过，仅人工核对 YAML。

---

### Task 3: 文档

**Files:**
- Create: `doc/18-指标与监控.md`
- Modify: `doc/README.md`
- Modify: `doc/07-部署指南.md`
- Modify: `doc/17-健康检查看板.md`

**Interfaces:**
- Consumes: 已定端口与账号
- Produces: 用户可按文档打开 Grafana 并理解与 HealthChecks 分工

- [ ] **Step 1: 写 `doc/18-指标与监控.md`**

内容须覆盖：

1. 与 `/health-ui` 分工（健康阈值 vs 时间序列）
2. 启动：`docker compose up -d` 后访问表（Grafana/Prometheus/metrics/cAdvisor）
3. Grafana `admin`/`admin`；预置看板 **Seven Overview**
4. 本机 `dotnet run` 时如何改 scrape（示例：`host.docker.internal:5000`）
5. 安全：勿公网裸露
6. Windows/cAdvisor 限制一句

- [ ] **Step 2: 更新索引与部署指南**

`doc/README.md` 增加一行指向 `18-指标与监控.md`。

`doc/07-部署指南.md` 访问表增加：

| Grafana | http://localhost:3000（admin/admin，见 [18-指标与监控](./18-指标与监控.md)） |
| Prometheus | http://localhost:9090 |
| API metrics | http://localhost:5000/metrics |

- [ ] **Step 3: `doc/17-健康检查看板.md` 交叉链接**

在「与 K8s / 运维的关系」或文末增加：资源曲线见 [18-指标与监控](./18-指标与监控.md)。

- [ ] **Step 4: 全量测试**

```powershell
dotnet test Seven.Net8/Seven.Tests/Seven.Tests.csproj
```

Expected: 全部通过（含 Metrics + Health）

---

## Spec Coverage Checklist

| 规格要求 | Task |
|---------|------|
| prometheus-net `/metrics` | Task 1 |
| Compose Prometheus/Grafana/cAdvisor/node-exporter | Task 2 |
| 预置 datasource + dashboard | Task 2 |
| 文档与端口说明 | Task 3 |
| 不改 HealthChecks | Task 1 Step 7 |
| 匿名、无自定义业务 Counter | 全任务 |

## Self-Review Notes

- `UseSevenMetrics` 放在管道最前，保证记录异常中间件写入后的最终状态码。
- PromQL 以实际 `/metrics` 名为准；实现时若 `http_request_duration_seconds_count` 不存在，改为 prometheus-net 实际导出名称。
- cAdvisor 镜像源 `gcr.io` 在国内可能慢，可换镜像加速或 `ghcr.io/google/cadvisor` 等同功能镜像，端口映射保持 `8081:8080`。
