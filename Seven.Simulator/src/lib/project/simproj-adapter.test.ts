import { adaptSimProj, adaptSimProjJson } from './simproj-adapter'

function assert(condition: boolean, message: string) {
  if (!condition) throw new Error(message)
}

const FIXTURE_JSON = `{
  "version": 1,
  "meta": { "name": "MinimalRCS", "runtimeMode": "Simulation", "simCommsMode": "Trigger" },
  "map": {
    "packId": "stacker",
    "nodes": [
      { "id": "n1", "code": "Stk.A-01", "x": 40, "y": 40 },
      { "id": "n2", "code": "Stk.A-02", "x": 120, "y": 40 }
    ],
    "edges": [{ "id": "e1", "from": "n1", "to": "n2" }],
    "devices": [
      { "id": "d1", "code": "SRM01", "type": "SRM", "x": 80, "y": 120 },
      { "id": "d2", "code": "SH01", "type": "Shuttle", "x": 200, "y": 40 }
    ],
    "connections": [{ "id": "c1", "from": "d1.fork", "to": "n1.io" }],
    "requestPoints": [{ "code": "Stk.RP_IN_01", "mappedLocationCode": "Stk.A-01" }]
  }
}`

// inline minimal source
{
  const { project, warnings } = adaptSimProj({
    meta: { name: 'Inline' },
    map: {
      packId: 'stacker',
      nodes: [{ id: 'n1', code: 'A1', x: 0, y: 0 }],
      devices: [{ id: 'd1', code: 'SRM01', type: 'SRM', x: 10, y: 10 }],
    },
  })
  assert(project.meta.name === 'Inline', 'meta.name mapped')
  assert(project.map.nodes.length === 1, 'one node')
  assert(project.map.devices.length === 1, 'one device')
  assert(warnings.length === 0, 'no warnings for supported type')
}

// unsupported device type → warning, not blocked
{
  const { project, warnings } = adaptSimProj({
    map: {
      devices: [{ code: 'SH01', type: 'Shuttle', x: 0, y: 0 }],
    },
  })
  assert(project.map.devices[0]?.type === 'Shuttle', 'unsupported device kept')
  assert(warnings.some((w) => w.includes('Shuttle')), 'warning for Shuttle')
}

// fixture JSON string
{
  const { project, warnings } = adaptSimProjJson(FIXTURE_JSON)
  assert(project.meta.name === 'MinimalRCS', 'fixture name')
  assert(project.map.nodes.length === 2, 'fixture nodes')
  assert(project.map.edges.length === 1, 'fixture edge')
  assert(project.map.requestPoints.length === 1, 'fixture request point')
  assert(warnings.some((w) => w.includes('Shuttle')), 'fixture warns on Shuttle')
}

console.log('simproj-adapter: 3 cases passed')
