# Prometheus 指标与监控栈设计

状态：已批准（用户 2026-08-25）

## 目标

在 Seven API 暴露 Prometheus `/metrics`，并在 `docker-compose` 中提供 Prometheus + Grafana + cAdvisor + node-exporter，用于查看进程/HTTP 与容器/主机 CPU、内存、磁盘 IO 等时间序列。与 HealthChecks（`/health`、`/health-ui`）分工：后者做依赖健康阈值，本栈做资源与流量趋势。

## 决策摘要

| 主题 | 决策 |
|---|---|
| .NET 库 | `prometheus-net.AspNetCore` |
| API 端点 | `/metrics`，匿名 |
| 主机/容器资源 | cAdvisor + node-exporter |
| 可视化 | Grafana（预置 datasource + 基础 dashboard） |
| Compose | 默认纳入主 `docker-compose.yml`（方案 1，无 monitoring profile） |
| 鉴权 | 本期不加；面向本地/内网，文档注明勿公网裸露 |

## 架构

```
Seven.WebApi
  └─ UseHttpMetrics + MapMetrics("/metrics")

docker-compose:
  prometheus   :9090  scrape → seven-api:8080/metrics, cadvisor, node-exporter
  grafana      :3000  datasource → prometheus；预置看板
  cadvisor     :8081  容器 CPU/内存/磁盘 IO/网络
  node-exporter :9100  主机 CPU/内存/磁盘/文件系统

分工:
  /health、/health-ui  → 依赖是否健康
  /metrics + Grafana   → 资源与请求时间序列
```

本地 `dotnet run` 时，默认 scrape 仍指向 Compose 内 `seven-api`；抓本机进程需改 `prometheus.yml` 或使用 `host.docker.internal:5000`（文档说明）。

## 文件变更

| 操作 | 路径 |
|---|---|
| 改 | `Seven.Net8/Seven.WebApi/Seven.WebApi.csproj`（加 prometheus-net.AspNetCore） |
| 增 | `Seven.Net8/Seven.WebApi/MetricsExtensions.cs`（可选薄封装） |
| 改 | `Seven.Net8/Seven.WebApi/Program.cs` |
| 改 | `docker-compose.yml` |
| 增 | `deploy/monitoring/prometheus.yml` |
| 增 | `deploy/monitoring/grafana/provisioning/datasources/*.yml` |
| 增 | `deploy/monitoring/grafana/provisioning/dashboards/*.yml` + dashboard JSON |
| 改 | `Seven.Net8/Seven.Tests/Integration/HealthApiTests.cs` 或新建 Metrics 测试 |
| 增 | `doc/18-指标与监控.md` |
| 改 | `doc/README.md`、`doc/07-部署指南.md`、`doc/17-健康检查看板.md` |

## 指标范围

| 来源 | 内容 |
|---|---|
| API `/metrics` | HTTP 请求量/延迟、进程内存、GC 等（prometheus-net 默认 + HttpMetrics） |
| cAdvisor | 容器 CPU、内存、磁盘 IO、网络 |
| node-exporter | 主机 CPU、内存、磁盘、文件系统 |

## 默认访问与验收

| 服务 | 地址 | 账号 |
|---|---|---|
| Grafana | http://localhost:3000 | admin / admin |
| Prometheus | http://localhost:9090 | 无 |
| API metrics | http://localhost:5000/metrics | 匿名 |
| cAdvisor UI | http://localhost:8081 | — |

验收：

1. `GET /metrics` → 200，含进程或 HTTP 指标  
2. Prometheus Targets：`seven-api`、`cadvisor`、`node-exporter` 为 UP  
3. Grafana 预置看板可见 API 与容器资源图  
4. `/health`、`/health-ui` 行为不变  

## 范围外

- `/metrics` 鉴权、业务自定义 Counter  
- K8s ServiceMonitor（可仅文档提及）  
- 修改现有 HealthChecks 逻辑  
- Compose monitoring profile（本期默认启动监控服务）
