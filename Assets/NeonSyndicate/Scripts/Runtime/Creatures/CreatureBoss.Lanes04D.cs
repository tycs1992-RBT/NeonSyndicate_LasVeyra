using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace NeonSyndicate
{
    /// <summary>
    /// The upgraded fights' telegraph shapes: floor lanes (a dim strip with a brighter bar that grows along it over the
    /// wind-up, then flashes when it bursts) and beams (a thin glowing bar between two points: the Queen's aim lines and
    /// beam flashes, the Djinn's decoy pillars). Both are pooled flat additive meshes with no colliders and one shared
    /// material, tinted per renderer with a property block (no material is made per lane, beam or frame).
    /// </summary>
    public sealed partial class CreatureBoss
    {
        private sealed class LaneFx
        {
            public GameObject Go;
            public Transform Base;
            public Transform Bar;
            public MeshRenderer BaseR;
            public MeshRenderer BarR;
            public MaterialPropertyBlock Mpb;
        }

        private sealed class Lane04DLive
        {
            public Lane04D Geo;
            public float Y;
            public LaneFx Fx;
            public float T;
            public float Delay;
            public float Fill;
            public float Damage;
            public DamageFlags Flags;
            public Color Color;
            public int Serial;
            public bool Burst;
            public float BurstT;
        }

        private sealed class BeamFx
        {
            public GameObject Go;
            public MeshRenderer R;
            public MaterialPropertyBlock Mpb;
        }

        private sealed class Beam04DLive
        {
            public BeamFx Fx;
            public Vector3 A;
            public Vector3 B;
            public float Width;
            public Color Color;
            public float Alpha;
            /// <summary>Seconds left of a flash fade (0: a steady line until released).</summary>
            public float Fade;
            public float FadeLife;
            public int Serial;
            public bool Done;
        }

        private static readonly int TintId04D = Shader.PropertyToID("_TintColor");
        private static readonly int BaseColorId04D = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId04D = Shader.PropertyToID("_Color");

        private static Transform _root04D;
        private static Material _glowMat04D;
        private static readonly List<LaneFx> _laneFree04D = new List<LaneFx>();
        private static readonly List<BeamFx> _beamFree04D = new List<BeamFx>();
        private static readonly List<GameObject> _shardFree04D = new List<GameObject>();

        private readonly List<Lane04DLive> _lanes04D = new List<Lane04DLive>();
        private readonly List<Beam04DLive> _beams04D = new List<Beam04DLive>();
        private readonly Lane04D[] _laneScratch04D = new Lane04D[8];
        private int _laneSoundFrame = -1;

        /// <summary>Called from BossProfileStatics (Game.ResetStatics): the pools and the shared material start empty.</summary>
        public static void ResetStatics04D()
        {
            _root04D = null;
            _glowMat04D = null;
            _laneFree04D.Clear();
            _beamFree04D.Clear();
            _shardFree04D.Clear();
        }

        private static Transform Root04D
        {
            get
            {
                if (_root04D == null) _root04D = new GameObject("Boss Telegraphs 04D").transform;
                return _root04D;
            }
        }

        private static Material GlowMat04D
        {
            get
            {
                if (_glowMat04D == null) _glowMat04D = Mats.Additive("Boss Telegraph 04D", Color.white, UIKit.White);
                return _glowMat04D;
            }
        }

        private static void Tint04D(MeshRenderer r, MaterialPropertyBlock mpb, Color c)
        {
            if (r == null) return;
            c.a = Mathf.Clamp01(c.a);
            mpb.Clear();
            mpb.SetColor(TintId04D, c);
            mpb.SetColor(BaseColorId04D, c);
            mpb.SetColor(ColorId04D, c);
            r.SetPropertyBlock(mpb);
        }

        private static MeshRenderer FlatQuad04D(string name, Transform parent)
        {
            GameObject q = Shapes.MeshObject(name, Shapes.QuadMesh, new[] { GlowMat04D }, parent, false);
            q.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            MeshRenderer r = q.GetComponent<MeshRenderer>();
            r.receiveShadows = false;
            r.shadowCastingMode = ShadowCastingMode.Off;
            return r;
        }

        // ================================================================== lanes

        private static LaneFx TakeLaneFx04D()
        {
            while (_laneFree04D.Count > 0)
            {
                LaneFx f = _laneFree04D[_laneFree04D.Count - 1];
                _laneFree04D.RemoveAt(_laneFree04D.Count - 1);
                if (f != null && f.Go != null)
                {
                    f.Go.SetActive(true);
                    return f;
                }
            }
            LaneFx n = new LaneFx();
            n.Go = new GameObject("Boss Lane");
            n.Go.transform.SetParent(Root04D, false);
            n.BaseR = FlatQuad04D("Base", n.Go.transform);
            n.BarR = FlatQuad04D("Bar", n.Go.transform);
            n.Base = n.BaseR.transform;
            n.Bar = n.BarR.transform;
            n.Mpb = new MaterialPropertyBlock();
            return n;
        }

        private static void FreeLaneFx04D(LaneFx f)
        {
            if (f == null || f.Go == null) return;
            f.Go.SetActive(false);
            if (!_laneFree04D.Contains(f)) _laneFree04D.Add(f);
        }

        /// <summary>
        /// Marks a lane on the floor that fills over fill seconds (after delay) and then bursts, hurting a player standing
        /// on it with a clear line from the lane's origin (damage 0: a harmless marker, such as the Queen's beam lanes,
        /// whose beam decides the hit). Null when the fight no longer owns new things.
        /// </summary>
        private Lane04DLive AddLane04D(Lane04D geo, float y, float delay, float fill, float damage, DamageFlags flags, Color color, int serial)
        {
            if (_ledger == null || !_ledger.Open(BossOwned04D.Lane)) return null;
            Lane04DLive l = new Lane04DLive();
            l.Geo = geo;
            l.Y = y;
            l.Delay = Mathf.Max(0f, delay);
            l.Fill = Mathf.Max(0.3f, fill);
            l.Damage = damage;
            l.Flags = flags;
            l.Color = color;
            l.Serial = serial;
            l.Fx = TakeLaneFx04D();
            PlaceLane04D(l, 0f, 0f);
            _lanes04D.Add(l);
            return l;
        }

        /// <summary>Puts a lane's strip on the floor: the dim base the whole length, the bright bar grown to fill.</summary>
        private static void PlaceLane04D(Lane04DLive l, float fill, float alpha)
        {
            LaneFx f = l.Fx;
            if (f == null || f.Go == null) return;
            Vector3 mid = l.Geo.Origin + l.Geo.Dir * (l.Geo.Length * 0.5f);
            mid.y = l.Y + 0.09f;
            f.Go.transform.SetPositionAndRotation(mid, Quaternion.Euler(0f, MathUtil.YawOf(l.Geo.Dir), 0f));
            float w = l.Geo.HalfWidth * 2f;
            float len = Mathf.Max(0.1f, l.Geo.Length);
            f.Base.localScale = new Vector3(w, len, 1f);
            float bl = Mathf.Max(0.02f, Mathf.Clamp01(fill)) * len;
            f.Bar.localScale = new Vector3(w * 0.55f, bl, 1f);
            f.Bar.localPosition = new Vector3(0f, 0.012f, -len * 0.5f + bl * 0.5f);
            Tint04D(f.BaseR, f.Mpb, l.Color * (0.32f * alpha));
            Tint04D(f.BarR, f.Mpb, l.Color * (0.75f * alpha));
        }

        private void UpdateLanes04D(float dt)
        {
            for (int i = _lanes04D.Count - 1; i >= 0; i--)
            {
                Lane04DLive l = _lanes04D[i];
                if (l.Burst)
                {
                    l.BurstT += dt;
                    float k = Mathf.Clamp01(l.BurstT / 0.35f);
                    if (l.Fx != null && l.Fx.Go != null)
                    {
                        Tint04D(l.Fx.BaseR, l.Fx.Mpb, l.Color * (1.4f * (1f - k)));
                        Tint04D(l.Fx.BarR, l.Fx.Mpb, new Color(1f, 0.95f, 0.85f, 1f) * (0.9f * (1f - k)));
                    }
                    if (k >= 1f) RemoveLaneAt04D(i);
                    continue;
                }
                l.T += dt;
                if (l.T < l.Delay)
                {
                    PlaceLane04D(l, 0f, 0f);
                    continue;
                }
                float fill = Mathf.Clamp01((l.T - l.Delay) / l.Fill);
                float pulse = fill > 0.75f ? 0.75f + 0.25f * Mathf.Sin(Time.time * 30f) : 1f;
                PlaceLane04D(l, fill, pulse);
                if (fill < 1f) continue;
                l.Burst = true;
                l.BurstT = 0f;
                LaneBurst04D(l);
            }
        }

        private void LaneBurst04D(Lane04DLive l)
        {
            Vector3 o = new Vector3(l.Geo.Origin.x, l.Y, l.Geo.Origin.z);
            if (Game.Fx != null)
            {
                for (int k = 1; k <= 3; k++)
                {
                    Vector3 at = o + l.Geo.Dir * (l.Geo.Length * k / 3.5f) + Vector3.up * 0.3f;
                    Game.Fx.CrystalBurst(at, l.Color, 6, 0.5f);
                    if (l.Damage > 0f) Game.Fx.RockBurst(at, Species.HitColor, 3, 0.4f);
                }
            }
            if (Game.Audio != null && l.Damage > 0f && Time.frameCount != _laneSoundFrame)
            {
                _laneSoundFrame = Time.frameCount;
                Game.Audio.PlayVar(SfxId.ExplosionSmall, o + l.Geo.Dir * (l.Geo.Length * 0.5f), 0.4f, 0.2f, 50f);
            }
            if (l.Damage <= 0f) return;
            PlayerController p = Game.Player;
            if (p == null || p.IsDead) return;
            Vector3 pp = p.InVehicle && p.CurrentVehicle != null ? p.CurrentVehicle.transform.position : p.transform.position;
            if (Mathf.Abs(pp.y - l.Y) > 3f || !BossLanes04D.InLane(l.Geo, pp)) return;
            // The rupture runs along the floor from its origin: a wall in between stops it.
            if (Physics.Linecast(o + Vector3.up * 1f, p.ChestPosition, Layers.WorldMask, QueryTriggerInteraction.Ignore)) return;
            HitPlayer04D(l.Damage, o, DamageType.Melee, l.Flags, 3f);
        }

        private void RemoveLaneAt04D(int i)
        {
            Lane04DLive l = _lanes04D[i];
            FreeLaneFx04D(l.Fx);
            l.Fx = null;
            _lanes04D.RemoveAt(i);
            if (_ledger != null) _ledger.Release(BossOwned04D.Lane);
        }

        /// <summary>One lane vanishes now (a beam broken before it fired).</summary>
        private void RemoveLane04D(Lane04DLive l)
        {
            if (l == null) return;
            int i = _lanes04D.IndexOf(l);
            if (i >= 0) RemoveLaneAt04D(i);
        }

        /// <summary>A move broken in its wind-up: its lanes that have not burst vanish harmlessly.</summary>
        private void ReleaseMoveLanes04D(int serial)
        {
            for (int i = _lanes04D.Count - 1; i >= 0; i--) if (_lanes04D[i].Serial == serial && !_lanes04D[i].Burst) RemoveLaneAt04D(i);
        }

        /// <summary>Every lane goes (cleanup); returns how many there were.</summary>
        private int ReleaseAllLanes04D()
        {
            int n = _lanes04D.Count;
            for (int i = n - 1; i >= 0; i--) RemoveLaneAt04D(i);
            return n;
        }

        private int LiveLanes04D { get { return _lanes04D.Count; } }

        /// <summary>The ground height under a point (or the point's own height when there is no ground).</summary>
        private static float GroundY04D(Vector3 p, float up)
        {
            float g;
            return CreatureDirector.GroundAt(p, up, out g) ? g : p.y;
        }

        // ================================================================== beams

        private static BeamFx TakeBeamFx04D()
        {
            while (_beamFree04D.Count > 0)
            {
                BeamFx f = _beamFree04D[_beamFree04D.Count - 1];
                _beamFree04D.RemoveAt(_beamFree04D.Count - 1);
                if (f != null && f.Go != null)
                {
                    f.Go.SetActive(true);
                    return f;
                }
            }
            BeamFx n = new BeamFx();
            n.Go = Shapes.MeshObject("Boss Beam", Shapes.CubeMesh, new[] { GlowMat04D }, Root04D, false);
            n.Go.layer = Layers.IgnoreRaycast;
            n.R = n.Go.GetComponent<MeshRenderer>();
            n.R.receiveShadows = false;
            n.R.shadowCastingMode = ShadowCastingMode.Off;
            n.Mpb = new MaterialPropertyBlock();
            return n;
        }

        private static void FreeBeamFx04D(BeamFx f)
        {
            if (f == null || f.Go == null) return;
            f.Go.SetActive(false);
            if (!_beamFree04D.Contains(f)) _beamFree04D.Add(f);
        }

        /// <summary>A glowing bar from a to b (an aim line, a beam, a decoy pillar). Null when the fight no longer owns new things.</summary>
        private Beam04DLive AddBeam04D(Vector3 a, Vector3 b, float width, Color color, float alpha, int serial)
        {
            if (_ledger == null || !_ledger.Open(BossOwned04D.Beam)) return null;
            Beam04DLive bm = new Beam04DLive();
            bm.Fx = TakeBeamFx04D();
            bm.Width = width;
            bm.Color = color;
            bm.Alpha = alpha;
            bm.Serial = serial;
            SetBeam04D(bm, a, b);
            _beams04D.Add(bm);
            return bm;
        }

        private static void SetBeam04D(Beam04DLive bm, Vector3 a, Vector3 b)
        {
            if (bm == null) return;
            bm.A = a;
            bm.B = b;
            BeamFx f = bm.Fx;
            if (f == null || f.Go == null) return;
            Vector3 d = b - a;
            float len = d.magnitude;
            if (len < 0.01f)
            {
                f.Go.SetActive(false);
                return;
            }
            if (!f.Go.activeSelf) f.Go.SetActive(true);
            f.Go.transform.SetPositionAndRotation((a + b) * 0.5f, Quaternion.LookRotation(d / len, Mathf.Abs(d.y / len) > 0.95f ? Vector3.forward : Vector3.up));
            f.Go.transform.localScale = new Vector3(bm.Width, bm.Width, len);
            Tint04D(f.R, f.Mpb, bm.Color * bm.Alpha);
        }

        /// <summary>The beam fires: full brightness, then it fades away over life seconds and is released.</summary>
        private static void FlashBeam04D(Beam04DLive bm, float width, float life)
        {
            if (bm == null || bm.Done) return;
            bm.Width = width;
            bm.Alpha = 1.6f;
            bm.Fade = life;
            bm.FadeLife = life;
            SetBeam04D(bm, bm.A, bm.B);
        }

        private void UpdateBeams04D(float dt)
        {
            for (int i = _beams04D.Count - 1; i >= 0; i--)
            {
                Beam04DLive bm = _beams04D[i];
                if (bm.Done)
                {
                    RemoveBeamAt04D(i);
                    continue;
                }
                if (bm.FadeLife <= 0f) continue;
                bm.Fade -= dt;
                if (bm.Fade <= 0f)
                {
                    RemoveBeamAt04D(i);
                    continue;
                }
                float k = bm.Fade / bm.FadeLife;
                if (bm.Fx != null && bm.Fx.Go != null) Tint04D(bm.Fx.R, bm.Fx.Mpb, bm.Color * (1.6f * k));
            }
        }

        /// <summary>Marks a beam for release (it goes on the next update).</summary>
        private static void ReleaseBeam04D(Beam04DLive bm)
        {
            if (bm != null) bm.Done = true;
        }

        private void RemoveBeamAt04D(int i)
        {
            Beam04DLive bm = _beams04D[i];
            FreeBeamFx04D(bm.Fx);
            bm.Fx = null;
            bm.Done = true;
            _beams04D.RemoveAt(i);
            if (_ledger != null) _ledger.Release(BossOwned04D.Beam);
        }

        /// <summary>A move broken in its wind-up: its steady aim lines vanish (beams already flashing finish their fade).</summary>
        private void ReleaseMoveBeams04D(int serial)
        {
            for (int i = _beams04D.Count - 1; i >= 0; i--) if (_beams04D[i].Serial == serial && _beams04D[i].FadeLife <= 0f) RemoveBeamAt04D(i);
        }

        private int ReleaseAllBeams04D()
        {
            int n = _beams04D.Count;
            for (int i = n - 1; i >= 0; i--) RemoveBeamAt04D(i);
            return n;
        }

        // ================================================================== shard bodies

        /// <summary>A pooled glowing shard body for the Djinn's choir (one cached emissive material).</summary>
        private static GameObject TakeShardBody04D()
        {
            while (_shardFree04D.Count > 0)
            {
                GameObject g = _shardFree04D[_shardFree04D.Count - 1];
                _shardFree04D.RemoveAt(_shardFree04D.Count - 1);
                if (g != null)
                {
                    g.SetActive(true);
                    return g;
                }
            }
            GameObject n = Shapes.MeshObject("Choir Shard", Shapes.CubeMesh, new[] { Mats.Emissive(ChoirColor04D, 3f) }, Root04D, false);
            n.layer = Layers.IgnoreRaycast;
            n.transform.localScale = new Vector3(0.16f, 0.16f, 0.62f);
            return n;
        }

        private static void FreeShardBody04D(GameObject g)
        {
            if (g == null) return;
            g.SetActive(false);
            if (!_shardFree04D.Contains(g)) _shardFree04D.Add(g);
        }
    }
}
