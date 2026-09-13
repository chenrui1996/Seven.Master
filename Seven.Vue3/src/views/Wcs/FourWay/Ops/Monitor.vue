<template>
  <OpsFloorPlan
    pack="fourway"
    :title="t('wcsOps.fwMonitor.title')"
    :subtitle="t('wcsOps.fwMonitor.subtitle')"
  />
</template>

<script setup lang="ts">
import { useI18n } from 'vue-i18n'
import OpsFloorPlan from '../../../../components/wcs-ops/OpsFloorPlan.vue'

const { t } = useI18n()
</script>
