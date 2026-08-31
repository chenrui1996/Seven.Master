<script setup lang="ts">
import { ref } from 'vue'
import type { SimMapDevice, SimMapNode } from '@/lib/project/schema'
import ThreeScene from './ThreeScene.vue'

defineProps<{
  nodes: SimMapNode[]
  devices: SimMapDevice[]
}>()

const threeSceneRef = ref<InstanceType<typeof ThreeScene> | null>(null)

function nudgeDevice(deviceCode?: string) {
  threeSceneRef.value?.nudgeDevice(deviceCode)
}

defineExpose({ nudgeDevice })
</script>

<template>
  <el-card header="3D 地图预览">
    <ThreeScene ref="threeSceneRef" :nodes="nodes" :devices="devices" />
    <p v-if="!nodes.length && !devices.length" class="hint">
      暂无地图数据，请先在地图编辑器中添加节点与设备。
    </p>
  </el-card>
</template>

<style scoped>
.hint {
  margin: 0.75rem 0 0;
  font-size: 13px;
  color: #64748b;
}
</style>
