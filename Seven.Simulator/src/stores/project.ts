import { defineStore } from 'pinia'
import { ref } from 'vue'

export interface SimFeatures {
  wms: boolean
  orchestrationBus: boolean
  hotStore: boolean
  deviceComm: boolean
  simulator: boolean
  wcsPacks: {
    stacker: boolean
    fourWay: boolean
    boxSort: boolean
  }
}

export interface SimProject {
  version: number
  meta: {
    name: string
    features: SimFeatures
    runtimeMode: 'Simulation' | 'Production'
  }
  map: {
    packId: string
    nodes: { id: string; code: string; x: number; y: number }[]
    edges: { id: string; from: string; to: string }[]
    devices: { id: string; code: string; type: string; x: number; y: number }[]
  }
}

const defaultFeatures = (): SimFeatures => ({
  wms: true,
  orchestrationBus: true,
  hotStore: false,
  deviceComm: false,
  simulator: true,
  wcsPacks: { stacker: true, fourWay: false, boxSort: false },
})

export const useProjectStore = defineStore(
  'simProject',
  () => {
    const project = ref<SimProject>({
      version: 1,
      meta: {
        name: 'Demo',
        features: defaultFeatures(),
        runtimeMode: 'Simulation',
      },
      map: { packId: 'stacker', nodes: [], edges: [], devices: [] },
    })

    function exportAppsettingsSnippet(): string {
      const f = project.value.meta.features
      return JSON.stringify(
        {
          Features: {
            Wms: f.wms,
            OrchestrationBus: f.orchestrationBus,
            HotStore: f.hotStore,
            DeviceComm: f.deviceComm,
            Simulator: f.simulator,
            WcsPacks: {
              Stacker: f.wcsPacks.stacker,
              FourWay: f.wcsPacks.fourWay,
              BoxSort: f.wcsPacks.boxSort,
            },
          },
        },
        null,
        2,
      )
    }

    function downloadProject() {
      const blob = new Blob([JSON.stringify(project.value, null, 2)], { type: 'application/json' })
      const url = URL.createObjectURL(blob)
      const a = document.createElement('a')
      a.href = url
      a.download = `${project.value.meta.name || 'project'}.sevenproj.json`
      a.click()
      URL.revokeObjectURL(url)
    }

    async function importProject(file: File) {
      const text = await file.text()
      project.value = JSON.parse(text) as SimProject
    }

    return { project, exportAppsettingsSnippet, downloadProject, importProject }
  },
  { persist: true },
)
