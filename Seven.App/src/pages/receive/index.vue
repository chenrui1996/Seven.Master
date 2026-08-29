<script setup lang="ts">
import { onShow } from '@dcloudio/uni-app'
import { computed, ref } from 'vue'
import { fetchPendingInbound, receiveFloor } from '../../api/pda'

type Line = {
  lineNo: number
  materialCode: string
  remainingQty: number
  containerCode?: string
  fromLocation?: string
}
type Order = { id: number; orderNo: string; lines: Line[] }

const orders = ref<Order[]>([])
const selectedId = ref<number | null>(null)
const lineNo = ref(1)
const qty = ref('')
const containerCode = ref('')
const receiveLocationCode = ref('')
const submitting = ref(false)

const selected = computed(() => orders.value.find((o) => o.id === selectedId.value) || null)

onShow(load)

async function load() {
  const res = await fetchPendingInbound()
  if (res.status && res.data) {
    orders.value = res.data
    if (!selectedId.value && orders.value.length) selectOrder(orders.value[0])
  }
}

function selectOrder(o: Order) {
  selectedId.value = o.id
  const line = o.lines[0]
  if (line) {
    lineNo.value = line.lineNo
    qty.value = String(line.remainingQty)
    containerCode.value = line.containerCode || ''
    receiveLocationCode.value = line.fromLocation || ''
  }
}

function onScanContainer() {
  // 真机：扫码枪多为键盘楔入；此处保留扫码 API 入口
  // @ts-expect-error uni scan
  uni.scanCode?.({
    success: (r: { result: string }) => {
      containerCode.value = r.result
    },
    fail: () => uni.showToast({ title: '请手动输入或使用扫码枪', icon: 'none' }),
  })
}

function onScanLoc() {
  // @ts-expect-error uni scan
  uni.scanCode?.({
    success: (r: { result: string }) => {
      receiveLocationCode.value = r.result
    },
    fail: () => uni.showToast({ title: '请手动输入或使用扫码枪', icon: 'none' }),
  })
}

async function submit() {
  if (!selectedId.value) return
  const q = Number(qty.value)
  if (!q || q <= 0) {
    uni.showToast({ title: '数量无效', icon: 'none' })
    return
  }
  if (!containerCode.value || !receiveLocationCode.value) {
    uni.showToast({ title: '请填写容器与收货位', icon: 'none' })
    return
  }
  submitting.value = true
  try {
    const res = await receiveFloor(selectedId.value, {
      lineNo: lineNo.value,
      qty: q,
      containerCode: containerCode.value.trim(),
      receiveLocationCode: receiveLocationCode.value.trim(),
    })
    if (res.status) {
      uni.showToast({ title: '收货成功', icon: 'success' })
      containerCode.value = ''
      await load()
    }
  } finally {
    submitting.value = false
  }
}
</script>

<template>
  <view class="page">
    <view class="section-title">待收货入库单</view>
    <scroll-view scroll-y class="list">
      <view
        v-for="o in orders"
        :key="o.id"
        class="card"
        :class="{ active: o.id === selectedId }"
        @click="selectOrder(o)"
      >
        <text class="no">{{ o.orderNo }}</text>
        <text class="meta">{{ o.lines.length }} 行待收</text>
      </view>
      <view v-if="!orders.length" class="empty">暂无待收货单据</view>
    </scroll-view>

    <view v-if="selected" class="form">
      <view class="section-title">扫码组盘 · {{ selected.orderNo }}</view>
      <view class="row">
        <text class="label">行号</text>
        <input v-model.number="lineNo" class="input" type="number" />
      </view>
      <view class="row">
        <text class="label">数量</text>
        <input v-model="qty" class="input" type="digit" />
      </view>
      <view class="row">
        <text class="label">容器</text>
        <input v-model="containerCode" class="input" placeholder="扫描或输入" confirm-type="next" />
        <button size="mini" @click="onScanContainer">扫</button>
      </view>
      <view class="row">
        <text class="label">收货位</text>
        <input
          v-model="receiveLocationCode"
          class="input"
          placeholder="如 Stk.RECV-01"
          confirm-type="done"
          @confirm="submit"
        />
        <button size="mini" @click="onScanLoc">扫</button>
      </view>
      <button class="submit" type="primary" :loading="submitting" @click="submit">确认收货</button>
    </view>
  </view>
</template>

<style scoped>
.page {
  padding: 24rpx;
}
.section-title {
  font-size: 28rpx;
  color: #5a6a75;
  margin: 12rpx 0 16rpx;
}
.list {
  max-height: 280rpx;
}
.card {
  background: #fff;
  padding: 24rpx;
  border-radius: 12rpx;
  margin-bottom: 12rpx;
  display: flex;
  justify-content: space-between;
}
.card.active {
  border: 2rpx solid #2c5f6e;
}
.no {
  font-weight: 600;
  color: #1b3a4b;
}
.meta {
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
  width: 120rpx;
  color: #5a6a75;
  font-size: 26rpx;
}
.input {
  flex: 1;
  border: 1px solid #dce3ea;
  border-radius: 8rpx;
  padding: 16rpx 20rpx;
}
.submit {
  margin-top: 12rpx;
  background: #2c5f6e !important;
}
</style>
