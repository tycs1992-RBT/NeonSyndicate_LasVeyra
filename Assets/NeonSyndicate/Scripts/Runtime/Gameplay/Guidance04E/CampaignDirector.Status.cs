using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// CampaignDirector, the STATUS pause page (after SAVE / LOAD): the module label and the source revision (editor Git
    /// files, or the packaged build manifest, or "revision unavailable"), the save mode and the world region, the
    /// preferences that switch earlier content off, every installed 04E destination with the reason when one is missing
    /// (including whether the opening giver is a person, a terminal or absent), companion and fusion availability, every
    /// module row the boot reported with its detail, next action and count, and a Track opening lead button. Nothing on
    /// it shows usernames, credentials or full paths. Its text is rebuilt twice a second, not every frame.
    /// </summary>
    public sealed partial class CampaignDirector
    {
        private const int StatusRowsPerPage = 7;
        private int _statusPage;
        private float _statusBuiltAt = -10f;
        private string _statusLeft = "";
        private readonly List<string> _statusRows = new List<string>();
        private readonly List<Color> _statusColors = new List<Color>();

        private void DrawStatus(Rect r)
        {
            UIKit.Rect(r, UIKit.Panel);
            if (Time.unscaledTime - _statusBuiltAt > 0.5f) BuildStatusText();
            float x = r.x + 24f, y = r.y + 14f;
            float colW = (r.width - 72f) * 0.5f;
            UIKit.Text(new Rect(x, y, r.width - 48f, 36f), ModuleStatus.Label.ToUpperInvariant(), 30, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Bold);
            UIKit.Text(new Rect(x, y + 4f, r.width - 48f, 30f), Game.Version, 16, new Color(0.75f, 0.78f, 0.82f), TextAnchor.UpperRight);
            y += 44f;
            UIKit.Text(new Rect(x, y, colW, r.yMax - y - 70f), _statusLeft, 17, Color.white, TextAnchor.UpperLeft, FontStyle.Normal, true, true);
            // Modules, paged.
            float rx = x + colW + 24f;
            UIKit.Text(new Rect(rx, y, colW, 26f), "MODULES (live runtime registration)", 19, UIKit.Cyan, TextAnchor.UpperLeft, FontStyle.Bold);
            int pages = Mathf.Max(1, (_statusRows.Count + StatusRowsPerPage - 1) / StatusRowsPerPage);
            _statusPage = Mathf.Clamp(_statusPage, 0, pages - 1);
            float ry = y + 32f;
            for (int i = _statusPage * StatusRowsPerPage; i < Mathf.Min(_statusRows.Count, (_statusPage + 1) * StatusRowsPerPage); i++)
            {
                UIKit.Rect(new Rect(rx, ry + 4f, 6f, 70f), _statusColors[i]);
                UIKit.Text(new Rect(rx + 14f, ry, colW - 14f, 84f), _statusRows[i], 16, Color.white, TextAnchor.UpperLeft, FontStyle.Normal, true, true);
                ry += 88f;
            }
            float by = r.yMax - 60f;
            if (pages > 1)
            {
                if (UIKit.Button(new Rect(rx, by, 170f, 44f), "Prev page", false, 18)) _statusPage = (_statusPage + pages - 1) % pages;
                if (UIKit.Button(new Rect(rx + 180f, by, 170f, 44f), "Next page", false, 18)) _statusPage = (_statusPage + 1) % pages;
                UIKit.Text(new Rect(rx + 360f, by + 10f, 200f, 26f), "Page " + (_statusPage + 1) + " of " + pages, 17, Color.white, TextAnchor.UpperLeft);
            }
            if (UIKit.Button(new Rect(x, by, 320f, 44f), "Track opening lead", false, 19))
            {
                string id = MainLeadQuest();
                if (id.Length == 0) Notify(Game.Quests == null || Game.Quests.Find(QuestIds04E.Main[0]) == null
                    ? "The campaign quests are not loaded in this build." : "Every main quest is complete.", 4f);
                else TrackQuestLead(id, true);
                _statusBuiltAt = -10f;
            }
        }

        private void BuildStatusText()
        {
            _statusBuiltAt = Time.unscaledTime;
            StringBuilder sb = new StringBuilder();
            sb.Append("<b>Build:</b> ").Append(BuildInfo04E.Describe()).Append('\n');
            bool freeRoam = Game.Root != null && Game.Root.FreeRoam;
            int story = Game.Missions != null ? Game.Missions.StoryProgress : 0;
            int total = Game.Missions != null ? Game.Missions.Story.Count : 0;
            sb.Append("<b>Save mode:</b> ").Append(freeRoam ? "Free Roam" : "Story, " + story + " of " + total + " missions passed")
              .Append(_sessionActive ? (_sessionLoaded ? " (loaded save)" : " (new session)") : "").Append('\n');
            sb.Append("<b>Region:</b> ").Append(RegionText()).Append('\n');
            sb.Append("<b>Preferences:</b> inhabited venues ").Append(InhabitedCitySettings04C.Enabled ? "on" : "OFF (Settings, applies at the next start)")
              .Append(", residents ").Append(InhabitedCitySettings04C.Residents ? "on" : "off").Append(", room sound ").Append(InhabitedCitySettings04C.Ambience ? "on" : "off")
              .Append(", graphics ").Append(Settings.Graphics).Append("\n\n");
            sb.Append("<b><color=#4cf0ff>INSTALLED DESTINATIONS</color></b>\n");
            foreach (Destination04E d in InstalledDestinations())
            {
                sb.Append(d.Installed ? "<color=#60ff80>installed</color>  " : "<color=#ff7060>missing</color>  ").Append(d.Name);
                if (!d.Installed && !string.IsNullOrEmpty(d.Reason)) sb.Append(" <color=#bbbbbb>(").Append(d.Reason).Append(")</color>");
                sb.Append('\n');
            }
            sb.Append('\n').Append("<b>Opening giver:</b> ").Append(GiverPresence(CastIds04E.Nessa)).Append('\n');
            sb.Append("<b>Cast:</b> ").Append(CastSummary()).Append('\n');
            sb.Append("<b>Underworld entrance:</b> ").Append(UnderText()).Append('\n');
            sb.Append("<b>Companion:</b> ").Append(CompanionText()).Append('\n');
            sb.Append("<b>Fusion:</b> ").Append(FusionText());
            _statusLeft = sb.ToString();

            _statusRows.Clear();
            _statusColors.Clear();
            foreach (ModuleRecord m in ModuleStatus.All)
            {
                if (m == null) continue;
                StringBuilder row = new StringBuilder();
                row.Append("<b>").Append(m.Title).Append("</b>  ").Append(StateWord(m.State));
                if (m.Count >= 0) row.Append("   <color=#ffd24d>").Append(m.Count).Append(' ').Append(m.CountLabel).Append("</color>");
                if (!string.IsNullOrEmpty(m.Detail)) row.Append("\n<size=15>").Append(m.Detail).Append("</size>");
                if (!string.IsNullOrEmpty(m.NextAction)) row.Append("\n<size=15><color=#9fe7ff>Next: ").Append(m.NextAction).Append("</color></size>");
                _statusRows.Add(row.ToString());
                _statusColors.Add(StateColor(m.State));
            }
            if (_statusRows.Count == 0)
            {
                _statusRows.Add("No module reported in this session. The boot did not reach the 04E steps; see the Console.");
                _statusColors.Add(StateColor(ModuleState.Failed));
            }
        }

        private static string StateWord(ModuleState s)
        {
            switch (s)
            {
                case ModuleState.Loaded: return "<color=#60ff80>loaded</color>";
                case ModuleState.Partial: return "<color=#ffd24d>partial</color>";
                case ModuleState.Disabled: return "<color=#bbbbbb>disabled</color>";
                case ModuleState.Failed: return "<color=#ff6060>failed</color>";
                default: return "<color=#888888>not started</color>";
            }
        }

        private static Color StateColor(ModuleState s)
        {
            switch (s)
            {
                case ModuleState.Loaded: return new Color(0.38f, 1f, 0.5f);
                case ModuleState.Partial: return new Color(1f, 0.82f, 0.3f);
                case ModuleState.Disabled: return new Color(0.6f, 0.6f, 0.6f);
                case ModuleState.Failed: return new Color(1f, 0.35f, 0.35f);
                default: return new Color(0.4f, 0.4f, 0.4f);
            }
        }

        private static string RegionText()
        {
            WorldRegion region = WorldContext.RegionAt(Game.PlayerPosition, true);
            switch (region)
            {
                case WorldRegion.Underworld:
                    string id = Game.Underheart != null ? Game.Underheart.PlayerRegion : "";
                    return "under the city, " + UnderheartIds.RegionTitle(id);
                case WorldRegion.Interior:
                    string venue = Game.Inhabited != null ? Game.Inhabited.OccupiedId : "";
                    return "inside " + VenueTitle(venue);
                case WorldRegion.Veil: return "the Veil (Inner World)";
                default: return "the city streets, " + CityLayout.DistrictName(CityLayout.DistrictAt(Game.PlayerPosition));
            }
        }

        /// <summary>"in person at ...", "message terminal at ... (why)" or "not placed (why)".</summary>
        private string GiverPresence(string castId)
        {
            CastSlot s = Slot(castId);
            if (s == null) return "unknown";
            EnsureBound(s);
            if (s.Actor != null) return s.Def.Name + ", in person at " + s.PostName;
            if (s.Terminal != null) return s.Def.Name + ", message terminal at " + s.PostName + " (" + s.SpawnFailure + "); counted as a terminal, not a person";
            string gate = GateReason(s);
            if (gate.Length > 0) return s.Def.Name + ", not placed: " + gate;
            return s.Def.Name + ", post bound at " + s.PostName + " (appears within " + Mathf.RoundToInt(GuidanceRules04E.StreamIn) + " m)";
        }

        private string CastSummary()
        {
            int people = 0, terminals = 0, bound = 0;
            foreach (CastSlot s in _slots)
            {
                if (s.Actor != null) people++;
                if (s.Terminal != null) terminals++;
                if (s.PostBound) bound++;
            }
            return bound + " of " + _slots.Count + " posts bound, " + people + " in the world now, " + terminals + " terminal(s)";
        }

        private static string UnderText()
        {
            UnderheartWorld u = Game.Underheart;
            if (u == null) return "the Underheart did not start";
            if (!u.Ready) return "not built: " + u.Status;
            VenueBinding04C ph = Game.Inhabited != null ? Game.Inhabited.FindVenue(VenueIds04E.Pressure) : null;
            return "the Pressure House stair" + (ph != null && ph.Available ? "" : " (the Pressure House venue did not load)") + ". " + u.Status;
        }

        private static string CompanionText()
        {
            BondSystem b = Game.Bonds;
            if (b == null) return "the bond system did not start";
            CompanionRecord a = b.Active;
            if (a != null)
            {
                string rank = a.Rank >= 0 && a.Rank < BondIds.RankNames.Length ? BondIds.RankNames[a.Rank] : "rank " + a.Rank;
                return a.Name + " (" + BondIds.Title(a.Archetype) + ", " + rank + ")";
            }
            if (b.Roster.Count > 0) return b.Roster.Count + " bonded, none active (choose one at a care station or the BONDS page)";
            return "none yet: the bond chapter is \"A Creature Is Not a Battery\"";
        }

        private static string FusionText()
        {
            PersonalGarage g = Game.Garage;
            if (g == null || Game.Fusion == null) return "the garage or fusion system did not start";
            if (!g.FusionUnlocked) return "not unlocked yet: Jax's chapter \"Two Hearts, One Chassis\"";
            if (Game.Fusion.IsFused) return "fused (" + FusionIds.Title(Game.Fusion.ActiveForm) + ")";
            string why;
            return Game.Fusion.CanFuse(out why) ? "ready" : "unlocked; right now: " + why;
        }
    }
}
