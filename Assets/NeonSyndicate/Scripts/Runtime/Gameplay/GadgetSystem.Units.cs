using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace NeonSyndicate
{
    /// <summary>
    /// The one-sided bullet face of a Lattice shield. Enemy bullets and blasts that find it (WeaponController and
    /// Explosions look for an IDamageable) are soaked by the shield instead of reaching the player.
    /// </summary>
    public sealed class GadgetShieldFace : MonoBehaviour, IDamageable
    {
        public Action<DamageInfo> OnHit;
        public Func<bool> Alive;

        public void TakeDamage(DamageInfo info)
        {
            if (OnHit != null) OnHit(info);
        }

        public bool IsAlive { get { return Alive != null && Alive(); } }
    }

    /// <summary>
    /// The deployed gadgets: turrets, homing mines, drones, shields, vapor clouds and scanner pulses, the marks the
    /// scanners leave, and gadget mods lying on the ground. Every hit goes through the shared damage API
    /// (DamageInfo and IDamageable.TakeDamage) with the player as the source, so kills, loot, reputation and the wanted
    /// level work exactly as for the player's own weapons.
    /// </summary>
    public sealed partial class GadgetSystem
    {
        private readonly List<Unit> _units = new List<Unit>();
        private readonly List<MarkEntry> _marks = new List<MarkEntry>();
        private readonly List<ModDrop> _modDrops = new List<ModDrop>();
        private static readonly List<CharacterBase> _near = new List<CharacterBase>();

        private sealed class MarkEntry
        {
            public CharacterBase Target;
            public float Until;
            /// <summary>Damage taken multiplier this mark put on the target (1 when it only reveals).</summary>
            public float Scale = 1f;
        }

        private sealed class ModDrop
        {
            public GadgetModSave Mod;
            public GameObject Go;
            public Transform Chip;
            public Vector3 Pos;
            public float Age;
        }

        // ------------------------------------------------------------------ shared helpers

        /// <summary>
        /// Fighters near a point: people hostile to the player (not in vehicles) and creatures that are engaged with the
        /// player (or any visible creature when passive is true, for the scanners).
        /// </summary>
        private static void Hostiles(Vector3 c, float r, bool passive, List<CharacterBase> into)
        {
            into.Clear();
            float r2 = r * r;
            List<NpcController> npcs = NpcController.All;
            for (int i = 0; i < npcs.Count; i++)
            {
                NpcController n = npcs[i];
                if (n == null || n.IsDead || n.InVehicle || !n.IsHostileToPlayer) continue;
                if ((n.transform.position - c).sqrMagnitude <= r2) into.Add(n);
            }
            List<Creature> cs = Creature.All;
            for (int i = 0; i < cs.Count; i++)
            {
                Creature cr = cs[i];
                if (cr == null || !cr.Targetable) continue;
                if (!passive && !cr.IsEngaged) continue;
                float rr = r + cr.Radius;
                if ((cr.transform.position - c).sqrMagnitude <= rr * rr) into.Add(cr);
            }
        }

        private static bool Clear(Vector3 from, Vector3 to)
        {
            return !Physics.Linecast(from, to, Layers.WorldMask, QueryTriggerInteraction.Ignore);
        }

        private static CharacterBase Nearest(Vector3 from, float reach, bool needSight, List<CharacterBase> skip)
        {
            Hostiles(from, reach, false, _near);
            CharacterBase best = null;
            float bd = float.MaxValue;
            for (int i = 0; i < _near.Count; i++)
            {
                CharacterBase t = _near[i];
                if (skip != null && skip.Contains(t)) continue;
                float d = (t.transform.position - from).sqrMagnitude;
                if (d >= bd) continue;
                if (needSight && !Clear(from, t.ChestPosition)) continue;
                bd = d;
                best = t;
            }
            return best;
        }

        private static bool Valid(CharacterBase t)
        {
            return t != null && !t.IsDead;
        }

        /// <summary>One gadget hit on a target through the shared damage API, with the player as the source.</summary>
        private static void Hit(CharacterBase t, float amount, Vector3 from, DamageType type, float poise)
        {
            PlayerController p = Game.Player;
            if (p == null || t == null || t.Health == null || !t.Health.IsAlive || amount <= 0f) return;
            Vector3 at = t.ChestPosition;
            DamageInfo info = DamageInfo.Make(amount, at, at - from, p.gameObject, type);
            info.Poise = poise;
            info.Force = type == DamageType.Explosion ? 6f : 0.8f;
            t.Health.TakeDamage(info);
        }

        public bool AnyUnitActive
        {
            get
            {
                for (int i = 0; i < _units.Count; i++) if (!_units[i].Done && _units[i].CountsAsActive) return true;
                return false;
            }
        }

        /// <summary>Time left of the gadget deployed from a slot (0 to 1), or -1 when none is out.</summary>
        public float SlotLife(int slot)
        {
            for (int i = 0; i < _units.Count; i++)
            {
                Unit u = _units[i];
                if (u.Slot == slot && u.CountsAsActive && !u.Done && u.Life > 0f) return Mathf.Clamp01(1f - u.Age / u.Life);
            }
            return -1f;
        }

        public int LiveUnits { get { return _units.Count; } }
        public int LiveMarks { get { return _marks.Count; } }

        // ------------------------------------------------------------------ spawning

        private bool Spawn(PlayerController p, GadgetSpec spec, int slot, Vector3 pos, Vector3 fwd)
        {
            int level = PlayerLevel;
            float power = GadgetRules.Power(spec, level, PowerBonus(slot));
            float life = GadgetRules.Duration(spec, DurationBonus(slot));
            float radius = GadgetRules.Radius(spec, RadiusBonus(slot));
            float reach = spec.Reach * (1f + RadiusBonus(slot));
            // One live gadget per slot: a new one replaces the old.
            for (int i = 0; i < _units.Count; i++) if (_units[i].Slot == slot && _units[i].CountsAsActive) _units[i].Done = true;
            Vector3 hand = p.transform.position + Vector3.up * 1.4f + p.transform.right * 0.3f;
            switch (spec.Kind)
            {
                case GadgetKind.Turret:
                {
                    Transform head, muzzle;
                    GameObject go = GadgetModels.Turret(spec.Variant == 1, spec.Color, out head, out muzzle);
                    TurretUnit u = new TurretUnit { Head = head, Muzzle = muzzle, Flame = spec.Variant == 1 };
                    Add(u, spec, slot, go, power, life, radius, reach);
                    u.StartFlight(hand, pos, Quaternion.LookRotation(fwd));
                    return true;
                }
                case GadgetKind.Mine:
                {
                    if (spec.Variant == 0)
                    {
                        Transform body;
                        GameObject go = GadgetModels.Mine(spec.Color, 1f, out body);
                        MineUnit u = new MineUnit { Body = body, Speed = 7f };
                        Add(u, spec, slot, go, power, life, radius, reach);
                        u.StartFlight(hand, pos, Quaternion.LookRotation(fwd));
                    }
                    else
                    {
                        GameObject go = GadgetModels.Canister(spec.Color);
                        LitterUnit u = new LitterUnit();
                        Add(u, spec, slot, go, power, life, radius, reach);
                        u.StartFlight(hand, pos, Quaternion.LookRotation(fwd));
                    }
                    return true;
                }
                case GadgetKind.Drone:
                {
                    Transform[] rotors;
                    GameObject go = GadgetModels.Drone(spec.Color, out rotors);
                    go.transform.position = hand;
                    DroneUnit u = new DroneUnit { Rotors = rotors, Healer = spec.Variant == 1 };
                    Add(u, spec, slot, go, power, life, radius, reach);
                    return true;
                }
                case GadgetKind.Barrier:
                {
                    bool handheld = spec.Variant == 1;
                    if (handheld)
                    {
                        // 04E: the handheld Buckler is the ward (aspect geometry, capacity, the shared damage path).
                        SpawnWard(p, spec, slot, pos, fwd, power, life, radius, reach);
                        return true;
                    }
                    GameObject face;
                    Renderer panel;
                    GameObject go = GadgetModels.Barrier(handheld, spec.Color, out face, out panel);
                    go.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(fwd));
                    float hp = GadgetRules.ShieldHealth(spec, level, PowerBonus(slot));
                    BarrierUnit u = new BarrierUnit { Handheld = handheld, Hp = hp, MaxHp = hp, Panel = panel };
                    Add(u, spec, slot, go, power, life, radius, reach);
                    GadgetShieldFace f = face.AddComponent<GadgetShieldFace>();
                    f.OnHit = u.Absorb;
                    f.Alive = () => !u.Done && u.Hp > 0f;
                    GadgetSounds.Play(GadgetSounds.Clip.ShieldUp, pos + Vector3.up, 0.6f, handheld ? 1.15f : 1f, 0.6f);
                    return true;
                }
                case GadgetKind.Scanner:
                {
                    if (spec.Variant == 0)
                    {
                        int n = RipplePulse(p.transform.position + Vector3.up, radius, life, false, spec.Color);
                        if (Game.Hud != null) Game.Hud.CombatText(n > 0 ? n + " marked" : "No one in range", spec.Color);
                    }
                    else
                    {
                        int n = LancePulse(p, reach, life, spec.Color);
                        if (Game.Hud != null) Game.Hud.CombatText(n > 0 ? n + " exposed" : "No one in the beam", spec.Color);
                    }
                    return true;
                }
                default:
                {
                    GameObject go = GadgetModels.Canister(spec.Color);
                    CloudUnit u = new CloudUnit { Healing = spec.Variant == 0 };
                    Add(u, spec, slot, go, power, life, radius, reach);
                    u.StartFlight(hand, pos, Quaternion.LookRotation(fwd));
                    return true;
                }
            }
        }

        private void Add(Unit u, GadgetSpec spec, int slot, GameObject go, float power, float life, float radius, float reach)
        {
            u.Owner = this;
            u.Spec = spec;
            u.Slot = slot;
            u.Go = go;
            u.Power = power;
            u.Life = life;
            u.Radius = radius;
            u.Reach = reach;
            go.transform.SetParent(transform, true);
            _units.Add(u);
        }

        private void UpdateUnits(float dt)
        {
            for (int i = _units.Count - 1; i >= 0; i--)
            {
                if (i >= _units.Count) continue;
                Unit u = _units[i];
                if (!u.Done && u.Go != null)
                {
                    try { u.Tick(dt); }
                    catch (Exception e)
                    {
                        Log.Exception("Gadget " + u.Spec.FullName, e);
                        u.Done = true;
                    }
                }
                if (!u.Done && u.Go != null) continue;
                try { u.End(); }
                catch (Exception e) { Log.Exception("Gadget end", e); }
                _units.RemoveAt(i);
            }
        }

        private void ClearUnits()
        {
            for (int i = 0; i < _units.Count; i++)
            {
                try { _units[i].End(); }
                catch (Exception e) { Log.Exception("Gadget end", e); }
            }
            _units.Clear();
        }

        // ------------------------------------------------------------------ marks

        /// <summary>Marks a target for a while; scale above 1 also makes it take that much more damage until the mark ends.</summary>
        public void MarkTarget(CharacterBase t, float seconds, float scale)
        {
            if (!Valid(t) || t.Health == null) return;
            MarkEntry m = null;
            for (int i = 0; i < _marks.Count; i++) if (_marks[i].Target == t) { m = _marks[i]; break; }
            if (m == null)
            {
                m = new MarkEntry { Target = t };
                _marks.Add(m);
            }
            m.Until = Mathf.Max(m.Until, Time.time + seconds);
            if (scale > m.Scale + 0.0001f)
            {
                t.Health.DamageScale *= scale / m.Scale;
                m.Scale = scale;
            }
        }

        public bool IsMarked(CharacterBase t)
        {
            for (int i = 0; i < _marks.Count; i++) if (_marks[i].Target == t) return true;
            return false;
        }

        private void UpdateMarks()
        {
            float now = Time.time;
            for (int i = _marks.Count - 1; i >= 0; i--)
            {
                MarkEntry m = _marks[i];
                if (m.Target == null)
                {
                    _marks.RemoveAt(i);
                    continue;
                }
                if (!m.Target.IsDead && now < m.Until) continue;
                Unmark(m);
                _marks.RemoveAt(i);
            }
        }

        private static void Unmark(MarkEntry m)
        {
            if (m.Target != null && m.Target.Health != null && m.Scale > 1f) m.Target.Health.DamageScale /= m.Scale;
            m.Scale = 1f;
        }

        private void ClearMarks()
        {
            for (int i = 0; i < _marks.Count; i++) Unmark(_marks[i]);
            _marks.Clear();
        }

        /// <summary>The Ripple pulse: marks every fighter (any visible creature too) within radius. Returns how many.</summary>
        private int RipplePulse(Vector3 center, float radius, float seconds, bool expose, Color c)
        {
            float scale = (1f + GearBuilds.Current.MarkedDamage) * (expose ? 1f + GadgetRules.ExposeBonus : 1f);
            Hostiles(center, radius, true, _near);
            int n = _near.Count;
            for (int i = 0; i < _near.Count; i++) MarkTarget(_near[i], seconds, scale);
            GameObject ring = GadgetModels.RingObject(transform, c, true);
            ring.transform.position = PedManager.Ground(center) + Vector3.up * 0.15f;
            PulseUnit u = new PulseUnit { Ring = ring.GetComponent<Renderer>(), MaxRadius = radius, Tint = c };
            u.CountsAsActive = false;
            Add(u, GadgetRules.Get(GadgetKind.Scanner, 0), -1, ring, 0f, 0.7f, radius, 0f);
            GadgetSounds.Play(GadgetSounds.Clip.Pulse, center, 0.7f, 1f, 0.4f);
            return n;
        }

        /// <summary>The Lance pulse: marks and exposes everyone in a narrow cone along the crosshair. Returns how many.</summary>
        private int LancePulse(PlayerController p, float reach, float seconds, Color c)
        {
            Vector3 apex = p.ChestPosition;
            Vector3 fwd = Game.Cam != null ? Game.Cam.AimRay.direction : p.transform.forward;
            float scale = (1f + GearBuilds.Current.MarkedDamage) * (1f + GadgetRules.ExposeBonus);
            Hostiles(apex, reach, true, _near);
            int n = 0;
            for (int i = 0; i < _near.Count; i++)
            {
                if (!GadgetRules.InCone(apex, fwd, GadgetRules.LanceHalfAngle, reach, _near[i].ChestPosition)) continue;
                MarkTarget(_near[i], seconds, scale);
                n++;
            }
            GameObject beam = Shapes.MeshObject("Lance Beam", Shapes.CubeMesh, new[] { new Material(Mats.Marker(new Color(c.r, c.g, c.b, 0.5f))) }, transform, false);
            beam.transform.SetPositionAndRotation(apex + fwd * (reach * 0.5f), Quaternion.LookRotation(fwd));
            beam.transform.localScale = new Vector3(0.12f, 0.12f, reach);
            PulseUnit u = new PulseUnit { Ring = beam.GetComponent<Renderer>(), MaxRadius = 0f, Tint = c, Beam = true, BeamLength = reach };
            u.CountsAsActive = false;
            Add(u, GadgetRules.Get(GadgetKind.Scanner, 1), -1, beam, 0f, 0.5f, 0f, reach);
            GadgetSounds.Play(GadgetSounds.Clip.Pulse, apex, 0.7f, 1.3f, 0.3f);
            return n;
        }

        // ------------------------------------------------------------------ mod drops

        private void SpawnModDrop(GadgetModSave mod, Vector3 at)
        {
            if (mod == null) return;
            Vector3 g = PedManager.Ground(at);
            Transform chip;
            GameObject go = GadgetModels.ModChip(GearSystem.RarityColor((Rarity)mod.Rarity), out chip);
            go.transform.SetParent(transform, true);
            go.transform.position = g;
            _modDrops.Add(new ModDrop { Mod = mod, Go = go, Chip = chip, Pos = g });
            if (_modDrops.Count > 20)
            {
                if (_modDrops[0].Go != null) Destroy(_modDrops[0].Go);
                _modDrops.RemoveAt(0);
            }
            GadgetSounds.Play(GadgetSounds.Clip.Chirp, g, 0.4f, 0.8f, 0.9f);
        }

        /// <summary>F2: a mod of the given rarity at the player's feet.</summary>
        public void DropModHere(Rarity r)
        {
            PlayerController p = Game.Player;
            if (p == null) return;
            SpawnModDrop(RollMod(r), p.transform.position + p.transform.forward * 2f);
        }

        private void UpdateModDrops(PlayerController p)
        {
            float t = Time.time;
            for (int i = _modDrops.Count - 1; i >= 0; i--)
            {
                ModDrop d = _modDrops[i];
                d.Age += Time.deltaTime;
                if (d.Go == null || d.Age > 300f)
                {
                    if (d.Go != null) Destroy(d.Go);
                    _modDrops.RemoveAt(i);
                    continue;
                }
                d.Chip.localRotation = Quaternion.Euler(0f, t * 140f, 0f);
                d.Chip.localPosition = new Vector3(0f, 0.6f + Mathf.Sin(t * 3f + i) * 0.08f, 0f);
                if (p == null || p.IsDead || p.InVehicle) continue;
                if ((p.transform.position - d.Pos).sqrMagnitude > 1.8f * 1.8f) continue;
                if (!AddMod(d.Mod, true)) continue;
                GadgetSounds.Play2D(GadgetSounds.Clip.ModPickup, 0.55f, 1f);
                Pad.Rumble(0.15f, 0.3f, 0.1f);
                Destroy(d.Go);
                _modDrops.RemoveAt(i);
            }
        }

        private void ClearModDrops()
        {
            for (int i = 0; i < _modDrops.Count; i++) if (_modDrops[i].Go != null) Destroy(_modDrops[i].Go);
            _modDrops.Clear();
        }

        // ================================================================== unit types

        private abstract class Unit
        {
            public GadgetSystem Owner;
            public GadgetSpec Spec;
            public int Slot;
            public GameObject Go;
            public float Age;
            public float Life;
            public bool Done;
            public float Power;
            public float Radius;
            public float Reach;
            /// <summary>False for instant effects (pulses): they do not count as a deployed gadget.</summary>
            public bool CountsAsActive = true;

            private bool _flying;
            private Vector3 _from, _to;
            private float _flightT;
            private Quaternion _landRot = Quaternion.identity;
            protected const float FlightTime = 0.45f;

            public bool Flying { get { return _flying; } }

            public void StartFlight(Vector3 from, Vector3 to, Quaternion landRotation)
            {
                _flying = true;
                _from = from;
                _to = to;
                _flightT = 0f;
                _landRot = landRotation;
                Go.transform.position = from;
            }

            /// <summary>Moves along the throw arc. Returns true while still in the air.</summary>
            protected bool Fly(float dt)
            {
                if (!_flying) return false;
                _flightT += dt / FlightTime;
                float t = Mathf.Clamp01(_flightT);
                float h = 1.2f + 0.08f * (_to - _from).magnitude;
                Go.transform.position = Vector3.Lerp(_from, _to, t) + Vector3.up * (4f * h * t * (1f - t));
                Go.transform.Rotate(540f * dt, 0f, 0f, Space.Self);
                if (t < 1f) return true;
                _flying = false;
                Go.transform.SetPositionAndRotation(_to, _landRot);
                OnLanded();
                return false;
            }

            protected virtual void OnLanded() { }

            public abstract void Tick(float dt);

            public virtual void End()
            {
                if (Go != null) Object.Destroy(Go);
                Go = null;
            }

            /// <summary>The usual end of a gadget: sparks and a soft flash where it stood.</summary>
            protected void Fizzle()
            {
                if (Go != null && Game.Fx != null)
                {
                    Game.Fx.Sparks(Go.transform.position + Vector3.up * 0.4f, Vector3.up * 2f, 8);
                    Game.Fx.Flash(Go.transform.position + Vector3.up * 0.5f, Spec.Color, 1.5f, 3f, 0.2f);
                }
            }
        }

        // ------------------------------------------------------------------ turret

        private sealed class TurretUnit : Unit
        {
            public Transform Head;
            public Transform Muzzle;
            public bool Flame;
            private CharacterBase _target;
            private float _retarget;
            private float _fireT;
            private float _soundT;

            public override void Tick(float dt)
            {
                if (Fly(dt)) return;
                Age += dt;
                if (Age >= Life)
                {
                    Fizzle();
                    Done = true;
                    return;
                }
                float rise = Mathf.Clamp01(Age / 0.35f);
                Go.transform.localScale = new Vector3(1f, Mathf.Lerp(0.3f, 1f, rise), 1f);
                _retarget -= dt;
                if (_retarget <= 0f || !Valid(_target) || (_target.transform.position - Go.transform.position).sqrMagnitude > Reach * Reach)
                {
                    _target = Nearest(Muzzle.position, Reach, !Flame, null);
                    _retarget = 0.35f;
                }
                _soundT -= dt;
                if (_target == null)
                {
                    Head.Rotate(0f, 45f * dt, 0f, Space.World);
                    return;
                }
                Vector3 dir = _target.ChestPosition - Head.position;
                if (dir.sqrMagnitude < 0.01f) return;
                Quaternion want = Quaternion.LookRotation(dir);
                Head.rotation = Quaternion.RotateTowards(Head.rotation, want, 360f * dt);
                _fireT -= dt;
                if (_fireT > 0f || Quaternion.Angle(Head.rotation, want) > 14f) return;
                _fireT = Spec.Interval;
                if (Flame) FireFlame();
                else FireRifle();
            }

            private void FireRifle()
            {
                Vector3 from = Muzzle.position;
                Vector3 to = _target.ChestPosition + UnityEngine.Random.insideUnitSphere * 0.15f;
                if (!Clear(from, to))
                {
                    _retarget = 0f;
                    return;
                }
                Hit(_target, Power, from, DamageType.Bullet, 4f);
                if (Game.Fx != null)
                {
                    Game.Fx.Muzzle(from, (to - from).normalized, 0.5f);
                    Game.Fx.Tracer(from, to);
                }
                if (_soundT <= 0f)
                {
                    _soundT = 0.08f;
                    GadgetSounds.Play(GadgetSounds.Clip.TurretShot, from, 0.45f, UnityEngine.Random.Range(0.95f, 1.08f), 1f);
                }
            }

            private void FireFlame()
            {
                Vector3 apex = Muzzle.position;
                Vector3 fwd = Head.forward;
                Hostiles(apex, Reach, false, _near);
                for (int i = 0; i < _near.Count; i++)
                {
                    CharacterBase t = _near[i];
                    if (GadgetRules.InCone(apex, fwd, GadgetRules.KilnHalfAngle, Reach, t.ChestPosition) && Clear(apex, t.ChestPosition)) Hit(t, Power, apex, DamageType.Fire, 2f);
                }
                if (Game.Fx != null)
                {
                    for (int k = 1; k <= 3; k++) Game.Fx.FirePuff(apex + fwd * (Reach * 0.25f * k) + UnityEngine.Random.insideUnitSphere * 0.3f, 0.5f + 0.35f * k);
                }
                if (_soundT <= 0f)
                {
                    _soundT = 0.45f;
                    GadgetSounds.Play(GadgetSounds.Clip.Flame, apex, 0.55f, UnityEngine.Random.Range(0.9f, 1.1f), 1f);
                }
            }
        }

        // ------------------------------------------------------------------ homing mines

        private sealed class MineUnit : Unit
        {
            public Transform Body;
            public float Speed = 7f;
            /// <summary>Shared with sibling seekers so each Litter seeker picks a different enemy.</summary>
            public List<CharacterBase> Claimed;
            private CharacterBase _target;
            private float _arm = 0.6f;
            private float _chirp;
            private float _look;

            protected override void OnLanded()
            {
                Go.transform.rotation = Quaternion.Euler(0f, Go.transform.eulerAngles.y, 0f);
            }

            public override void Tick(float dt)
            {
                if (Fly(dt)) return;
                Age += dt;
                if (Age >= Life)
                {
                    Explode();
                    return;
                }
                if (_arm > 0f)
                {
                    _arm -= dt;
                    if (_arm <= 0f) GadgetSounds.Play(GadgetSounds.Clip.Chirp, Go.transform.position, 0.4f, 1.1f, 1f);
                    return;
                }
                _look -= dt;
                if (!Valid(_target) && _look <= 0f)
                {
                    _look = 0.3f;
                    _target = Nearest(Go.transform.position + Vector3.up * 0.3f, Reach, false, Claimed);
                    if (_target != null && Claimed != null) Claimed.Add(_target);
                }
                if (!Valid(_target)) return;
                Vector3 pos = Go.transform.position;
                Vector3 d = _target.transform.position - pos;
                d.y = 0f;
                float dist = d.magnitude;
                Creature cr = _target as Creature;
                float touch = 1.2f + (cr != null ? cr.Radius : 0.3f);
                if (dist <= touch)
                {
                    Explode();
                    return;
                }
                Vector3 np = pos + d / Mathf.Max(0.001f, dist) * Mathf.Min(dist, Speed * dt);
                RaycastHit hit;
                if (Physics.Raycast(np + Vector3.up * 1.5f, Vector3.down, out hit, 4f, Layers.GroundMask, QueryTriggerInteraction.Ignore)) np.y = hit.point.y;
                Go.transform.SetPositionAndRotation(np, Quaternion.LookRotation(d));
                Body.localPosition = new Vector3(0f, Mathf.Abs(Mathf.Sin(Age * 22f)) * 0.04f, 0f);
                _chirp -= dt;
                if (_chirp <= 0f)
                {
                    _chirp = 0.5f;
                    GadgetSounds.Play(GadgetSounds.Clip.Chirp, np, 0.3f, 1.3f, 1f);
                }
            }

            private void Explode()
            {
                Done = true;
                if (Go == null) return;
                Vector3 c = Go.transform.position + Vector3.up * 0.4f;
                if (Game.Fx != null) Game.Fx.ExplosionFx(c, Mathf.Max(1.2f, Radius * 0.6f));
                if (Game.Audio != null) Game.Audio.PlayAt(SfxId.ExplosionSmall, c, 0.8f, UnityEngine.Random.Range(1.05f, 1.2f), 200f);
                if (Game.Cam != null) Game.Cam.Shake(Mathf.Clamp01(0.6f - Vector3.Distance(Game.Cam.transform.position, c) / 40f));
                Hostiles(c, Radius + 1.5f, false, _near);
                for (int i = 0; i < _near.Count; i++)
                {
                    CharacterBase t = _near[i];
                    Creature cr = t as Creature;
                    float dist = Mathf.Max(0f, (t.ChestPosition - c).magnitude - (cr != null ? cr.Radius : 0f));
                    float f = GadgetRules.BlastFalloff(dist, Radius);
                    // Cover softens a blast, as with every other explosion.
                    if (f > 0f && !Clear(c, t.ChestPosition)) f *= 0.3f;
                    if (f > 0f) Hit(t, Power * f, c, DamageType.Explosion, 25f);
                }
            }
        }

        /// <summary>The Litter canister: lands and splits into small seekers.</summary>
        private sealed class LitterUnit : Unit
        {
            public override void Tick(float dt)
            {
                if (Fly(dt)) return;
                Done = true;
            }

            protected override void OnLanded()
            {
                List<CharacterBase> claimed = new List<CharacterBase>();
                Vector3 at = Go.transform.position;
                for (int i = 0; i < GadgetRules.LitterCount; i++)
                {
                    Transform body;
                    GameObject go = GadgetModels.Mine(Spec.Color, 0.6f, out body);
                    float a = i * Mathf.PI * 2f / GadgetRules.LitterCount;
                    go.transform.position = at + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 0.5f;
                    go.transform.rotation = Quaternion.Euler(0f, a * Mathf.Rad2Deg, 0f);
                    MineUnit u = new MineUnit { Body = body, Speed = 8f, Claimed = claimed };
                    Owner.Add(u, Spec, Slot, go, Power, Life, Radius, Reach);
                }
                if (Game.Fx != null) Game.Fx.Sparks(at + Vector3.up * 0.3f, Vector3.up * 2f, 10);
                GadgetSounds.Play(GadgetSounds.Clip.Burst, at, 0.5f, 1.4f, 1f);
            }
        }

        // ------------------------------------------------------------------ drone

        private sealed class DroneUnit : Unit
        {
            public Transform[] Rotors;
            public bool Healer;
            private float _tick;
            private float _orbit;
            private float _chime;

            public override void Tick(float dt)
            {
                PlayerController p = Game.Player;
                Age += dt;
                if (Age >= Life || p == null || p.IsDead)
                {
                    Fizzle();
                    Done = true;
                    return;
                }
                _orbit += 30f * dt;
                Vector3 anchor = p.transform.position + Vector3.up * (2.3f + Mathf.Sin(Age * 2.2f) * 0.12f) + Quaternion.Euler(0f, _orbit, 0f) * Vector3.right * 1f;
                Go.transform.position = Vector3.Lerp(Go.transform.position, anchor, 1f - Mathf.Exp(-6f * dt));
                float yaw = Game.Cam != null ? Game.Cam.Yaw : p.transform.eulerAngles.y;
                Go.transform.rotation = Quaternion.Euler(4f, yaw, 0f);
                for (int i = 0; i < Rotors.Length; i++) if (Rotors[i] != null) Rotors[i].Rotate(0f, 1500f * dt, 0f, Space.Self);
                _tick -= dt;
                _chime -= dt;
                if (_tick > 0f) return;
                if (Healer) Mend(p);
                else Zap();
            }

            private void Mend(PlayerController p)
            {
                _tick = Spec.Interval;
                float k = Spec.Power > 0f ? Power / Spec.Power : 1f;
                p.Health.Heal(Power * Spec.Interval);
                p.Health.AddArmor(GadgetRules.SutureArmorPerSecond * Spec.Interval * k);
                if (_chime <= 0f)
                {
                    _chime = 2f;
                    if (Game.Fx != null) Game.Fx.Flash(p.ChestPosition, Spec.Color, 1.4f, 3f, 0.25f);
                    GadgetSounds.Play(GadgetSounds.Clip.Heal, Go.transform.position, 0.3f, 1f, 0.8f);
                }
            }

            private void Zap()
            {
                Vector3 from = Go.transform.position;
                CharacterBase t = Nearest(from, Reach, true, null);
                if (t == null)
                {
                    _tick = 0.25f;
                    return;
                }
                _tick = Spec.Interval;
                Vector3 at = t.ChestPosition;
                Hit(t, Power, from, DamageType.Power, 8f);
                if (Game.Fx != null)
                {
                    Game.Fx.Tracer(from, at);
                    Game.Fx.Sparks(at, Vector3.up * 1.5f, 6);
                    Game.Fx.Flash(at, Spec.Color, 2f, 4f, 0.12f);
                }
                GadgetSounds.Play(GadgetSounds.Clip.Zap, from, 0.45f, UnityEngine.Random.Range(0.95f, 1.1f), 1f);
            }
        }

        // ------------------------------------------------------------------ shields

        private sealed class BarrierUnit : Unit
        {
            public bool Handheld;
            public float Hp;
            public float MaxHp;
            public Renderer Panel;
            /// <summary>04E: this Buckler is the ward (GadgetSystem.Ward.cs owns its hits).</summary>
            public bool Ward;
            public bool Broken;
            public string Aspect = "";
            /// <summary>Frostglass: frontal hits before this time are deflected at no capacity cost.</summary>
            public float DeflectUntil = -99f;
            /// <summary>The ward's own panel material (capacity shown as brightness), destroyed with it.</summary>
            public Material PanelMat;
            /// <summary>The aspect part that moves (ring, lattice, plates).</summary>
            public Transform Spin;
            private float _pingT;

            public override void Tick(float dt)
            {
                Age += dt;
                if (Age >= Life)
                {
                    Fizzle();
                    Done = true;
                    return;
                }
                if (Handheld)
                {
                    PlayerController p = Game.Player;
                    if (p == null || p.IsDead || p.IsRagdolled || p.InVehicle)
                    {
                        Done = true;
                        return;
                    }
                    float yaw = Game.Cam != null ? Game.Cam.Yaw : p.transform.eulerAngles.y;
                    Vector3 dir = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
                    Go.transform.SetPositionAndRotation(p.transform.position + dir * 0.8f + Vector3.up * 0.1f, Quaternion.LookRotation(dir));
                }
                else
                {
                    float rise = Mathf.Clamp01(Age / 0.3f);
                    Go.transform.localScale = new Vector3(1f, Mathf.Lerp(0.05f, 1f, rise), 1f);
                }
                // Blink through the last two seconds or when nearly broken.
                if (Panel != null)
                {
                    bool blink = Life - Age < 2f || Hp < MaxHp * 0.2f;
                    Panel.enabled = !blink || Mathf.Repeat(Time.time * 6f, 1f) > 0.3f;
                }
                if (Ward) Owner.WardVisualTick(this, dt);
                _pingT -= dt;
            }

            public override void End()
            {
                if (PanelMat != null) Object.Destroy(PanelMat);
                PanelMat = null;
                base.End();
            }

            public void Absorb(DamageInfo info)
            {
                if (Ward)
                {
                    Owner.WardFaceHit(this, info);
                    return;
                }
                if (Done || info.FromPlayer || info.Amount <= 0f) return;
                Hp -= info.Amount;
                if (Game.Fx != null) Game.Fx.Sparks(info.Point, -info.Direction * 2f, 4);
                if (_pingT <= 0f)
                {
                    _pingT = 0.08f;
                    GadgetSounds.Play(GadgetSounds.Clip.ShieldHit, info.Point, 0.4f, UnityEngine.Random.Range(0.9f, 1.15f), 1f);
                }
                if (Hp > 0f) return;
                Done = true;
                Vector3 c = Go != null ? Go.transform.position + Vector3.up * 0.9f : info.Point;
                if (Game.Fx != null)
                {
                    Game.Fx.Sparks(c, Vector3.up * 3f, 30);
                    Game.Fx.Flash(c, Spec.Color, 4f, 6f, 0.3f);
                }
                GadgetSounds.Play(GadgetSounds.Clip.ShieldBreak, c, 0.7f, 1f, 0.8f);
            }
        }

        // ------------------------------------------------------------------ vapor clouds

        private sealed class CloudUnit : Unit
        {
            public bool Healing;
            private bool _open;
            private readonly List<Transform> _puffs = new List<Transform>();
            private float _tick;
            private float _smoke;
            private float _chime;

            protected override void OnLanded()
            {
                _open = true;
                Age = 0f;
                for (int i = Go.transform.childCount - 1; i >= 0; i--) Object.Destroy(Go.transform.GetChild(i).gameObject);
                Go.transform.rotation = Quaternion.identity;
                for (int i = 0; i < 6; i++)
                {
                    GameObject puff = GadgetModels.Puff(Go.transform, Spec.Color);
                    Vector2 o = UnityEngine.Random.insideUnitCircle * Radius * 0.55f;
                    puff.transform.localPosition = new Vector3(o.x, 0.6f + UnityEngine.Random.value * 0.8f, o.y);
                    _puffs.Add(puff.transform);
                }
                if (Game.Fx != null) Game.Fx.Flash(Go.transform.position + Vector3.up, Spec.Color, 3f, Radius * 2f, 0.3f);
                GadgetSounds.Play(GadgetSounds.Clip.Burst, Go.transform.position, 0.6f, Healing ? 1.1f : 0.9f, 1f);
            }

            public override void Tick(float dt)
            {
                if (Fly(dt)) return;
                if (!_open) return;
                Age += dt;
                if (Age >= Life)
                {
                    Done = true;
                    return;
                }
                float fade = Mathf.Clamp01((Life - Age) / 1f) * Mathf.Clamp01(Age / 0.3f);
                for (int i = 0; i < _puffs.Count; i++)
                {
                    Transform t = _puffs[i];
                    if (t == null) continue;
                    float s = Radius * (0.75f + 0.15f * Mathf.Sin(Age * 1.7f + i)) * fade;
                    t.localScale = new Vector3(s, s * 0.55f, s);
                    t.Rotate(0f, 20f * dt, 0f, Space.Self);
                }
                Vector3 c = Go.transform.position;
                _smoke -= dt;
                if (_smoke <= 0f && Game.Fx != null)
                {
                    _smoke = 0.35f;
                    Vector2 o = UnityEngine.Random.insideUnitCircle * Radius * 0.8f;
                    Game.Fx.Smoke(c + new Vector3(o.x, 0.4f, o.y), Vector3.up * 0.3f, 2.4f, Healing ? 0.05f : 0.55f);
                    if (!Healing) Game.Fx.FirePuff(c + new Vector3(o.y, 0.3f, o.x), 0.9f);
                }
                _tick -= dt;
                _chime -= dt;
                if (_tick > 0f) return;
                _tick = Spec.Interval;
                if (Healing)
                {
                    PlayerController p = Game.Player;
                    if (p == null || p.IsDead) return;
                    Vector3 d = p.transform.position - c;
                    if (Mathf.Abs(d.y) > 3f) return;
                    d.y = 0f;
                    if (d.magnitude > Radius) return;
                    p.Health.Heal(Power * Spec.Interval);
                    if (_chime <= 0f)
                    {
                        _chime = 1.5f;
                        GadgetSounds.Play2D(GadgetSounds.Clip.Heal, 0.3f, 1.1f);
                    }
                    return;
                }
                Hostiles(c, Radius, false, _near);
                for (int i = 0; i < _near.Count; i++)
                {
                    CharacterBase t = _near[i];
                    if (Mathf.Abs(t.transform.position.y - c.y) > 3f) continue;
                    Hit(t, Power, c, DamageType.Fire, 0f);
                }
            }
        }

        // ------------------------------------------------------------------ pulse visuals

        private sealed class PulseUnit : Unit
        {
            public Renderer Ring;
            public float MaxRadius;
            public Color Tint;
            public bool Beam;
            public float BeamLength;

            public override void Tick(float dt)
            {
                Age += dt;
                float t = Mathf.Clamp01(Age / Mathf.Max(0.05f, Life));
                if (Ring != null)
                {
                    Color c = Tint;
                    c.a = 0.6f * (1f - t);
                    Ring.sharedMaterial.color = c;
                }
                if (Beam) Go.transform.localScale = new Vector3(0.12f + 0.5f * t, 0.12f + 0.5f * t, BeamLength);
                else
                {
                    float r = Mathf.Max(0.5f, MaxRadius * t);
                    Go.transform.localScale = new Vector3(r, 1f, r);
                }
                if (t >= 1f) Done = true;
            }

            public override void End()
            {
                if (Ring != null && Ring.sharedMaterial != null) Object.Destroy(Ring.sharedMaterial);
                base.End();
            }
        }
    }
}
