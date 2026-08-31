<script setup lang="ts">
import { onShow } from '@dcloudio/uni-app'
import { ref } from 'vue'
import {
  confirmCycleCount,
  fetchCycleCount,
  fetchPendingCycleCount,
  recordCycleCount,
} from '../../api/pda'

type Pending = {
  id: number
  orderNo: string
  status: number
  totalLines: number
  countedLines: number
}

type Line = {
  lineNo: number
  locationCode: string
  materialCode: string
  containerCode?: string
  bookQty: number
  countQty: number
  diffQty: number
  counted: boolean
}

type Detail = {
  id: number
  orderNo: string
  status: number
  lines: Line[]
}

const list = ref<Pending[]>([])
const detail = ref<Detail | null>(null)
const locationCode = ref('')
const containerCode = ref('')
const countQty = ref('')
const submitting = ref(false)

onShow(loadList)

async function loadList() {
  const res = await fetchPendingCycleCount()
  if (res.status && res.data) list.value = res.data
}

async function open(item: Pending) {
  const res = await fetchCycleCount(item.id)
  if (res.status && res.data) {
    detail.value = res.data
    locationCode.value = ''
    containerCode.value = ''
    countQty.value = ''
  }
}

function back() {
  detail.value = null
  loadList()
}

function pickLine(line: Line) {
  locationCode.value = line.locationCode
  containerCode.value = line.containerCode || ''
  countQty.value = line.counted ? String(line.countQty) : String(line.bookQty)
}

function onScanLoc() {
  // @ts-expect-error uni scan
  uni.scanCode?.({
    success: (r: { result: string }) => {
      locationCode.value = r.result
    },
    fail: () => uni.showToast({ title: '请手动输入或使用扫码枪', icon: 'none' }),
  })
}

async function submitRecord() {
  if (!detail.value) return
  const qty = Number(countQty.value)
  if (!locationCode.value.trim()) {
    uni.showToast({ title: '请扫描库位', icon: 'none' })
    return
  }
  if (Number.isNaN(qty) || qty < 0) {
    uni.showToast({ title: '实盘数量无效', icon: 'none' })
    return
  }
  submitting.value = true
  try {
    const res = await recordCycleCount(detail.value.id, {
      lineNo: 0,
      countQty: qty,
      locationCode: locationCode.value.trim(),
      containerCode: containerCode.value.trim() || undefined,
    })
    if (res.status) {
      uni.showToast({ title: '录入成功', icon: 'success' })
      const refreshed = await fetchCycleCount(detail.value.id)
      if (refreshed.status && refreshed.data) detail.value = refreshed.data
      countQty.value = ''
    }
  } finally {
    submitting.value = false
  }
}

async function submitConfirm() {
  if (!detail.value) return
  submitting.value = true
  try {
    const res = await confirmCycleCount(detail.value.id)
    if (res.status) {
      uni.showToast({ title: '调账成功', icon: 'success' })
      back()
    }
  } finally {
    submitting.value = false
  }
}
</script>

<template>
  <view class="page">
    <template v-if="!detail">
      <view class="section-title">待盘点单</view>
      <view v-for="item in list" :key="item.id" class="card" @click="open(item)">
        <view class="row1">
          <text class="code">{{ item.orderNo }}</text>
          <text class="qty">{{ item.countedLines }}/{{ item.totalLines }}</text>
        </view>
        <view class="row2">状态 {{ item.status }} · 点此录入</view>
      </view>
      <view v-if="!list.length" class="empty">暂无待盘点</view>
    </template>

    <template v-else>
      <view class="toolbar">
        <button size="mini" @click="back">返回</button>
        <text class="code">{{ detail.orderNo }}</text>
      </view>

      <view class="section-title">盘点行</view>
      <view
        v-for="line in detail.lines"
        :key="line.lineNo"
        class="card"
        :class="{ done: line.counted }"
        @click="pickLine(line)"
      >
        <view class="row1">
          <text class="code">{{ line.locationCode }}</text>
          <text class="qty">{{ line.counted ? '已盘' : '未盘' }}</text>
        </view>
        <view class="row2">
          {{ line.materialCode }}
          <text v-if="line.containerCode"> · {{ line.containerCode }}</text>
          · 账面 {{ line.bookQty }}
          <text v-if="line.counted"> · 实盘 {{ line.countQty }} · 差 {{ line.diffQty }}</text>
        </view>
      </view>

      <view class="form">
        <view class="section-title">实盘录入</view>
        <view class="row">
          <text class="label">库位</text>
          <input v-model="locationCode" class="input" placeholder="扫描库位" />
          <button size="mini" @click="onScanLoc">扫</button>
        </view>
        <view class="row">
          <text class="label">容器</text>
          <input v-model="containerCode" class="input" placeholder="可选" />
        </view>
        <view class="row">
          <text class="label">实盘</text>
          <input v-model="countQty" class="input" type="digit" placeholder="实盘数量" />
        </view>
        <button class="submit" type="primary" :loading="submitting" @click="submitRecord">确认录入</button>
        <button
          class="confirm"
          :disabled="!detail.lines.length || detail.lines.some((l) => !l.counted)"
          :loading="submitting"
          @click="submitConfirm"
        >
          全部已盘 · 调账
        </button>
      </view>
    </template>
  </view>
</template>

<style scoped>
.page {
  padding: 24rpx;
}
.toolbar {
  display: flex;
  align-items: center;
  gap: 16rpx;
  margin-bottom: 8rpx;
}
.section-title {
  font-size: 28rpx;
  color: #5a6a75;
  margin: 12rpx 0 16rpx;
}
.card {
  background: #fff;
  padding: 24rpx;
  border-radius: 12rpx;
  margin-bottom: 12rpx;
}
.card.done {
  border-left: 6rpx solid #2c5f6e;
}
.row1 {
  display: flex;
  justify-content: space-between;
}
.code {
  font-weight: 700;
  color: #1b3a4b;
  font-size: 30rpx;
}
.qty {
  color: #2c5f6e;
}
.row2 {
  margin-top: 8rpx;
  color: #8899a6;
  font-size: 24rpx;
}
.empty {
  text-align: center;
  color: #999;
  padding: 40rpx;
}
.form {
  margin-top: 24rpx;
  background: #fff;
  border-radius: 16rpx;
  padding: 24rpx;
}
.row {
  display: flex;
  align-items: center;
  gap: 12rpx;
  margin-bottom: 20rpx;
}
.label {
  width: 100rpx;
  color: #5a6a75;
}
.input {
  flex: 1;
  border: 1px solid #dce3ea;
  border-radius: 8rpx;
  padding: 16rpx 20rpx;
}
.submit {
  background: #2c5f6e !important;
  margin-bottom: 16rpx;
}
.confirm {
  background: #1b3a4b !important;
  color: #fff !important;
}
</style>
