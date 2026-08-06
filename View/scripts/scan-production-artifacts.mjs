import fs from 'node:fs'
import path from 'node:path'

const root = path.resolve('dist')
if (!fs.existsSync(root)) {
  console.error('Artifact scan failed: dist does not exist. Run the production build first.')
  process.exit(1)
}

const forbiddenPaths = [/playwright-report/i, /test-results/i, /__snapshots__/i, /[/\\](e2e|uat|fixtures)[/\\]/i]
const forbiddenContent = [
  { name: 'Playwright runtime/test source', pattern: /@playwright\/test|playwright-report|toHaveScreenshot\s*\(/i },
  { name: 'visual/E2E mock marker', pattern: /__VSHIELD_VISUAL_MOCK__|mock[-_ ]session|UAT_[A-Z_]+PASSWORD/i },
  { name: 'private key', pattern: /-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----/ },
  { name: 'embedded bearer token', pattern: /Bearer\s+eyJ[A-Za-z0-9_-]{8,}\.[A-Za-z0-9_-]{8,}/ },
]
const textExtensions = new Set(['.js', '.css', '.html', '.json', '.map', '.txt', '.svg'])
const failures = []
let filesScanned = 0

function walk(directory) {
  for (const entry of fs.readdirSync(directory, { withFileTypes: true })) {
    const file = path.join(directory, entry.name)
    const relative = path.relative(root, file)
    if (forbiddenPaths.some(pattern => pattern.test(relative))) failures.push(`${relative}: forbidden test artifact path`)
    if (entry.isDirectory()) walk(file)
    else {
      filesScanned += 1
      if (!textExtensions.has(path.extname(entry.name).toLowerCase())) continue
      const content = fs.readFileSync(file, 'utf8')
      for (const check of forbiddenContent) if (check.pattern.test(content)) failures.push(`${relative}: ${check.name}`)
    }
  }
}

walk(root)
if (failures.length) {
  console.error(`Production artifact scan failed (${failures.length} finding(s)):\n- ${failures.join('\n- ')}`)
  process.exit(1)
}
console.log(`Production artifact scan passed: ${filesScanned} files; no snapshots, E2E/UAT fixtures, mock sessions or credential signatures found.`)
