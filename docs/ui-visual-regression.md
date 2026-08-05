# V-Shield UI visual regression

## Canonical environment

Linux is the canonical environment for release visual regression. The RC2
baseline was rendered with the same operating-system family and browser bundle
as the GitHub Actions release job:

```text
OS: Ubuntu Noble (Playwright container)
Container: mcr.microsoft.com/playwright:v1.62.1-noble
Container digest: sha256:dcc5531e97840b9b5e794f2814476b21571c5124a3fca2267d73041f56e7580e
Node: 22.23.1
npm: 10.9.8
Playwright: 1.62.1
Chromium: 151.0.7922.34
Locale: vi-VN
Timezone: Asia/Ho_Chi_Minh
Workers: 1
```

The Playwright configuration fixes the five viewport projects, light color
scheme, reduced motion, locale and timezone. Screenshot assertions disable CSS
animations and hide the caret. Test fixtures provide deterministic API data and
fixed timestamps for the RC-scope routes.

## Baseline scope and naming

The RC visual suite contains 24 cases across five projects, for 120 Linux
snapshots:

```text
<case>-desktop-1920-linux.png
<case>-desktop-1440-linux.png
<case>-tablet-768-linux.png
<case>-tablet-1024-linux.png
<case>-mobile-390-linux.png
```

Windows snapshots remain OS-specific developer references with the equivalent
`-win32.png` suffix. They must not be renamed or copied to create Linux
baselines. The five legacy `dashboard-*-win32.png` files are outside RC2 scope;
`/operations-dashboard` remains covered.

## Verification commands

From `View` in the canonical Linux environment:

```bash
npm ci
npm run design:check
npm test
npm run test:e2e
npm run test:visual
npm run check
npm run build
npm run security:artifacts:all
npm audit
```

The release workflow only compares snapshots. It never updates them.

## Intentional baseline update

1. Start from the approved release commit in a clean detached worktree.
2. Use the pinned Playwright Linux container and Node/npm versions above.
3. Run `npm run test:visual:update` inside that container.
4. Confirm exactly 120 Linux snapshots, 24 for each viewport, with no missing
   or unexpected file.
5. Run `npm run test:visual` again without the update flag.
6. Review side-by-side contact sheets and full-resolution images for layout,
   content, responsive state, modal clipping, theme and data stability.
7. Stage only reviewed `*-linux.png` files. Never stage reports, traces, videos,
   screenshots from failures or storage state.
8. Run the complete release gate and sensitive-artifact scan before commit.

Do not update canonical baselines from Windows, auto-update snapshots in the
release workflow, weaken diff thresholds, catch snapshot failures, or accept a
new baseline solely because the update command exited successfully.
