using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The cosmetic chassis trims (real geometry under the bike's lean pivot, no stats) and the one handling tune.
    /// Reward catalogs offer these ids; ids are stable (saves remember them), so append only.
    /// </summary>
    public static class ChassisTrims
    {
        public const string QuartzCrown = "trim_quartz_crown";
        public const string EmberFins = "trim_ember_fins";
        public const string FrostPlates = "trim_frost_plates";
        public const string StormRails = "trim_storm_rails";
        /// <summary>A glowing coupler line along both flanks of the fairing (appended for the second trim reward).</summary>
        public const string CouplerGlow = "trim_coupler_glow";

        /// <summary>Every cosmetic trim (the first-fusion appearance choice offers from this list).</summary>
        public static readonly string[] All = { QuartzCrown, EmberFins, FrostPlates, StormRails, CouplerGlow };

        public static bool IsTrim(string id) { return System.Array.IndexOf(All, id) >= 0; }

        /// <summary>True for an unlock that is a handling tune rather than a look (the Jax sidegrade).</summary>
        public static bool IsTune(string id)
        {
            return id == FusionRules.TuneGrip || id == "uh_handling_sidegrade" || id == "handling_sidegrade" || id == "handling_canal_grip";
        }

        /// <summary>True for any id the garage can unlock: a look or a handling tune.</summary>
        public static bool IsKnown(string id) { return IsTrim(id) || IsTune(id); }

        /// <summary>
        /// The garage id for an unlock id: tunes become the one grip tune, and the trim ids an early reward catalog used
        /// (which the garage never knew) map to the real look they described. Unknown ids come back unchanged.
        /// </summary>
        public static string Canonical(string id)
        {
            if (string.IsNullOrEmpty(id)) return "";
            if (IsTune(id)) return FusionRules.TuneGrip;
            switch (id)
            {
                case "trim_ember_ribs": return EmberFins;
                case "trim_quartz_crest": return QuartzCrown;
                case "trim_canal_chrome": return FrostPlates;
                case "trim_jax_stripes": return StormRails;
                // The early "vent lift" sidegrade was never built; a claim of it keeps the one sidegrade that exists.
                case "handling_vent_lift": return FusionRules.TuneGrip;
                default: return id;
            }
        }

        public static string Title(string id)
        {
            switch (id)
            {
                case QuartzCrown: return "Quartz Crown";
                case EmberFins: return "Ember Fins";
                case FrostPlates: return "Frostglass Plates";
                case StormRails: return "Storm Rails";
                case CouplerGlow: return "Coupler Glow";
                case FusionRules.TuneGrip: return "Grip Tune (more grip, slightly lower top speed)";
                case "": return "None";
                default: return id;
            }
        }
    }

    /// <summary>
    /// Pure rules of the personal garage: when a personal chassis record is created, record defaults, the safe
    /// normalization of old, empty or malformed save blocks, registration eligibility at Jax's bay, delivery health and
    /// where the bay is when the venue layout has no "bay" anchor. Engine-free.
    /// </summary>
    public static class GarageRules
    {
        /// <summary>The most chassis records a player can own (registration refuses beyond it).</summary>
        public const int MaxOwned = 6;
        /// <summary>Jax's papers for registering a bike as the personal chassis.</summary>
        public const int RegisterCost = 750;
        /// <summary>A wrecked chassis comes back from Jax patched up to this share of its health (disclosed, needs a delivery).</summary>
        public const float RecoveryHealth01 = 0.4f;
        /// <summary>The bay in the garage venue's local frame when its layout has no "bay" anchor: the middle of the
        /// marked vehicle bay (stripes at x -2.8 and 2.8, z 0.25 to 13.75), clear of the repair point at z 10.5.</summary>
        public static readonly Vector3 BayLocalFallback = new Vector3(0f, 0.012f, 6f);
        /// <summary>How far (flat) the chassis may stand from the bay centre and still count as parked in the bay
        /// (inside the marked stripes at x -2.8 and 2.8).</summary>
        public const float BayRadius = 2.8f;
        public const float EngineUpgradeMax = 5f;

        /// <summary>A personal chassis exists once the prologue gave the story bike, and always in Free Roam.</summary>
        public static bool ShouldCreatePersonal(bool freeRoam, int storyProgress)
        {
            return freeRoam || storyProgress >= 1;
        }

        /// <summary>The paint a new record of a class starts with (the sport class is Malik's bronze story bike).</summary>
        public static Color DefaultPaint(string chassisClass)
        {
            switch (chassisClass)
            {
                case FusionIds.Cruiser: return new Color(0.2f, 0.24f, 0.3f);
                case FusionIds.Skiff: return new Color(0.75f, 0.42f, 0.12f);
                default: return new Color(0.45f, 0.32f, 0.2f);
            }
        }

        public static bool IsClass(string id) { return id == FusionIds.Sport || id == FusionIds.Cruiser || id == FusionIds.Skiff; }

        /// <summary>A fresh record (full health, no upgrades, every already unlocked trim copied in).</summary>
        public static ChassisRecord NewRecord(int uid, string chassisClass, Color paint, string source, IList<string> unlockedTrims)
        {
            ChassisRecord r = new ChassisRecord();
            r.Uid = uid;
            r.Class = IsClass(chassisClass) ? chassisClass : FusionIds.Sport;
            r.PaintR = Mathf.Clamp01(paint.r);
            r.PaintG = Mathf.Clamp01(paint.g);
            r.PaintB = Mathf.Clamp01(paint.b);
            r.Health01 = 1f;
            r.Source = source ?? "";
            if (unlockedTrims != null) foreach (string t in unlockedTrims) if (!string.IsNullOrEmpty(t) && !r.Trims.Contains(t)) r.Trims.Add(t);
            return r;
        }

        /// <summary>The next unused uid (never reuses one, so an old body can never be mistaken for a new record).</summary>
        public static int AllocateUid(GarageSave s)
        {
            int max = 0;
            foreach (ChassisRecord r in s.Chassis) if (r != null && r.Uid > max) max = r.Uid;
            int uid = Mathf.Max(s.NextUid, max + 1);
            s.NextUid = uid + 1;
            return uid;
        }

        /// <summary>
        /// Repairs a loaded block in place and returns it: a missing block becomes empty (nothing granted), broken lists
        /// are rebuilt, duplicate or invalid uids are fixed, health and paint are clamped, unknown forms and profiles are
        /// dropped, the personal uid points at an owned record, and the uid counter moves past every uid in use.
        /// </summary>
        public static GarageSave Normalize(GarageSave s)
        {
            if (s == null) s = new GarageSave();
            if (s.Chassis == null) s.Chassis = new List<ChassisRecord>();
            if (s.Forms == null) s.Forms = new List<string>();
            if (s.UnlockedTrims == null) s.UnlockedTrims = new List<string>();
            if (s.Meters == null) s.Meters = new List<FormMeterSave>();
            if (s.Profile == null) s.Profile = "";
            if (!FusionRules.IsProfile(s.Profile)) s.Profile = "";
            if (s.Version < 1) s.Version = 1;
            if (s.NextUid < 1) s.NextUid = 1;
            HashSet<int> seen = new HashSet<int>();
            List<ChassisRecord> kept = new List<ChassisRecord>();
            int max = 0;
            foreach (ChassisRecord r in s.Chassis) if (r != null && r.Uid > max) max = r.Uid;
            foreach (ChassisRecord r in s.Chassis)
            {
                if (r == null) continue;
                if (r.Uid <= 0 || seen.Contains(r.Uid)) r.Uid = ++max;
                seen.Add(r.Uid);
                if (!IsClass(r.Class)) r.Class = FusionIds.Sport;
                r.Health01 = Finite01(r.Health01, 1f);
                r.PaintR = Finite01(r.PaintR, 0.45f);
                r.PaintG = Finite01(r.PaintG, 0.32f);
                r.PaintB = Finite01(r.PaintB, 0.2f);
                r.EngineUpgrade = Mathf.Clamp(r.EngineUpgrade, 0, (int)EngineUpgradeMax);
                r.ArmorUpgrade = Mathf.Clamp(r.ArmorUpgrade, 0, 3);
                if (r.Trims == null) r.Trims = new List<string>();
                List<string> trims = new List<string>();
                foreach (string t in r.Trims)
                {
                    string k = ChassisTrims.Canonical(t);
                    if (k.Length > 0 && !trims.Contains(k)) trims.Add(k);
                }
                r.Trims = trims;
                r.Trim = r.Trim == null ? "" : ChassisTrims.Canonical(r.Trim);
                if (ChassisTrims.IsTune(r.Trim)) r.Trim = "";
                r.Tune = r.Tune == null ? "" : ChassisTrims.Canonical(r.Tune);
                if (r.Tune.Length > 0 && !ChassisTrims.IsTune(r.Tune)) r.Tune = "";
                if (r.Source == null) r.Source = "";
                if (r.Wrecked && r.Health01 > 0f) r.Health01 = 0f;
                foreach (string t in r.Trims) if (!s.UnlockedTrims.Contains(t)) s.UnlockedTrims.Add(t);
                kept.Add(r);
            }
            while (kept.Count > MaxOwned) kept.RemoveAt(kept.Count - 1);
            s.Chassis = kept;
            // Every record carries every unlocked look (a cosmetic unlock is the player's, not one bike's).
            List<string> pool = new List<string>();
            foreach (string t in s.UnlockedTrims)
            {
                string k = ChassisTrims.Canonical(t);
                if (k.Length > 0 && !pool.Contains(k)) pool.Add(k);
            }
            s.UnlockedTrims = pool;
            foreach (ChassisRecord r in s.Chassis)
            {
                foreach (string t in pool) if (!r.Trims.Contains(t)) r.Trims.Add(t);
                if (r.Trim.Length > 0 && !r.Trims.Contains(r.Trim)) r.Trim = "";
                if (r.Tune.Length > 0 && !r.Trims.Contains(r.Tune)) r.Tune = "";
            }
            int highest = 0;
            foreach (ChassisRecord r in s.Chassis) if (r.Uid > highest) highest = r.Uid;
            if (s.NextUid <= highest) s.NextUid = highest + 1;
            bool personalOwned = false;
            foreach (ChassisRecord r in s.Chassis) if (r.Uid == s.PersonalUid) personalOwned = true;
            if (!personalOwned) s.PersonalUid = s.Chassis.Count > 0 ? s.Chassis[0].Uid : 0;
            List<string> forms = new List<string>();
            foreach (string f in s.Forms) if (FusionIds.Has(f) && !forms.Contains(f)) forms.Add(f);
            s.Forms = forms;
            List<FormMeterSave> meters = new List<FormMeterSave>();
            HashSet<string> meterForms = new HashSet<string>();
            foreach (FormMeterSave m in s.Meters)
            {
                if (m == null || !FusionIds.Has(m.Form) || meterForms.Contains(m.Form)) continue;
                meterForms.Add(m.Form);
                FormAbilitySpec spec = FusionRules.Ability(m.Form);
                m.Heat = Finite01(m.Heat, 0f);
                m.Cooldown = Mathf.Clamp(float.IsNaN(m.Cooldown) ? 0f : m.Cooldown, 0f, Mathf.Max(0f, spec.Cooldown));
                m.Charges = Mathf.Clamp(m.Charges, 0, Mathf.Max(0, spec.MaxCharges));
                m.Recharge = Mathf.Clamp(float.IsNaN(m.Recharge) ? 0f : m.Recharge, 0f, Mathf.Max(0f, spec.RechargeSeconds));
                meters.Add(m);
            }
            s.Meters = meters;
            s.FusionCooldown = Mathf.Clamp(float.IsNaN(s.FusionCooldown) ? 0f : s.FusionCooldown, 0f, FusionRules.CooldownAfterRecovery);
            return s;
        }

        private static float Finite01(float v, float fallback)
        {
            if (float.IsNaN(v) || float.IsInfinity(v)) return fallback;
            return Mathf.Clamp01(v);
        }

        /// <summary>
        /// Why a bike cannot be registered as the personal chassis at Jax's bay ("" when it can). Riding or stealing a
        /// bike never makes it personal; this deliberate, paid registration in the bay is the only way.
        /// </summary>
        public static string WhyNotRegister(bool isBike, bool isPolice, string vehicleName, bool destroyed, bool missionCritical,
            bool isDriver, bool inBay, bool stopped, bool alreadyPersonal, int ownedCount, int money)
        {
            if (!isBike) return "Only a hover bike can be a personal chassis.";
            if (isPolice) return "Jax won't paper a police bike.";
            if (string.IsNullOrEmpty(FusionRules.ChassisClassOf(vehicleName))) return "This bike model has no coupler mount.";
            if (destroyed) return "This bike is wrecked.";
            if (missionCritical) return "This bike belongs to a job.";
            if (!isDriver) return "Ride it into the bay yourself.";
            if (!inBay) return "Park inside Jax's bay to register a bike.";
            if (!stopped) return "Stop the bike in the bay first.";
            if (alreadyPersonal) return "This is already your chassis.";
            if (ownedCount >= MaxOwned) return "Your garage is full (" + MaxOwned + " chassis).";
            if (money < RegisterCost) return "Jax's papers cost " + RegisterCost + ". You're short.";
            return "";
        }

        /// <summary>The health a delivered body starts with: exactly what the record says, or the patched-up floor after a wreck.</summary>
        public static float DeliveryHealth01(float recorded, bool wrecked)
        {
            if (wrecked || recorded <= 0f) return RecoveryHealth01;
            return Mathf.Clamp01(recorded);
        }

        /// <summary>Applies a trim choice to a record: "" clears it; a trim must be a known look and unlocked. Idempotent (no stacking).</summary>
        public static bool ApplyTrim(ChassisRecord r, string trimId)
        {
            if (r == null) return false;
            trimId = trimId ?? "";
            if (trimId.Length > 0 && (!r.Trims.Contains(trimId) || !ChassisTrims.IsTrim(trimId))) return false;
            r.Trim = trimId;
            return true;
        }

        /// <summary>The next look in the cycle None, then each unlocked trim in catalog order (unknown ids are never offered).</summary>
        public static string NextTrim(ChassisRecord r)
        {
            List<string> cycle = new List<string> { "" };
            foreach (string t in ChassisTrims.All) if (r != null && r.Trims.Contains(t)) cycle.Add(t);
            int i = cycle.IndexOf(r != null ? r.Trim : "");
            return cycle[(i + 1) % cycle.Count];
        }
    }
}
