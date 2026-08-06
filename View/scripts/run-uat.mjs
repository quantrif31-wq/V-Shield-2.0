import { spawnSync } from 'node:child_process'

const smokeOnly = process.argv.includes('--smoke')
const run = (command, args) => spawnSync(command, args, {
  cwd: process.cwd(), env: process.env, stdio: 'inherit', shell: process.platform === 'win32',
}).status ?? 1

const preflightArgs = ['scripts/validate-uat-env.mjs', ...(smokeOnly ? ['--smoke'] : [])]
const preflightExit = run('node', preflightArgs)
let testExit = 1
if (preflightExit === 0) {
  const testArgs = ['playwright', 'test', '--config=playwright.uat.config.js']
  if (smokeOnly) testArgs.push('uat/smoke.spec.js')
  testExit = run('npx', testArgs)
}
const scanExit = run('node', ['scripts/scan-sensitive-artifacts.mjs'])
process.exit(preflightExit || testExit || scanExit)
