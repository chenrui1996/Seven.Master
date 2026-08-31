import { validateTopology } from './topologyValidator'
import type { SimProject } from '../project/schema'

type SimMap = SimProject['map']

function assert(condition: boolean, message: string) {
  if (!condition) throw new Error(message)
}

function baseMap(overrides: Partial<SimMap> = {}): SimMap {
  return {
    packId: 'stacker',
    nodes: [{ id: 'n1', code: 'A1', x: 0, y: 0 }],
    edges: [],
    devices: [],
    connections: [],
    requestPoints: [],
    ...overrides,
  }
}

// valid map
{
  const errors = validateTopology(baseMap())
  assert(errors.length === 0, 'valid map should have no errors')
}

// duplicate node code
{
  const errors = validateTopology(
    baseMap({
      nodes: [
        { id: 'n1', code: 'DUP', x: 0, y: 0 },
        { id: 'n2', code: 'DUP', x: 80, y: 0 },
      ],
    }),
  )
  assert(errors.some((e) => e.code === 'DUPLICATE_NODE_CODE'), 'duplicate node code')
}

// dangling edge
{
  const errors = validateTopology(
    baseMap({
      edges: [{ id: 'e1', from: 'n1', to: 'missing-node' }],
    }),
  )
  assert(errors.some((e) => e.code === 'EDGE_TO_MISSING'), 'dangling edge to')
}

// missing packId
{
  const errors = validateTopology(baseMap({ packId: '' }))
  assert(errors.some((e) => e.code === 'PACK_ID_MISSING'), 'missing packId')
}

// no nodes
{
  const errors = validateTopology(baseMap({ nodes: [] }))
  assert(errors.some((e) => e.code === 'NO_NODES'), 'no nodes')
}

// dangling connection port
{
  const errors = validateTopology(
    baseMap({
      devices: [{ id: 'd1', code: 'SRM01', type: 'SRM', x: 100, y: 0 }],
      connections: [{ id: 'c1', from: 'd1.fork', to: 'ghost.out' }],
    }),
  )
  assert(
    errors.some((e) => e.code.endsWith('_DEVICE')),
    'connection referencing missing device',
  )
}

console.log('topologyValidator: 6 cases passed')
