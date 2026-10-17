using System;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Resolves the 04E quest place tokens in the live world: Venue:&lt;venue&gt;.&lt;anchor&gt; (an authored point inside a
    /// 04C or 04E room), Under:&lt;anchor&gt; (an Underheart anchor), Cast:&lt;id&gt; (the cast member's live actor, else their
    /// post), Arsenal:case and Arsenal:lane (the real 04D resonance case and practice lane). Results are exact 3D points:
    /// never projected to the street or ground-snapped from above, so a target under a ceiling or inside a room keeps its
    /// height. Every miss says why, so a quest can wait or fail with a readable reason instead of pointing somewhere wrong.
    /// </summary>
    public static class CampaignPlacesRuntime04E
    {
        private static string _lastWarned = "";

        public static void ResetStatics()
        {
            _lastWarned = "";
        }

        /// <summary>A token's live point and facing, or false with the reason.</summary>
        public static bool TryResolve(string at, out Vector3 pos, out float yaw, out string why)
        {
            pos = Vector3.zero;
            yaw = 0f;
            why = null;
            string bad = CampaignRules04E.PlaceTokenError(at);
            if (bad != null)
            {
                why = "Bad place '" + at + "': " + bad;
                return false;
            }
            try
            {
                if (at.StartsWith(CampaignRules04E.VenuePrefix, StringComparison.Ordinal)) return Venue(at, out pos, out yaw, out why);
                if (at.StartsWith(CampaignRules04E.UnderPrefix, StringComparison.Ordinal)) return Under(at, out pos, out yaw, out why);
                if (at.StartsWith(CampaignRules04E.CastPrefix, StringComparison.Ordinal)) return Cast(at, out pos, out yaw, out why);
                return Arsenal(at, out pos, out yaw, out why);
            }
            catch (Exception e)
            {
                Log.Exception("Quest place " + at, e);
                why = "The place '" + at + "' could not be resolved (see the Console).";
                return false;
            }
        }

        /// <summary>Why a token cannot resolve right now (null when it can).</summary>
        public static string Problem(string at)
        {
            Vector3 p;
            float y;
            string why;
            return TryResolve(at, out p, out y, out why) ? null : why;
        }

        /// <summary>The delegate QuestPlaces uses for tokens (giver spots, fail-condition areas, contract places).</summary>
        public static bool TryResolveForPlaces(string token, out Vector3 pos)
        {
            float yaw;
            string why;
            bool ok = TryResolve(token, out pos, out yaw, out why);
            if (!ok && why != null && why != _lastWarned)
            {
                _lastWarned = why;
                Log.Warn("Quest place: " + why);
            }
            return ok;
        }

        /// <summary>A token, or a named place, site, "District:" or "World" point: where it is now (false when unknown).</summary>
        public static bool TryResolveAny(string at, out Vector3 pos)
        {
            pos = Vector3.zero;
            if (string.IsNullOrEmpty(at)) return false;
            if (CampaignRules04E.IsPlaceToken(at))
            {
                float yaw;
                string why;
                return TryResolve(at, out pos, out yaw, out why);
            }
            if (Game.Layout == null) return false;
            if (QuestPlaces.GiverPosition(Game.Layout, at, 0f, 0f, out pos)) return true;
            return QuestPlaces.Resolve(Game.Layout, at, 0f, 0f, Vector3.zero, Vector3.zero, false, Game.PlayerPosition, 0, out pos);
        }

        // ------------------------------------------------------------------ kinds

        private static bool Venue(string at, out Vector3 pos, out float yaw, out string why)
        {
            pos = Vector3.zero;
            yaw = 0f;
            string v, a;
            CampaignRules04E.SplitVenue(at, out v, out a);
            if (Game.Inhabited == null || !Game.Inhabited.IsOperational)
            {
                why = "The inhabited venues are unavailable, so " + VenueTitle(v) + " cannot be entered.";
                return false;
            }
            VenueBinding04C b = Game.Inhabited.FindVenue(v);
            if (b == null)
            {
                why = VenueTitle(v) + " is not in this build.";
                return false;
            }
            if (!b.Available || b.View == null)
            {
                why = VenueTitle(v) + " is unavailable: " + b.Status;
                return false;
            }
            if (a != "exit" && b.View.LocalAnchor(a) == Vector3.zero)
            {
                why = VenueTitle(v) + " has no '" + a + "' point in its layout.";
                return false;
            }
            pos = b.View.WorldPoint(a);
            yaw = b.View.AnchorYaw(a);
            why = null;
            return MathUtil.IsFinite(pos);
        }

        private static bool Under(string at, out Vector3 pos, out float yaw, out string why)
        {
            pos = Vector3.zero;
            yaw = 0f;
            if (Game.Underheart == null)
            {
                why = "The Underheart did not start.";
                return false;
            }
            if (!Game.Underheart.Ready)
            {
                why = "The Underheart is not available: " + Game.Underheart.Status;
                return false;
            }
            string anchor = CampaignRules04E.UnderAnchor(at);
            if (!Game.Underheart.TryAnchor(anchor, out pos, out yaw))
            {
                why = "The Underheart has no built point '" + anchor + "'.";
                return false;
            }
            why = null;
            return MathUtil.IsFinite(pos);
        }

        private static bool Cast(string at, out Vector3 pos, out float yaw, out string why)
        {
            pos = Vector3.zero;
            yaw = 0f;
            string id = CampaignRules04E.CastOf(at);
            if (Game.Campaign == null)
            {
                why = CastIds04E.Name(id) + " cannot be found: campaign guidance did not start.";
                return false;
            }
            NpcController actor = Game.Campaign.CastActor(id);
            if (actor != null && !actor.IsDead)
            {
                pos = actor.transform.position;
                yaw = actor.transform.eulerAngles.y;
                why = null;
                return true;
            }
            if (Game.Campaign.CastPost(id, out pos, out yaw))
            {
                why = null;
                return MathUtil.IsFinite(pos);
            }
            string reason;
            Game.Campaign.CastAvailable(id, out reason);
            why = CastIds04E.Name(id) + " is not at a post: " + (string.IsNullOrEmpty(reason) ? "unknown reason" : reason);
            return false;
        }

        private static bool Arsenal(string at, out Vector3 pos, out float yaw, out string why)
        {
            pos = Vector3.zero;
            yaw = 0f;
            if (Game.Arsenal == null)
            {
                why = "The crystal arsenal (04D) did not start.";
                return false;
            }
            pos = at == CampaignRules04E.ArsenalLane ? Game.Arsenal.LaneShooter : Game.Arsenal.CasePosition;
            if (pos == Vector3.zero)
            {
                why = "The resonance case has not been placed yet: walk near the Veyra Lofts arrival.";
                return false;
            }
            why = null;
            return MathUtil.IsFinite(pos);
        }

        // ------------------------------------------------------------------ names

        /// <summary>The readable title of a venue id (the live catalog title when bound).</summary>
        public static string VenueTitle(string v)
        {
            VenueBinding04C b = Game.Inhabited != null ? Game.Inhabited.FindVenue(v) : null;
            if (b != null && b.Definition != null && !string.IsNullOrEmpty(b.Definition.Title)) return b.Definition.Title;
            switch (v)
            {
                case VenueIds04E.Dispatch: return "Cinder Dispatch";
                case VenueIds04E.Pressure: return "The Pressure House";
                case VenueIds04E.Bondkeeper: return "Bondkeeper's Refuge";
                case VenueIds04E.Office: return "The Resonance Office";
                case "lofts": return "Veyra Lofts";
                case "clinic": return "Mara Solane Street Clinic";
                case "forge": return "Dante Rook's Crystal Forge";
                case "inn": return "Cinder Diner";
                case "garage": return "Jax Calder Customs";
                case "archive": return "Resonance Archive";
                case "observatory": return "Sundial Survey Observatory";
                case "depot": return "Rift Freight Depot";
                default: return v ?? "";
            }
        }

        /// <summary>A short readable name for a place token ("Cinder Dispatch", "Lantern Refuge", "Jax Calder").</summary>
        public static string Label(string at)
        {
            if (string.IsNullOrEmpty(at)) return "";
            string v, a;
            if (CampaignRules04E.SplitVenue(at, out v, out a)) return VenueTitle(v);
            if (at.StartsWith(CampaignRules04E.UnderPrefix, StringComparison.Ordinal)) return UnderheartIds.RegionTitle(CampaignRules04E.RegionOf(at));
            string c = CampaignRules04E.CastOf(at);
            if (c.Length > 0) return CastIds04E.Name(c);
            if (at == CampaignRules04E.ArsenalLane) return "the practice lane";
            if (at == CampaignRules04E.ArsenalCase) return "the resonance case";
            return at;
        }
    }
}
