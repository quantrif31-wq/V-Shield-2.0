import fs from 'node:fs'
import { expect, test } from '@playwright/test'
import { loginViaUi } from './helpers/auth.js'
import { uatEnvironment } from './helpers/environment.js'

const plan = uatEnvironment.performancePlanJson
  ? JSON.parse(uatEnvironment.performancePlanJson)
  : uatEnvironment.performancePlanPath && fs.existsSync(uatEnvironment.performancePlanPath)
    ? JSON.parse(fs.readFileSync(uatEnvironment.performancePlanPath, 'utf8'))
    : { iterations: 20, routes: [] }

const percentile = (values, ratio) => {
  if (!values.length) return null
  const sorted = [...values].sort((a, b) => a - b)
  return Math.round(sorted[Math.min(sorted.length - 1, Math.ceil(sorted.length * ratio) - 1)] * 100) / 100
}
const summary = values => ({ count: values.length, p50: percentile(values, .5), p75: percentile(values, .75), p95: percentile(values, .95) })

test('controlled UAT performance sample reports p50/p75/p95', async ({ page }, testInfo) => {
  test.setTimeout(Math.max(180_000, plan.iterations * plan.routes.length * 15_000))
  await page.addInitScript(() => {
    window.__vshieldUatMetrics = { lcp: 0, cls: 0, inp: 0, app: [] }
    try { new PerformanceObserver(list => { window.__vshieldUatMetrics.lcp = list.getEntries().at(-1)?.startTime || 0 }).observe({ type: 'largest-contentful-paint', buffered: true }) } catch {}
    try { new PerformanceObserver(list => { for (const entry of list.getEntries()) if (!entry.hadRecentInput) window.__vshieldUatMetrics.cls += entry.value }).observe({ type: 'layout-shift', buffered: true }) } catch {}
    try { new PerformanceObserver(list => { for (const entry of list.getEntries()) window.__vshieldUatMetrics.inp = Math.max(window.__vshieldUatMetrics.inp, entry.duration || 0) }).observe({ type: 'event', buffered: true, durationThreshold: 16 }) } catch {}
    window.addEventListener('vshield:observability', event => {
      if (event.detail?.name === 'performance_metric') window.__vshieldUatMetrics.app.push(event.detail.metadata)
    })
  })
  await loginViaUi(page, 'Admin')
  const samples = { lcp: [], inp: [], cls: [], ttfb: [], routeLoad: [], dynamicImport: [], mapInitialization: [], cameraInitialization: [] }

  for (const route of plan.routes) {
    for (let iteration = 0; iteration < plan.iterations; iteration += 1) {
      await page.goto(route.path, { waitUntil: 'domcontentloaded' })
      const target = page.locator(route.interactionSelector).first()
      await expect(target, `${route.path} safe interaction target`).toBeVisible()
      if (route.interactionType === 'click') await target.click()
      else await target.focus()
      await page.waitForTimeout(500)
      const metrics = await page.evaluate(() => {
        const navigation = performance.getEntriesByType('navigation')[0]
        const scripts = performance.getEntriesByType('resource').filter(entry => entry.initiatorType === 'script' && entry.name.includes('/assets/'))
        return {
          web: window.__vshieldUatMetrics,
          ttfb: navigation ? navigation.responseStart - navigation.requestStart : 0,
          routeLoad: navigation?.duration || 0,
          dynamicImport: scripts.length ? Math.max(...scripts.map(entry => entry.duration)) : 0,
        }
      })
      if (metrics.web.lcp) samples.lcp.push(metrics.web.lcp)
      if (metrics.web.inp) samples.inp.push(metrics.web.inp)
      samples.cls.push(metrics.web.cls)
      samples.ttfb.push(metrics.ttfb)
      samples.routeLoad.push(metrics.routeLoad)
      samples.dynamicImport.push(metrics.dynamicImport)
      for (const metric of metrics.web.app) {
        if (metric.metric === plan.map?.metric) samples.mapInitialization.push(metric.value)
        if (metric.metric === plan.camera?.metric) samples.cameraInitialization.push(metric.value)
      }
    }
  }

  const report = {
    generatedAt: new Date().toISOString(),
    environment: 'UAT',
    networkProfile: uatEnvironment.networkProfile,
    iterations: plan.iterations,
    routes: plan.routes.map(route => route.path),
    metrics: Object.fromEntries(Object.entries(samples).map(([name, values]) => [name, summary(values)])),
  }
  await testInfo.attach('performance-summary.json', { body: Buffer.from(JSON.stringify(report, null, 2)), contentType: 'application/json' })
  expect(report.metrics.lcp.count).toBeGreaterThanOrEqual(plan.iterations)
  expect(report.metrics.inp.count, 'INP requires an approved real interaction sample').toBeGreaterThanOrEqual(plan.iterations)
  expect(report.metrics.ttfb.count).toBe(plan.iterations * plan.routes.length)
  expect(report.metrics.mapInitialization.count, 'Map initialization must emit real UAT samples').toBeGreaterThanOrEqual(plan.iterations)
  expect(report.metrics.cameraInitialization.count, 'Camera initialization must emit real UAT samples').toBeGreaterThanOrEqual(plan.iterations)
})
