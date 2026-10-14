// Graphics presets. "auto" picks one from the GPU name.
export const PRESETS = {
  low: { name: 'Low', pixelRatio: 0.75, shadows: 0, shadowRange: 60, post: false, ao: false, aa: 'none', msaa: false, viewDistance: 650, texScale: 0.5, cars: 16, peds: 18, exposure: 1.05 },
  medium: { name: 'Medium', pixelRatio: 1, shadows: 2048, shadowRange: 80, post: true, ao: false, aa: 'fxaa', msaa: false, viewDistance: 900, texScale: 0.75, cars: 22, peds: 26, exposure: 1 },
  high: { name: 'High', pixelRatio: 1.25, shadows: 2048, shadowRange: 95, post: true, ao: false, aa: 'smaa', msaa: false, viewDistance: 1200, texScale: 1, cars: 28, peds: 34, exposure: 1 },
  ultra: { name: 'Ultra', pixelRatio: 2, shadows: 4096, shadowRange: 120, post: true, ao: true, aa: 'smaa', msaa: true, viewDistance: 1600, texScale: 1, cars: 34, peds: 42, exposure: 1 },
};

export function detectPreset(renderer) {
  try {
    const gl = renderer.getContext();
    const ext = gl.getExtension('WEBGL_debug_renderer_info');
    const name = (ext ? gl.getParameter(ext.UNMASKED_RENDERER_WEBGL) : gl.getParameter(gl.RENDERER)) || '';
    const n = name.toLowerCase();
    if (n.includes('swiftshader') || n.includes('llvmpipe') || n.includes('software')) return 'low';
    if (n.includes('nvidia') || n.includes('geforce') || n.includes('rtx') || n.includes('radeon rx') || n.includes('radeon pro')) return 'ultra';
    if (n.includes('apple m') || n.includes('apple gpu')) return 'high';
    if (n.includes('intel') || n.includes('uhd') || n.includes('iris')) return 'medium';
    return 'high';
  } catch (e) {
    return 'medium';
  }
}
