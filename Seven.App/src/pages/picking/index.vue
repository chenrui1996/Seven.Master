<script setup lang="ts">
import { onShow } from '@dcloudio/uni-app'
import { ref } from 'vue'
import { confirmPick, fetchPendingPicking } from '../../api/pda'

type Task = {
  id: number
  taskNo: string
  outboundOrderId: number
  orderNo: string
  materialCode: string
  bookQty: number
  pickQty: number
  fromLocation?: string
  toLocation?: string
  containerCode?: string
  status: number
}

const list = ref<Task[]>([])
const selected = ref<Task | null>(null)
const pickQty = ref('')
const containerCode = ref('')
const fromLocation = ref('')
const submitting = ref(false)

onShow(load)

async function load() {
  const res = await fetchPendingPicking()
  if (res.status && res.data) {
    list.value = res.data
    if (!selected.value && list.value.length) selectTask(list.value[0])
  }
}

function selectTask(t: Task) {
  selected.value = t
  const remain = t.bookQty - (t.pickQty || 0)
  pickQty.value = String(remain > 0 ? remain : t.bookQty)
  containerCode.value = t.containerCode || ''
  fromLocation.value = t.fromLocation || ''
}

function onScanContainer() {
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
      fromLocation.value = r.result
    },
    fail: () => uni.showToast({ title: '请手动输入或使用扫码枪', icon: 'none' }),
  })
}

async function submit() {
  if (!selected.value) return
  const q = Number(pickQty.value)
  if (!q || q <= 0) {
    uni.showToast({ title: '数量无效', icon: 'none' })
    return
  }
  submitting.value = true
  try {
    const res = await confirmPick({
      pickingTaskId: selected.value.id,
      pickQty: q,
      containerCode: containerCode.value.trim() || undefined,
      fromLocation: fromLocation.value.trim() || undefined,
    })
    if (res.status) {
      uni.showToast({ title: '拣选成功', icon: 'success' })
      selected.value = null
      containerCode.value = ''
      fromLocation.value = ''
      await load()
    }
  } finally {
    submitting.value = false
  }
}
</script>

<template>
  <view class="page">
    <view class="section-title">待拣选任务</view>
    <scroll-view scroll-y class="list">
      <view
        v-for="t in list"
        :key="t.id"
        class="card"
        :class="{ active: selected?.id === t.id }"
        @click="selectTask(t)"
      >
        <view class="row1">
          <text class="code">{{ t.taskNo }}</text>
          <text class="qty">× {{ t.bookQty }}</text>
        </view>
        <view class="row2">
          {{ t.orderNo }} · {{ t.materialCode }}
          <text v-if="t.fromLocation"> · {{ t.fromLocation }}</text>
        </view>
      </view>
      <view v-if="!list.length" class="empty">暂无待拣选任务</view>
    </scroll-view>

    <view v-if="selected" class="form">
      <view class="section-title">确认拣选 · {{ selected.taskNo }}</view>
      <view class="row">
        <text class="label">数量</text>
        <input v-model="pickQty" class="input" type="digit" />
      </view>
      <view class="row">
        <text class="label">容器</text>
        <input v-model="containerCode" class="input" placeholder="扫描或输入" confirm-type="next" />
        <button size="mini" @click="onScanContainer">扫</button>
      </view>
      <view class="row">
        <text class="label">库位</text>
        <input
          v-model="fromLocation"
          class="input"
          placeholder="取货库位"
          confirm-type="done"
          @confirm="submit"
        />
        <button size="mini" @click="onScanLoc">扫</button>
      </view>
      <button class="submit" type="primary" :loading="submitting" @click="submit">确认拣选</button>
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
  max-height: 360rpx;
}
.card {
  background: #fff;
  padding: 24rpx;
  border-radius: 12rpx;
  margin-bottom: 12rpx;
}
.card.active {
  border: 2rpx solid #2c5f6e;
}
.row1 {
  display: flex;
  justify-content: space-between;
}
.code {
  font-weight: 700;
  color: #1b3a4b;
  font-size: 32rpx;
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
