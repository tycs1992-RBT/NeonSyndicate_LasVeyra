using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// 04E: the ward. The raised handheld Lattice Shield (Buckler) becomes a directional defense in the shared player
    /// damage path (PlayerDamageHooks, key "ward04e", order 100): hits from about 70 degrees each side of its facing are
    /// taken off its capacity (blasts by angle share), the physical bullet face and the hook drain the same pool (one hit,
    /// one drain: the face takes bullets that physically stop on it, never blasts, which the hook shares by angle), a
    /// break shatters it and it recovers on the gadget's own cooldown. Aspects: Ember Mantle (bounded heat buffer),
    /// Frostglass (deflection window), Volt Lattice (controlled discharge from blocks), Void Screen (repositioning window
    /// on break, longer recovery). Nothing is ever reflected back, and the ward drains by absorbed amount, so beams that
    /// arrive as ticks are integrated by time. Numbers in WardRules04E and BuildStatRules04E.
    /// </summary>
    public sealed partial class GadgetSystem
    {
        public const string WardHookKey = "ward04e";
        public const int WardHookOrder = 100;

        private BarrierUnit _ward;
        private bool _wardBrokeLast;
        private WardHeatBuffer _heat;
        private readonly VoltLatticeCharge _volt = new VoltLatticeCharge();
        private float _voidUntil = -99f;
        private float _voidPhaseUntil = -99f;
        private float _wardPingAt;
        private float _deflectTextAt;
        private float _heatFxAt;
        private float _heatMaxClock;
        private float _voidFxAt;
        private string _wardLastEvent = "";

        // ------------------------------------------------------------------ queries (HUD, BUILD page, other systems)

        /// <summary>True while a raised ward (handheld Buckler) stands in front of the player.</summary>
        public bool WardRaised { get { return _ward != null && !_ward.Done && _ward.Go != null; } }

        /// <summary>True during a Void Screen repositioning window (faster on foot; read by GearSystem.MoveSpeedMultiplier).</summary>
        public bool VoidWindowActive { get { return Time.time < _voidUntil; } }

        /// <summary>The gadget slot holding the Buckler, or -1.</summary>
        public int WardSlot { get { return Array.IndexOf(Slots, WardRules04E.BucklerId); } }

        public WardState04E WardStateNow
        {
            get
            {
                int slot = WardSlot;
                return WardRules04E.StateOf(IsUnlocked(WardRules04E.BucklerId), slot >= 0, WardRaised, slot >= 0 && Cooldowns[slot].Ready, _wardBrokeLast);
            }
        }

        /// <summary>Capacity left on the raised ward (0 when lowered).</summary>
        public float WardCapacityNow { get { return WardRaised ? _ward.Hp : 0f; } }

        /// <summary>The raised ward's full capacity, or what raising it from its slot would give.</summary>
        public float WardCapacityMax { get { return WardRaised ? _ward.MaxHp : WardCapacityFor(Mathf.Max(0, WardSlot)); } }

        /// <summary>Seconds until the ward can be raised again (0 when ready or not slotted).</summary>
        public float WardCooldownLeft { get { int s = WardSlot; return s >= 0 ? Cooldowns[s].Remaining : 0f; } }

        /// <summary>The ward is reachable only through the early entitlement (the modest variant applies below level 11).</summary>
        public bool WardIsStarterVariant
        {
            get { return !GadgetRules.IsUnlocked(WardRules04E.BucklerId, PlayerLevel, AllUnlocked) && WardRules04E.StarterVariant(PlayerLevel); }
        }

        /// <summary>Ward capacity if raised from a slot now (level, that slot's gadget power, starter variant, Kiln Crown).</summary>
        public float WardCapacityFor(int slot)
        {
            bool crown = Game.Builds != null && Game.Builds.KilnCrown;
            bool earlyOnly = !GadgetRules.IsUnlocked(WardRules04E.BucklerId, PlayerLevel, AllUnlocked);
            return WardRules04E.Capacity(PlayerLevel, PowerBonus(slot), earlyOnly, crown);
        }

        public float HeatBufferFraction { get { return _heat != null ? _heat.Fraction : 1f; } }
        public float HeatBufferValue { get { return _heat != null ? _heat.Value : WardRules04E.MantleMax(PlayerLevel); } }
        public float VoltChargeFraction { get { return _volt.Fraction; } }

        private WardHeatBuffer Heat
        {
            get
            {
                if (_heat == null) _heat = new WardHeatBuffer(WardRules04E.MantleMax(PlayerLevel));
                return _heat;
            }
        }

        private static string ActiveAspect { get { return Game.Builds != null && Game.Builds.WardAvailable ? Game.Builds.ActiveAspect : ""; } }

        // ------------------------------------------------------------------ lifecycle hooks

        /// <summary>The starter ward entitlement changed (granted, rolled back, new game or load): re-check unlocks now.</summary>
        public void OnWardEntitled()
        {
            int buckler = WardRules04E.BucklerId;
            if (!IsUnlocked(buckler))
            {
                // A rolled-back or reset entitlement takes the Buckler out of its slot again (level unlocks are untouched).
                for (int i = 0; i < Slots.Length; i++) if (Slots[i] == buckler) Slots[i] = -1;
                _announced.Remove(buckler);
                RaiseChanged();
            }
            _unlockTimer = 0f;
        }

        /// <summary>The active aspect changed (never while raised): the Volt charge starts over; the heat buffer keeps its value.</summary>
        public void OnAspectChanged()
        {
            _volt.Reset();
        }

        private void ResetWard()
        {
            _ward = null;
            _wardBrokeLast = false;
            _heat = null;
            _volt.Reset();
            _voidUntil = -99f;
            _voidPhaseUntil = -99f;
            _wardLastEvent = "";
        }

        private void AnnounceStarterWard()
        {
            int slot = WardSlot;
            string how = slot >= 0
                ? "It is in gadget slot " + (slot + 1) + ": hold " + KeyLabel(slot) + " to raise it, let go to keep it up for its duration."
                : "Put it in a gadget slot in the pause menu's GADGETS tab, then hold that slot's key to raise it.";
            Game.Hud.Banner("STARTER WARD", "Lattice Shield (Buckler). " + how + " Watch its capacity bar; it breaks and recovers on its cooldown.", GadgetRules.Get(WardRules04E.BucklerId).Color, 6f);
            GadgetSounds.Play2D(GadgetSounds.Clip.ShieldUp, 0.55f, 1.1f);
        }

        /// <summary>Builds and raises the ward from a slot (called by Spawn for the handheld Buckler).</summary>
        private void SpawnWard(PlayerController p, GadgetSpec spec, int slot, Vector3 pos, Vector3 fwd, float power, float life, float radius, float reach)
        {
            string aspect = ActiveAspect;
            bool crown = Game.Builds != null && Game.Builds.KilnCrown;
            bool halo = Game.Builds != null && Game.Builds.WardVisualOn;
            GameObject face;
            Renderer panel;
            Material panelMat;
            Transform spin;
            GameObject go = GadgetModels.WardBuckler(aspect, crown, halo, out face, out panel, out panelMat, out spin);
            go.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(fwd));
            float hp = WardCapacityFor(slot);
            BarrierUnit u = new BarrierUnit { Handheld = true, Ward = true, Hp = hp, MaxHp = hp, Panel = panel, PanelMat = panelMat, Spin = spin, Aspect = aspect };
            if (aspect == WardRules04E.Frostglass) u.DeflectUntil = Time.time + WardRules04E.DeflectWindow;
            Add(u, spec, slot, go, power, life, radius, reach);
            GadgetShieldFace f = face.AddComponent<GadgetShieldFace>();
            f.OnHit = u.Absorb;
            f.Alive = () => !u.Done && u.Hp > 0f;
            _ward = u;
            _wardBrokeLast = false;
            _volt.Reset();
            GadgetSounds.Play(GadgetSounds.Clip.ShieldUp, pos + Vector3.up, 0.6f, 1.15f, 0.6f);
            if (aspect == WardRules04E.Frostglass && Game.Hud != null) Game.Hud.CombatText("Deflect window", WardColor(aspect));
            _wardLastEvent = "Raised (" + Mathf.RoundToInt(hp) + " capacity" + (aspect.Length > 0 ? ", " + WardRules04E.AspectTitle(aspect) : "") + ")";
        }

        public static Color WardColor(string aspect)
        {
            switch (aspect)
            {
                case WardRules04E.EmberMantle: return new Color(1f, 0.5f, 0.18f);
                case WardRules04E.Frostglass: return new Color(0.7f, 0.95f, 1f);
                case WardRules04E.VoltLattice: return new Color(0.4f, 0.65f, 1f);
                case WardRules04E.VoidScreen: return new Color(0.75f, 0.4f, 1f);
                default: return new Color(0.35f, 0.65f, 1f);
            }
        }

        // ------------------------------------------------------------------ the shared damage path

        /// <summary>Hits from the player themself, their companions and allies are never taken by the ward.</summary>
        private static bool Friendly(DamageInfo info)
        {
            if (info.FromPlayer || info.Origin != DamageOrigin.Player) return true;
            if (info.Source == null) return false;
            int gang;
            Team t = TeamTag.Of(info.Source, out gang);
            return t == Team.Player || t == Team.Companion;
        }

        /// <summary>
        /// The ward's player damage modifier. Order inside: a Void Screen phase lets the hit pass through you; heat rules
        /// (Coolant Draught, Ember Mantle buffer) for fire; then a raised ward intercepts by angle.
        /// </summary>
        private bool WardHook(ref DamageInfo info)
        {
            if (!(info.Amount > 0f) || Friendly(info)) return true;
            float now = Time.time;
            if (now < _voidPhaseUntil && WardRules04E.Interceptable(info.Type))
            {
                info.Amount = 0f;
                return false;
            }
            bool mantle = Game.Builds != null && Game.Builds.EmberMantleActive;
            if (info.Type == DamageType.Fire)
            {
                bool coolant = Game.Crafting != null && Game.Crafting.BuffLeft(RewardCatalog04E.RecipeCoolant) > 0f;
                if (coolant || mantle)
                {
                    float paid;
                    info.Amount = BuildStatRules04E.ApplyHeat(info.Amount, coolant, mantle ? Heat : null, out paid);
                    if (paid > 0f) HeatFx(info.Point);
                }
            }
            if (!WardRaised || !(info.Amount > 0f)) return info.Amount > 0f;
            Vector3 toAttacker = -info.Direction;
            toAttacker.y = 0f;
            bool hasDir = toAttacker.sqrMagnitude > 0.01f;
            Vector3 fwd = _ward.Go.transform.forward;
            fwd.y = 0f;
            float angle = hasDir && fwd.sqrMagnitude > 1e-4f ? Vector3.Angle(fwd, toAttacker) : 0f;
            float share = WardRules04E.InterceptShare(info.Type, angle, hasDir, (info.Flags & DamageFlags.Unblockable) != 0);
            if (share <= 0f) return true;
            bool deflect = now < _ward.DeflectUntil;
            float drain = info.Type == DamageType.Fire && mantle ? WardRules04E.MantleWardFireDrain : 1f;
            WardHitResult04E r = WardRules04E.Absorb(_ward.Hp, info.Amount, share, drain, deflect);
            _ward.Hp = r.CapacityLeft;
            info.Amount = r.Remaining;
            if (r.Absorbed > 0f) info.Flags &= ~DamageFlags.Knockdown;
            Vector3 at = info.Point.sqrMagnitude > 0f ? info.Point : _ward.Go.transform.position + Vector3.up;
            AfterWardHit(r, at, info.Type == DamageType.Melee);
            if (r.Broke && BreakWard(at)) info.Amount = 0f;
            return info.Amount > 0f;
        }

        /// <summary>A projectile that physically stopped on the ward's bullet face (the player is never also hit by it).</summary>
        private void WardFaceHit(BarrierUnit u, DamageInfo info)
        {
            if (u.Done || info.FromPlayer || !(info.Amount > 0f)) return;
            // Blasts reach the player too; the damage hook takes them by angle share, so the face never also drains for them.
            if (info.Type == DamageType.Explosion) return;
            if (u != _ward) return;
            bool mantle = Game.Builds != null && Game.Builds.EmberMantleActive;
            float drain = info.Type == DamageType.Fire && mantle ? WardRules04E.MantleWardFireDrain : 1f;
            WardHitResult04E r = WardRules04E.Absorb(u.Hp, info.Amount, 1f, drain, Time.time < u.DeflectUntil);
            u.Hp = r.CapacityLeft;
            AfterWardHit(r, info.Point, false);
            if (r.Broke || u.Hp <= 0f) BreakWard(info.Point);
        }

        private void AfterWardHit(WardHitResult04E r, Vector3 at, bool melee)
        {
            PlayerController p = Game.Player;
            if (p != null && p.Anim != null) p.Anim.GuardHit = Mathf.Max(p.Anim.GuardHit, melee ? 0.8f : 0.45f);
            Color c = WardColor(_ward != null ? _ward.Aspect : "");
            if (r.Deflected)
            {
                if (Game.Fx != null) Game.Fx.Flash(at, c, 2.5f, 4f, 0.15f);
                if (Game.Hud != null && Time.unscaledTime - _deflectTextAt > 0.5f)
                {
                    _deflectTextAt = Time.unscaledTime;
                    Game.Hud.CombatText("Deflect", c);
                }
                GadgetSounds.Play(GadgetSounds.Clip.ShieldHit, at, 0.5f, 1.4f, 1f);
                return;
            }
            if (r.Absorbed <= 0f) return;
            if (Game.Fx != null) Game.Fx.Sparks(at, Vector3.up * 1.5f, melee ? 8 : 4);
            if (Time.time - _wardPingAt > 0.08f)
            {
                _wardPingAt = Time.time;
                GadgetSounds.Play(GadgetSounds.Clip.ShieldHit, at, 0.45f, UnityEngine.Random.Range(0.9f, 1.15f), 1f);
            }
            if (Game.Builds != null && Game.Builds.VoltLatticeActive && _volt.Feed(r.Absorbed, Time.time)) VoltDischarge();
        }

        /// <summary>Capacity ran out: the ward shatters and recovers on its cooldown. True when Void Screen negated the overflow.</summary>
        private bool BreakWard(Vector3 at)
        {
            if (_ward == null || _ward.Done) return false;
            BarrierUnit u = _ward;
            u.Hp = 0f;
            u.Broken = true;
            u.Done = true;
            _wardBrokeLast = true;
            _volt.Reset();
            Vector3 c = u.Go != null ? u.Go.transform.position + Vector3.up * 0.9f : at;
            if (Game.Fx != null)
            {
                Game.Fx.Sparks(c, Vector3.up * 3f, 30);
                Game.Fx.Flash(c, WardColor(u.Aspect), 4f, 6f, 0.3f);
            }
            GadgetSounds.Play(GadgetSounds.Clip.ShieldBreak, c, 0.7f, 1f, 0.8f);
            _wardLastEvent = "Broken at " + Time.time.ToString("0.0");
            if (Game.Builds != null && Game.Builds.VoidScreenActive)
            {
                float now = Time.time;
                _voidUntil = now + WardRules04E.VoidWindow;
                _voidPhaseUntil = now + WardRules04E.VoidPhase;
                if (u.Slot >= 0 && u.Slot < Cooldowns.Length) Cooldowns[u.Slot].Extend(WardRules04E.VoidCooldownCost);
                if (Game.Hud != null) Game.Hud.CombatText("Void Screen: move!", WardColor(WardRules04E.VoidScreen));
                GadgetSounds.Play(GadgetSounds.Clip.Pulse, c, 0.6f, 0.6f, 0.6f);
                return true;
            }
            if (Game.Hud != null) Game.Hud.CombatText("Ward broken", new Color(1f, 0.5f, 0.4f));
            return false;
        }

        /// <summary>Volt Lattice: one discharge into up to three visible foes in front of the ward (shared damage API, the player as source).</summary>
        private void VoltDischarge()
        {
            if (_ward == null || _ward.Go == null) return;
            Vector3 c = _ward.Go.transform.position + Vector3.up * 0.9f;
            Vector3 fwd = _ward.Go.transform.forward;
            Hostiles(c, WardRules04E.VoltRange, false, _near);
            float dmg = WardRules04E.VoltDamage(PlayerLevel);
            int hits = 0;
            Color col = WardColor(WardRules04E.VoltLattice);
            _near.Sort((a, b) => (a.transform.position - c).sqrMagnitude.CompareTo((b.transform.position - c).sqrMagnitude));
            for (int i = 0; i < _near.Count && hits < WardRules04E.VoltTargets; i++)
            {
                CharacterBase t = _near[i];
                Vector3 chest = t.ChestPosition;
                if (!GadgetRules.InCone(c, fwd, WardRules04E.HalfAngle, WardRules04E.VoltRange + 1f, chest)) continue;
                if (!Clear(c, chest)) continue;
                Hit(t, dmg, c, DamageType.Power, 25f);
                hits++;
                if (Game.Fx != null)
                {
                    Game.Fx.Tracer(c, chest);
                    Game.Fx.Flash(chest, col, 2f, 4f, 0.12f);
                }
            }
            if (Game.Fx != null) Game.Fx.Flash(c, col, 3f, 5f, 0.2f);
            GadgetSounds.Play(GadgetSounds.Clip.Zap, c, 0.6f, 0.85f, 1f);
            if (Game.Hud != null) Game.Hud.CombatText(hits > 0 ? "Volt discharge x" + hits : "Volt discharge", col);
            _wardLastEvent = "Volt discharge (" + hits + " hit)";
        }

        private void HeatFx(Vector3 at)
        {
            if (Time.time - _heatFxAt < 0.3f) return;
            _heatFxAt = Time.time;
            PlayerController p = Game.Player;
            Vector3 c = p != null ? p.ChestPosition : at;
            if (Game.Fx != null) Game.Fx.Flash(c, WardColor(WardRules04E.EmberMantle), 1.2f, 2.5f, 0.12f);
        }

        // ------------------------------------------------------------------ frame

        private void UpdateWard(PlayerController p, float dt)
        {
            if (_ward != null && (_ward.Done || _ward.Go == null))
            {
                _wardBrokeLast = _ward.Broken;
                if (!_ward.Broken) _wardLastEvent = "Ended (time)";
                _ward = null;
                _volt.Reset();
            }
            WardHeatBuffer h = Heat;
            _heatMaxClock -= dt;
            if (_heatMaxClock <= 0f)
            {
                _heatMaxClock = 0.5f;
                h.SetMax(WardRules04E.MantleMax(PlayerLevel));
            }
            h.Tick(dt);
            if (p == null || p.IsDead) return;
            if (Game.Builds != null && Game.Builds.EmberMantleActive && p.Health.Burning > 0f)
            {
                float cut = h.Smother(p.Health.Burning, dt);
                if (cut > 0f) p.Health.Burning = Mathf.Max(0f, p.Health.Burning - cut);
            }
            if (VoidWindowActive && Game.Fx != null && Time.time - _voidFxAt > 0.12f)
            {
                _voidFxAt = Time.time;
                Game.Fx.Smoke(p.transform.position + Vector3.up * 0.3f, Vector3.up * 0.4f, 1.2f, 0.35f);
            }
        }

        /// <summary>The ward's visuals while raised: panel brightness follows capacity, the aspect parts move.</summary>
        private void WardVisualTick(BarrierUnit u, float dt)
        {
            float frac = u.MaxHp > 0f ? Mathf.Clamp01(u.Hp / u.MaxHp) : 0f;
            if (u.PanelMat != null)
            {
                Color c = WardColor(u.Aspect);
                c.a = Mathf.Lerp(0.1f, 0.4f, frac);
                u.PanelMat.color = c;
            }
            if (u.Spin != null)
            {
                switch (u.Aspect)
                {
                    case WardRules04E.VoidScreen: u.Spin.Rotate(0f, 0f, 90f * dt, Space.Self); break;
                    case WardRules04E.VoltLattice: u.Spin.localScale = Vector3.one * (1f + 0.08f * Mathf.Sin(Time.time * 18f) * _volt.Fraction); break;
                    case WardRules04E.EmberMantle: u.Spin.localScale = new Vector3(1f, 1f + 0.06f * Mathf.Sin(Time.time * 4f), 1f); break;
                    default: u.Spin.Rotate(0f, 0f, 20f * dt, Space.Self); break;
                }
            }
        }

        // ------------------------------------------------------------------ rollback and debug

        /// <summary>Takes back a mod a reward just added (commit-or-rollback of a failed claim): no Lumen is paid.</summary>
        public bool RemoveModForRollback(GadgetModSave m)
        {
            if (m == null || !Mods.Contains(m)) return false;
            for (int i = 0; i < SlotMods.Length; i++) if (SlotMods[i] == m) SlotMods[i] = null;
            Mods.Remove(m);
            RaiseChanged();
            return true;
        }

        public string WardDebugLine()
        {
            return "Ward: " + WardRules04E.StateLabel(WardStateNow) + ", slot " + (WardSlot + 1) + ", capacity " + Mathf.RoundToInt(WardCapacityNow) + " / " + Mathf.RoundToInt(WardCapacityMax) +
                   (WardIsStarterVariant ? " (starter variant)" : "") + ", cooldown " + WardCooldownLeft.ToString("0.0") + " s. Heat buffer " + Mathf.RoundToInt(HeatBufferValue) +
                   ", Volt charge " + Mathf.RoundToInt(_volt.Charge) + ", Void window " + (VoidWindowActive ? "open" : "closed") + ". Hook " + WardHookKey + " (order " + WardHookOrder + "), " +
                   PlayerDamageHooks.Count + " player damage hook(s). Last: " + _wardLastEvent;
        }
    }
}
