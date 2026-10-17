using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The live side of the personal garage: one body per chassis record (Vehicle.ChassisUid), spawned from the record
    /// with its paint, upgrades, health and trim, and sampled back into the record twice a second so a cleaned-up or
    /// wrecked body leaves an exact record behind.
    /// </summary>
    public sealed partial class PersonalGarage
    {
        /// <summary>
        /// Binds a live vehicle as the body of a record. A different body that still carried this uid stops being the
        /// chassis (it becomes an ordinary bike), so two bodies of one chassis never exist.
        /// </summary>
        public void BindBody(ChassisRecord r, Vehicle v)
        {
            if (r == null || v == null) return;
            Vehicle old;
            if (_bodies.TryGetValue(r.Uid, out old) && old != null && old != v) old.ChassisUid = 0;
            foreach (KeyValuePair<int, Vehicle> kv in new List<KeyValuePair<int, Vehicle>>(_bodies))
                if (kv.Value == v && kv.Key != r.Uid) _bodies.Remove(kv.Key);
            _bodies[r.Uid] = v;
            v.ChassisUid = r.Uid;
            v.StolenByPlayer = true;
            v.Locked = false;
            if (Game.Fusion == null || !Game.Fusion.Owns(v)) v.FusionHandling = FusionRules.TuneHandling(r.Tune);
            try { ChassisTrimArt.Apply(v, r.Trim, r.Class); }
            catch (System.Exception e) { Log.Exception("Chassis trim", e); }
        }

        /// <summary>
        /// Spawns the body of a record at a ground point (parked, ready to ride, yours). The model follows the class; paint,
        /// engine and armour upgrades and health come from the record (a wreck comes back patched up). Replaces any
        /// previous body of the same record.
        /// </summary>
        public Vehicle SpawnBody(ChassisRecord r, Vector3 ground, float yaw, bool trafficParked)
        {
            if (r == null) return null;
            VehicleDef def = VehicleDb.Find(FusionRules.VehicleNameOf(r.Class)) ?? VehicleDb.CourierBike;
            Vehicle v;
            if (trafficParked && Game.Traffic != null) v = Game.Traffic.SpawnParkedAt(def, ground, yaw, r.Paint);
            else
            {
                v = Vehicle.Spawn(def, ground, yaw, r.Paint, true);
                if (Game.Traffic != null) v.transform.SetParent(Game.Traffic.transform, true);
            }
            v.Parked = false;
            v.EngineUpgrade = Mathf.Clamp(r.EngineUpgrade, 0, (int)GarageRules.EngineUpgradeMax);
            v.ArmorUpgrade = Mathf.Clamp(r.ArmorUpgrade, 0, 3);
            float h = GarageRules.DeliveryHealth01(r.Health01, r.Wrecked);
            v.Health = def.Health * h;
            r.Health01 = h;
            r.Wrecked = false;
            Vehicle previous = BodyOf(r.Uid);
            if (previous != null && previous != v && previous.OccupantCount() == 0) previous.ChassisUid = 0;
            BindBody(r, v);
            ReportStatus();
            return v;
        }

        /// <summary>Copies every live body's health, paint and upgrades into its record (a wreck marks the record).</summary>
        public void SyncFromBodies()
        {
            List<int> gone = null;
            foreach (KeyValuePair<int, Vehicle> kv in _bodies)
            {
                ChassisRecord r = Find(kv.Key);
                Vehicle v = kv.Value;
                if (r == null || v == null)
                {
                    if (gone == null) gone = new List<int>();
                    gone.Add(kv.Key);
                    continue;
                }
                if (v.ChassisUid != kv.Key)
                {
                    if (gone == null) gone = new List<int>();
                    gone.Add(kv.Key);
                    continue;
                }
                if (v.IsDestroyed)
                {
                    r.Health01 = 0f;
                    r.Wrecked = true;
                    v.ChassisUid = 0;
                    ChassisTrimArt.Apply(v, "", r.Class);
                    if (gone == null) gone = new List<int>();
                    gone.Add(kv.Key);
                    Log.Info("GARAGE_04E chassis uid=" + r.Uid + " wrecked; Jax patches it up on the next delivery.");
                    continue;
                }
                if (v.Def != null && v.Def.Health > 0f) r.Health01 = Mathf.Clamp01(v.Health / v.Def.Health);
                r.PaintR = Mathf.Clamp01(v.PaintColor.r);
                r.PaintG = Mathf.Clamp01(v.PaintColor.g);
                r.PaintB = Mathf.Clamp01(v.PaintColor.b);
                r.EngineUpgrade = Mathf.Clamp(v.EngineUpgrade, 0, (int)GarageRules.EngineUpgradeMax);
                r.ArmorUpgrade = Mathf.Clamp(v.ArmorUpgrade, 0, 3);
            }
            if (gone != null) foreach (int uid in gone) _bodies.Remove(uid);
        }

        private void Update()
        {
            if (!Game.IsPlaying) return;
            if (_sessionPending)
            {
                _sessionPending = false;
                try { EnsurePersonal(); }
                catch (System.Exception e) { Log.Exception("Garage session start", e); }
                ReportStatus();
            }
            _syncTimer -= Time.deltaTime;
            if (_syncTimer > 0f) return;
            _syncTimer = 0.5f;
            SyncFromBodies();
        }
    }
}
