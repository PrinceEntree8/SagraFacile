import { defineConfig } from '@playwright/test';

// Perf-only Playwright config: Chromium headless, no retries, JSONL vitals output.
// Target server: http://localhost:5000 (override with BASE_URL env var).
// Run: cd tests/browser && npm ci && npx playwright install chromium && npm run perf:vitals
export default defineConfig({
  testDir: '.',
  testMatch: 'perf-vitals.spec.ts',
  fullyParallel: true,
  retries: 0,
  // 5-10 concurrent headless contexts looping the public pages.
  workers: Number(process.env.VITALS_WORKERS ?? 6),
  timeout: 60_000,
  use: {
    baseURL: process.env.BASE_URL ?? 'http://localhost:5000',
    browserName: 'chromium',
    headless: true,
  },
  reporter: [['list']],
});
