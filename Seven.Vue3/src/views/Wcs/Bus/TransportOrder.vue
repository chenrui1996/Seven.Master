<template>
  <div class="wcs-ops seven-page">
    <h1 class="ops-title">{{ t('wcsOps.transport.title') }}</h1>
    <p class="ops-sub">{{ t('wcsOps.transport.subtitle') }}</p>

    <div class="ops-toolbar">
      <el-button type="primary" :loading="loading" @click="load">{{ t('wcsOps.transport.refresh') }}</el-button>
    </div>

    <div class="ops-split">
      <div class="ops-panel">
        <el-table :data="rows" stripe border v-loading="loading" highlight-current-row @current-change="onSelect">
          <el-table-column prop="id" label="Id" min-width="220" show-overflow-tooltip>
            <template #default="{ row }"><span class="ops-mono">{{ row.id }}</span></template>
          </el-table-column>
          <el-table-column prop="containerCode" :label="t('wcsOps.transport.container')" min-width="110" />
          <el-table-column prop="fromLocationCode" :label="t('wcsOps.transport.from')" min-width="110" />
          <el-table-column prop="toLocationCode" :label="t('wcsOps.transport.to')" min-width="110" />
          <el-table-column prop="status" :label="t('wcsOps.transport.status')" width="90" />
          <el-table-column prop="refType" :label="t('wcsOps.transport.refType')" width="110" />
          <el-table-column prop="failReason" :label="t('wcsOps.transport.failReason')" min-width="140" show-overflow-tooltip />
        </el-table>
      </div>
      <div class="ops-panel" v-if="detail">
        <h3>{{ t('wcsOps.transport.legs') }} · <span class="ops-mono">{{ detail.containerCode }}</span></h3>
        <el-timeline v-if="(detail.legs || []).length">
          <el-timeline-item
            v-for="leg in detail.legs"
            :key="leg.id"
            :type="legStatusType(leg.status)"
            :timestamp="`Seq ${leg.seq} · ${leg.packId}`"
            placement="top"
          >
            <div class="ops-mono">{{ leg.fromCode }} → {{ leg.toCode }}</div>
            <div>{{ t('wcsOps.transport.legStatus', { status: leg.status }) }} <span v-if="leg.message">· {{ leg.message }}</span></div>
          </el-timeline-item>
        </el-timeline>
        <el-empty v-else :description="t('wcsOps.transport.noLegs')" />
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import http from '../../../api/http'
import '../../../styles/wcs-ops.css'

const { t } = useI18n()

interface BusRow {
  id: string
  containerCode: string
  fromLocationCode: string
  toLocationCode: string
  status: number
  refType?: string
  failReason?: string
}

interface LegRow {
  id: string
  seq: number
  packId: string
  fromCode: string
  toCode: string
  status: number
  message?: string
}

const rows = ref<BusRow[]>([])
const loading = ref(false)
const detail = ref<(BusRow & { legs?: LegRow[] }) | null>(null)

function legStatusType(status: number): 'primary' | 'success' | 'warning' | 'danger' | 'info' {
  if (status >= 3) return 'success'
  if (status === 2) return 'warning'
  if (status < 0) return 'danger'
  return 'primary'
}

async function load() {
  loading.value = true
  try {
    const res = await http.post<{ status: boolean; data?: { rows?: BusRow[] } }>('/api/BusTransportOrder/getPageData', {
      page: 1,
      rows: 100,
    })
    if (res.status) rows.value = res.data?.rows ?? []
  } finally {
    loading.value = false
  }
}

async function onSelect(row: BusRow | null) {
  if (!row) {
    detail.value = null
    return
  }
  const res = await http.get<{ status: boolean; data?: BusRow & { legs?: LegRow[] } }>(
    `/api/BusTransportOrder/${row.id}`,
  )
  if (res.status) detail.value = res.data ?? null
}

onMounted(load)
</script>
