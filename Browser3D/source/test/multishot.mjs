// Several screenshots in one browser session: node test/multishot.mjs steps.json [query]
// steps.json: [{ "out": "test/a.png", "script": "...js...", "wait": 2000 }, ...]
import { chromium } from 'playwright';
import path from 'node:path';
import fs from 'node:fs';

const steps = JSON.parse(fs.readFileSync(process.argv[2], 'utf8'));
const query = process.argv[3] || 'quality=high';
const width = Number(process.env.W || 1280), height = Number(process.env.H || 720);
const browser = await chromium.launch({ args: ['--use-angle=swiftshader', '--enable-unsafe-swiftshader', '--ignore-gpu-blocklist', '--enable-webgl'] });
const page = await browser.newPage({ viewport: { width, height } });
const logs = [];
page.on('console', (m) => { if (m.type() === 'error' || m.type() === 'warning' || m.text().startsWith('[ns]')) logs.push(`[${m.type()}] ${m.text()}`); });
page.on('pageerror', (e) => logs.push(`[pageerror] ${e.message}\n${e.stack || ''}`));
const t0 = Date.now();
await page.goto('file://' + path.resolve(process.env.PAGE || 'dist/NeonSyndicate3D.html') + '?' + query);
try {
  await page.waitForFunction(() => window.__ns && window.__ns.ready, null, { timeout: 300000 });
} catch (e) {
  console.log('TIMEOUT', await page.evaluate(() => document.getElementById('loadErr')?.textContent + ' | ' + document.getElementById('loadLabel')?.textContent));
}
console.log(`ready after ${((Date.now() - t0) / 1000).toFixed(1)}s`);
for (const s of steps) {
  if (s.script) {
    try {
      const r = await page.evaluate(s.script);
      if (r !== undefined && r !== null) console.log(`${s.out}:`, typeof r === 'string' ? r : JSON.stringify(r));
    } catch (e) { console.log('script error', e.message); }
  }
  await page.waitForTimeout(s.wait || 2000);
  if (s.out) await page.screenshot({ path: s.out, timeout: 180000 });
}
for (const l of logs.slice(0, 40)) console.log(l);
await browser.close();
