// Bundles the game into one self-contained HTML file (works offline from a double-click).
import * as esbuild from 'esbuild';
import fs from 'node:fs';

const minify = !process.argv.includes('--dev');
const result = await esbuild.build({
  entryPoints: ['src/main.js'],
  bundle: true,
  format: 'iife',
  target: ['chrome100', 'safari15', 'firefox100'],
  minify,
  write: false,
  legalComments: 'none',
  logLevel: 'warning',
  alias: { 'three/addons': './node_modules/three/examples/jsm' },
});
const js = result.outputFiles[0].text.replace(/<\/script>/g, '<\\/script>');
const html = fs.readFileSync('index.html', 'utf8').replace('<!--GAME_SCRIPT-->', () => `<script>${js}</script>`);
fs.mkdirSync('dist', { recursive: true });
fs.writeFileSync('dist/NeonSyndicate3D.html', html);
console.log(`dist/NeonSyndicate3D.html ${(html.length / 1024 / 1024).toFixed(2)} MB`);
// Page fragment for hosts that wrap content in their own document (title and style first, then the body).
const head = html.slice(html.indexOf('<head>') + 6, html.indexOf('</head>'));
const body = html.slice(html.indexOf('<body>') + 6, html.lastIndexOf('</body>'));
const title = (head.match(/<title>[\s\S]*?<\/title>/) || [''])[0];
const styles = (head.match(/<style>[\s\S]*?<\/style>/g) || []).join('\n');
const fragment = `${title}\n${styles}\n${body}`;
fs.writeFileSync('dist/NeonSyndicate3D.fragment.html', fragment);
console.log(`dist/NeonSyndicate3D.fragment.html ${(fragment.length / 1024 / 1024).toFixed(2)} MB`);
