import fs from 'node:fs'
import path from 'node:path'

const roots = [
  'src/views/Wcs',
  'src/components/wcs-ops',
  'src/views/Scada',
  'src/views/Wms',
  'src/extension/Wms',
  'src/extension/Business',
]
const re = /(['"`])([^'"`]*[\u4e00-\u9fff][^'"`]*)\1/g
const hits = new Map()

function walk(d) {
  if (!fs.existsSync(d)) return
  for (const f of fs.readdirSync(d, { withFileTypes: true })) {
    const p = path.join(d, f.name)
    if (f.isDirectory()) walk(p)
    else if (/\.(vue|ts)$/.test(f.name)) {
      const t = fs.readFileSync(p, 'utf8')
      const local = []
      let m
      while ((m = re.exec(t))) local.push(m[2])
      if (local.length) hits.set(p.replace(/\\/g, '/'), [...new Set(local)])
    }
  }
}
roots.forEach(walk)
let n = 0
for (const [p, arr] of hits) {
  console.log('\n## ' + p)
  for (const s of arr) {
    console.log(' - ' + JSON.stringify(s))
    n++
  }
}
console.log('\nTOTAL_STRINGS', n, 'FILES', hits.size)
