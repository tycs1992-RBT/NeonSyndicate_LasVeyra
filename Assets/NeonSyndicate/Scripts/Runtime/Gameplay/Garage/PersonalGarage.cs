using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>One owned personal chassis: a durable identity, never a transient GameObject id.</summary>
    [Serializable]
    public sealed class ChassisRecord
    {
        public int Uid;
        /// <summary>A FusionIds chassis class: sport, cruiser or skiff.</summary>
        public string Class = "sport";
        public float PaintR = 0.45f, PaintG = 0.32f, PaintB = 0.2f;
        public float Health01 = 1f;
        public int EngineUpgrade;
        public string Trim = "";
        public List<string> Trims = new List<string>();
        public string Source = "";
        // ---- appended by the FUSION work stream (04E) ----
        /// <summary>The chassis was wrecked; the next delivery comes patched up (GarageRules.RecoveryHealth01).</summary>
        public bool Wrecked;
        /// <summary>Jax's armour upgrade level carried by the live body (Vehicle.ArmorUpgrade).</summary>
        public int ArmorUpgrade;
        /// <summary>The applied handling tune ("" none, or FusionRules.TuneGrip once unlocked).</summary>
        public string Tune = "";

        public Color Paint { get { return new Color(PaintR, PaintG, PaintB); } }
    }

    /// <summary>One form's ability meters in the save (so a reload is never a free refill).</summary>
    [Serializable]
    public sealed class FormMeterSave
    {
        public string Form = "";
        public float Heat;
        public float Cooldown;
        public int Charges;
        public float Recharge;
        public bool Overheated;
    }

    /// <summary>
    /// Game.Garage: the player's personal chassis records (the bike Bring my bike delivers, registered at Jax's bay),
    /// fusion entitlement and unlocked forms and trims. One live body per record, marked with Vehicle.ChassisUid.
    /// Riding or stealing a bike never makes it personal; only the story bike, the Free Roam bike, a reward chassis or a
    /// paid registration at Jax's bay does.
    /// </summary>
    public sealed partial class PersonalGarage : MonoBehaviour, IWorldSystem
    {
        private GarageSave _save = new GarageSave();
        private readonly Dictionary<int, Vehicle> _bodies = new Dictionary<int, Vehicle>();
        private float _syncTimer;
        private bool _sessionPending = true;

        public void Init()
        {
            WorldSystems.Register(this);
            ReportStatus();
        }

        /// <summary>The active personal chassis record (null before the prologue gives the story bike).</summary>
        public ChassisRecord Personal { get { return Find(_save.PersonalUid); } }
        public IList<ChassisRecord> Owned { get { return _save.Chassis; } }

        /// <summary>The live save block (read only for other systems; change it through the methods).</summary>
        public GarageSave Data { get { return _save; } }

        public ChassisRecord Find(int uid)
        {
            if (uid == 0) return null;
            for (int i = 0; i < _save.Chassis.Count; i++) if (_save.Chassis[i] != null && _save.Chassis[i].Uid == uid) return _save.Chassis[i];
            return null;
        }

        public ChassisRecord FindClass(string chassisClass)
        {
            for (int i = 0; i < _save.Chassis.Count; i++) if (_save.Chassis[i] != null && _save.Chassis[i].Class == chassisClass) return _save.Chassis[i];
            return null;
        }

        /// <summary>True when the vehicle is the live body of an owned personal chassis.</summary>
        public bool IsPersonal(Vehicle v)
        {
            if (v == null || v.ChassisUid == 0 || v.IsDestroyed) return false;
            Vehicle bound;
            return _bodies.TryGetValue(v.ChassisUid, out bound) && bound == v && Find(v.ChassisUid) != null;
        }

        /// <summary>The record a vehicle's body belongs to (null for every other vehicle).</summary>
        public ChassisRecord RecordOf(Vehicle v)
        {
            return IsPersonal(v) ? Find(v.ChassisUid) : null;
        }

        /// <summary>Plain reason a vehicle is not a personal chassis ("" when it is), for prompts and quest steps.</summary>
        public string WhyNotPersonal(Vehicle v)
        {
            if (v == null) return "You are not on a vehicle.";
            if (IsPersonal(v)) return "";
            if (v.Def == null || !v.Def.IsBike) return "Fusion needs your own hover bike. Cars are not supported.";
            if (v.IsPolice) return "A police bike can never be your chassis.";
            if (v.MissionCritical) return "This bike belongs to the job.";
            if (Personal == null) return "You don't have a personal chassis yet.";
            return "This bike isn't yours. Phone > Bring my bike delivers your " + FusionRules.VehicleNameOf(Personal.Class) + ", or register this one at Jax's bay.";
        }

        /// <summary>The live body of a record (null when none exists right now).</summary>
        public Vehicle BodyOf(int uid)
        {
            Vehicle v;
            if (!_bodies.TryGetValue(uid, out v)) return null;
            if (v == null || v.IsDestroyed) return null;
            return v;
        }

        public Vehicle PersonalBody { get { ChassisRecord r = Personal; return r != null ? BodyOf(r.Uid) : null; } }

        /// <summary>Makes sure a personal chassis record exists (the story or Free Roam bike) and returns it.</summary>
        public ChassisRecord EnsurePersonal()
        {
            ChassisRecord r = Personal;
            if (r != null) return r;
            if (_save.Chassis.Count > 0)
            {
                _save.PersonalUid = _save.Chassis[0].Uid;
                return Personal;
            }
            bool freeRoam = Game.Root != null && Game.Root.FreeRoam;
            int story = Game.Missions != null ? Game.Missions.StoryProgress : 0;
            if (!GarageRules.ShouldCreatePersonal(freeRoam, story)) return null;
            r = AddRecord(FusionIds.Sport, GarageRules.DefaultPaint(FusionIds.Sport), freeRoam ? "free_roam" : "story");
            _save.PersonalUid = r.Uid;
            Log.Info("GARAGE_04E personal chassis created uid=" + r.Uid + " class=" + r.Class + " source=" + r.Source);
            ReportStatus();
            return r;
        }

        private ChassisRecord AddRecord(string chassisClass, Color paint, string source)
        {
            int uid = GarageRules.AllocateUid(_save);
            ChassisRecord r = GarageRules.NewRecord(uid, chassisClass, paint, source, _save.UnlockedTrims);
            _save.Chassis.Add(r);
            return r;
        }

        /// <summary>
        /// The story bike (prologue and Start After the Prologue) becomes the personal chassis body: the record is
        /// created from it the first time, and later spawns of the story bike bind to the same record.
        /// </summary>
        public ChassisRecord AdoptStoryBike(Vehicle v)
        {
            if (v == null || v.Def == null || !v.Def.IsBike) return null;
            string cls = FusionRules.ChassisClassOf(v.Def.Name);
            if (string.IsNullOrEmpty(cls)) return null;
            ChassisRecord r = Personal;
            if (r == null)
            {
                r = AddRecord(cls, v.PaintColor, "story");
                _save.PersonalUid = r.Uid;
                Log.Info("GARAGE_04E story bike adopted as personal chassis uid=" + r.Uid);
            }
            if (r.Class != cls) return r;
            BindBody(r, v);
            ReportStatus();
            return r;
        }

        /// <summary>Registers the player's current bike as their personal chassis (refuses borrowed, police or mission vehicles).</summary>
        public bool RegisterFromVehicle(Vehicle v, out string reason)
        {
            PlayerController p = Game.Player;
            bool driver = p != null && v != null && p.CurrentVehicle == v && p.Seat == 0;
            Vector3 bay; float radius;
            bool inBay = v != null && Game.Fusion != null && Game.Fusion.TryBay(out bay, out radius) && MathUtil.FlatDistance(v.transform.position, bay) <= radius && Mathf.Abs(v.transform.position.y - bay.y) < 2.5f;
            int money = Game.Economy != null ? Game.Economy.Money : 0;
            reason = v == null ? "No bike to register." : GarageRules.WhyNotRegister(v.Def != null && v.Def.IsBike, v.IsPolice, v.Def != null ? v.Def.Name : "",
                v.IsDestroyed, v.MissionCritical || (Game.Missions != null && Game.Missions.ProtectsVehicle(v)), driver, inBay, v.Speed < 1f, IsPersonal(v), _save.Chassis.Count, money);
            if (reason.Length > 0) return false;
            if (Game.Economy == null || !Game.Economy.Spend(GarageRules.RegisterCost, "Jax's papers"))
            {
                reason = "Jax's papers cost " + GarageRules.RegisterCost + ". You're short.";
                return false;
            }
            ChassisRecord r = AddRecord(FusionRules.ChassisClassOf(v.Def.Name), v.PaintColor, "registered");
            r.Health01 = v.Def.Health > 0f ? Mathf.Clamp01(v.Health / v.Def.Health) : 1f;
            r.EngineUpgrade = Mathf.Clamp(v.EngineUpgrade, 0, (int)GarageRules.EngineUpgradeMax);
            r.ArmorUpgrade = Mathf.Clamp(v.ArmorUpgrade, 0, 3);
            _save.PersonalUid = r.Uid;
            v.StolenByPlayer = true;
            v.Locked = false;
            BindBody(r, v);
            Log.Info("GARAGE_04E registered " + v.Def.Name + " as personal chassis uid=" + r.Uid);
            reason = "Jax papers the " + v.Def.Name + ". It's your chassis now (#" + r.Uid + ").";
            ReportStatus();
            return true;
        }

        /// <summary>Adds an owned chassis class (a Jax side-quest reward); idempotent.</summary>
        public ChassisRecord GrantChassis(string chassisClass, string source)
        {
            if (!GarageRules.IsClass(chassisClass)) return null;
            ChassisRecord r = FindClass(chassisClass);
            if (r != null) return r;
            if (_save.Chassis.Count >= GarageRules.MaxOwned) return null;
            r = AddRecord(chassisClass, GarageRules.DefaultPaint(chassisClass), source ?? "reward");
            if (_save.PersonalUid == 0) _save.PersonalUid = r.Uid;
            Log.Info("GARAGE_04E granted chassis " + chassisClass + " uid=" + r.Uid + " source=" + source);
            ReportStatus();
            return r;
        }

        /// <summary>Makes an owned record the active personal chassis (Bring my bike brings it next).</summary>
        public bool SetPersonal(int uid)
        {
            if (Find(uid) == null) return false;
            _save.PersonalUid = uid;
            ReportStatus();
            return true;
        }

        public bool FusionUnlocked { get { return _save.FusionUnlocked; } }

        /// <summary>The fusion entitlement (the first fusion's reward). The Ventwing, the form earned with it, unlocks too.</summary>
        public void UnlockFusion(string source)
        {
            bool changed = !_save.FusionUnlocked;
            _save.FusionUnlocked = true;
            if (!_save.Forms.Contains(FusionIds.Ventwing)) _save.Forms.Add(FusionIds.Ventwing);
            if (changed) Log.Info("GARAGE_04E fusion unlocked source=" + source);
            ReportStatus();
        }

        public bool FormUnlocked(string formId) { return !string.IsNullOrEmpty(formId) && _save.Forms.Contains(formId); }

        public void UnlockForm(string formId, string source)
        {
            if (!FusionIds.Has(formId) || _save.Forms.Contains(formId)) return;
            _save.Forms.Add(formId);
            Log.Info("GARAGE_04E form unlocked " + formId + " source=" + source);
            ReportStatus();
        }

        /// <summary>
        /// Unlocks a chassis look for every owned chassis (and any chassis owned later). A tune id (the handling
        /// sidegrade) unlocks the grip tune instead. True when it was newly unlocked.
        /// </summary>
        public bool UnlockTrim(string trimId, string source)
        {
            if (string.IsNullOrEmpty(trimId)) return false;
            if (ChassisTrims.IsTune(trimId)) trimId = FusionRules.TuneGrip;
            if (_save.UnlockedTrims.Contains(trimId)) return false;
            _save.UnlockedTrims.Add(trimId);
            foreach (ChassisRecord r in _save.Chassis) if (r != null && !r.Trims.Contains(trimId)) r.Trims.Add(trimId);
            Log.Info("GARAGE_04E trim unlocked " + trimId + " source=" + source);
            return true;
        }

        /// <summary>Applies an unlocked look to the personal chassis ("" removes it). Cosmetic only; repeat calls rebuild, never stack.</summary>
        public bool ApplyTrim(string trimId)
        {
            ChassisRecord r = Personal;
            if (r == null || !GarageRules.ApplyTrim(r, trimId)) return false;
            Vehicle body = BodyOf(r.Uid);
            if (body != null) ChassisTrimArt.Apply(body, r.Trim, r.Class);
            return true;
        }

        /// <summary>Switches the grip tune on the personal chassis ("" off). Only an unlocked tune applies.</summary>
        public bool SetTune(string tuneId)
        {
            ChassisRecord r = Personal;
            if (r == null) return false;
            tuneId = tuneId ?? "";
            if (tuneId.Length > 0 && !r.Trims.Contains(tuneId)) return false;
            r.Tune = tuneId;
            Vehicle body = BodyOf(r.Uid);
            if (body != null && (Game.Fusion == null || !Game.Fusion.Owns(body))) body.FusionHandling = FusionRules.TuneHandling(r.Tune);
            return true;
        }

        /// <summary>The Ventwing handling profile chosen at Jax's bay ("" until chosen).</summary>
        public string Profile { get { return _save.Profile; } }

        /// <summary>The profile in effect: the saved choice, else the campaign's "fusion_profile" decision, else Updraft.</summary>
        public string ActiveProfile
        {
            get
            {
                if (FusionRules.IsProfile(_save.Profile)) return _save.Profile;
                string choice = Game.Quests != null ? Game.Quests.Choice("fusion_profile") : "";
                return FusionRules.IsProfile(choice) ? choice : FusionRules.ProfileUpdraft;
            }
        }

        public bool SetProfile(string profile)
        {
            if (!FusionRules.IsProfile(profile)) return false;
            _save.Profile = profile;
            return true;
        }

        // ================================================================== world system

        public void ResetAll()
        {
            _save = GarageRules.Normalize(new GarageSave());
            _bodies.Clear();
            _sessionPending = true;
            _syncTimer = 0f;
        }

        public void Capture(SaveData d)
        {
            SyncFromBodies();
            if (Game.Fusion != null) Game.Fusion.CaptureMeters(_save);
            d.Garage = Clone(_save);
        }

        public void Restore(SaveData d)
        {
            _save = GarageRules.Normalize(Clone(d != null ? d.Garage : null));
            _bodies.Clear();
            if (Game.Fusion != null) Game.Fusion.RestoreMeters(_save);
            ReportStatus();
        }

        private static GarageSave Clone(GarageSave s)
        {
            if (s == null) return new GarageSave();
            GarageSave c = new GarageSave
            {
                Version = s.Version, NextUid = s.NextUid, PersonalUid = s.PersonalUid, FusionUnlocked = s.FusionUnlocked,
                Profile = s.Profile, FusionCooldown = s.FusionCooldown
            };
            if (s.Forms != null) c.Forms.AddRange(s.Forms);
            if (s.UnlockedTrims != null) c.UnlockedTrims.AddRange(s.UnlockedTrims);
            if (s.Chassis != null)
            {
                foreach (ChassisRecord r in s.Chassis)
                {
                    if (r == null) { c.Chassis.Add(null); continue; }
                    ChassisRecord k = new ChassisRecord
                    {
                        Uid = r.Uid, Class = r.Class, PaintR = r.PaintR, PaintG = r.PaintG, PaintB = r.PaintB, Health01 = r.Health01,
                        EngineUpgrade = r.EngineUpgrade, Trim = r.Trim, Source = r.Source, Wrecked = r.Wrecked, ArmorUpgrade = r.ArmorUpgrade, Tune = r.Tune
                    };
                    if (r.Trims != null) k.Trims.AddRange(r.Trims);
                    c.Chassis.Add(k);
                }
            }
            if (s.Meters != null)
            {
                foreach (FormMeterSave m in s.Meters)
                {
                    if (m == null) continue;
                    c.Meters.Add(new FormMeterSave { Form = m.Form, Heat = m.Heat, Cooldown = m.Cooldown, Charges = m.Charges, Recharge = m.Recharge, Overheated = m.Overheated });
                }
            }
            return c;
        }

        /// <summary>Reports the garage's real state to the STATUS page (what exists, not what is planned).</summary>
        public void ReportStatus()
        {
            ChassisRecord r = Personal;
            string detail = r == null ? "No personal chassis yet (the prologue gives the story bike; Free Roam starts with one)."
                : "Personal chassis: " + FusionRules.VehicleNameOf(r.Class) + " #" + r.Uid + (BodyOf(r.Uid) != null ? " (in the world)" : " (stored; Phone > Bring my bike)") +
                  ". Fusion " + (_save.FusionUnlocked ? "unlocked" : "locked") + ", forms " + _save.Forms.Count + "/" + FusionIds.Forms.Length + ".";
            ModuleStatus.Report("garage", "Personal garage", r == null ? ModuleState.Partial : ModuleState.Loaded, detail,
                r == null ? "Finish the prologue or start Free Roam." : "");
            ModuleStatus.SetCount("garage", _save.Chassis.Count, "owned chassis");
        }
    }

    /// <summary>The garage's durable block (SaveData.Garage). Append fields only.</summary>
    [Serializable]
    public sealed class GarageSave
    {
        public int Version = 1;
        public int NextUid = 1;
        public int PersonalUid;
        public List<ChassisRecord> Chassis = new List<ChassisRecord>();
        public bool FusionUnlocked;
        public List<string> Forms = new List<string>();
        /// <summary>The fusion handling profile chosen at Jax's bay ("" until chosen).</summary>
        public string Profile = "";
        // ---- appended by the FUSION work stream (04E) ----
        /// <summary>Every chassis look the player has unlocked (copied to each owned record).</summary>
        public List<string> UnlockedTrims = new List<string>();
        /// <summary>Form ability meters (heat, cooldown, charges) so a reload is never a free refill.</summary>
        public List<FormMeterSave> Meters = new List<FormMeterSave>();
        /// <summary>Seconds left before the next fusion.</summary>
        public float FusionCooldown;
    }
}
