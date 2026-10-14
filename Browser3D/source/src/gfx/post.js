// Post-processing chain: ambient occlusion, bloom, filmic tone mapping and anti-aliasing.
import * as THREE from 'three';
import { EffectComposer } from 'three/addons/postprocessing/EffectComposer.js';
import { RenderPass } from 'three/addons/postprocessing/RenderPass.js';
import { UnrealBloomPass } from 'three/addons/postprocessing/UnrealBloomPass.js';
import { OutputPass } from 'three/addons/postprocessing/OutputPass.js';
import { ShaderPass } from 'three/addons/postprocessing/ShaderPass.js';
import { SMAAPass } from 'three/addons/postprocessing/SMAAPass.js';
import { GTAOPass } from 'three/addons/postprocessing/GTAOPass.js';
import { FXAAShader } from 'three/addons/shaders/FXAAShader.js';

// Grade after tone mapping: warm sunlit highlights, cool shade, a gentle contrast curve and a touch of vignette.
const GradeShader = {
  uniforms: {
    tDiffuse: { value: null },
    vignette: { value: 0.18 },
    saturation: { value: 1.13 },
    warmth: { value: 0.024 },
    contrast: { value: 0.14 },
    damage: { value: 0 },
    inner: { value: 0 },
    flash: { value: 0 },
    time: { value: 0 },
  },
  vertexShader: /* glsl */`
    varying vec2 vUv;
    void main() { vUv = uv; gl_Position = projectionMatrix * modelViewMatrix * vec4(position, 1.0); }`,
  fragmentShader: /* glsl */`
    uniform sampler2D tDiffuse;
    uniform float vignette;
    uniform float saturation;
    uniform float warmth;
    uniform float contrast;
    uniform float damage;
    uniform float inner;
    uniform float flash;
    uniform float time;
    varying vec2 vUv;
    float hash(vec2 p) { return fract(sin(dot(p, vec2(12.9898, 78.233))) * 43758.5453); }
    void main() {
      vec2 dc = vUv - 0.5;
      vec4 c = texture2D(tDiffuse, vUv);
      // Inner World: colour fringing toward the edges.
      if (inner > 0.001) {
        vec2 off = dc * 0.006 * inner;
        c.r = texture2D(tDiffuse, vUv + off).r;
        c.b = texture2D(tDiffuse, vUv - off).b;
      }
      float l = dot(c.rgb, vec3(0.2126, 0.7152, 0.0722));
      c.rgb = mix(vec3(l), c.rgb, saturation);
      // Gentle S-curve: a little more punch in sunlit highlights and shade.
      vec3 sc = clamp(c.rgb, 0.0, 1.0);
      c.rgb = mix(c.rgb, sc * sc * (3.0 - 2.0 * sc), contrast);
      c.rgb += vec3(warmth, warmth * 0.4, -warmth * 0.6) * smoothstep(0.35, 1.0, l);
      c.rgb += vec3(-0.01, 0.0, 0.015) * (1.0 - smoothstep(0.0, 0.35, l));
      // Inner World grade: drained toward violet and crimson, heavier vignette, film grain.
      if (inner > 0.001) {
        float li = dot(c.rgb, vec3(0.2126, 0.7152, 0.0722));
        vec3 tone = vec3(li * 1.15, li * 0.62, li * 0.9) + vec3(0.03, 0.0, 0.025);
        c.rgb = mix(c.rgb, mix(c.rgb, tone, 0.45), inner);
        c.rgb += (hash(vUv * 917.0 + time) - 0.5) * 0.05 * inner;
      }
      vec2 d = vUv - 0.5;
      float v = 1.0 - dot(d, d) * (vignette + inner * 0.7) * 2.2;
      c.rgb *= clamp(v, 0.0, 1.0);
      // Third Eye flash: the image inverts for an instant as the worlds swap.
      if (flash > 0.001) {
        float f = flash * flash;
        c.rgb = mix(c.rgb, vec3(1.0) - c.rgb, f * 0.65);
        c.rgb += vec3(0.5, 0.2, 0.55) * f * 0.5;
      }
      // Low health: desaturate and redden the edges.
      float edge = smoothstep(0.15, 0.75, length(d) * 1.4);
      c.rgb = mix(c.rgb, mix(vec3(l) * vec3(1.0, 0.75, 0.75), vec3(0.5, 0.0, 0.0), edge * 0.6), damage * 0.6);
      gl_FragColor = c;
    }`,
};

export class Post {
  constructor(renderer, scene, camera, quality) {
    this.renderer = renderer;
    this.quality = quality;
    this.enabled = quality.post;
    const size = renderer.getDrawingBufferSize(new THREE.Vector2());
    if (!this.enabled) return;
    const rt = new THREE.WebGLRenderTarget(size.x, size.y, { type: THREE.HalfFloatType, samples: quality.msaa ? 4 : 0 });
    this.composer = new EffectComposer(renderer, rt);
    this.renderPass = new RenderPass(scene, camera);
    this.composer.addPass(this.renderPass);
    if (quality.ao) {
      this.gtao = new GTAOPass(scene, camera, size.x, size.y);
      this.gtao.output = GTAOPass.OUTPUT.Default;
      this.gtao.blendIntensity = 0.9;
      this.gtao.updateGtaoMaterial({ radius: 1.4, distanceExponent: 1.5, thickness: 2.5, scale: 1.0, samples: 12, screenSpaceRadius: false });
      this.gtao.updatePdMaterial({ lumaPhi: 10, depthPhi: 2, normalPhi: 3, radius: 6, rings: 2, samples: 12 });
      // The AO normal pass ignores alpha cutouts, so leaves, weeds and chain-link would occlude as solid cards.
      const baseHide = this.gtao.overrideVisibility.bind(this.gtao);
      this.gtao.overrideVisibility = function () {
        baseHide();
        this.scene.traverse((o) => { if (o.material && !Array.isArray(o.material) && o.material.alphaTest > 0) o.visible = false; });
      };
      this.composer.addPass(this.gtao);
    }
    this.bloom = new UnrealBloomPass(new THREE.Vector2(size.x, size.y), 0.25, 0.55, 0.92);
    this.composer.addPass(this.bloom);
    this.output = new OutputPass();
    this.composer.addPass(this.output);
    this.grade = new ShaderPass(GradeShader);
    this.composer.addPass(this.grade);
    if (quality.aa === 'smaa') {
      this.aa = new SMAAPass(size.x, size.y);
      this.composer.addPass(this.aa);
    } else if (quality.aa === 'fxaa') {
      this.aa = new ShaderPass(FXAAShader);
      this.aa.material.uniforms.resolution.value.set(1 / size.x, 1 / size.y);
      this.composer.addPass(this.aa);
    }
  }

  setNight(night) {
    if (!this.bloom) return;
    // Bloom works on scene-linear values before exposure: by day only very bright highlights glow.
    this.bloom.strength = 0.08 + night * 0.32;
    this.bloom.threshold = 3.5 - night * 2.1;
    this.bloom.radius = 0.45 + night * 0.15;
  }

  setDamage(k) {
    if (this.grade) this.grade.uniforms.damage.value = k;
  }

  setInner(k, flash = 0) {
    if (!this.grade) return;
    const u = this.grade.uniforms;
    u.inner.value = k;
    u.flash.value = flash * Math.sin(Math.min(1, flash) * Math.PI);
    u.time.value = (u.time.value + 0.37) % 100;
  }

  setPixelRatio(r) {
    if (!this.enabled) return;
    this.composer.setPixelRatio(r);
    this.resize(window.innerWidth, window.innerHeight);
  }

  resize(w, h) {
    if (!this.enabled) return;
    const size = this.renderer.getDrawingBufferSize(new THREE.Vector2());
    this.composer.setSize(w, h);
    if (this.aa && this.aa.material && this.aa.material.uniforms.resolution) this.aa.material.uniforms.resolution.value.set(1 / size.x, 1 / size.y);
  }

  render(scene, camera) {
    if (!this.enabled) {
      this.renderer.render(scene, camera);
      return;
    }
    this.renderPass.camera = camera;
    if (this.gtao) this.gtao.camera = camera;
    this.composer.render();
  }
}
