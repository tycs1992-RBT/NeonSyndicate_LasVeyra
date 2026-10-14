// Art-directed sky: saturated blue zenith, hazy horizon, sun glow and disk. Values are scene-linear (HDR).
import * as THREE from 'three';

const vert = /* glsl */`
  varying vec3 vDir;
  void main() {
    vDir = position;
    vec4 p = modelViewMatrix * vec4(position, 1.0);
    gl_Position = projectionMatrix * p;
    gl_Position.z = gl_Position.w; // always at the far plane
  }`;

const frag = /* glsl */`
  uniform vec3 sunDir;
  uniform vec3 zenith;
  uniform vec3 horizon;
  uniform vec3 ground;
  uniform vec3 sunColor;
  uniform float glowAmt;
  uniform float diskAmt;
  varying vec3 vDir;
  void main() {
    vec3 d = normalize(vDir);
    float h = d.y;
    float t = pow(clamp(h, 0.0, 1.0), 0.42);
    vec3 col = mix(horizon, zenith, t);
    col = mix(col, ground, smoothstep(0.0, -0.12, h));
    float c = dot(d, sunDir);
    float cp = max(c, 0.0);
    col += sunColor * (pow(cp, 6.0) * 0.18 + pow(cp, 48.0) * 0.55 + pow(cp, 400.0) * 1.6) * glowAmt;
    // Warm band along the horizon on the sun's side.
    col += sunColor * 0.22 * pow(1.0 - clamp(abs(h), 0.0, 1.0), 8.0) * (c * 0.5 + 0.5) * glowAmt;
    col += sunColor * smoothstep(0.99955, 0.99975, c) * diskAmt;
    gl_FragColor = vec4(col, 1.0);
    #include <tonemapping_fragment>
    #include <colorspace_fragment>
  }`;

export function createSkyDome(radius = 4000) {
  const uniforms = {
    sunDir: { value: new THREE.Vector3(0, 1, 0) },
    zenith: { value: new THREE.Color(0.2, 0.4, 0.85) },
    horizon: { value: new THREE.Color(0.8, 0.88, 0.96) },
    ground: { value: new THREE.Color(0.5, 0.5, 0.5) },
    sunColor: { value: new THREE.Color(1, 0.95, 0.85) },
    glowAmt: { value: 1 },
    diskAmt: { value: 40 },
  };
  const mat = new THREE.ShaderMaterial({ uniforms, vertexShader: vert, fragmentShader: frag, side: THREE.BackSide, depthWrite: false, fog: false });
  const mesh = new THREE.Mesh(new THREE.SphereGeometry(radius, 48, 24), mat);
  mesh.frustumCulled = false;
  mesh.renderOrder = -10;
  return { mesh, uniforms };
}
