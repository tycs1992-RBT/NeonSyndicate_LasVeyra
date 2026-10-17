using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The changed arrival neighborhood of 04E around the ordinary Veyra Lofts arrival: the Cinder Dispatch frontage and
    /// its crystal receiver mast straight across Veyra Avenue (plus the receiver crown that replaces the tower's roof
    /// antenna), dispatch frontage activity (crates of cores, a walk-in sign, a turning beacon), the warm conduit along the
    /// forecourt from the dispatch to the Pressure House door, the open and lit Lofts entrance with Nessa's small dispatch
    /// board, and the companion perch outside Bondkeeper's Refuge. Placement comes from the engine-free ArrivalPlan04E and
    /// is checked again here against the ground, colliders and interaction markers. Never throws; reports through
    /// ModuleStatus ("venues_04e" and "arrival_04e").
    /// </summary>
    public static partial class ArrivalScene04E
    {
        /// <summary>The warm conduit's ground route from Cinder Dispatch to the Pressure House door (empty until built).</summary>
        public static readonly List<Vector3> ConduitPath = new List<Vector3>();

        /// <summary>True once the arrival dressing exists in the world.</summary>
        public static bool Built { get; private set; }

        /// <summary>True while a quest asks the player to follow the conduit (its joints brighten and chase).</summary>
        public static bool ConduitLit { get; private set; }

        /// <summary>The plan the scene was built from (null before Build).</summary>
        public static ArrivalPlan04E Plan { get; private set; }

        /// <summary>A plain one-line account of what was built and what was skipped (for STATUS and logs).</summary>
        public static string Status { get; private set; } = "Not built yet.";

        private static ArrivalScene04EController _controller;
        private static int _sparksOverride;   // 0 automatic (until uh05_02_sparks is done), 1 forced on, 2 stopped
        private static bool _nessaBoardBuilt;

        /// <summary>Builds the arrival dressing after the venues are active. Never throws; reports through ModuleStatus.</summary>
        public static void Build(Transform parent)
        {
            if (Built) return;
            try { BuildInternal(parent); }
            catch (Exception e)
            {
                Log.Exception("Arrival scene 04E", e);
                Status = "Arrival dressing failed: " + e.Message;
                ModuleStatus.Report("arrival_04e", "Arrival neighborhood", ModuleState.Failed, Status, "The rest of the game still runs. Send the Console error along.");
            }
        }

        /// <summary>Lights the conduit route while a quest asks the player to follow it.</summary>
        public static void SetConduitLit(bool lit)
        {
            if (ConduitLit == lit) return;
            ConduitLit = lit;
            if (_controller != null) _controller.OnConduitChanged(lit);
        }

        /// <summary>
        /// The ambient sparks in the Pressure House (fuse cabinet, vent line, nest vent). They run until the vent is diverted
        /// in uh05_02_sparks: that beat sets the campaign flag QuestRuntimeRules04E.PressureVentFlag (saved, so a reload keeps
        /// them off), and a finished uh05_02_sparks also stops them. SetPressureSparks overrides both for this session.
        /// </summary>
        public static void SetPressureSparks(bool active) { _sparksOverride = active ? 1 : 2; }

        /// <summary>Back to automatic sparks (on until the vent is diverted). Session resets do this.</summary>
        public static void ClearPressureSparksOverride() { _sparksOverride = 0; }

        public static bool PressureSparksActive
        {
            get
            {
                if (_sparksOverride == 1) return true;
                if (_sparksOverride == 2) return false;
                if (Game.Quests == null) return true;
                return !Game.Quests.Flag(QuestRuntimeRules04E.PressureVentFlag) && !Game.Quests.IsDone("uh05_02_sparks");
            }
        }

        /// <summary>Where Nessa Vale stands: on the Lofts forecourt beside her board, facing the arrival. False when unbuilt.</summary>
        public static bool TryNessaPost(out Vector3 pos, out float yaw)
        {
            pos = Vector3.zero;
            yaw = 0f;
            if (Plan == null || !Plan.NessaPostPlaced || !_nessaBoardBuilt) return false;
            pos = Plan.NessaPost;
            yaw = Plan.NessaYaw;
            return true;
        }

        /// <summary>
        /// True when a point (with a radius) stands in front of or inside one of the four new 04E venue doors. The 04D
        /// resonance case, practice lane and breach site use it so they never block a new entrance.
        /// </summary>
        public static bool BlocksNewVenueDoor(Vector3 point, float radius)
        {
            InhabitedCitySystem city = Game.Inhabited;
            if (city == null || city.Venues == null) return false;
            foreach (VenueBinding04C venue in city.Venues)
            {
                if (venue == null || venue.Definition == null || !VenueIds04E.Has(venue.Definition.Id) || !venue.Available) continue;
                if (InhabitedCityBindings04C.InDoorZone04E(venue.Origin, venue.Yaw, point, radius)) return true;
            }
            return false;
        }

        /// <summary>The PLACES page text for the four 04E venues (null for any other id).</summary>
        public static string PlacePurpose(string id)
        {
            switch (id)
            {
                case VenueIds04E.Dispatch: return "Cinder Dispatch, straight across Veyra Avenue from your door: talk to the dispatcher, inspect receivers A and B, take Ion's call and choose a build at the crystal-repair fitting bench. It coordinates the existing services; Mara's clinic and Dante's forge stay where they are.";
                case VenueIds04E.Pressure: return "The Pressure House: a utility workroom with a fuse cabinet, a vent valve, a nest corner and the service stair down to the Underheart. Follow the warm conduit along the forecourt from Cinder Dispatch to its door.";
                case VenueIds04E.Bondkeeper: return "Bondkeeper's Refuge, across Core Avenue from Cinder Diner: a creature care room with a treatment bench (the care station), a habitat enclosure, an indoor training yard and a perch.";
                case VenueIds04E.Office: return "The Resonance Office, two doors from the archive: a faction records office with a clerk counter, a records wall, a public terminal, a back records room and a testimony bench.";
                default: return null;
            }
        }

        public static void ResetStatics()
        {
            ConduitPath.Clear();
            Built = false;
            ConduitLit = false;
            Plan = null;
            Status = "Not built yet.";
            _controller = null;
            _sparksOverride = 0;
            _nessaBoardBuilt = false;
        }

        // ------------------------------------------------------------------ build

        private static void BuildInternal(Transform parent)
        {
            InhabitedCitySystem city = Game.Inhabited;
            ReportVenues(city);
            if (city == null || !city.IsOperational || !city.EnabledAtBoot || Game.Layout == null)
            {
                Status = city == null ? "The inhabited city did not start, so the arrival dressing was not placed." :
                    !city.EnabledAtBoot ? "Inhabited city venues are turned off in Settings, so the arrival dressing was not placed." :
                    "The city layout or venues are not ready, so the arrival dressing was not placed.";
                ModuleStatus.Report("arrival_04e", "Arrival neighborhood", city != null && !city.EnabledAtBoot ? ModuleState.Disabled : ModuleState.Failed, Status,
                    city != null && !city.EnabledAtBoot ? "Settings > Inhabited city, then restart." : "Restart the game; send the Console log if it repeats.");
                Log.Warn("ARRIVAL_SCENE_04E " + Status);
                return;
            }
            Plan = ArrivalPlan04E.Make(Game.Layout, city.Venues, v => v.Available);
            Physics.SyncTransforms();
            GameObject root = new GameObject("Arrival Scene 04E");
            root.transform.SetParent(parent, false);
            _controller = root.AddComponent<ArrivalScene04EController>();
            _controller.Init();
            List<string> skipped = new List<string>();
            int built = 0, planned = 0;
            foreach (ArrivalPiece04E piece in Plan.Pieces)
            {
                planned++;
                if (!piece.Placed) { skipped.Add(piece.Id + ": " + piece.Reason); continue; }
                string why = null;
                try { why = BuildPiece(piece, root.transform); }
                catch (Exception e) { why = "build error (" + e.Message + ")"; Log.Exception("Arrival 04E piece " + piece.Id, e); }
                if (why == null) built++;
                else { piece.Placed = false; piece.Reason = why; skipped.Add(piece.Id + ": " + why); }
            }
            _nessaBoardBuilt = Plan.Find("lofts_board") != null && Plan.Find("lofts_board").Placed;

            bool conduit = false;
            if (Plan.ConduitPlaced)
            {
                string why = null;
                try { why = BuildConduit(root.transform); }
                catch (Exception e) { why = "build error (" + e.Message + ")"; Log.Exception("Arrival 04E conduit", e); }
                conduit = why == null;
                if (conduit) { ConduitPath.Clear(); ConduitPath.AddRange(Plan.ConduitRoute); }
                else { Plan.ConduitReason = why; skipped.Add("conduit: " + why); }
            }
            else skipped.Add("conduit: " + Plan.ConduitReason);

            bool crown = false;
            if (Plan.RoofCrownPlaced)
            {
                string why = null;
                try { why = BuildRoofCrown(root.transform); }
                catch (Exception e) { why = "build error (" + e.Message + ")"; Log.Exception("Arrival 04E roof crown", e); }
                crown = why == null;
                if (!crown) skipped.Add("roof crown: " + why);
            }
            else skipped.Add("roof crown: " + Plan.RoofCrownReason);

            try { _controller.BindVenues(city); }
            catch (Exception e) { Log.Exception("Arrival 04E venue effects", e); skipped.Add("venue effects: " + e.Message); }

            Built = true;
            int total = planned + 2;
            int done = built + (conduit ? 1 : 0) + (crown ? 1 : 0);
            Status = done + " of " + total + " arrival set pieces built" + (skipped.Count == 0 ? "." : "; skipped " + string.Join("; ", skipped.ToArray()) + ".");
            ModuleStatus.Report("arrival_04e", "Arrival neighborhood", done == total ? ModuleState.Loaded : ModuleState.Partial,
                done == total ? "" : Status, done == total ? null : "The skipped pieces leave the original street in place; nothing else is affected.");
            ModuleStatus.SetCount("arrival_04e", done, "arrival set pieces built");
            Log.Info("ARRIVAL_SCENE_04E pieces=" + built + "/" + planned + " conduit=" + (conduit ? "built" : "skipped") + " crown=" + (crown ? "built" : "skipped") +
                (skipped.Count == 0 ? "" : " skipped: " + string.Join("; ", skipped.ToArray())));
        }

        /// <summary>The STATUS row for the four new venues: how many were built on their parcels and why any were not.</summary>
        private static void ReportVenues(InhabitedCitySystem city)
        {
            if (city == null) { ModuleStatus.Report("venues_04e", "New venues (04E)", ModuleState.Failed, "The inhabited city did not start."); ModuleStatus.SetCount("venues_04e", 0, "of 4 new venues built"); return; }
            if (!city.EnabledAtBoot)
            {
                ModuleStatus.Report("venues_04e", "New venues (04E)", ModuleState.Disabled, "Inhabited city venues are turned off in Settings.", "Settings > Inhabited city, then restart.");
                ModuleStatus.SetCount("venues_04e", 0, "of 4 new venues built");
                return;
            }
            int ready = 0;
            List<string> missing = new List<string>();
            foreach (string id in VenueIds04E.All)
            {
                VenueBinding04C v = city.FindVenue(id);
                if (v != null && v.Available) ready++;
                else missing.Add((v != null ? v.Definition.Title : id) + ": " + (v != null ? v.Status : "not in the venue catalog"));
            }
            ModuleStatus.Report("venues_04e", "New venues (04E)", ready == 4 ? ModuleState.Loaded : ready > 0 ? ModuleState.Partial : ModuleState.Failed,
                ready == 4 ? "" : string.Join(" ", missing.ToArray()),
                ready == 4 ? null : "Unbuilt venues keep the original building; quests that need them say so.");
            ModuleStatus.SetCount("venues_04e", ready, "of 4 new venues built");
        }
    }

    /// <summary>Reset hub for every static the VENUES work stream adds.</summary>
    public static class VenueStatics04E
    {
        public static void ResetStatics()
        {
            ArrivalScene04E.ResetStatics();
            VenueRoomTones04E.ResetStatics();
        }
    }
}
