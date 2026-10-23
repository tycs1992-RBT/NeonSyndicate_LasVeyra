using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// QuestBook, H8S guidance for the running quest through the existing tracker and journal: the tracker line gains
    /// the objective's distance and its direction from where the camera looks ("85 m ahead left, above"), rebuilt only
    /// when those words change; the journal shows where the current step points (one label rule for Realm:, Venue:,
    /// Under: and Cast: words, with its kind) and the phase hint; and each phase's hint (QuestHintsH8S, keyed by quest
    /// and phase id, so no quest file changes) shows once as a toast when the phase begins.
    /// </summary>
    public sealed partial class QuestBook
    {
        private int _whereKeyH8S = int.MinValue;
        private string _whereH8S = "";
        /// <summary>The distance words the tracker line was last built with (compared by reference).</summary>
        private string _trackerWhereH8S;
        private int _trackerMaxFontH8S = 19;
        private QuestRun _hintRunH8S;
        private int _hintPhaseH8S = -1;
        private int _journalKeyH8S = int.MinValue;
        private QuestRun _journalRunH8S;
        private string _journalWhereSourceH8S;
        private string _journalWhereH8S = "";
        private string _journalHintH8S = "";

        /// <summary>The current step of a run (null when out of range).</summary>
        private static QuestStep CurrentStepH8S(QuestRun r)
        {
            if (r == null || r.Quest == null || r.Quest.Phases == null || r.Phase < 0 || r.Phase >= r.Quest.Phases.Count) return null;
            QuestPhase ph = r.Quest.Phases[r.Phase];
            if (ph == null || ph.Steps == null || r.Step < 0 || r.Step >= ph.Steps.Count) return null;
            return ph.Steps[r.Step];
        }

        /// <summary>The id of the run's current phase ("" when out of range).</summary>
        private static string PhaseIdH8S(QuestRun r)
        {
            if (r == null || r.Quest == null || r.Quest.Phases == null || r.Phase < 0 || r.Phase >= r.Quest.Phases.Count) return "";
            QuestPhase ph = r.Quest.Phases[r.Phase];
            return ph != null ? ph.Id ?? "" : "";
        }

        /// <summary>
        /// The distance and direction to the objective ("" when the run has no target now). A new string is made only
        /// when the words change, so callers rebuild their own text when the reference differs from the one they used.
        /// </summary>
        private string TrackerWhereH8S()
        {
            Gps gps = Game.Gps;
            PlayerController pl = Game.Player;
            if (gps == null || pl == null || !gps.HasMissionTarget)
            {
                if (_whereKeyH8S != int.MinValue)
                {
                    _whereKeyH8S = int.MinValue;
                    _whereH8S = "";
                }
                return _whereH8S;
            }
            Vector3 target = gps.MissionTarget;
            Vector3 pp = Game.PlayerPosition;
            Vector3 view = Game.Cam != null ? Game.Cam.transform.forward : pl.transform.forward;
            int bucket = GuideTextRulesH8S.DistanceBucket(MathUtil.FlatDistance(pp, target));
            int sector = GuideTextRulesH8S.Sector(view, target - pp);
            int vertical = GuideTextRulesH8S.Vertical(target.y - pp.y);
            int key = GuideTextRulesH8S.Key(bucket, sector, vertical);
            if (key != _whereKeyH8S)
            {
                _whereKeyH8S = key;
                _whereH8S = GuideTextRulesH8S.Describe(bucket, sector, vertical);
            }
            return _whereH8S;
        }

        /// <summary>Shows the phase's hint once as a toast when a phase begins (only phases that have one).</summary>
        private void PhaseHintToastH8S(QuestRun r)
        {
            if (r == null || (r == _hintRunH8S && r.Phase == _hintPhaseH8S)) return;
            _hintRunH8S = r;
            _hintPhaseH8S = r.Phase;
            string hint = QuestHintsH8S.For(r.Quest != null ? r.Quest.Id : "", PhaseIdH8S(r));
            if (hint.Length > 0 && Game.Hud != null) Game.Hud.Notify("Hint: " + hint, 7f);
        }

        /// <summary>The readable label of a place word, the same rule for every kind (with the live titles).</summary>
        public static string PlaceLabelH8S(string token)
        {
            switch (GuideTextRulesH8S.KindOf(token))
            {
                case PlaceKindH8S.Realm: return GuideTextRulesH8S.RealmTitle(token);
                case PlaceKindH8S.Venue:
                case PlaceKindH8S.Under:
                case PlaceKindH8S.Cast: return CampaignPlacesRuntime04E.Label(token);
                case PlaceKindH8S.Named: return GuideTextRulesH8S.Label(token, null, null, null);
                default: return "";
            }
        }

        /// <summary>The journal's two H8S lines for the running quest: where the step points, and the phase hint.</summary>
        private float DrawJournalGuideH8S(Rect r, float y, QuestRun run)
        {
            string where = TrackerWhereH8S();
            QuestStep step = CurrentStepH8S(run);
            string token = step != null ? step.At : "";
            int key = run.Phase * 1000 + run.Step;
            if (run != _journalRunH8S || key != _journalKeyH8S || !ReferenceEquals(where, _journalWhereSourceH8S))
            {
                _journalRunH8S = run;
                _journalKeyH8S = key;
                _journalWhereSourceH8S = where;
                string label = PlaceLabelH8S(token);
                string tag = GuideTextRulesH8S.KindTag(GuideTextRulesH8S.KindOf(token));
                string place = label.Length > 0 ? label + (tag.Length > 0 ? " (" + tag + ")" : "") : "";
                string line = place.Length > 0 && where.Length > 0 ? place + ", " + where : (place.Length > 0 ? place : where);
                _journalWhereH8S = line.Length > 0 ? "<b>Where:</b> " + line : "";
                string hint = QuestHintsH8S.For(run.Quest != null ? run.Quest.Id : "", PhaseIdH8S(run));
                _journalHintH8S = hint.Length > 0 ? "<b>Hint:</b> " + hint : "";
            }
            int font = AccessibilityRulesH8S.Font(20, Settings.GuideTextScaleH8S, 26);
            if (_journalWhereH8S.Length > 0)
            {
                UIKit.Text(new Rect(r.x, y, r.width, 32f), _journalWhereH8S, font, new Color(1f, 0.86f, 0.45f), TextAnchor.UpperLeft, FontStyle.Normal, true, true);
                y += font + 14f;
            }
            if (_journalHintH8S.Length > 0)
            {
                UIKit.Text(new Rect(r.x, y, r.width * 0.8f, font * 2f + 16f), _journalHintH8S, font, new Color(0.75f, 0.9f, 1f), TextAnchor.UpperLeft, FontStyle.Normal, true, true);
                y += font * 2f + 20f;
            }
            return y;
        }
    }
}
