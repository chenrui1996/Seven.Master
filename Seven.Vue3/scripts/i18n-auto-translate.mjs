/**
 * 自动翻译脚本：以 zh-CN.json 为源，补齐目标语言缺失的键
 *
 * 用法：
 *   node scripts/i18n-auto-translate.mjs --target en-US
 *   node scripts/i18n-auto-translate.mjs --target ja-JP
 *
 * 环境变量（任选其一，用于机器翻译）：
 *   DEEPL_AUTH_KEY   - DeepL API（推荐，质量高）
 *   GOOGLE_API_KEY   - Google Cloud Translation API
 *   LIBRETRANSLATE_URL - 自建/公共 LibreTranslate 端点（默认 https://libretranslate.com）
 *
 * 无 API Key 时：仅输出缺失键清单，不覆盖已有翻译
 */
import fs from 'node:fs'
import path from 'node:path'
import { fileURLToPath } from 'node:url'

const __dirname = path.dirname(fileURLToPath(import.meta.url))
const LANG_DIR = path.resolve(__dirname, '../src/locales/lang')
const SOURCE = 'zh-CN'

const args = process.argv.slice(2)
const targetIdx = args.indexOf('--target')
const target = targetIdx >= 0 ? args[targetIdx + 1] : 'en-US'

if (!target || target === SOURCE) {
  console.error('请指定 --target，例如：--target en-US')
  process.exit(1)
}

function flatten(obj, prefix = '') {
  const out = {}
  for (const [k, v] of Object.entries(obj)) {
    const key = prefix ? `${prefix}.${k}` : k
    if (v && typeof v === 'object' && !Array.isArray(v)) {
      Object.assign(out, flatten(v, key))
    } else {
      out[key] = String(v)
    }
  }
  return out
}

function unflatten(flat) {
  const out = {}
  for (const [key, value] of Object.entries(flat)) {
    const parts = key.split('.')
    let cur = out
    for (let i = 0; i < parts.length - 1; i++) {
      cur[parts[i]] = cur[parts[i]] ?? {}
      cur = cur[parts[i]]
    }
    cur[parts[parts.length - 1]] = value
  }
  return out
}

function readJson(file) {
  return JSON.parse(fs.readFileSync(file, 'utf8'))
}

function writeJson(file, data) {
  fs.writeFileSync(file, JSON.stringify(data, null, 2) + '\n', 'utf8')
}

const langMap = {
  'en-US': 'en',
  'ja-JP': 'ja',
  'zh-CN': 'zh',
}

async function translateText(text, toLang) {
  if (process.env.DEEPL_AUTH_KEY) {
    const params = new URLSearchParams({
      auth_key: process.env.DEEPL_AUTH_KEY,
      text,
      target_lang: toLang === 'en' ? 'EN' : toLang.toUpperCase(),
      source_lang: 'ZH',
    })
    const res = await fetch('https://api-free.deepl.com/v2/translate', {
      method: 'POST',
      headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
      body: params,
    })
    if (!res.ok) throw new Error(`DeepL error: ${res.status}`)
    const data = await res.json()
    return data.translations[0].text
  }

  if (process.env.GOOGLE_API_KEY) {
    const url = new URL('https://translation.googleapis.com/language/translate/v2')
    url.searchParams.set('key', process.env.GOOGLE_API_KEY)
    const res = await fetch(url, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ q: text, source: 'zh-CN', target: toLang, format: 'text' }),
    })
    if (!res.ok) throw new Error(`Google Translate error: ${res.status}`)
    const data = await res.json()
    return data.data.translations[0].translatedText
  }

  const base = process.env.LIBRETRANSLATE_URL || 'https://libretranslate.com'
  const res = await fetch(`${base}/translate`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ q: text, source: 'zh', target: toLang, format: 'text' }),
  })
  if (!res.ok) throw new Error(`LibreTranslate error: ${res.status}`)
  const data = await res.json()
  return data.translatedText
}

async function main() {
  const sourcePath = path.join(LANG_DIR, `${SOURCE}.json`)
  const targetPath = path.join(LANG_DIR, `${target}.json`)

  const sourceFlat = flatten(readJson(sourcePath))
  const targetFlat = fs.existsSync(targetPath) ? flatten(readJson(targetPath)) : {}

  const missing = Object.keys(sourceFlat).filter((k) => !(k in targetFlat))
  if (missing.length === 0) {
    console.log(`✅ ${target} 已包含全部 ${Object.keys(sourceFlat).length} 个键`)
    return
  }

  console.log(`📋 ${target} 缺失 ${missing.length} 个键`)

  const hasTranslator =
    process.env.DEEPL_AUTH_KEY || process.env.GOOGLE_API_KEY || process.env.LIBRETRANSLATE_URL

  if (!hasTranslator) {
    console.log('\n未配置翻译 API，缺失键列表：')
    missing.forEach((k) => console.log(`  - ${k}: ${sourceFlat[k]}`))
    console.log('\n配置 DEEPL_AUTH_KEY / GOOGLE_API_KEY / LIBRETRANSLATE_URL 后重新运行可自动补齐。')
    return
  }

  const toLang = langMap[target] || 'en'
  const merged = { ...targetFlat }

  for (const key of missing) {
    const text = sourceFlat[key]
    try {
      merged[key] = await translateText(text, toLang)
      console.log(`  ✓ ${key}`)
      await new Promise((r) => setTimeout(r, 200))
    } catch (err) {
      console.warn(`  ✗ ${key}: ${err.message}`)
      merged[key] = text
    }
  }

  writeJson(targetPath, unflatten(merged))
  console.log(`\n✅ 已写入 ${targetPath}`)
}

main().catch((err) => {
  console.error(err)
  process.exit(1)
})
