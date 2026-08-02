# 部署样例

| 路径 | 说明 |
|------|------|
| `nginx/default.conf` | 前端静态 + `/api` `/hub` 反代 |
| `k8s/api-deployment.yaml` | API ConfigMap/Secret/Deployment/Service/Ingress |
| `k8s/web-deployment.yaml` | 前端 Deployment/Service + Nginx ConfigMap |
| `k8s/README.md` | `kubectl apply` 顺序 |

功能开关可通过 ConfigMap 注入，例如 `Features__MessageQueue: "true"`。详见 [doc/14-功能开关.md](../doc/14-功能开关.md) 与 [doc/07-部署指南.md](../doc/07-部署指南.md)。
