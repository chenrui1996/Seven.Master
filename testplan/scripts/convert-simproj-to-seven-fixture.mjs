/**
 * Convert RCS4Shuttle `.simproj.json` → Seven WCS seed fixtures (Fw/Stk).
 *
 * Usage:
 *   node convert-simproj-to-seven-fixture.mjs <simproj.json> --pack fourway|stacker --out <file.json>
 */
import fs from 'node:fs'
import path from 'node:path'

function stripPort(ref) {
  if (!ref) return ''
  const i = String(ref).lastIndexOf('.')
  if (i <= 0) return String(ref)
  const suffix = String(ref).slice(i + 1)
  // port-like suffixes
  if (/^(top|bottom|left|right|front|back|in|out|P_\w+)$/i.test(suffix)) {
    return String(ref).slice(0, i)
  }
  return String(ref)
}

function parseArgs(argv) {
  const args = { pack: 'fourway', out: '', src: '' }
  for (let i = 2; i < argv.length; i++) {
    const a = argv[i]
    if (a === '--pack') args.pack = argv[++i]
    else if (a === '--out') args.out = argv[++i]
    else if (!a.startsWith('-')) args.src = a
  }
  return args
}

function convertFourWay(src, projectName) {
  const map = src.map || {}
  const devices = map.devices || []
  const byId = Object.fromEntries(devices.map((d) => [d.id, d]))
  const layers = (map.layers || []).map((l) => ({
    code: `Fw.L${String(l.id).padStart(2, '0')}`,
    name: l.name || `Layer ${l.id}`,
    sourceLayerId: l.id,
  }))

  /** @type {Record<string, any>} */
  const mapsByLayer = {}
  for (const layer of layers) {
    mapsByLayer[layer.code] = {
      code: `FW-${projectName}-${layer.code}`,
      layerCode: layer.code,
      isActive: true,
      nodes: [],
      routes: [],
    }
  }

  const nodeSeen = new Map() // layerCode|code -> true
  function ensureNode(layerCode, code, x, y, kind) {
    const key = `${layerCode}|${code}`
    if (nodeSeen.has(key)) return
    nodeSeen.set(key, true)
    mapsByLayer[layerCode]?.nodes.push({ code, x: x ?? 0, y: y ?? 0, kind: kind || 'Track' })
  }

  for (const d of devices) {
    const layerId = d.layer ?? 1
    const layerCode = `Fw.L${String(layerId).padStart(2, '0')}`
    const code = d.wcsAddress || d.id
    if (['ShuttleMainTrack', 'ShuttleSubTrack', 'ChainTrackConveyor', 'ChainConveyor', 'PalletLift'].includes(d.type)) {
      ensureNode(layerCode, code, d.pos2d?.x, d.pos2d?.y, d.type)
    }
    // subtrack slots → location candidates
    if (d.type === 'ShuttleSubTrack') {
      const slotCount = d.params?.slotCount || 1
      for (let s = 1; s <= slotCount; s++) {
        const loc = `Fw.${code}.S${s}`
        ensureNode(layerCode, loc, (d.pos2d?.x || 0) + s * 8, d.pos2d?.y, 'Location')
      }
    }
  }

  // hoist on all layers
  for (const d of devices.filter((x) => x.type === 'PalletLift')) {
    for (const layer of layers) {
      ensureNode(layer.code, d.wcsAddress || d.id, d.pos2d?.x, d.pos2d?.y, 'Hoist')
    }
  }

  for (const c of map.connections || []) {
    const fromId = stripPort(c.from)
    const toId = stripPort(c.to)
    const a = byId[fromId]
    const b = byId[toId]
    if (!a || !b) continue
    const layerId = c.layer ?? a.layer ?? b.layer ?? 1
    const layerCode = `Fw.L${String(layerId).padStart(2, '0')}`
    const from = a.wcsAddress || a.id
    const to = b.wcsAddress || b.id
    ensureNode(layerCode, from, a.pos2d?.x, a.pos2d?.y, a.type)
    ensureNode(layerCode, to, b.pos2d?.x, b.pos2d?.y, b.type)
    mapsByLayer[layerCode]?.routes.push({
      fromCode: from,
      toCode: to,
      weight: 1,
      capacity: 1,
      bidirectional: true,
    })
  }

  // bidirectional expand
  for (const layerCode of Object.keys(mapsByLayer)) {
    const routes = mapsByLayer[layerCode].routes
    const extra = []
    for (const r of routes) {
      if (!r.bidirectional) continue
      extra.push({ fromCode: r.toCode, toCode: r.fromCode, weight: r.weight, capacity: r.capacity })
    }
    mapsByLayer[layerCode].routes = [...routes.map(({ bidirectional, ...r }) => r), ...extra]
  }

  const gateways = devices
    .filter((d) => d.type === 'ChainConveyor' && d.params?.isGateway)
    .map((d) => ({ code: `Fw.${d.wcsAddress || d.id}`, source: d.wcsAddress || d.id, layer: d.layer || 1 }))

  const hoist = devices
    .filter((d) => d.type === 'PalletLift')
    .map((d) => ({
      hoistNo: d.wcsAddress || d.id,
      dockingHeights: d.params?.dockingHeights || [],
      allowShuttle: !!d.params?.allowShuttle,
      apByLayer: layers.map((l) => ({ layerCode: l.code, address: d.wcsAddress || d.id })),
    }))

  const shuttles = devices
    .filter((d) => d.type === 'Shuttle')
    .map((d) => ({
      code: d.wcsAddress || d.id,
      layer: d.layer || 1,
      layerCode: `Fw.L${String(d.layer || 1).padStart(2, '0')}`,
      pos: d.pos2d,
    }))

  const parking = layers.map((l, i) => ({
    code: `Fw.PARK-${l.code}`,
    layerCode: l.code,
    aisleCode: `Fw.A${i + 1}`,
    status: 'Free',
  }))

  return {
    source: {
      file: path.basename(String(globalThis.__SIMPROJ_SRC || 'singapore')),
      project: src.meta?.name || projectName,
      packId: 'fourway',
    },
    canvas: map.canvas,
    layers,
    maps: Object.values(mapsByLayer),
    gateways,
    hoist,
    shuttles,
    parking,
    requestPoints: [
      ...layers.map((l) => ({ code: `Fw.RP-${l.code}-LAYER`, pointType: 'LayerRequest', layerCode: l.code })),
      ...layers.map((l) => ({ code: `Fw.RP-${l.code}-AISLE`, pointType: 'AisleRequest', layerCode: l.code })),
      ...layers.map((l) => ({ code: `Fw.RP-${l.code}-LOC`, pointType: 'LocationRequest', layerCode: l.code })),
      ...hoist.flatMap((h) =>
        h.apByLayer.flatMap((p) => [
          { code: `Fw.HOIST-${h.hoistNo}-AP-${p.layerCode}`, pointType: 'HoistAp', layerCode: p.layerCode, address: p.address },
          { code: `Fw.HOIST-${h.hoistNo}-EP-${p.layerCode}`, pointType: 'HoistEp', layerCode: p.layerCode, address: p.address },
        ]),
      ),
    ],
    scenarios: [
      'same-layer-inbound-path',
      'traffic-edge-mutex',
      'parking-reserve-dispatch',
      'cross-layer-hoist-three-stage',
      'max-shuttle-density-skip-aisle',
    ],
  }
}

function convertStacker(src, projectName) {
  const map = src.map || {}
  const devices = map.devices || []
  const racks = devices.filter((d) => d.type === 'BeamRack')
  const points = devices.filter((d) => d.type === 'CoordPoint')
  const srms = devices.filter((d) => d.type === 'SRM')
  const conveyors = devices.filter((d) =>
    ['RollerConveyor', 'ChainConveyor', 'FourWayTransferConveyor'].includes(d.type),
  )

  const aisles = racks.map((r, idx) => {
    const aisleCode = `Stk.A${idx + 1}`
    const locPoints = points.filter((p) => p.params?.rackId === r.id)
    const locations = locPoints.map((p) => ({
      code: `Stk.${p.id}`,
      aisleCode,
      column: p.params?.col ?? 0,
      layer: p.params?.layer ?? 0,
      depth: p.params?.depth ?? 1,
      binGroupCode: `BG-${r.id}-C${p.params?.col ?? 0}-L${p.params?.layer ?? 0}`,
      x: p.pos2d?.x ?? 0,
      y: p.pos2d?.y ?? 0,
      elevation: p.params?.elevation,
      cellHeight: r.params?.cellHeight,
    }))
    // pair shallow/deep for double-deep groups
    const groups = {}
    for (const loc of locations) {
      const g = loc.binGroupCode
      if (!groups[g]) groups[g] = []
      groups[g].push(loc)
    }
    return {
      aisleCode,
      rackId: r.id,
      srmHint: srms[idx]?.wcsAddress || srms[idx]?.id || srms[0]?.id,
      doubleDeep: (r.params?.depth || 1) >= 2,
      columns: r.params?.columns,
      layers: r.params?.layers,
      cellHeight: r.params?.cellHeight,
      locations,
      binGroups: Object.entries(groups).map(([code, members]) => ({
        code,
        depths: members.map((m) => m.depth).sort(),
        members: members.map((m) => m.code),
      })),
    }
  })

  const nodes = []
  const routes = []
  const nodeSeen = new Set()
  function addNode(code, x, y, kind) {
    if (nodeSeen.has(code)) return
    nodeSeen.add(code)
    nodes.push({ code, x: x ?? 0, y: y ?? 0, kind })
  }

  for (const c of conveyors) addNode(c.wcsAddress || c.id, c.pos2d?.x, c.pos2d?.y, c.type)
  for (const s of srms) addNode(s.wcsAddress || s.id, s.pos2d?.x, s.pos2d?.y, 'SRM')
  // sample location nodes (all would be huge; keep all for correctness of depth tests)
  for (const a of aisles) {
    for (const loc of a.locations) addNode(loc.code, loc.x, loc.y, 'Location')
  }

  for (const c of map.connections || []) {
    const fromId = stripPort(c.from)
    const toId = stripPort(c.to)
    const a = devices.find((d) => d.id === fromId)
    const b = devices.find((d) => d.id === toId)
    if (!a || !b) continue
    const from = a.wcsAddress || a.id
    const to = b.wcsAddress || b.id
    addNode(from, a.pos2d?.x, a.pos2d?.y, a.type)
    addNode(to, b.pos2d?.x, b.pos2d?.y, b.type)
    routes.push({ fromCode: from, toCode: to, weight: 1, capacity: 1, exeStackCode: a.type === 'SRM' || b.type === 'SRM' ? 'SRM' : 'CVY' })
    routes.push({ fromCode: to, toCode: from, weight: 1, capacity: 1, exeStackCode: a.type === 'SRM' || b.type === 'SRM' ? 'SRM' : 'CVY' })
  }

  // within-aisle synthetic edges: adjacent columns same layer/depth for PathDispatcher demos
  for (const aisle of aisles) {
    const byKey = {}
    for (const loc of aisle.locations) {
      const k = `${loc.layer}|${loc.depth}`
      if (!byKey[k]) byKey[k] = []
      byKey[k].push(loc)
    }
    for (const list of Object.values(byKey)) {
      list.sort((x, y) => x.column - y.column)
      for (let i = 0; i < list.length - 1; i++) {
        routes.push({
          fromCode: list[i].code,
          toCode: list[i + 1].code,
          weight: 1,
          capacity: 1,
          exeStackCode: 'SRM',
        })
        routes.push({
          fromCode: list[i + 1].code,
          toCode: list[i].code,
          weight: 1,
          capacity: 1,
          exeStackCode: 'SRM',
        })
      }
    }
  }

  const requestPoints = aisles.map((a, i) => ({
    code: `Stk.RP-${a.aisleCode}`,
    pointType: 'AisleRequest',
    aisleCode: a.aisleCode,
    destinationPointCode: conveyors[i]?.wcsAddress || conveyors[0]?.wcsAddress || a.srmHint,
  }))

  return {
    source: {
      file: path.basename(String(globalThis.__SIMPROJ_SRC || 'srm-demo')),
      project: src.meta?.name || projectName,
      packId: 'stacker',
    },
    canvas: map.canvas,
    srms: srms.map((s) => ({ code: s.wcsAddress || s.id, pos: s.pos2d })),
    aisles: aisles.map((a) => ({
      aisleCode: a.aisleCode,
      rackId: a.rackId,
      doubleDeep: a.doubleDeep,
      columns: a.columns,
      layers: a.layers,
      cellHeight: a.cellHeight,
      locationCount: a.locations.length,
      sampleLocations: a.locations.slice(0, 6),
      binGroupSample: a.binGroups.slice(0, 3),
    })),
    locations: aisles.flatMap((a) => a.locations),
    locationProfiles: aisles.flatMap((a) =>
      a.binGroups.map((g) => ({
        binGroupCode: g.code,
        locationCodes: g.members,
        inLockBin: false,
        outLockBin: false,
      })),
    ),
    map: {
      code: `STK-${projectName}`,
      isActive: true,
      nodes,
      routes,
    },
    requestPoints,
    deviceCoders: aisles.flatMap((a) =>
      a.locations.slice(0, 20).map((loc) => ({
        locationCode: loc.code,
        pointCode: loc.code,
      })),
    ),
    scenarios: [
      'sudr-aisle-then-location',
      'double-deep-no-orphan-deep',
      'depth-guard-transfer',
      'path-dijkstra-merge-exestack',
      'pri-gate-same-group',
      'height-policy-filter-aisle',
    ],
    stats: {
      coordPoints: points.length,
      depth1: points.filter((p) => p.params?.depth === 1).length,
      depth2: points.filter((p) => p.params?.depth === 2).length,
      racks: racks.length,
      srms: srms.length,
      routeCount: routes.length,
      nodeCount: nodes.length,
    },
  }
}

const args = parseArgs(process.argv)
if (!args.src || !args.out) {
  console.error('Usage: node convert-simproj-to-seven-fixture.mjs <simproj.json> --pack fourway|stacker --out <out.json>')
  process.exit(1)
}

globalThis.__SIMPROJ_SRC = args.src
const raw = JSON.parse(fs.readFileSync(args.src, 'utf8'))
const name = (raw.meta?.name || 'Imported').replace(/\s+/g, '')
const fixture = args.pack === 'stacker' ? convertStacker(raw, name) : convertFourWay(raw, name)
fs.mkdirSync(path.dirname(args.out), { recursive: true })
fs.writeFileSync(args.out, JSON.stringify(fixture, null, 2))
console.log(
  'Wrote',
  args.out,
  'pack=',
  args.pack,
  'maps/nodes=',
  fixture.maps
    ? fixture.maps.map((m) => `${m.layerCode}:${m.nodes.length}/${m.routes.length}`).join(',')
    : `${fixture.map?.nodes?.length}/${fixture.map?.routes?.length}`,
  'locations=',
  fixture.locations?.length ?? 'n/a',
)
