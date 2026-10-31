using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    public sealed class Spell
    {
        public string Id;
        public string Name;
        public string Description;
        public Color Color;
        public float Cost;
        public float Cooldown;
    }

    /// <summary>
    /// Malik's crystal powers: the Third Eye (tap Caps Lock or T for bullet time, hold for 0.7 s to shift through the Veil
    /// into the Inner World) and the ten Mythic Las Veyra spell stones plus Sunbolt, cycled with [ and ] and cast with B.
    /// Everything runs on one crystal energy meter that refills over time (not inside the Veil, which drains it), with
    /// kills and with stunts.
    /// </summary>
    public sealed partial class Abilities : MonoBehaviour
    {
        // Partial: the hero powers (Shard Gaze beam, hero unlock sync, the beam visual) live in Abilities.HeroH8S.cs.
        /// <summary>Power damage multiplier from Focus, gear affixes and the skill trees (SkillMods.PowerDamage).</summary>
        public static float PowerScale
        {
            get { return (Game.Progress != null ? Game.Progress.PowerMultiplier : 1f) * (Game.Gear != null ? Game.Gear.PowerMultiplier : 1f) * SkillMods.PowerDamage; }
        }

        public const int SunboltIndex = 10;

        public static readonly Spell[] Spells =
        {
            new Spell { Id = "EMBER_CHAIN", Name = "Ember Chain", Description = "Fire lash that jumps between nearby hostiles.", Color = new Color(1f, 0.45f, 0.15f), Cost = 30f, Cooldown = 6f },
            new Spell { Id = "QUARTZ_WARD", Name = "Quartz Ward", Description = "Crystal barrier that stops incoming rounds.", Color = new Color(0.45f, 0.65f, 1f), Cost = 35f, Cooldown = 10f },
            new Spell { Id = "MIRAGE_STEP", Name = "Mirage Step", Description = "Blink forward and leave the police chasing a ghost.", Color = new Color(0.95f, 0.9f, 0.6f), Cost = 20f, Cooldown = 3f },
            new Spell { Id = "STATIC_LANCE", Name = "Static Lance", Description = "Lightning spear that stalls engines and stuns.", Color = new Color(0.55f, 0.5f, 1f), Cost = 25f, Cooldown = 4f },
            new Spell { Id = "BRASS_GRAVITY", Name = "Brass Gravity Well", Description = "Drags enemies, cars and props into a sigil.", Color = new Color(0.95f, 0.75f, 0.35f), Cost = 40f, Cooldown = 12f },
            new Spell { Id = "GLASS_SIGHT", Name = "Glass Sight", Description = "Reveals hidden collectibles and enemies nearby.", Color = new Color(0.3f, 0.85f, 1f), Cost = 15f, Cooldown = 6f },
            new Spell { Id = "CANAL_MEND", Name = "Canal Mend", Description = "Blue street-water magic that heals you.", Color = new Color(0.25f, 0.85f, 0.6f), Cost = 35f, Cooldown = 15f },
            new Spell { Id = "ROOF_ROC_CALL", Name = "Roc Call", Description = "A sky creature sweeps the block and strikes hostiles.", Color = new Color(0.75f, 0.35f, 0.85f), Cost = 50f, Cooldown = 25f },
            new Spell { Id = "GHOST_FREQ", Name = "Ghost Frequency", Description = "Silences you and scrambles enemy aim.", Color = new Color(0.45f, 0.65f, 0.9f), Cost = 30f, Cooldown = 20f },
            new Spell { Id = "DRIVE_SIGIL", Name = "Drive Sigil", Description = "Supercharges your vehicle's engine, grip and armor.", Color = new Color(0.95f, 0.75f, 0.25f), Cost = 25f, Cooldown = 14f },
            new Spell { Id = "SUNBOLT", Name = "Sunbolt", Description = "The Third Eye's first gift: a blazing crystal orb.", Color = new Color(1f, 0.7f, 0.25f), Cost = 18f, Cooldown = 1.2f },
            // H8S hero powers (appended: saves list unlocks by index). Opens with the Super Leap; hold the cast button to keep it burning.
            new Spell { Id = "SHARD_GAZE", Name = "Shard Gaze", Description = "A focused crystal beam from the hands. Hold to keep it burning.", Color = new Color(0.55f, 0.95f, 1f), Cost = 10f, Cooldown = 1.5f },
        };

        public static float EnemyAccuracyScale = 1f;
        public static bool GhostActive;

        public float Energy = 60f;
        public float MaxEnergy = 100f;
        /// <summary>Max energy from collectibles and story; levels and gear add on top (see Progression).</summary>
        public float BaseMaxEnergy = 100f;
        public bool ThirdEyeUnlocked;
        public bool ThirdEyeActive { get; private set; }
        public readonly bool[] Unlocked = new bool[Spells.Length];
        public int Selected = SunboltIndex;
        public string Law = "";
        public float GlassSightTimer { get; private set; }
        public float RevealTimer { get; private set; }
        /// <summary>0..1 while the Third Eye input is held toward a Veil shift (drives the HUD ring); 0 otherwise.</summary>
        public float VeilHold { get; private set; }
        private readonly HoldGesture _eyeGesture = new HoldGesture(InnerWorld.HoldSeconds);
        private readonly float[] _cooldown = new float[Spells.Length];
        private float _wardTimer;
        private float _ghostTimer;
        private float _mendTimer;
        private GameObject _wardFx;
        private readonly Collider[] _buf = new Collider[48];
        private readonly List<LineRenderer> _beams = new List<LineRenderer>();
        private float _beamTimer;
        private Vector3 _gravityPoint;
        private float _gravityTimer;

        public static void ResetStatics()
        {
            EnemyAccuracyScale = 1f;
            GhostActive = false;
        }

        public bool AnyUnlocked
        {
            get
            {
                for (int i = 0; i < Unlocked.Length; i++) if (Unlocked[i]) return true;
                return false;
            }
        }

        public bool Mercy { get { return Law == "Mercy"; } }
        public bool Power { get { return Law == "Power"; } }

        /// <summary>Crystal energy the Third Eye's bullet time burns per real second while it is open (less after the Law of Power).</summary>
        public float ThirdEyeDrain { get { return Power ? 9f : 12f; } }

        public void Unlock(int index)
        {
            if (index < 0 || index >= Unlocked.Length) return;
            Unlocked[index] = true;
            Selected = index;
        }

        public void AddMaxEnergy(float amount)
        {
            BaseMaxEnergy = Mathf.Min(200f, BaseMaxEnergy + amount);
            MaxEnergy = Mathf.Min(BaseMaxEnergy + 400f, MaxEnergy + amount);
            Energy = Mathf.Min(MaxEnergy, Energy + amount);
        }

        public void AddEnergy(float amount)
        {
            Energy = Mathf.Clamp(Energy + amount, 0f, MaxEnergy);
        }

        public float CooldownOf(int i)
        {
            return i >= 0 && i < _cooldown.Length ? _cooldown[i] : 0f;
        }

        // ------------------------------------------------------------------ update

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            for (int i = 0; i < _cooldown.Length; i++) if (_cooldown[i] > 0f) _cooldown[i] -= dt;
            PlayerController p = Game.Player;
            if (p == null) return;
            bool canAct = Game.IsPlaying && !GInput.GameplayBlocked && !p.IsDead;
            bool inVeil = Game.Inner != null && Game.Inner.IsInner;
            if (ThirdEyeActive)
            {
                Energy -= dt * ThirdEyeDrain;
                if (Energy <= 0f || !canAct || p.IsDead) SetThirdEye(false);
            }
            else if (!inVeil) Energy = Mathf.Min(MaxEnergy, Energy + dt * 2.2f * SkillMods.EnergyRegen);

            SyncHeroUnlocksH8S();
            if (canAct)
            {
                bool car = p.InVehicle;
                // Inside the Veil the hold always works, so a quest that carried a player without the Third Eye in can leave.
                if (ThirdEyeUnlocked || inVeil) UpdateThirdEyeInput(car, dt);
                else ResetEyeGesture();
                if (!car && Act.NextPower) CycleSpell(1);
                if (Act.PrevPower) CycleSpell(-1);
                if (car ? Act.VehiclePower : Act.CastPower) Cast(Selected);
            }
            else ResetEyeGesture();
            UpdateEffects(Time.deltaTime, dt);
        }

        /// <summary>
        /// Third Eye input: a tap toggles bullet time exactly as before (decided on release), holding for
        /// InnerWorld.HoldSeconds shifts through the Veil instead (and never also toggles bullet time).
        /// </summary>
        private void UpdateThirdEyeInput(bool car, float dt)
        {
            bool holdable;
            bool pressed = Act.ThirdEyePressed(car, out holdable);
            if (pressed && !holdable)
            {
                // Tap-only inputs (the D-pad tap in vehicles, Caps Lock on macOS legacy input) toggle at once.
                SetThirdEye(!ThirdEyeActive);
                return;
            }
            HoldGesture.Result r = _eyeGesture.Update(pressed, Act.ThirdEyeHeld(car), dt);
            if (r == HoldGesture.Result.Tap) SetThirdEye(!ThirdEyeActive);
            else if (r == HoldGesture.Result.Hold && Game.Inner != null) Game.Inner.PlayerShift();
            VeilHold = _eyeGesture.Progress;
        }

        private void ResetEyeGesture()
        {
            _eyeGesture.Reset();
            VeilHold = 0f;
        }

        public void SetThirdEye(bool on)
        {
            if (on && (Energy < 8f || !ThirdEyeUnlocked)) return;
            if (ThirdEyeActive == on) return;
            ThirdEyeActive = on;
            if (on)
            {
                GameTime.Request("thirdeye", Power ? 0.28f : 0.35f);
                if (Game.Audio != null) Game.Audio.Play2D(SfxId.Heartbeat, 0.7f);
            }
            else GameTime.Release("thirdeye");
        }

        private void CycleSpell(int dir)
        {
            if (!AnyUnlocked) return;
            for (int step = 1; step <= Spells.Length; step++)
            {
                int i = ((Selected + dir * step) % Spells.Length + Spells.Length) % Spells.Length;
                if (Unlocked[i])
                {
                    Selected = i;
                    if (Game.Hud != null) Game.Hud.Notify("Power: " + Spells[i].Name, 1.5f);
                    if (Game.Audio != null) Game.Audio.Play2D(SfxId.UIHover, 0.5f);
                    return;
                }
            }
        }

        public bool Cast(int index)
        {
            if (index < 0 || index >= Spells.Length || !Unlocked[index]) return false;
            Spell s = Spells[index];
            PlayerController p = Game.Player;
            if (p == null || p.IsDead || p.IsRagdolled) return false;
            if (_cooldown[index] > 0f) return false;
            // Skill trees and brews make powers cheaper (SkillMods.PowerCost).
            float cost = s.Cost * SkillMods.PowerCost;
            if (Energy < cost)
            {
                if (Game.Hud != null) Game.Hud.Notify("Not enough crystal energy.", 1.5f);
                return false;
            }
            bool ok;
            switch (index)
            {
                case 0: ok = EmberChain(p); break;
                case 1: ok = QuartzWard(p); break;
                case 2: ok = MirageStep(p); break;
                case 3: ok = StaticLance(p); break;
                case 4: ok = GravityWell(p); break;
                case 5:
                    GlassSightTimer = 12f;
                    RevealTimer = 12f;
                    ok = true;
                    // Resonance: Glass Sight grows a crystal shell over the nearest hostiles in front of you.
                    ResonanceSystem.PrimeAround(p.transform.position, ResonanceRules.GlassSightRange, ResonanceRules.GlassSightMarks, ResonanceState.Glassbound, true);
                    break;
                case 6: _mendTimer = 3f; p.Health.Burning = 0f; ok = true; break;
                case 7: ok = RocCall(p); break;
                case 8: _ghostTimer = 20f; ok = true; break;
                case 9: ok = DriveSigil(p); break;
                case ShardGazeIndex: ok = !p.InVehicle && p.BeginBeamH8S(); break;
                default: ok = Sunbolt(p); break;
            }
            if (!ok) return false;
            Energy -= cost;
            _cooldown[index] = s.Cooldown;
            if (Game.Audio != null) Game.Audio.Play2D(index == 1 ? SfxId.Shield : SfxId.Power, 0.65f);
            if (Game.Fx != null) Game.Fx.Flash(p.ChestPosition, s.Color, 3f, 8f, 0.3f);
            Threats.Report(p.transform.position, 25f, p.gameObject, ThreatKind.Power);
            return true;
        }

        // ------------------------------------------------------------------ spells

        private bool Sunbolt(PlayerController p)
        {
            Vector3 origin = p.InVehicle ? p.CurrentVehicle.transform.position + Vector3.up * (p.CurrentVehicle.Def.Height + 0.5f) : p.ChestPosition + p.transform.forward * 0.6f + Vector3.up * 0.2f;
            Vector3 dir = (p.AimPoint - origin).normalized;
            // Like every other damaging power: Focus, gear and the skill trees (PowerScale). The bolt applies no bonus of its own.
            float dmg = (Power ? 190f : 130f) * PowerScale;
            float radius = Power ? 6f : 4.5f;
            Projectile.Sunbolt(origin + dir * 0.5f, dir, dmg, radius, p.gameObject);
            if (p.Anim != null) p.Anim.ThrowT = 0.5f;
            return true;
        }

        private bool EmberChain(PlayerController p)
        {
            List<CharacterBase> targets = FindHostiles(p.transform.position, 28f, 4, true);
            if (targets.Count == 0)
            {
                if (Game.Hud != null) Game.Hud.Notify("No hostiles in range.", 1.2f);
                return false;
            }
            Vector3 from = p.ChestPosition;
            foreach (CharacterBase t in targets)
            {
                DamageInfo info = DamageInfo.Make((Power ? 70f : 50f) * PowerScale, t.ChestPosition, t.ChestPosition - from, p.gameObject, DamageType.Fire);
                t.Health.TakeDamage(info);
                ResonanceSystem.PrimeSpell(t, Spells[0].Id);
                Beam(from, t.ChestPosition, Spells[0].Color);
                from = t.ChestPosition;
                if (Game.Fx != null) for (int i = 0; i < 6; i++) Game.Fx.FirePuff(t.ChestPosition + Random.insideUnitSphere * 0.4f, 0.6f);
            }
            return true;
        }

        private bool QuartzWard(PlayerController p)
        {
            _wardTimer = Mercy ? 10f : 6f;
            p.Health.Invulnerable = true;
            if (_wardFx == null)
            {
                _wardFx = Shapes.MeshObject("Quartz Ward", Shapes.SphereMesh, new[] { Mats.Marker(new Color(0.45f, 0.65f, 1f, 0.22f)) }, p.transform, false);
                _wardFx.transform.localPosition = new Vector3(0f, 1f, 0f);
                _wardFx.transform.localScale = Vector3.one * 2.4f;
            }
            _wardFx.SetActive(true);
            return true;
        }

        private bool MirageStep(PlayerController p)
        {
            if (p.InVehicle) return false;
            Vector3 dir = Game.Cam != null ? Game.Cam.FlatForward : p.transform.forward;
            Vector3 start = p.transform.position;
            float dist = 9f;
            RaycastHit hit;
            int mask = Layers.WorldMask | (1 << Layers.VehicleShell) | (1 << Layers.Debris);
            if (Physics.SphereCast(start + Vector3.up * 1f, 0.35f, dir, out hit, dist, mask, QueryTriggerInteraction.Ignore)) dist = Mathf.Max(0f, hit.distance - 0.4f);
            if (dist < 1f) return false;
            Vector3 dest = start + dir * dist;
            RaycastHit g;
            if (Physics.Raycast(dest + Vector3.up * 2f, Vector3.down, out g, 6f, mask, QueryTriggerInteraction.Ignore)) dest = g.point + Vector3.up * 0.05f;
            if (Game.Fx != null)
            {
                for (int i = 0; i < 12; i++) Game.Fx.Smoke(start + Vector3.up * Random.Range(0.2f, 1.8f), Random.insideUnitSphere, 0.8f, 0.85f);
                Game.Fx.Flash(dest + Vector3.up, Spells[2].Color, 2.5f, 6f, 0.25f);
            }
            p.Teleport(dest, p.transform.eulerAngles.y);
            if (Game.Wanted != null) Game.Wanted.SuppressSightings(2.5f);
            if (Game.Audio != null) Game.Audio.PlayAt(SfxId.Whoosh, dest, 0.7f);
            return true;
        }

        private bool StaticLance(PlayerController p)
        {
            Vector3 from = p.InVehicle ? p.CurrentVehicle.transform.position + Vector3.up * (p.CurrentVehicle.Def.Height + 0.3f) : p.ChestPosition + p.transform.forward * 0.4f;
            Vector3 to = p.AimPoint;
            Vector3 dir = (to - from).normalized;
            RaycastHit hit;
            if (Physics.Raycast(from, dir, out hit, 120f, Layers.ShootMask, QueryTriggerInteraction.Ignore))
            {
                to = hit.point;
                Vehicle v = hit.collider.attachedRigidbody != null ? hit.collider.attachedRigidbody.GetComponent<Vehicle>() : null;
                if (v != null && v != p.CurrentVehicle)
                {
                    v.ApplyDamage(140f, p.gameObject);
                    v.EngineRunning = false;
                    StalledEngine st = v.gameObject.GetComponent<StalledEngine>();
                    if (st == null) st = v.gameObject.AddComponent<StalledEngine>();
                    st.Remaining = 8f;
                }
                else
                {
                    CharacterBase c = hit.collider.GetComponentInParent<CharacterBase>();
                    if (c != null && c != p && !c.IsDead)
                    {
                        DamageInfo info = DamageInfo.Make((Power ? 90f : 65f) * PowerScale, hit.point, dir, p.gameObject, DamageType.Power);
                        info.Force = 6f;
                        c.Health.TakeDamage(info);
                        ResonanceSystem.PrimeSpell(c, Spells[3].Id);
                        c.Knockdown(dir * 4f + Vector3.up * 2f, hit.point, 2.5f);
                    }
                    else
                    {
                        IDamageable d = hit.collider.GetComponentInParent<IDamageable>();
                        if (d != null) d.TakeDamage(DamageInfo.Make(80f * PowerScale, hit.point, dir, p.gameObject, DamageType.Power));
                    }
                }
                if (Game.Fx != null) Game.Fx.Sparks(hit.point, hit.normal * 4f, 30);
            }
            else to = from + dir * 120f;
            Beam(from, to, Spells[3].Color);
            if (Game.Audio != null) Game.Audio.PlayAt(SfxId.Thunder, to, 0.5f, 1.8f, 120f);
            return true;
        }

        private bool GravityWell(PlayerController p)
        {
            Vector3 point = p.AimPoint;
            if (Vector3.Distance(point, p.transform.position) > 45f) point = p.transform.position + (point - p.transform.position).normalized * 45f;
            _gravityPoint = point;
            _gravityTimer = 1.6f;
            int n = Physics.OverlapSphereNonAlloc(point, 10f, _buf, (1 << Layers.Characters) | (1 << Layers.Ragdoll), QueryTriggerInteraction.Ignore);
            HashSet<CharacterBase> done = new HashSet<CharacterBase>();
            for (int i = 0; i < n; i++)
            {
                CharacterBase c = _buf[i].GetComponentInParent<CharacterBase>();
                if (c == null || c == p || c.InVehicle || !done.Add(c)) continue;
                Vector3 pull = (point - c.transform.position);
                c.Knockdown(pull.normalized * Mathf.Min(9f, pull.magnitude * 1.2f) + Vector3.up * 3f, c.ChestPosition, 2f);
                if (c.IsHostileTo())
                {
                    c.Health.TakeDamage(DamageInfo.Make(25f * PowerScale, c.ChestPosition, pull, p.gameObject, DamageType.Power));
                    ResonanceSystem.PrimeSpell(c, Spells[4].Id);
                }
            }
            // 04E: the Underheart boss is not on the Characters layer; it takes the damage and the mark but is never pulled.
            UBossLockProxy04E boss = UBossLockProxy04E.Active;
            if (boss != null && !done.Contains(boss) && (boss.transform.position - point).sqrMagnitude <= 100f)
            {
                Vector3 toBoss = point - boss.transform.position;
                boss.Health.TakeDamage(DamageInfo.Make(25f * PowerScale, boss.ChestPosition, toBoss, p.gameObject, DamageType.Power));
                ResonanceSystem.PrimeSpell(boss, Spells[4].Id);
            }
            return true;
        }

        private bool RocCall(PlayerController p)
        {
            List<CharacterBase> targets = FindHostiles(p.transform.position, 45f, 12, false);
            foreach (CharacterBase t in targets)
            {
                t.Health.TakeDamage(DamageInfo.Make((Power ? 45f : 32f) * PowerScale, t.ChestPosition, Vector3.down, p.gameObject, DamageType.Power));
                ResonanceSystem.PrimeSpell(t, Spells[7].Id);
                t.Knockdown(Random.insideUnitSphere * 3f + Vector3.up * 2f, t.ChestPosition, 1.5f);
            }
            RevealTimer = 12f;
            if (Game.Audio != null) Game.Audio.Play2D(SfxId.WindLoop, 0.8f, 1.4f);
            if (Game.Cam != null) Game.Cam.Shake(0.35f);
            if (Game.Hud != null) Game.Hud.Notify(targets.Count > 0 ? "The Roc strikes " + targets.Count + " hostiles." : "The Roc circles the block. No hostiles nearby.", 2f);
            return true;
        }

        private bool DriveSigil(PlayerController p)
        {
            if (!p.InVehicle)
            {
                if (Game.Hud != null) Game.Hud.Notify("Drive Sigil needs a vehicle.", 1.5f);
                return false;
            }
            Vehicle v = p.CurrentVehicle;
            v.BoostForce = 1.6f;
            v.BoostGrip = 1.3f;
            v.BoostArmor = 0.3f;
            v.BoostTimer = 14f;
            return true;
        }

        private List<CharacterBase> FindHostiles(Vector3 center, float radius, int max, bool frontOnly)
        {
            List<CharacterBase> list = new List<CharacterBase>();
            Vector3 fwd = Game.Cam != null ? Game.Cam.FlatForward : Vector3.forward;
            foreach (NpcController n in NpcController.All)
            {
                if (n == null || n.IsDead || !n.IsHostileToPlayer) continue;
                Vector3 to = n.transform.position - center;
                if (to.sqrMagnitude > radius * radius) continue;
                if (frontOnly && Vector3.Dot(MathUtil.Flat(to).normalized, fwd) < 0.2f) continue;
                list.Add(n);
            }
            // Creatures are fair game for every spell (a burrowed wyrm or a stealthed shade is out of reach).
            for (int i = 0; i < Creature.All.Count; i++)
            {
                Creature c = Creature.All[i];
                if (c == null || !c.Targetable) continue;
                Vector3 to = c.transform.position - center;
                float r = radius + c.Radius;
                if (to.sqrMagnitude > r * r) continue;
                if (frontOnly && Vector3.Dot(MathUtil.Flat(to).normalized, fwd) < 0.2f) continue;
                list.Add(c);
            }
            // 04E: the running Underheart boss, through its lock stand-in. Its Health is the boss body's own, so damage
            // still passes the fight's filter, and Knockdown does nothing on it (it has no rig).
            UBossLockProxy04E boss = UBossLockProxy04E.Active;
            if (boss != null)
            {
                Vector3 tb = boss.transform.position - center;
                if (tb.sqrMagnitude <= radius * radius && (!frontOnly || Vector3.Dot(MathUtil.Flat(tb).normalized, fwd) >= 0.2f)) list.Add(boss);
            }
            list.Sort((a, b) => (a.transform.position - center).sqrMagnitude.CompareTo((b.transform.position - center).sqrMagnitude));
            if (list.Count > max) list.RemoveRange(max, list.Count - max);
            return list;
        }

        private void Beam(Vector3 a, Vector3 b, Color c)
        {
            LineRenderer lr = null;
            foreach (LineRenderer l in _beams) if (!l.enabled) { lr = l; break; }
            if (lr == null)
            {
                GameObject go = new GameObject("Spell Beam");
                go.transform.SetParent(transform, false);
                lr = go.AddComponent<LineRenderer>();
                lr.positionCount = 2;
                lr.useWorldSpace = true;
                lr.widthMultiplier = 0.12f;
                lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                _beams.Add(lr);
            }
            lr.sharedMaterial = Mats.Glow(c);
            lr.SetPosition(0, a);
            lr.SetPosition(1, b);
            lr.enabled = true;
            _beamTimer = 0.25f;
        }

        private void UpdateEffects(float dt, float unscaled)
        {
            PlayerController p = Game.Player;
            if (_beamTimer > 0f)
            {
                _beamTimer -= unscaled;
                if (_beamTimer <= 0f) foreach (LineRenderer l in _beams) l.enabled = false;
            }
            if (_wardTimer > 0f)
            {
                _wardTimer -= dt;
                if (_wardTimer <= 0f)
                {
                    if (p != null && !Game.Root.GodMode) p.Health.Invulnerable = false;
                    if (_wardFx != null) _wardFx.SetActive(false);
                }
            }
            if (_mendTimer > 0f && p != null)
            {
                _mendTimer -= dt;
                p.Health.Heal(dt * (Mercy ? 35f : 24f));
                if (Game.Fx != null && Random.value < 0.3f) Game.Fx.Splash(p.transform.position + Vector3.up * 1.5f, 0.15f);
            }
            if (_ghostTimer > 0f) _ghostTimer -= dt;
            GhostActive = _ghostTimer > 0f;
            EnemyAccuracyScale = GhostActive ? 2.6f : 1f;
            if (GlassSightTimer > 0f) GlassSightTimer -= dt;
            if (RevealTimer > 0f) RevealTimer -= dt;
            if (_gravityTimer > 0f)
            {
                _gravityTimer -= dt;
                if (Game.Fx != null) Game.Fx.Sparks(_gravityPoint + Random.insideUnitSphere * 3f, (_gravityPoint - Random.insideUnitSphere * 3f).normalized, 3);
                int n = Physics.OverlapSphereNonAlloc(_gravityPoint, 12f, _buf, (1 << Layers.Vehicles) | (1 << Layers.Debris) | (1 << Layers.Ragdoll), QueryTriggerInteraction.Ignore);
                for (int i = 0; i < n; i++)
                {
                    Rigidbody rb = _buf[i].attachedRigidbody;
                    if (rb == null || rb.isKinematic) continue;
                    if (p != null && p.InVehicle && rb == p.CurrentVehicle.Body) continue;
                    Vector3 to = _gravityPoint + Vector3.up * 1.5f - rb.worldCenterOfMass;
                    rb.AddForce(to.normalized * Mathf.Min(14f, to.magnitude * 3f), ForceMode.Acceleration);
                }
            }
        }

        public void OnKill()
        {
            // Conduit adds to the energy every takedown restores (SkillMods.KillEnergy).
            AddEnergy(6f + SkillMods.KillEnergy);
        }
    }

    /// <summary>Restarts an engine stalled by Static Lance.</summary>
    public sealed class StalledEngine : MonoBehaviour
    {
        public float Remaining = 8f;

        private void Update()
        {
            Remaining -= Time.deltaTime;
            if (Remaining > 0f) return;
            Vehicle v = GetComponent<Vehicle>();
            if (v != null && !v.IsDestroyed) v.EngineRunning = true;
            Destroy(this);
        }
    }

    public static class CharacterExtensions
    {
        public static bool IsHostileTo(this CharacterBase c)
        {
            NpcController n = c as NpcController;
            if (n != null) return n.IsHostileToPlayer;
            Creature cr = c as Creature;
            return cr != null && cr.IsHostileToPlayer;
        }
    }
}
