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
    }

    /// <summary>
    /// Game.Garage: the player's personal chassis records (the bike Bring my bike delivers, registered at Jax's bay),
    /// fusion entitlement and unlocked forms and trims. FOUNDATION STUB: the FUSION work stream replaces the bodies.
    /// </summary>
    public sealed partial class PersonalGarage : MonoBehaviour, IWorldSystem
    {
        public void Init()
        {
            WorldSystems.Register(this);
        }

        public ChassisRecord Personal { get { return null; } }
        public IList<ChassisRecord> Owned { get { return new List<ChassisRecord>(); } }

        /// <summary>True when the vehicle is the live body of an owned personal chassis.</summary>
        public bool IsPersonal(Vehicle v) { return false; }

        /// <summary>Makes sure a personal chassis record exists (the story or Free Roam bike) and returns it.</summary>
        public ChassisRecord EnsurePersonal() { return null; }

        /// <summary>Registers the player's current bike as their personal chassis (refuses borrowed, police or mission vehicles).</summary>
        public bool RegisterFromVehicle(Vehicle v, out string reason)
        {
            reason = "The garage is not implemented in this build.";
            return false;
        }

        /// <summary>Adds an owned chassis class (a Jax side-quest reward); idempotent.</summary>
        public ChassisRecord GrantChassis(string chassisClass, string source) { return null; }

        public bool FusionUnlocked { get { return false; } }
        public void UnlockFusion(string source) { }
        public bool FormUnlocked(string formId) { return false; }
        public void UnlockForm(string formId, string source) { }
        public bool UnlockTrim(string trimId, string source) { return false; }
        public bool ApplyTrim(string trimId) { return false; }

        public void ResetAll() { }
        public void Capture(SaveData d) { }
        public void Restore(SaveData d) { }
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
    }
}
