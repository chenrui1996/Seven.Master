<template>
  <el-popover placement="bottom-end" :width="360" trigger="click" @show="onShow">
    <template #reference>
      <button
        type="button"
        class="alarm-bell cursor-pointer"
        :aria-label="t('alarm.bellLabel')"
        :title="t('alarm.bellLabel')"
      >
        <el-badge :value="alarmStore.activeCount" :hidden="alarmStore.activeCount === 0" :max="99">
          <el-icon :size="20"><Bell /></el-icon>
        </el-badge>
      </button>
    </template>

    <div class="alarm-panel">
      <div class="alarm-panel__header">
        <span>{{ t('alarm.recentTitle') }}</span>
        <el-tag v-if="alarmStore.hubConnected" type="success" size="small" effect="plain">
          {{ t('alarm.live') }}
        </el-tag>
        <el-tag v-else type="info" size="small" effect="plain">{{ t('alarm.offline') }}</el-tag>
      </div>

      <div v-if="alarmStore.recentAlarms.length === 0" class="alarm-empty">
        {{ t('alarm.empty') }}
      </div>

      <ul v-else class="alarm-list">
        <li v-for="item in alarmStore.recentAlarms" :key="item.alarmId" class="alarm-item">
          <div class="alarm-item__top">
            <el-tag :type="levelTagType(item.level)" size="small" effect="dark">{{ item.code }}</el-tag>
            <span class="alarm-time">{{ formatTime(item.createDate) }}</span>
          </div>
          <p class="alarm-msg">{{ item.message }}</p>
          <div v-if="item.deviceName" class="alarm-meta">{{ item.deviceName }}</div>
        </li>
      </ul>

      <div class="alarm-panel__footer">
        <el-button type="primary" link @click="goAlarmPage">{{ t('alarm.viewAll') }}</el-button>
      </div>
    </div>
  </el-popover>
</template>

<script setup lang="ts">
import { Bell } from '@element-plus/icons-vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { useAlarmStore } from '../stores/alarm'

const { t } = useI18n()
const router = useRouter()
const alarmStore = useAlarmStore()

function onShow() {
  alarmStore.fetchActiveCount()
}

function goAlarmPage() {
  router.push('/Sys_Alarm')
}

function levelTagType(level: number): 'info' | 'warning' | 'danger' {
  if (level >= 4) return 'danger'
  if (level >= 3) return 'danger'
  if (level >= 2) return 'warning'
  return 'info'
}

function formatTime(value?: string) {
  if (!value) return ''
  return new Date(value).toLocaleString()
}
</script>

<style scoped>
.alarm-bell {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 36px;
  height: 36px;
  border: 1px solid var(--seven-border-light);
  border-radius: var(--seven-radius);
  background: var(--seven-bg-panel);
  color: var(--seven-text);
  transition: border-color 0.2s ease, color 0.2s ease;
}

.alarm-bell:hover {
  border-color: var(--seven-danger);
  color: var(--seven-danger);
}

.alarm-panel__header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
  margin-bottom: 12px;
  font-weight: 600;
  font-size: 14px;
}

.alarm-empty {
  padding: 24px 0;
  text-align: center;
  color: var(--seven-text-muted);
  font-size: 13px;
}

.alarm-list {
  list-style: none;
  margin: 0;
  padding: 0;
  max-height: 320px;
  overflow-y: auto;
}

.alarm-item {
  padding: 10px 0;
  border-bottom: 1px dashed var(--seven-border-light);
}

.alarm-item:last-child {
  border-bottom: none;
}

.alarm-item__top {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
  margin-bottom: 4px;
}

.alarm-time {
  font-size: 11px;
  color: var(--seven-text-muted);
  font-family: var(--seven-font-mono);
}

.alarm-msg {
  margin: 0;
  font-size: 13px;
  line-height: 1.5;
  color: var(--seven-text);
}

.alarm-meta {
  margin-top: 4px;
  font-size: 11px;
  color: var(--seven-text-muted);
  font-family: var(--seven-font-mono);
}

.alarm-panel__footer {
  margin-top: 8px;
  text-align: right;
}
</style>
