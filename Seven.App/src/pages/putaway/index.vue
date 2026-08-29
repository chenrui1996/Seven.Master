<script setup lang="ts">
import { onShow } from '@dcloudio/uni-app'
import { ref } from 'vue'
import { confirmPutaway, fetchPendingPutaway } from '../../api/pda'

type Item = {
  detailId: number
  orderNo: string
  detailNo: number
  materialCode: string
  qty: number
  containerCode: string
  receiveLocationCode: string
}

const list = ref<Item[]>([])
const selected = ref<Item | null>(null)
const toLocationCode = ref('')
const submitting = ref(false)

onShow(load)

async function load() {
  const res = await fetchPendingPutaway()
  if (res.status && res.data) {
    list.value = res.data
    if (!selected.value && list.value.length) selected.value = list.value[0]
  }
}

function pick(item: Item) {
  selected.value = item
  toLocationCode.value = ''
}

function onScanLoc() {
  // @ts-expect-error uni scan
  uni.scanCode?.({
    success: (r: { result: string }) => {
      toLocationCode.value = r.result
    },
    fail: () => uni.showToast({ title: '请手动输入或使用扫码枪', icon: 'none' }),
  })
}

async function submit() {
  if (!selected.value) return
  if (!toLocationCode.value.trim()) {
    uni.showToast({ title: '请扫描目标库位', icon: 'none' })
    return
  }
  submitting.value = true
  try {
    const res = await confirmPutaway({
      containerCode: selected.value.containerCode,
      toLocationCode: toLocationCode.value.trim(),
      detailId: selected.value.detailId,
    })
    if (res.status) {
      uni.showToast({ title: '上架成功', icon: 'success' })
      selected.value = null
      toLocationCode.value = ''
      await load()
    }
  } finally {
    submitting.value = false
  }
}
</script>

<template>
  <view class="page">
    <view class="section-title">待上架容器</view>
    <view
      v-for="item in list"
      :key="item.detailId"
      class="card"
      :class="{ active: selected?.detailId === item.detailId }"
      @click="pick(item)"
    >
      <view class="row1">
        <text class="code">{{ item.containerCode }}</text>
        <text class="qty">× {{ item.qty }}</text>
      </view>
      <view class="row2">
        {{ item.orderNo }} · {{ item.materialCode }} · 自 {{ item.receiveLocationCode }}
      </view>
    </view>
    <view v-if="!list.length" class="empty">暂无待上架</view>

    <view v-if="selected" class="form">
      <view class="section-title">上架确认 · {{ selected.containerCode }}</view>
      <view class="row">
        <text class="label">目标位</text>
        <input
          v-model="toLocationCode"
          class="input"
          placeholder="扫描目标库位"
          confirm-type="done"
          @confirm="submit"
        />
        <button size="mini" @click="onScanLoc">扫</button>
      </view>
      <button class="submit" type="primary" :loading="submitting" @click="submit">确认上架</button>
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
}
.input {
  flex: 1;
  border: 1px solid #dce3ea;
  border-radius: 8rpx;
  padding: 16rpx 20rpx;
}
.submit {
  background: #2c5f6e !important;
}
</style>
