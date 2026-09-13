import fs from 'node:fs'
import path from 'node:path'

const roots = process.argv.slice(2).length ? process.argv.slice(2) : ['src/views', 'src/components', 'src/extension']
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
const ranked = [...hits.entries()].map(([p, a]) => [p, a.length, a.slice(0, 10)]).sort((a, b) => b[1] - a[1])
console.log('files', hits.size)
for (const [p, n, s] of ranked.slice(0, 40)) {
  console.log('\n##', n, p)
  for (const x of s) console.log(' -', JSON.stringify(x))
}
