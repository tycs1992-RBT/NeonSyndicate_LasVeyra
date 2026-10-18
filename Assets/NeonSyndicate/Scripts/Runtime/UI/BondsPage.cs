using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The BONDS pause page (Order 12): every bonded companion with its kind, rank and state on the left; on the right
    /// the selected one in full: rename (keyboard), role, rank and trust toward the next rank, health and state with the
    /// reason, both assists with their real numbers and cooldowns, fusion readiness and form, the Harmonized
    /// specialization, the evolution stage, crystal tuning with the previewed cost against what you hold, care status,
    /// and make-active, send-home and summon (out of combat). With no companion yet it says where the first one is.
    /// </summary>
    public static class BondsPage
    {
        private static int _selected;
        private static string _message = "";
        private static string _rename = "";
        private static int _renameUid;
        private static GUIStyle _field;

        public static void ResetStatics()
        {
            _selected = 0;
            _message = "";
            _rename = "";
            _renameUid = 0;
            _field = null;
        }

        public static void Register()
        {
            MenuPages.Register(new MenuPage { Title = "BONDS", Order = 12, Draw = Draw });
        }

        public static void Draw(Rect r)
        {
            UIKit.Rect(r, UIKit.Panel);
            BondSystem b = Game.Bonds;
            float pad = 20f;
            UIKit.Text(new Rect(r.x + pad, r.y + 10f, 400f, 40f), "BONDS", 32, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Bold);
            UIKit.Text(new Rect(r.x + 170f, r.y + 20f, r.width - 190f, 24f), "Companions are friends, not weapons: they follow, help on command and grow through quests, care and crystal tuning.", 16, new Color(0.82f, 0.82f, 0.85f), TextAnchor.UpperLeft);
            if (b == null)
            {
                UIKit.Text(new Rect(r.x + 40f, r.y + 80f, r.width - 80f, 40f), "Bonds are not available in this session (see the STATUS page).", 22, Color.white, TextAnchor.UpperLeft);
                return;
            }
            if (b.Roster.Count == 0)
            {
                DrawEmpty(new Rect(r.x + pad, r.y + 70f, r.width - pad * 2f, r.height - 90f), b);
                return;
            }
            float listW = Mathf.Min(420f, r.width * 0.3f);
            Rect list = new Rect(r.x + pad, r.y + 64f, listW, r.height - 84f);
            Rect detail = new Rect(list.xMax + pad, list.y, r.xMax - list.xMax - pad * 2f, list.height);
            CompanionRecord active = b.Active;
            CompanionRecord sel = b.Find(_selected);
            if (sel == null) sel = active ?? b.Roster[0];
            _selected = sel.Uid;
            float rowH = 72f;
            for (int i = 0; i < b.Roster.Count; i++)
            {
                CompanionRecord c = b.Roster[i];
                if (c == null || !BondIds.Has(c.Archetype)) continue;
                DrawRow(new Rect(list.x, list.y + i * (rowH + 6f), list.width, rowH), c, c == sel, c == active);
            }
            float ly = list.y + b.Roster.Count * (rowH + 6f) + 10f;
            UIKit.Text(new Rect(list.x, ly, list.width, 80f), ControlsText(), 15, new Color(0.75f, 0.8f, 0.86f), TextAnchor.UpperLeft, FontStyle.Normal, true, true);
            DrawDetail(detail, b, sel, sel == active);
        }

        private static string ControlsText()
        {
            if (Pad.Connected && Pad.LastUsed)
                return "On foot: " + Pad.Tag(PadButton.LB) + " + D-pad down to make it follow or wait (hold to call it), " + Pad.Tag(PadButton.LB) + " + D-pad up for its assist.";
            return "On foot: tap N to make it follow or wait, hold N to call it to your side, U for its assist.";
        }

        private static void DrawEmpty(Rect r, BondSystem b)
        {
            UIKit.Text(new Rect(r.x, r.y, r.width, 34f), "NOT BONDED YET", 26, Color.white, TextAnchor.UpperLeft, FontStyle.Bold);
            string text = b.RescueActive
                ? "A rescue is under way: release the extraction clamps without attacking the creature, keep your distance while it warns, walk in calmly and tune its resonance."
                : "The first bond comes from the main lead A Creature Is Not a Battery: Odile Marsh at Bondkeeper's Refuge asks you to free a clamped quartz drakeling on the Pressure House service level. Free it without harming it and it will follow you, find the safe way and help in a fight.";
            UIKit.Text(new Rect(r.x, r.y + 44f, Mathf.Min(r.width, 1100f), 120f), text, 19, new Color(0.88f, 0.9f, 0.94f), TextAnchor.UpperLeft, FontStyle.Normal, true, true);
            QuestBook book = Game.Quests;
            string leadId = Game.Campaign != null ? Game.Campaign.CompanionLeadId04F() : "";
            QuestDef lead = book != null ? book.Find(leadId) : null;
            QuestDef rescue = book != null ? book.Find(GuidanceRules04F.RescueQuest) : null;
            string gate = rescue != null ? book.WhyLocked(rescue, null) : "The rescue quest is not loaded (see STATUS).";
            string route = lead != null
                ? "Next real prerequisite: " + lead.Title + ". Meet " + Game.Campaign.GiverPlace(lead) + "."
                : (rescue != null && book.IsDone(rescue.Id)
                    ? "The rescue is already complete, but no bonded roster entry is present. See STATUS; the game will not manufacture a duplicate companion."
                    : "The rescue route is unavailable. See STATUS for loaded quest and companion modules.");
            UIKit.Text(new Rect(r.x, r.y + 152f, Mathf.Min(r.width, 1100f), 64f), route
                + (string.IsNullOrEmpty(gate) ? " Rescue prerequisites are satisfied." : " Rescue prerequisite: " + gate),
                17, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Normal, true, true);
            if (lead != null && UIKit.Button(new Rect(r.x, r.y + 222f, 300f, 42f), "TRACK COMPANION LEAD", false, 18))
            {
                Game.Campaign.TrackCompanionLead04F();
                _message = "Lead tracked. Close the menu and follow the entrance / floor route. Nothing has been accepted yet.";
            }
            if (!string.IsNullOrEmpty(_message))
                UIKit.Text(new Rect(r.x + 320f, r.y + 226f, r.width - 320f, 42f), _message, 16, UIKit.Cyan,
                    TextAnchor.UpperLeft, FontStyle.Normal, true, true);
            float y = r.y + 286f;
            for (int i = 0; i < CompanionArchetypes.All.Length; i++)
            {
                CompanionProfile p = CompanionArchetypes.All[i];
                UIKit.Rect(new Rect(r.x, y, 6f, 52f), p.Accent);
                UIKit.Text(new Rect(r.x + 16f, y, 600f, 26f), p.Title, 20, p.Accent, TextAnchor.UpperLeft, FontStyle.Bold);
                UIKit.Text(new Rect(r.x + 16f, y + 26f, Mathf.Min(r.width - 20f, 1100f), 24f), p.Role, 16, new Color(0.8f, 0.82f, 0.86f), TextAnchor.UpperLeft);
                y += 64f;
            }
        }

        private static void DrawRow(Rect r, CompanionRecord c, bool selected, bool active)
        {
            bool hot, focused;
            if (ProgressionUI.Hotspot(r, out hot, out focused)) _selected = c.Uid;
            CompanionProfile p = BondSystem.ProfileOf(c);
            UIKit.Rect(r, selected ? new Color(1f, 1f, 1f, 0.14f) : (hot ? new Color(1f, 1f, 1f, 0.09f) : new Color(0f, 0f, 0f, 0.45f)));
            UIKit.Rect(new Rect(r.x, r.y, 6f, r.height), p.Accent);
            UIKit.Text(new Rect(r.x + 16f, r.y + 6f, r.width - 120f, 26f), BondSystem.NameOf(c) + (active ? "  (active)" : ""), 20, Color.white, TextAnchor.UpperLeft, FontStyle.Bold);
            UIKit.Text(new Rect(r.x + 16f, r.y + 32f, r.width - 20f, 20f), p.Title + "  " + BondRules.RankName(c.Rank), 15, p.Accent, TextAnchor.UpperLeft, FontStyle.Bold);
            UIKit.Text(new Rect(r.x + 16f, r.y + 50f, r.width - 20f, 20f), BondSystem.StateWord(c), 14, new Color(0.75f, 0.78f, 0.82f), TextAnchor.UpperLeft);
            UIKit.Bar(new Rect(r.xMax - 108f, r.y + 14f, 96f, 6f), c.Health01, c.State == (int)CompanionState.Downed ? new Color(0.5f, 0.5f, 0.55f) : UIKit.Health, new Color(1f, 1f, 1f, 0.12f));
            if (selected) UIKit.Frame(r, UIKit.Gold, 2f);
            if (focused) UIKit.Frame(new Rect(r.x - 3f, r.y - 3f, r.width + 6f, r.height + 6f), UIKit.Cyan, 3f);
        }

        private static void DrawDetail(Rect r, BondSystem b, CompanionRecord c, bool active)
        {
            CompanionProfile p = BondSystem.ProfileOf(c);
            UIKit.Rect(r, new Color(0f, 0f, 0f, 0.35f));
            float x = r.x + 18f, w = r.width - 36f, y = r.y + 12f;
            float col2 = x + w * 0.5f + 10f, cw = w * 0.5f - 10f;
            // Name and rename (keyboard).
            UIKit.Text(new Rect(x, y, w * 0.5f, 36f), BondSystem.NameOf(c), 30, Color.white, TextAnchor.UpperLeft, FontStyle.Bold);
            if (_renameUid != c.Uid)
            {
                _renameUid = c.Uid;
                _rename = BondSystem.NameOf(c);
            }
            if (_field == null)
            {
                _field = new GUIStyle(GUI.skin.textField);
                if (Mats.UIFont != null) _field.font = Mats.UIFont;
                _field.fontSize = 20;
                _field.alignment = TextAnchor.MiddleLeft;
                _field.padding = new RectOffset(8, 8, 4, 4);
            }
            _rename = GUI.TextField(new Rect(col2, y + 2f, cw - 140f, 34f), _rename ?? "", BondRules.MaxNameLength, _field);
            if (UIKit.Button(new Rect(col2 + cw - 130f, y + 2f, 130f, 34f), "Rename", false, 18))
            {
                string clean = BondRules.SanitizeName(_rename, "");
                if (clean.Length == 0) _message = "Type a name first (keyboard).";
                else
                {
                    b.Rename(c.Uid, clean);
                    _rename = BondSystem.NameOf(c);
                    _message = "Renamed to " + _rename + ".";
                }
            }
            y += 42f;
            UIKit.Text(new Rect(x, y, w, 24f), p.Title + ". " + p.Role, 17, p.Accent, TextAnchor.UpperLeft, FontStyle.Normal, true, true);
            y += 30f;
            // Rank and trust.
            int next = BondRules.NextTrustTarget(c.Rank);
            string rankLine = "Rank: " + BondRules.RankName(c.Rank) + "   Trust " + c.Trust + (next > 0 ? " / " + next : "");
            UIKit.Text(new Rect(x, y, cw, 24f), rankLine, 18, Color.white, TextAnchor.UpperLeft, FontStyle.Bold);
            float floor = BondRules.TrustFloor(c.Rank);
            float fill = next > 0 ? Mathf.Clamp01((c.Trust - floor) / Mathf.Max(1f, next - floor)) : 1f;
            UIKit.Bar(new Rect(x, y + 26f, cw, 7f), fill, p.Accent, new Color(1f, 1f, 1f, 0.12f));
            string nextText;
            if (c.Rank == BondIds.RankStray) nextText = "Next: Bonded at its first care. " + BondRules.RankUnlock(BondIds.RankBonded);
            else if (c.Rank >= BondIds.RankAwakened) nextText = "Awakened: the bond is complete.";
            else if (c.Rank == BondIds.RankHarmonized) nextText = "Next: Awakened through the final bond evolution. " + BondRules.RankUnlock(BondIds.RankAwakened);
            else nextText = "Next: " + BondRules.RankName(c.Rank + 1) + " at " + next + " trust. " + BondRules.RankUnlock(c.Rank + 1);
            UIKit.Text(new Rect(x, y + 36f, cw, 40f), nextText, 15, new Color(0.8f, 0.83f, 0.88f), TextAnchor.UpperLeft, FontStyle.Normal, true, true);
            // Health and state.
            UIKit.Text(new Rect(col2, y, cw, 24f), "Health " + Mathf.RoundToInt(c.Health01 * b.MaxHealthOf(c)) + " / " + Mathf.RoundToInt(b.MaxHealthOf(c)), 18, Color.white, TextAnchor.UpperLeft, FontStyle.Bold);
            UIKit.Bar(new Rect(col2, y + 26f, cw, 7f), c.Health01, c.State == (int)CompanionState.Downed ? new Color(0.5f, 0.5f, 0.55f) : UIKit.Health, new Color(1f, 1f, 1f, 0.12f));
            string state = "State: " + BondSystem.StateWord(c);
            if (c.State == (int)CompanionState.Downed) state += ". Back in " + BondRules.Clock(c.RecoverIn) + ", or at once at a care station.";
            UIKit.Text(new Rect(col2, y + 36f, cw, 40f), state, 15, new Color(0.8f, 0.83f, 0.88f), TextAnchor.UpperLeft, FontStyle.Normal, true, true);
            y += 84f;
            // Assists.
            float eCd = BondRules.ExploreCooldown(c.Archetype, c.Specialization);
            UIKit.Text(new Rect(x, y, cw, 24f), "Explore: " + p.ExploreTitle + "  (" + Mathf.RoundToInt(eCd) + " s)", 17, p.Accent, TextAnchor.UpperLeft, FontStyle.Bold);
            UIKit.Text(new Rect(x, y + 24f, cw, 60f), p.ExploreText, 15, new Color(0.85f, 0.87f, 0.9f), TextAnchor.UpperLeft, FontStyle.Normal, true, true);
            string combatHead = "Combat: " + p.CombatTitle + "  (" + Mathf.RoundToInt(BondRules.CombatCooldown(c.Archetype, c.Rank, c.Specialization)) + " s)";
            UIKit.Text(new Rect(col2, y, cw, 24f), combatHead, 17, BondRules.CanCombatAssist(c.Rank) ? p.Accent : ProgressionUI.Dim, TextAnchor.UpperLeft, FontStyle.Bold);
            UIKit.Text(new Rect(col2, y + 24f, cw, 60f), CombatNumbers(c, p), 15, new Color(0.85f, 0.87f, 0.9f), TextAnchor.UpperLeft, FontStyle.Normal, true, true);
            y += 88f;
            // Fusion, evolution, specialization.
            string why;
            bool canFuse = b.CanFuse(c.Uid, out why);
            string form = FusionIds.Title(FusionIds.FormFor(c.Archetype));
            UIKit.Text(new Rect(x, y, cw, 24f), "Fusion form: " + form, 17, Color.white, TextAnchor.UpperLeft, FontStyle.Bold);
            UIKit.Text(new Rect(x, y + 24f, cw, 44f), canFuse ? "Ready on the bond side. Fusion also needs your personal chassis and the fusion license (Jax)." : why, 15, canFuse ? ProgressionUI.Good : new Color(0.85f, 0.8f, 0.7f), TextAnchor.UpperLeft, FontStyle.Normal, true, true);
            int evo = BondRules.EvolutionStage(c.Rank, c.Evolution);
            UIKit.Text(new Rect(col2, y, cw, 24f), "Look: " + (evo == 2 ? "Awakened form" : (evo == 1 ? "Harmonized accents" : "Base form")), 17, Color.white, TextAnchor.UpperLeft, FontStyle.Bold);
            UIKit.Text(new Rect(col2, y + 24f, cw, 44f), "Evolution changes its body and glow only, never its stats.", 15, new Color(0.75f, 0.78f, 0.82f), TextAnchor.UpperLeft, FontStyle.Normal, true, true);
            y += 70f;
            y = DrawSpecialization(new Rect(x, y, w, 100f), b, c, p);
            y = DrawTuning(new Rect(x, y, w, 90f), b, c, p);
            // Care and roster actions.
            string care = c.CareCooldown > 0f ? "Care gives trust again in " + BondRules.Clock(c.CareCooldown) + "." : "Care is ready: the treatment bench at Bondkeeper's Refuge or the care corner at Lantern Refuge.";
            UIKit.Text(new Rect(x, y, w, 22f), care + "  Cared for " + c.CareCount + " time" + (c.CareCount == 1 ? "" : "s") + ".", 15, new Color(0.8f, 0.85f, 0.9f), TextAnchor.UpperLeft);
            y += 28f;
            float bw = Mathf.Min(260f, (w - 20f) / 3f);
            if (!active)
            {
                if (UIKit.Button(new Rect(x, y, bw, 42f), "Make active", false, 20)) b.SetActive(c.Uid, out _message);
            }
            else if (c.State == (int)CompanionState.Resting)
            {
                if (UIKit.Button(new Rect(x, y, bw, 42f), "Summon", false, 20)) b.Summon(c.Uid, out _message);
            }
            else
            {
                if (UIKit.Button(new Rect(x, y, bw, 42f), "Send home to rest", false, 20)) b.SendHome(c.Uid, out _message);
            }
            if (!string.IsNullOrEmpty(_message)) UIKit.Text(new Rect(x + bw + 20f, y + 4f, w - bw - 20f, 40f), _message, 16, UIKit.Cyan, TextAnchor.UpperLeft, FontStyle.Normal, true, true);
        }

        private static string CombatNumbers(CompanionRecord c, CompanionProfile p)
        {
            if (!BondRules.CanCombatAssist(c.Rank)) return "Unlocks at Bonded (its first care). " + p.CombatText;
            switch (p.Combat)
            {
                case CombatAssistKind.Brace:
                {
                    BraceStats s = BondRules.Brace(c.Rank, c.Tuning, c.Specialization);
                    return "Blocks " + Mathf.RoundToInt(s.Absorb * 100f) + "% of hits from a " + Mathf.RoundToInt(s.HalfAngle * 2f) + " degree arc for " + Mathf.RoundToInt(s.Duration) + " s, up to " + Mathf.RoundToInt(s.Capacity) + " damage, while you stay within " + Mathf.RoundToInt(s.CoverRange) + " m." + (s.HeatShade > 0f ? " Shades half of hazard heat." : "");
                }
                case CombatAssistKind.Disruption:
                    return Mathf.RoundToInt(BondRules.AssistDamage(c.Archetype, c.Rank, c.Tuning, c.Specialization)) + " damage and a " + BondRules.DisruptSeconds(false, false, c.Rank, c.Specialization).ToString("0.0") + " s stagger that stops shots (elites shorter, bosses only lose balance).";
                default:
                    return Mathf.RoundToInt(BondRules.AssistDamage(c.Archetype, c.Rank, c.Tuning, c.Specialization)) + " damage and a Glassbound mark: your next ranged hit sets off a Resonance Burst.";
            }
        }

        private static float DrawSpecialization(Rect r, BondSystem b, CompanionRecord c, CompanionProfile p)
        {
            string[] specs = BondRules.Specializations(c.Archetype);
            if (!BondRules.CanSpecialize(c.Rank))
            {
                UIKit.Text(new Rect(r.x, r.y, r.width, 22f), "Specialization: unlocks at Harmonized (" + BondRules.TrustFloor(BondIds.RankHarmonized) + " trust): " + BondRules.SpecTitle(specs[0]) + " or " + BondRules.SpecTitle(specs[1]) + ".", 15, ProgressionUI.Dim, TextAnchor.UpperLeft, FontStyle.Normal, true, true);
                return r.y + 30f;
            }
            UIKit.Text(new Rect(r.x, r.y, r.width, 22f), "Specialization (free to change out of combat):", 16, Color.white, TextAnchor.UpperLeft, FontStyle.Bold);
            float bw = (r.width - 16f) * 0.5f;
            for (int i = 0; i < specs.Length && i < 2; i++)
            {
                Rect br = new Rect(r.x + i * (bw + 16f), r.y + 24f, bw, 36f);
                bool on = c.Specialization == specs[i];
                if (UIKit.Button(br, BondRules.SpecTitle(specs[i]), on, 18)) b.SetSpecialization(c.Uid, specs[i], out _message);
                UIKit.Text(new Rect(br.x, br.yMax + 2f, bw, 40f), BondRules.SpecText(specs[i]), 14, new Color(0.8f, 0.83f, 0.88f), TextAnchor.UpperLeft, FontStyle.Normal, true, true);
            }
            return r.y + 106f;
        }

        private static float DrawTuning(Rect r, BondSystem b, CompanionRecord c, CompanionProfile p)
        {
            UIKit.Text(new Rect(r.x, r.y, r.width, 22f), "Crystal tuning: level " + c.Tuning + " of " + BondRules.MaxTuning, 16, Color.white, TextAnchor.UpperLeft, FontStyle.Bold);
            if (c.Tuning >= BondRules.MaxTuning)
            {
                UIKit.Text(new Rect(r.x, r.y + 24f, r.width, 22f), "Fully tuned.", 15, ProgressionUI.Good, TextAnchor.UpperLeft);
                return r.y + 52f;
            }
            BondCost[] cost = BondRules.TuningCost(c.Archetype, c.Tuning + 1);
            float cx = r.x;
            for (int i = 0; i < cost.Length; i++)
            {
                int have = Game.Materials != null ? Game.Materials.Count(cost[i].Material) : 0;
                bool ok = have >= cost[i].Count;
                string t = cost[i].Count + " " + MaterialDb.NameOf(cost[i].Material) + " (" + have + ")";
                float tw = UIKit.Measure(t, 15).x + 22f;
                UIKit.Text(new Rect(cx, r.y + 24f, tw, 22f), t, 15, ok ? ProgressionUI.Good : ProgressionUI.Bad, TextAnchor.UpperLeft);
                cx += tw;
            }
            UIKit.Text(new Rect(r.x, r.y + 46f, r.width - 240f, 22f), BondRules.TuningEffect(c.Archetype, c.Tuning + 1), 14, new Color(0.8f, 0.83f, 0.88f), TextAnchor.UpperLeft);
            bool afford = b.CanAffordTuning(c);
            bool combat = CombatDirector.InCombat;
            if (ProgressionUI.Button(new Rect(r.xMax - 230f, r.y + 22f, 230f, 40f), afford ? "Tune (spend)" : "Not enough materials", false, 18, afford && !combat))
            {
                if (combat) _message = "Not during a fight.";
                else if (!afford) _message = "You need " + BondSystem.CostText(cost, true) + ". Underground salvage and creatures drop these.";
                else b.TryTune(c.Uid, out _message);
            }
            return r.y + 76f;
        }
    }
}
