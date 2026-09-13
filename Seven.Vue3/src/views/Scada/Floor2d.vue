<template>
  <div class="seven-page scada-floor2d">
    <el-alert
      type="warning"
      :closable="false"
      :title="t('scada.floorTitle')"
      :description="t('scada.floorDesc')"
      style="margin-bottom: 16px"
    />
    <div class="ops-toolbar" style="display: flex; gap: 8px; margin-bottom: 12px">
      <el-button type="primary" @click="$router.push('/Wcs/Stacker/Ops/Monitor')">
        {{ t('scada.goStackerMonitor') }}
      </el-button>
      <el-button type="primary" @click="$router.push('/Wcs/FourWay/Ops/Monitor')">
        {{ t('scada.goFourWayMonitor') }}
      </el-button>
    </div>
    <ScadaEmbed />
  </div>
</template>

<script setup lang="ts">
import { useI18n } from 'vue-i18n'
import ScadaEmbed from '../../components/wcs-ops/ScadaEmbed.vue'

const { t } = useI18n()
</script>
