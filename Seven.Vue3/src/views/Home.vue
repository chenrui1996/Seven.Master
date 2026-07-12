<template>
  <div class="seven-page home-dashboard">
    <div class="seven-kpi-grid">
      <div v-for="kpi in kpiCards" :key="kpi.label" class="seven-kpi-card" :class="kpi.variant">
        <div class="seven-kpi-label">{{ kpi.label }}</div>
        <div class="seven-kpi-value">{{ kpi.value }}</div>
        <div class="seven-kpi-meta">{{ kpi.meta }}</div>
      </div>
    </div>

    <el-row :gutter="16">
      <el-col :xs="24" :lg="14">
        <div class="seven-panel">
          <div class="seven-panel__header">
            <h3 class="seven-panel__title">{{ t('home.modulesTitle') }}</h3>
          </div>
          <div class="seven-panel__body module-grid">
            <div
              v-for="mod in moduleShortcuts"
              :key="mod.titleKey"
              class="module-card cursor-pointer"
              @click="onModuleClick(t(mod.titleKey))"
            >
              <div class="module-icon" :style="{ background: mod.color + '18', color: mod.color }">
                <el-icon :size="24"><component :is="mod.icon" /></el-icon>
              </div>
              <div class="module-info">
                <div class="module-title">{{ t(mod.titleKey) }}</div>
                <div class="module-desc">{{ t(mod.descKey) }}</div>
              </div>
              <el-icon class="module-arrow"><ArrowRight /></el-icon>
            </div>
          </div>
        </div>
      </el-col>

      <el-col :xs="24" :lg="10">
        <div class="seven-panel">
          <div class="seven-panel__header">
            <h3 class="seven-panel__title">{{ t('home.runtimeTitle') }}</h3>
          </div>
          <div class="seven-panel__body">
            <dl class="info-list">
              <div class="info-row">
                <dt>{{ t('home.operator') }}</dt>
                <dd>{{ userStore.userTrueName || userStore.userName }}</dd>
              </div>
              <div class="info-row">
                <dt>{{ t('home.permissions') }}</dt>
                <dd>{{ t('home.permissionsUnit', { count: userStore.permissions.length }) }}</dd>
              </div>
              <div class="info-row">
                <dt>{{ t('home.version') }}</dt>
                <dd>{{ t('home.versionValue') }}</dd>
              </div>
              <div class="info-row">
                <dt>{{ t('home.techStack') }}</dt>
                <dd>{{ t('home.techStackValue') }}</dd>
              </div>
            </dl>
            <el-divider />
            <p class="system-desc">{{ t('home.systemDesc') }}</p>
          </div>
        </div>

        <div class="seven-panel" style="margin-top: 16px">
          <div class="seven-panel__header">
            <h3 class="seven-panel__title">{{ t('home.deviceTitle') }}</h3>
          </div>
          <div class="seven-panel__body device-list">
            <div v-for="dev in devices" :key="dev.name" class="device-row">
              <span class="device-name">{{ dev.name }}</span>
              <el-tag :type="dev.status" size="small" effect="plain">{{ t(dev.labelKey) }}</el-tag>
            </div>
          </div>
        </div>
      </el-col>
    </el-row>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import { ArrowRight } from '@element-plus/icons-vue'
import { useRouter } from 'vue-router'
import { useUserStore } from '../stores/user'
import { useAlarmStore } from '../stores/alarm'
import { moduleShortcutKeys } from '../utils/menuIcons'

const { t } = useI18n()
const router = useRouter()
const userStore = useUserStore()
const alarmStore = useAlarmStore()

const moduleShortcuts = moduleShortcutKeys

const kpiCards = computed(() => [
  { label: t('home.kpiPendingTasks'), value: '128', meta: t('home.kpiPendingMeta'), variant: 'seven-kpi-card--warning' },
  { label: t('home.kpiOnlineDevices'), value: '24', meta: t('home.kpiOnlineMeta'), variant: 'seven-kpi-card--success' },
  { label: t('home.kpiThroughput'), value: '3,842', meta: t('home.kpiThroughputMeta'), variant: 'seven-kpi-card--info' },
  {
    label: t('home.kpiAlerts'),
    value: String(alarmStore.activeCount),
    meta: t('home.kpiAlertsMeta'),
    variant: 'seven-kpi-card--danger',
  },
])

const devices = [
  { name: 'Stacker-01', status: 'success' as const, labelKey: 'home.deviceRunning' },
  { name: 'AGV-Group-A', status: 'success' as const, labelKey: 'home.deviceRunning' },
  { name: 'Conveyor-L3', status: 'warning' as const, labelKey: 'home.deviceStandby' },
  { name: 'PLC-Master', status: 'success' as const, labelKey: 'home.deviceOnline' },
]

function onModuleClick(name: string) {
  if (name === t('home.moduleAlert')) {
    router.push('/Sys_Alarm')
    return
  }
  ElMessage.info(t('home.moduleDeveloping', { name }))
}

onMounted(() => {
  alarmStore.fetchActiveCount()
})
</script>

<style scoped>
.home-dashboard {
  max-width: 1400px;
}

.header-actions :deep(.el-tag) {
  display: flex;
  align-items: center;
  gap: 4px;
  font-family: var(--seven-font-mono);
  font-size: 12px;
}

.module-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: var(--seven-space-3);
}

@media (max-width: 640px) {
  .module-grid {
    grid-template-columns: 1fr;
  }
}

.module-card {
  display: flex;
  align-items: center;
  gap: 14px;
  padding: 14px 16px;
  border: 1px solid var(--seven-border-light);
  border-radius: var(--seven-radius);
  transition: border-color 0.2s ease, box-shadow 0.2s ease, transform 0.2s ease;
}

.module-card:hover {
  border-color: var(--seven-accent);
  box-shadow: var(--seven-shadow);
  transform: translateY(-1px);
}

.module-icon {
  width: 44px;
  height: 44px;
  border-radius: 8px;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}

.module-info {
  flex: 1;
  min-width: 0;
}

.module-title {
  font-weight: 600;
  font-size: 14px;
  color: var(--seven-primary-dark);
}

.module-desc {
  font-size: 12px;
  color: var(--seven-text-muted);
  margin-top: 2px;
}

.module-arrow {
  color: var(--seven-text-muted);
  transition: color 0.2s ease, transform 0.2s ease;
}

.module-card:hover .module-arrow {
  color: var(--seven-accent);
  transform: translateX(2px);
}

.info-list {
  margin: 0;
}

.info-row {
  display: flex;
  justify-content: space-between;
  padding: 8px 0;
  border-bottom: 1px dashed var(--seven-border-light);
  font-size: 13px;
}

.info-row:last-child {
  border-bottom: none;
}

.info-row dt {
  color: var(--seven-text-muted);
}

.info-row dd {
  margin: 0;
  font-family: var(--seven-font-mono);
  font-weight: 500;
  color: var(--seven-primary-dark);
}

.system-desc {
  margin: 0;
  font-size: 13px;
  line-height: 1.7;
  color: var(--seven-text-muted);
}

.device-list {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.device-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: var(--seven-space-2) var(--seven-space-3);
  background: var(--seven-bg-subtle);
  border-radius: 4px;
  border: 1px solid var(--seven-border-light);
}

.device-name {
  font-family: var(--seven-font-mono);
  font-size: 13px;
  color: var(--seven-primary-dark);
}
</style>
