<template>
  <OpsFloorPlan
    pack="stacker"
    :title="t('wcsOps.stkMonitor.title')"
    :subtitle="t('wcsOps.stkMonitor.subtitle')"
  />
</template>

<script setup lang="ts">
import { useI18n } from 'vue-i18n'
import OpsFloorPlan from '../../../../components/wcs-ops/OpsFloorPlan.vue'

const { t } = useI18n()
</script>
