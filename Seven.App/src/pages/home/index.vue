<script setup lang="ts">
import { onShow } from '@dcloudio/uni-app'
import { ref } from 'vue'
import { fetchPdaMenu } from '../../api/pda'
import { useUserStore } from '../../stores/user'

const user = useUserStore()
const menus = ref<Array<{ code: string; title: string; path: string }>>([])

onShow(async () => {
  if (!user.token) {
    uni.reLaunch({ url: '/pages/login/index' })
    return
  }
  const res = await fetchPdaMenu()
  if (res.status && res.data) menus.value = res.data
  else
    menus.value = [
      { code: 'receive', title: '平库收货', path: '/pages/receive/index' },
      { code: 'putaway', title: '平库上架', path: '/pages/putaway/index' },
      { code: 'picking', title: '出库拣选', path: '/pages/picking/index' },
      { code: 'cyclecount', title: '盘点录入', path: '/pages/cyclecount/index' },
    ]
})

function go(path: string) {
  uni.navigateTo({ url: path })
}

function logout() {
  user.logout()
}
</script>

<template>
  <view class="page">
    <view class="hello">你好，{{ user.userName || '操作员' }}</view>
    <view class="grid">
      <view v-for="m in menus" :key="m.code" class="tile" @click="go(m.path)">
        <text class="tile-title">{{ m.title }}</text>
      </view>
    </view>
    <button class="logout" size="mini" @click="logout">退出</button>
  </view>
</template>

<style scoped>
.page {
  padding: 40rpx 32rpx;
}
.hello {
  font-size: 34rpx;
  color: #1b3a4b;
  margin-bottom: 40rpx;
}
.grid {
  display: flex;
  flex-wrap: wrap;
  gap: 24rpx;
}
.tile {
  width: calc(50% - 12rpx);
  height: 200rpx;
  background: #fff;
  border-radius: 16rpx;
  display: flex;
  align-items: center;
  justify-content: center;
  box-shadow: 0 4rpx 16rpx rgba(0, 0, 0, 0.06);
  border-left: 8rpx solid #2c5f6e;
}
.tile-title {
  font-size: 32rpx;
  font-weight: 600;
  color: #1b3a4b;
}
.logout {
  margin-top: 48rpx;
}
</style>
