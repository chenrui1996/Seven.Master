import { defineStore } from 'pinia'
import { ref } from 'vue'
import {
  defaultSimProject,
  mergeSimProject,
  type SimProject,
} from '../lib/project/schema'
import { adaptSimProjJson } from '../lib/project/simproj-adapter'

export type { SimFeatures, SimProject } from '../lib/project/schema'

export const useProjectStore = defineStore(
  'simProject',
  () => {
    const project = ref<SimProject>(defaultSimProject())

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
      project.value = mergeSimProject(JSON.parse(text) as Partial<SimProject>)
    }

    async function importSimProj(file: File): Promise<string[]> {
      const text = await file.text()
      const { project: adapted, warnings } = adaptSimProjJson(text)
      project.value = adapted
      return warnings
    }

    function applyImportedMap(map: SimProject['map']) {
      project.value.map = {
        ...project.value.map,
        packId: map.packId,
        nodes: map.nodes,
        edges: map.edges ?? [],
        devices: map.devices ?? [],
        connections: map.connections ?? [],
        requestPoints: map.requestPoints ?? [],
      }
    }

    return { project, exportAppsettingsSnippet, downloadProject, importProject, importSimProj, applyImportedMap }
  },
  {
    persist: {
      afterHydrate: (ctx) => {
        const s = ctx.store as unknown as { project: SimProject }
        s.project = mergeSimProject(s.project)
      },
    },
  },
)
