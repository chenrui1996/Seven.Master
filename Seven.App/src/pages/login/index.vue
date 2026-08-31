<script setup lang="ts">
import { ref } from 'vue'
import { useUserStore } from '../../stores/user'

const userName = ref('admin')
const password = ref('123456')
const loading = ref(false)
const user = useUserStore()

async function onLogin() {
  if (!userName.value || !password.value) {
    uni.showToast({ title: '请输入账号密码', icon: 'none' })
    return
  }
  loading.value = true
  try {
    await user.login(userName.value.trim(), password.value)
    uni.reLaunch({ url: '/pages/home/index' })
  } catch (e: any) {
    uni.showToast({ title: e?.message || '登录失败', icon: 'none' })
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <view class="page">
    <view class="brand">Seven PDA</view>
    <view class="sub">平库收货 / 上架</view>
    <view class="form">
      <input v-model="userName" class="input" placeholder="用户名" confirm-type="next" />
      <input
        v-model="password"
        class="input"
        password
        placeholder="密码"
        confirm-type="done"
        @confirm="onLogin"
      />
      <button class="btn" type="primary" :loading="loading" @click="onLogin">登录</button>
    </view>
  </view>
</template>

<style scoped>
.page {
  min-height: 100vh;
  padding: 120rpx 48rpx 48rpx;
  background: linear-gradient(180deg, #1b3a4b 0%, #2c5f6e 40%, #f0f2f5 40%);
}
.brand {
  color: #fff;
  font-size: 56rpx;
  font-weight: 700;
  letter-spacing: 2rpx;
}
.sub {
  color: rgba(255, 255, 255, 0.75);
  margin-top: 12rpx;
  margin-bottom: 80rpx;
  font-size: 28rpx;
}
.form {
  background: #fff;
  border-radius: 16rpx;
  padding: 40rpx 32rpx;
  box-shadow: 0 8rpx 32rpx rgba(27, 58, 75, 0.12);
}
.input {
  border: 1px solid #dce3ea;
  border-radius: 12rpx;
  padding: 24rpx 28rpx;
  margin-bottom: 24rpx;
  font-size: 30rpx;
}
.btn {
  margin-top: 12rpx;
  background: #1b3a4b !important;
  border-radius: 12rpx;
}
</style>
