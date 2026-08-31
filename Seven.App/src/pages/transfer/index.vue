<script setup lang="ts">
import { onShow } from '@dcloudio/uni-app'
import { ref } from 'vue'
import { completeTransfer, fetchPendingTransfer } from '../../api/pda'

type TransferRow = {
  id: number
  orderNo: string
  status: number
  remark?: string
  lines: Array<{
    lineNo: number
    materialCode: string
    qty: number
    fromLocation?: string
    toLocation?: string
  }>
}

const list = ref<TransferRow[]>([])
const selected = ref<TransferRow | null>(null)
const submitting = ref(false)

onShow(load)

async function load() {
  const res = await fetchPendingTransfer()
  if (res.status && res.data) {
    list.value = res.data
    if (!selected.value && list.value.length) selected.value = list.value[0]
  }
}

async function submit() {
  if (!selected.value) return
  submitting.value = true
  try {
    const res = await completeTransfer(selected.value.id)
    if (res.status) {
      uni.showToast({ title: '调拨完成', icon: 'success' })
      selected.value = null
      await load()
    }
  } finally {
    submitting.value = false
  }
}
</script>

<template>
  <view class="page">
    <view class="section-title">待完成调拨（已审）</view>
    <scroll-view scroll-y class="list">
      <view
        v-for="t in list"
        :key="t.id"
        class="card"
        :class="{ active: selected?.id === t.id }"
        @click="selected = t"
      >
        <view class="row1">
          <text class="code">{{ t.orderNo }}</text>
        </view>
        <view class="row2">
          {{ t.lines?.length || 0 }} 行
          <text v-if="t.remark"> · {{ t.remark }}</text>
        </view>
      </view>
      <view v-if="!list.length" class="empty">暂无已审调拨单</view>
    </scroll-view>

    <view v-if="selected" class="form">
      <view class="section-title">确认完成 · {{ selected.orderNo }}</view>
      <view v-for="l in selected.lines" :key="l.lineNo" class="line">
        #{{ l.lineNo }} {{ l.materialCode }} × {{ l.qty }}
        <text v-if="l.fromLocation"> · {{ l.fromLocation }}→{{ l.toLocation }}</text>
      </view>
      <button type="primary" :loading="submitting" @click="submit">完成调拨</button>
    </view>
  </view>
</template>

<style scoped>
.page {
  padding: 24rpx;
  display: flex;
  flex-direction: column;
  height: 100vh;
  box-sizing: border-box;
}
.section-title {
  font-size: 28rpx;
  font-weight: 600;
  color: #1b3a4b;
  margin-bottom: 16rpx;
}
.list {
  flex: 1;
  max-height: 45vh;
}
.card {
  background: #fff;
  border-radius: 12rpx;
  padding: 24rpx;
  margin-bottom: 16rpx;
  border-left: 6rpx solid #94a3b8;
}
.card.active {
  border-left-color: #2c5f6e;
  background: #f0f7f9;
}
.row1 {
  display: flex;
  justify-content: space-between;
}
.code {
  font-weight: 600;
  color: #1b3a4b;
}
.row2,
.line {
  font-size: 24rpx;
  color: #64748b;
  margin-top: 8rpx;
}
.empty {
  text-align: center;
  color: #94a3b8;
  padding: 48rpx;
}
.form {
  margin-top: 24rpx;
  padding-top: 16rpx;
  border-top: 1px solid #e2e8f0;
}
button {
  margin-top: 24rpx;
}
</style>
