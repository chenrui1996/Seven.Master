# Seven Master — Kubernetes 部署

在已安装 `kubectl` 且可访问集群的前提下，于本目录执行：

```bash
kubectl apply -f api-deployment.yaml
kubectl apply -f web-deployment.yaml
```

## 资源说明

| 文件 | 内容 |
|------|------|
| `api-deployment.yaml` | API ConfigMap/Secret、Deployment `seven-api`、Service、Ingress |
| `web-deployment.yaml` | 前端 Nginx ConfigMap、Deployment `seven-web`、Service |

Ingress 规则（`seven.local`）：

- `/api`、`/hub` → `seven-api:80`
- `/` → `seven-web:80`

## 镜像

- `seven-api:latest`：见 `Seven.Net8/Dockerfile`
- `seven-web:latest`：见 `Seven.Vue3/Dockerfile`

构建示例：

```bash
docker build -t seven-api:latest -f Seven.Net8/Dockerfile Seven.Net8
docker build -t seven-web:latest -f Seven.Vue3/Dockerfile Seven.Vue3
```

## 本地 hosts

```
127.0.0.1 seven.local
```

（若使用 minikube / kind，请按集群 Ingress 说明调整。）
