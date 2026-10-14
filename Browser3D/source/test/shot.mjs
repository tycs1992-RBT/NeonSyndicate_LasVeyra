// Headless screenshot tool: node test/shot.mjs <out.png> [js to run after load] [query]
import { chromium } from 'playwright';
import path from 'node:path';

const out = process.argv[2] || 'test/out.png';
const script = process.argv[3] || '';
const query = process.argv[4] || 'quality=high';
const width = Number(process.env.W || 1280), height = Number(process.env.H || 720);
const browser = await chromium.launch({ args: ['--use-angle=swiftshader', '--enable-unsafe-swiftshader', '--ignore-gpu-blocklist', '--enable-webgl'] });
const page = await browser.newPage({ viewport: { width, height } });
const logs = [];
page.on('console', (m) => { if (m.type() === 'error' || m.type() === 'warning') logs.push(`[${m.type()}] ${m.text()}`); });
page.on('pageerror', (e) => logs.push(`[pageerror] ${e.message}`));
const file = 'file://' + path.resolve('dist/NeonSyndicate3D.html') + '?' + query;
const t0 = Date.now();
await page.goto(file);
try {
  await page.waitForFunction(() => window.__ns && window.__ns.ready, null, { timeout: 240000 });
} catch (e) {
  console.log('TIMEOUT waiting for ready');
  const err = await page.evaluate(() => document.getElementById('loadErr')?.textContent + ' | ' + document.getElementById('loadLabel')?.textContent);
  console.log(err);
}
console.log(`ready in ${((Date.now() - t0) / 1000).toFixed(1)}s`);
if (script) {
  const r = await page.evaluate(script);
  if (r !== undefined) console.log('script:', typeof r === 'string' ? r : JSON.stringify(r));
}
await page.waitForTimeout(Number(process.env.WAIT || 2500));
await page.screenshot({ path: out });
const info = await page.evaluate(() => {
  const r = window.__ns && window.__ns.renderer;
  return r ? { calls: r.info.render.calls, tris: r.info.render.triangles, geos: r.info.memory.geometries, tex: r.info.memory.textures } : null;
});
console.log('render info', JSON.stringify(info));
for (const l of logs.slice(0, 30)) console.log(l);
await browser.close();
