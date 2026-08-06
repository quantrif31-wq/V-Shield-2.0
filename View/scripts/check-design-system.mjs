import { readFileSync, readdirSync, statSync } from 'node:fs'
import { extname, join, relative } from 'node:path'

const root = new URL('../src', import.meta.url).pathname.replace(/^\/(.:)/, '$1')
const strictRoots = [join(root, 'components', 'ui')]
const migratedModules = [
  join(root, 'pages', 'Employees.vue'),
  join(root, 'pages', 'PreRegistration.vue'),
  join(root, 'pages', 'Vehicles.vue'),
  join(root, 'pages', 'AccessLogs.vue'),
  join(root, 'pages', 'DeviceManagement.vue'),
  join(root, 'pages', 'WatchlistQueue.vue'),
  join(root, 'pages', 'AiReviewQueue.vue'),
  join(root, 'pages', 'RedactionQueue.vue'),
  join(root, 'pages', 'OperationsDashboard.vue'),
]
const allowedTokenFile = join(root, 'styles', 'tokens.css')
const violations = []
const legacy = { hex: 0, shadow: 0, radius: 0, tinyType: 0 }

function walk(dir) {
  return readdirSync(dir).flatMap((name) => {
    const path = join(dir, name)
    return statSync(path).isDirectory() ? walk(path) : [path]
  })
}

const files = walk(root).filter((file) => ['.vue', '.css'].includes(extname(file)))
for (const file of files) {
  if (file === allowedTokenFile) continue
  const source = readFileSync(file, 'utf8')
  const counts = {
    hex: (source.match(/#[0-9a-fA-F]{3,8}\b/g) || []).length,
    shadow: (source.match(/box-shadow\s*:/g) || []).length,
    radius: (source.match(/border-radius\s*:/g) || []).length,
    tinyType: (source.match(/font-size\s*:\s*(?:10|11)px/g) || []).length,
  }
  Object.keys(legacy).forEach((key) => { legacy[key] += counts[key] })

  if (strictRoots.some((dir) => file.startsWith(dir)) || migratedModules.includes(file)) {
    if (counts.hex) violations.push(`${relative(root, file)}: dùng ${counts.hex} màu hex trực tiếp`)
    if (counts.tinyType) violations.push(`${relative(root, file)}: dùng font-size nhỏ hơn 12px`)
  }
  if (migratedModules.includes(file)) {
    const localShadows = [...source.matchAll(/box-shadow\s*:\s*([^;]+)/g)].filter((match) => !match[1].trim().startsWith('var(')).length
    const localRadii = [...source.matchAll(/border-radius\s*:\s*([^;]+)/g)].filter((match) => !match[1].trim().startsWith('var(')).length
    if (localShadows) violations.push(`${relative(root, file)}: dùng ${localShadows} box-shadow không qua token`)
    if (localRadii) violations.push(`${relative(root, file)}: dùng ${localRadii} border-radius không qua token`)
  }
}

console.log(`Legacy baseline — hex: ${legacy.hex}, shadow: ${legacy.shadow}, radius: ${legacy.radius}, tiny type: ${legacy.tinyType}`)
if (violations.length) {
  console.error('Vi phạm trong shared UI hoặc module đã migrate:\n' + violations.map((item) => `- ${item}`).join('\n'))
  process.exitCode = 1
} else {
  console.log('Shared UI và module đã migrate tuân thủ token, màu và cỡ chữ tối thiểu.')
}
