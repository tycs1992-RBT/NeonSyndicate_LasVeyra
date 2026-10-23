using System;
using System.Globalization;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>The kind of place a quest step points at, read from its place word (H8S waypoint labels).</summary>
    public enum PlaceKindH8S
    {
        None = 0,
        /// <summary>"Realm:" anchors of the crystal realms (the stair porch, the witness chamber, the root spring).</summary>
        Realm = 1,
        /// <summary>"Venue:" rooms and doors of a walk-in venue.</summary>
        Venue = 2,
        /// <summary>"Under:" anchors in the Underheart below the city.</summary>
        Under = 3,
        /// <summary>"Cast:" a named person at their post.</summary>
        Cast = 4,
        /// <summary>Any other named place (a site, a point of interest, a district).</summary>
        Named = 5
    }

    /// <summary>
    /// The words of the H8S guidance: rounded distances that only change in sensible steps (so the tracker text is
    /// rebuilt rarely), the direction of a target relative to where the camera looks (ahead, ahead left, behind right,
    /// and above or below), and one consistent label and tag per place kind for Realm:, Venue:, Under: and Cast: words.
    /// Venue, region and person names come in through functions so the live titles are used. Engine-free and tested.
    /// </summary>
    public static class GuideTextRulesH8S
    {
        /// <summary>Within this flat distance the target counts as "here".</summary>
        public const float HereDistance = 4f;
        /// <summary>A height difference beyond this reads as above or below.</summary>
        public const float VerticalThreshold = 3f;

        private static readonly string[] SectorWords = { "ahead", "ahead right", "right", "behind right", "behind", "behind left", "left", "ahead left" };

        /// <summary>The distance rounded to a readable step: 0 means here, then 5 m steps under 50, 10 m under 200, 25 m under 1 km, 100 m beyond.</summary>
        public static int DistanceBucket(float metres)
        {
            if (!(metres >= HereDistance)) return 0;
            if (float.IsInfinity(metres)) return 99900;
            if (metres < 50f) return Mathf.Max(5, Mathf.RoundToInt(metres / 5f) * 5);
            if (metres < 200f) return Mathf.RoundToInt(metres / 10f) * 10;
            if (metres < 1000f) return Mathf.RoundToInt(metres / 25f) * 25;
            return Mathf.Min(99900, Mathf.RoundToInt(metres / 100f) * 100);
        }

        /// <summary>"here", "85 m" or "1.2 km".</summary>
        public static string DistanceText(int bucket)
        {
            if (bucket <= 0) return "here";
            if (bucket >= 1000) return (bucket / 1000f).ToString("0.0", CultureInfo.InvariantCulture) + " km";
            return bucket + " m";
        }

        /// <summary>
        /// Which of eight directions the target lies in, relative to where the view faces (flat): 0 ahead, 1 ahead right,
        /// 2 right, 3 behind right, 4 behind, 5 behind left, 6 left, 7 ahead left; -1 when either direction is unusable.
        /// </summary>
        public static int Sector(Vector3 viewForward, Vector3 toTarget)
        {
            float fx = viewForward.x, fz = viewForward.z, tx = toTarget.x, tz = toTarget.z;
            if (fx * fx + fz * fz < 1e-6f || tx * tx + tz * tz < 1e-6f) return -1;
            float delta = Mathf.DeltaAngle(Mathf.Atan2(fx, fz) * Mathf.Rad2Deg, Mathf.Atan2(tx, tz) * Mathf.Rad2Deg);
            float a = delta < 0f ? delta + 360f : delta;
            return Mathf.FloorToInt((a + 22.5f) / 45f) % 8;
        }

        /// <summary>The word for a sector ("" for -1).</summary>
        public static string SectorWord(int sector)
        {
            return sector >= 0 && sector < SectorWords.Length ? SectorWords[sector] : "";
        }

        /// <summary>1 above, -1 below, 0 level, from the target's height minus the player's.</summary>
        public static int Vertical(float heightDifference)
        {
            if (heightDifference > VerticalThreshold) return 1;
            if (heightDifference < -VerticalThreshold) return -1;
            return 0;
        }

        /// <summary>A number that changes only when the words would: rebuild the text when this key changes.</summary>
        public static int Key(int bucket, int sector, int vertical)
        {
            return bucket * 32 + (sector + 1) * 3 + (Mathf.Clamp(vertical, -1, 1) + 1);
        }

        /// <summary>"85 m ahead left, above", "here", "1.2 km behind". The direction is left out within HereDistance.</summary>
        public static string Describe(int bucket, int sector, int vertical)
        {
            string d = DistanceText(bucket);
            string v = vertical > 0 ? "above" : (vertical < 0 ? "below" : "");
            if (bucket <= 0) return v.Length > 0 ? d + ", " + v : d;
            string s = SectorWord(sector);
            string text = s.Length > 0 ? d + " " + s : d;
            return v.Length > 0 ? text + ", " + v : text;
        }

        // ================================================================== place words

        /// <summary>The kind of a place word.</summary>
        public static PlaceKindH8S KindOf(string token)
        {
            if (string.IsNullOrEmpty(token)) return PlaceKindH8S.None;
            if (token.StartsWith("Realm:", StringComparison.Ordinal)) return PlaceKindH8S.Realm;
            if (token.StartsWith(CampaignRules04E.VenuePrefix, StringComparison.Ordinal)) return PlaceKindH8S.Venue;
            if (token.StartsWith(CampaignRules04E.UnderPrefix, StringComparison.Ordinal)) return PlaceKindH8S.Under;
            if (token.StartsWith(CampaignRules04E.CastPrefix, StringComparison.Ordinal)) return PlaceKindH8S.Cast;
            if (token == "Giver" || token == "Previous" || token == "Player" || token == "World") return PlaceKindH8S.None;
            return PlaceKindH8S.Named;
        }

        /// <summary>The small tag drawn after a waypoint label, the same for every place of one kind.</summary>
        public static string KindTag(PlaceKindH8S kind)
        {
            switch (kind)
            {
                case PlaceKindH8S.Realm: return "realm";
                case PlaceKindH8S.Venue: return "inside";
                case PlaceKindH8S.Under: return "below the city";
                case PlaceKindH8S.Cast: return "person";
                default: return "";
            }
        }

        /// <summary>A readable name for a "Realm:" anchor ("Realm:hearth-lens" gives "Hearth Lens porch").</summary>
        public static string RealmTitle(string token)
        {
            if (string.IsNullOrEmpty(token) || !token.StartsWith("Realm:", StringComparison.Ordinal)) return "";
            string id = token.Substring(6);
            switch (id)
            {
                case "hearth-lens": return "Hearth Lens porch";
                case "oath-witness": return "Oath witness chamber";
                case "oath-seal": return "Oath seal";
                case "oath-terrace": return "Oath terrace";
                case "storm-shrine": return "Storm shrine";
                case "storm-spring": return "Root spring";
                case "storm-crossing": return "Storm crossing";
                case "storm-vein": return "Storm vein";
                case "storm-cavern": return "Storm cavern";
                case "storm-ledge": return "Storm ledge";
                case "castellan": return "Castellan Court";
                case "sovereign": return "Sovereign Court";
                case "gallery": return "Castellan gallery";
                case "threshold": return "Crystal Threshold";
                case "grotto": return "Shardwood grotto";
                case "observation": return "Observation point";
                case "forge-scar": return "Forge Scar";
                case "azure": return "Azure glade";
                default: return TitleCase(id);
            }
        }

        /// <summary>
        /// The waypoint label for a place word, the same rule for every kind: a realm anchor's title, a venue's live title,
        /// an Underheart region's title, a person's name, or a named place as written. venueTitle, regionTitle and
        /// castName may be null (the raw id is used then).
        /// </summary>
        public static string Label(string token, Func<string, string> venueTitle, Func<string, string> regionTitle, Func<string, string> castName)
        {
            switch (KindOf(token))
            {
                case PlaceKindH8S.Realm: return RealmTitle(token);
                case PlaceKindH8S.Venue:
                {
                    string venue, anchor;
                    if (!CampaignRules04E.SplitVenue(token, out venue, out anchor)) return token;
                    string title = venueTitle != null ? venueTitle(venue) : venue;
                    return string.IsNullOrEmpty(title) ? venue : title;
                }
                case PlaceKindH8S.Under:
                {
                    string region = CampaignRules04E.RegionOf(token);
                    string title = regionTitle != null ? regionTitle(region) : region;
                    return string.IsNullOrEmpty(title) ? token.Substring(CampaignRules04E.UnderPrefix.Length) : title;
                }
                case PlaceKindH8S.Cast:
                {
                    string id = CampaignRules04E.CastOf(token);
                    string name = castName != null ? castName(id) : id;
                    return string.IsNullOrEmpty(name) ? id : name;
                }
                case PlaceKindH8S.Named: return token.StartsWith(QuestPlaces.DistrictPrefix, StringComparison.Ordinal) ? token.Substring(QuestPlaces.DistrictPrefix.Length) : token;
                default: return "";
            }
        }

        private static string TitleCase(string id)
        {
            if (string.IsNullOrEmpty(id)) return "";
            char[] c = id.Replace('-', ' ').Replace('_', ' ').ToCharArray();
            bool start = true;
            for (int i = 0; i < c.Length; i++)
            {
                if (c[i] == ' ') { start = true; continue; }
                if (start) c[i] = char.ToUpperInvariant(c[i]);
                start = false;
            }
            return new string(c);
        }
    }
}
