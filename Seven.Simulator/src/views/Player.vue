<script setup lang="ts">
import axios from 'axios'
import { computed, onUnmounted, reactive, ref } from 'vue'
import { ElMessage } from 'element-plus'
import SimPlayerView from '../components/player/SimPlayerView.vue'
import http, { AUTH_TOKEN_KEY } from '../api/http'
import { onWcsMessageReceived, useSimWcsProxy } from '../lib/comms/simWcsProxy'
import { useProjectStore } from '../stores/project'

const store = useProjectStore()
const {
  connected: gatewayConnected,
  lastMessage: gatewayLastMessage,
  connect: connectSimWcsProxy,
  disconnect: disconnectSimWcsProxy,
  sendWcsMessage,
} = useSimWcsProxy()
const playerViewRef = ref<InstanceType<typeof SimPlayerView> | null>(null)
const log = ref<string[]>([])
const resetting = ref(false)
const inboundBusy = ref(false)
const show3d = ref(false)
const showGatewayPanel = ref(store.project.meta.simCommsMode === 'Gateway')
const gatewayBusy = ref(false)

const isGatewayMode = computed(
  () => store.project.meta.simCommsMode === 'Gateway' || showGatewayPanel.value,
)

const gatewayTest = reactive({
  connectionId: 'test-conn',
  payload: '{"type":"ping","source":"simulator"}',
})

const unsubscribeGateway = onWcsMessageReceived((msg) => {
  push(`Gateway ← [${msg.connectionId}] ${msg.payload}`)
  if (show3d.value) {
    playerViewRef.value?.nudgeDevice()
  }
})

onUnmounted(() => {
  unsubscribeGateway()
  void disconnectSimWcsProxy()
})

async function connectGateway() {
  gatewayBusy.value = true
  try {
    await connectSimWcsProxy()
    push('Gateway Hub 已连接')
    ElMessage.success('Gateway 已连接')
  } catch (e: unknown) {
    const msg = e instanceof Error ? e.message : String(e)
    push(`Gateway 连接失败: ${msg}`)
    ElMessage.error(`Gateway 连接失败: ${msg}`)
  } finally {
    gatewayBusy.value = false
  }
}

async function disconnectGateway() {
  gatewayBusy.value = true
  try {
    await disconnectSimWcsProxy()
    push('Gateway Hub 已断开')
    ElMessage.info('Gateway 已断开')
  } catch (e: unknown) {
    const msg = e instanceof Error ? e.message : String(e)
    push(`Gateway 断开失败: ${msg}`)
    ElMessage.error(`Gateway 断开失败: ${msg}`)
  } finally {
    gatewayBusy.value = false
  }
}

async function sendGatewayTest() {
  if (!gatewayConnected.value) {
    ElMessage.warning('请先连接 Gateway Hub')
    return
  }
  gatewayBusy.value = true
  try {
    await sendWcsMessage(gatewayTest.connectionId.trim(), gatewayTest.payload)
    push(`Gateway → [${gatewayTest.connectionId}] ${gatewayTest.payload}`)
    ElMessage.success('测试报文已发送')
  } catch (e: unknown) {
    const msg = e instanceof Error ? e.message : String(e)
    push(`Gateway 发送失败: ${msg}`)
    ElMessage.error(`Gateway 发送失败: ${msg}`)
  } finally {
    gatewayBusy.value = false
  }
}

const dest = reactive({
  containerCode: 'TP001',
  sourcePointCode: 'RP_IN_01',
  height: 1,
  weight: 1,
  checkResult: 'OK',
})

const feedback = reactive({
  deviceCode: 'SRM01',
  segmentId: '',
  resultCode: 'OK',
  message: 'done',
})

const inbound = reactive({
  orderNo: '',
  materialCode: 'MAT-01',
  qty: 10,
  containerCode: 'TP001',
})

const routePreviewLoading = ref(false)
const routePreview = ref<{ code: string; nodeIds: string[] }[]>([])
const routePreviewWarnings = ref<string[]>([])

function nodeCode(id: string) {
  return store.project.map.nodes.find((n) => n.id === id)?.code ?? id
}

async function previewRouteGroups() {
  routePreviewLoading.value = true
  try {
    const { data } = await http.post<{
      status: boolean
      message?: string
      data?: {
        suggested: { code: string; nodeIds: string[] }[]
        warnings: string[]
      }
    }>('/api/simulation/route-groups/preview', {
      packId: store.project.map.packId,
      nodes: store.project.map.nodes,
      edges: store.project.map.edges,
    })
    if (!data.status || !data.data) {
      push(`路径组预览失败: ${data.message || '未知'}`)
      ElMessage.error(data.message || '路径组预览失败')
      return
    }
    routePreview.value = data.data.suggested
    routePreviewWarnings.value = data.data.warnings
    push(`路径组预览: ${data.data.suggested.length} 组`)
    if (data.data.warnings.length) {
      ElMessage.warning(data.data.warnings.join('；'))
    } else {
      ElMessage.success(`建议 ${data.data.suggested.length} 个路径组`)
    }
  } catch (e: unknown) {
    const msg = e instanceof Error ? e.message : String(e)
    push(`路径组预览失败: ${msg}`)
    ElMessage.error('路径组预览失败')
  } finally {
    routePreviewLoading.value = false
  }
}

function push(msg: string) {
  log.value.unshift(`${new Date().toLocaleTimeString()} ${msg}`)
}

function authHint() {
  ElMessage.warning(
    '需 JWT：请先在 Seven.Vue3 登录（同域 localStorage token），详见 README「认证」',
  )
}

function isUnauthorized(e: unknown) {
  return axios.isAxiosError(e) && e.response?.status === 401
}

async function resetSimulation() {
  resetting.value = true
  try {
    const { data } = await http.post<{ status: boolean; message?: string }>('/api/simulation/reset', {
      projectName: store.project.meta.name,
    })
    if (!data.status) {
      push(`Reset 失败: ${data.message || '未知'}`)
      ElMessage.error(data.message || 'Reset 失败')
      return
    }
    push(`Reset: ${data.message || '已清运行态'}`)
    ElMessage.success(data.message || '已 Reset')
  } catch (e: unknown) {
    const msg = e instanceof Error ? e.message : String(e)
    push(`Reset 失败: ${msg}`)
    ElMessage.error(`Reset 失败: ${msg}`)
  } finally {
    resetting.value = false
  }
}

async function quickInbound() {
  if (!localStorage.getItem(AUTH_TOKEN_KEY)) {
    authHint()
    return
  }
  inboundBusy.value = true
  const orderNo = inbound.orderNo.trim() || `SIM-IN-${Date.now()}`
  try {
    const { data: createRes } = await http.post<{
      status: boolean
      message?: string
      data?: { id: number; orderNo: string }
    }>('/api/WmsInboundOrder/add', {
      orderNo,
      orderType: 1,
      lines: [
        {
          lineNo: 1,
          materialCode: inbound.materialCode,
          qty: inbound.qty,
          containerCode: inbound.containerCode || undefined,
        },
      ],
    })
    if (!createRes.status || !createRes.data?.id) {
      push(`建单失败: ${createRes.message || '未知'}`)
      ElMessage.error(createRes.message || '建单失败')
      return
    }
    const id = createRes.data.id
    push(`入库单已创建: ${createRes.data.orderNo} (#${id})`)

    const { data: approveRes } = await http.post<{ status: boolean; message?: string }>(
      `/api/WmsInboundOrder/approve/${id}`,
    )
    if (!approveRes.status) {
      push(`审核失败: ${approveRes.message || '未知'}`)
      ElMessage.error(approveRes.message || '审核失败')
      return
    }
    push(`入库单已审核: ${orderNo}`)
    ElMessage.success('快捷入库：建单 + 审核完成')
  } catch (e: unknown) {
    if (isUnauthorized(e)) {
      authHint()
      push('401 未授权')
      return
    }
    const msg = e instanceof Error ? e.message : String(e)
    push(`快捷入库失败: ${msg}`)
    ElMessage.error(`快捷入库失败: ${msg}`)
  } finally {
    inboundBusy.value = false
  }
}

async function simulateDestination() {
  if (!localStorage.getItem(AUTH_TOKEN_KEY)) {
    authHint()
    return
  }
  try {
    await http.post('/api/Wcs/Triggers/destination-request', dest)
    push('SUDR 语义：destination-request 已发送')
    ElMessage.success('已触发目的地申请')
  } catch (e: unknown) {
    if (isUnauthorized(e)) {
      authHint()
      push('401 未授权')
      return
    }
    const msg = e instanceof Error ? e.message : String(e)
    push(`失败: ${msg}`)
    ElMessage.error('触发失败（需 API 开启 Stacker/Bus，并登录）')
  }
}

async function simulateFeedback() {
  if (!localStorage.getItem(AUTH_TOKEN_KEY)) {
    authHint()
    return
  }
  try {
    await http.post('/api/Wcs/Triggers/segment-feedback', {
      ...feedback,
      segmentId: feedback.segmentId || crypto.randomUUID(),
    })
    push('段反馈已发送')
    ElMessage.success('已触发段反馈')
  } catch (e: unknown) {
    if (isUnauthorized(e)) {
      authHint()
      push('401 未授权')
      return
    }
    const msg = e instanceof Error ? e.message : String(e)
    push(`失败: ${msg}`)
    ElMessage.error('触发失败')
  }
}
</script>

<template>
  <div class="sim-page">
    <h1>③ 仿真测试</h1>
    <p class="hint">
      模式：{{ store.project.meta.runtimeMode }} / {{ store.project.meta.simCommsMode }}。
      单机走 Trigger API；Reset 清运行态保留 SIM_ 主数据。
    </p>

    <div class="toolbar" style="margin-bottom: 1rem">
      <el-button type="warning" :loading="resetting" @click="resetSimulation">Reset 运行态</el-button>
      <el-switch v-model="show3d" active-text="3D 预览" />
      <el-switch
        v-if="store.project.meta.simCommsMode !== 'Gateway'"
        v-model="showGatewayPanel"
        active-text="Gateway 面板"
      />
    </div>

    <SimPlayerView
      v-if="show3d"
      ref="playerViewRef"
      :nodes="store.project.map.nodes"
      :devices="store.project.map.devices"
      style="margin-bottom: 1rem"
    />

    <el-card v-if="isGatewayMode" header="Gateway · SignalR WCS 代理" style="margin-bottom: 1rem">
      <p class="hint">
        Hub：<code>/hubs/sim-wcs-proxy</code>。Connect 后 Send 测试报文，订阅者收到
        <code>OnWcsMessageReceived</code>；开启 3D 时设备网格会轻微跳动。
      </p>
      <div class="toolbar" style="margin-bottom: 0.75rem">
        <el-tag :type="gatewayConnected ? 'success' : 'info'">
          {{ gatewayConnected ? '已连接' : '未连接' }}
        </el-tag>
        <el-button type="primary" :loading="gatewayBusy" :disabled="gatewayConnected" @click="connectGateway">
          Connect
        </el-button>
        <el-button :loading="gatewayBusy" :disabled="!gatewayConnected" @click="disconnectGateway">
          Disconnect
        </el-button>
      </div>
      <el-form label-width="110px">
        <el-form-item label="ConnectionId">
          <el-input v-model="gatewayTest.connectionId" />
        </el-form-item>
        <el-form-item label="Payload">
          <el-input v-model="gatewayTest.payload" type="textarea" :rows="2" />
        </el-form-item>
        <el-button type="primary" :loading="gatewayBusy" :disabled="!gatewayConnected" @click="sendGatewayTest">
          Send test payload
        </el-button>
      </el-form>
      <p v-if="gatewayLastMessage" class="hint last-msg">
        最近收到：
        <strong>[{{ gatewayLastMessage.connectionId }}]</strong>
        {{ gatewayLastMessage.payload }}
        <span class="ts">({{ new Date(gatewayLastMessage.receivedAt).toLocaleTimeString() }})</span>
      </p>
    </el-card>

    <el-row :gutter="16">
      <el-col :span="12">
        <el-card header="单机 · 目的地申请（SUDR）">
          <el-form label-width="110px">
            <el-form-item label="托盘"><el-input v-model="dest.containerCode" /></el-form-item>
            <el-form-item label="申请点"><el-input v-model="dest.sourcePointCode" /></el-form-item>
            <el-form-item label="CheckResult"><el-input v-model="dest.checkResult" /></el-form-item>
            <el-button type="primary" @click="simulateDestination">发送</el-button>
          </el-form>
        </el-card>
      </el-col>
      <el-col :span="12">
        <el-card header="单机 · 段反馈">
          <el-form label-width="110px">
            <el-form-item label="设备"><el-input v-model="feedback.deviceCode" /></el-form-item>
            <el-form-item label="结果"><el-input v-model="feedback.resultCode" /></el-form-item>
            <el-button type="primary" @click="simulateFeedback">发送</el-button>
          </el-form>
        </el-card>
      </el-col>
    </el-row>

    <el-card header="路径组预览" style="margin-top: 1rem">
      <p class="hint">基于当前地图边做连通分量启发式分组；四向环检测给出警告。</p>
      <el-button type="primary" :loading="routePreviewLoading" @click="previewRouteGroups">
        预览路径组
      </el-button>
      <el-table
        v-if="routePreview.length"
        :data="routePreview"
        size="small"
        border
        style="margin-top: 0.75rem"
      >
        <el-table-column prop="code" label="组编码" width="120" />
        <el-table-column label="节点">
          <template #default="{ row }">
            {{ row.nodeIds.map((id: string) => nodeCode(id)).join(' → ') }}
          </template>
        </el-table-column>
      </el-table>
      <el-alert
        v-for="(w, i) in routePreviewWarnings"
        :key="i"
        :title="w"
        type="warning"
        show-icon
        :closable="false"
        style="margin-top: 0.5rem"
      />
    </el-card>

    <el-card header="快捷入库（建单 + 审核）" style="margin-top: 1rem">
      <el-form inline>
        <el-form-item label="单号">
          <el-input v-model="inbound.orderNo" placeholder="留空自动生成" style="width: 160px" />
        </el-form-item>
        <el-form-item label="物料">
          <el-input v-model="inbound.materialCode" style="width: 120px" />
        </el-form-item>
        <el-form-item label="数量">
          <el-input-number v-model="inbound.qty" :min="1" />
        </el-form-item>
        <el-form-item label="托盘">
          <el-input v-model="inbound.containerCode" style="width: 100px" />
        </el-form-item>
        <el-button type="primary" :loading="inboundBusy" @click="quickInbound">建单并审核</el-button>
      </el-form>
      <p class="hint">需 WmsInboundOrder 权限；先在 Vue3 登录写入 token。</p>
    </el-card>

    <el-card header="运行日志" style="margin-top: 1rem">
      <pre class="log">{{ log.join('\n') || '暂无' }}</pre>
    </el-card>
  </div>
</template>

<style scoped>
.toolbar { display: flex; flex-wrap: wrap; gap: 0.5rem; align-items: center; }
.last-msg { margin-top: 0.75rem; word-break: break-all; }
.ts { color: #94a3b8; margin-left: 0.25rem; }
.log {
  margin: 0;
  max-height: 240px;
  overflow: auto;
  font-size: 12px;
  background: #0f2744;
  color: #d7e3f4;
  padding: 12px;
  border-radius: 6px;
}
</style>
