import { test } from '@playwright/test';
import * as fs from 'node:fs';
import * as path from 'node:path';
import { fileURLToPath } from 'node:url';

const here = path.dirname(fileURLToPath(import.meta.url));
const collectorPath = path.join(here, 'vitals-collector.js');
const collectorScript = fs.readFileSync(collectorPath, 'utf8');

// Pages under test with the selector that proves SSR content + live region rendered.
const PAGES = [
  { route: '/', readySelector: '.home-last-called-card' },
  { route: '/menu', readySelector: '.card' },
] as const;

const LOOPS = Number(process.env.VITALS_LOOPS ?? 10);
const SETTLE_MS = Number(process.env.VITALS_SETTLE_MS ?? 1500);
const OUT_FILE =
  process.env.VITALS_OUT ??
  path.join(here, '..', '..', 'reports', 'perf-vitals.jsonl');

interface VitalsSnapshot {
  fcp: number;
  lcp: number;
  cls: number;
  signalrReadyMs: number;
}

function appendJsonl(row: Record<string, unknown>) {
  fs.mkdirSync(path.dirname(OUT_FILE), { recursive: true });
  fs.appendFileSync(OUT_FILE, JSON.stringify(row) + '\n');
}

for (const page of PAGES) {
  test(`vitals ${page.route} x${LOOPS}`, async ({ browser }) => {
    // One headless context per worker; each loops over its page.
    const context = await browser.newContext();
    try {
      for (let i = 0; i < LOOPS; i++) {
        const p = await context.newPage();
        try {
          await p.addInitScript({ content: collectorScript });
          const navStart = Date.now();
          let status = 'ok';
          let statusCode: number | null = null;
          try {
            const resp = await p.goto(page.route, { waitUntil: 'domcontentloaded' });
            statusCode = resp?.status() ?? null;
            if (statusCode !== null && statusCode >= 400) status = `http-${statusCode}`;
            // Wait for SSR content / live region (also the SignalR-ready marker).
            await p.waitForSelector(page.readySelector, { timeout: 15_000 });
            await p.evaluate(() => (window as any).__markSignalrReady?.());
          } catch {
            status = 'navigation-or-selector-timeout';
          }
          // Let late LCP/CLS entries settle before snapshotting.
          await p.waitForTimeout(SETTLE_MS);
          const vitals = await p.evaluate(
            (): VitalsSnapshot =>
              (window as any).__vitalsSnapshot?.() ?? { fcp: -1, lcp: -1, cls: -1, signalrReadyMs: -1 },
          );
          appendJsonl({
            page: page.route,
            iteration: i,
            worker: Number(process.env.TEST_WORKER_INDEX ?? 0),
            ...vitals,
            status,
            statusCode,
            navStartEpoch: navStart,
          });
        } finally {
          await p.close();
        }
      }
    } finally {
      await context.close();
    }
  });
}
