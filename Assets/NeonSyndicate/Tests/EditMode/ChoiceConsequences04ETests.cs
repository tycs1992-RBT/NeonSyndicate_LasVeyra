using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    /// <summary>
    /// The 04E campaign choices that change play, offline: the descent window (uh05_13_crown) routes the Ash Reliquary
    /// into a dark early route or a lit late route with the optional elite detour, the window's heat and light rules touch
    /// only the reliquary, its heat columns are crossable without heat and survivable at worst with guaranteed equipment,
    /// the detour reward is optional, disclosed and paid once, the ledger route (uh05_09_ledger) branches and rejoins
    /// without a fight, the Matron's fate and the Regent's end change the Underheart's ecology, the refuge and the board
    /// from then on (both answers of each, read from the stored choice), and no campaign choice is left recorded with
    /// nothing reading it.
    /// </summary>
    public sealed class ChoiceConsequences04ETests
    {
        private static Dictionary<string, QuestDef> _quests;

        private static Dictionary<string, QuestDef> Quests
        {
            get
            {
                if (_quests == null)
                {
                    List<string> errors = new List<string>();
                    List<KeyValuePair<string, QuestFile>> files = QuestTestData.Load<QuestFile>("Quests", errors, new QuestJsonTrace());
                    Assert.That(errors, Is.Empty, string.Join("\n", errors.ToArray()));
                    _quests = new Dictionary<string, QuestDef>();
                    foreach (KeyValuePair<string, QuestFile> kv in files)
                    {
                        QuestDefaults.Normalize(kv.Value);
                        foreach (QuestDef q in kv.Value.Quests) _quests[q.Id] = q;
                    }
                }
                return _quests;
            }
        }

        private static QuestDef Q(string id)
        {
            QuestDef q;
            Assert.That(Quests.TryGetValue(id, out q), Is.True, id + " ships");
            return q;
        }

        private static QuestPhase Phase(QuestDef q, string id)
        {
            int i = QuestLogic.PhaseIndex(q, id);
            Assert.That(i, Is.GreaterThanOrEqualTo(0), q.Id + " has phase " + id);
            return q.Phases[i];
        }

        private static QuestStep First(QuestPhase ph, string type)
        {
            foreach (QuestStep s in ph.Steps) if (s.Type == type) return s;
            Assert.Fail(ph.Id + " has no " + type + " step");
            return null;
        }

        /// <summary>Phases reachable from a start phase, never entering the blocked phase (-1, the end, is included when reached).</summary>
        private static HashSet<int> ReachFrom(QuestDef q, string start, string blocked)
        {
            HashSet<int> seen = new HashSet<int>();
            Queue<int> open = new Queue<int>();
            int s = QuestLogic.PhaseIndex(q, start);
            int b = blocked == null ? -99 : QuestLogic.PhaseIndex(q, blocked);
            open.Enqueue(s);
            seen.Add(s);
            while (open.Count > 0)
            {
                int i = open.Dequeue();
                if (i < 0) continue;
                foreach (int n in QuestLogic.Successors(q, i))
                {
                    if (n == b || seen.Contains(n)) continue;
                    seen.Add(n);
                    open.Enqueue(n);
                }
            }
            return seen;
        }

        // ================================================================== the window rules

        [Test]
        public void TheWindowScalesHeatAndLightOnlyInTheAshReliquary()
        {
            string q = UnderheartIds.AshReliquary;
            Assert.That(UnderheartWindowRules.HeatScale(UnderheartWindowRules.Early, q), Is.LessThan(1f), "early is cooler");
            Assert.That(UnderheartWindowRules.HeatScale(UnderheartWindowRules.Late, q), Is.GreaterThan(1f), "late is hotter");
            Assert.That(UnderheartWindowRules.HeatScale(UnderheartWindowRules.None, q), Is.EqualTo(1f));
            Assert.That(UnderheartWindowRules.LightScale(UnderheartWindowRules.Early, q), Is.LessThan(0.5f), "early: the lamps are out");
            Assert.That(UnderheartWindowRules.LightScale(UnderheartWindowRules.Late, q), Is.GreaterThan(1f), "late: lit");
            Assert.That(UnderheartWindowRules.AmbientScale(UnderheartWindowRules.Early, q), Is.LessThan(1f));
            Assert.That(UnderheartWindowRules.AmbientScale(UnderheartWindowRules.Late, q), Is.GreaterThan(1f));
            Assert.That(UnderheartWindowRules.ColumnsLit(UnderheartWindowRules.Late), Is.True);
            Assert.That(UnderheartWindowRules.ColumnsLit(UnderheartWindowRules.Early), Is.False);
            Assert.That(UnderheartWindowRules.ColumnsLit(UnderheartWindowRules.None), Is.False);
            foreach (string w in new[] { UnderheartWindowRules.None, UnderheartWindowRules.Early, UnderheartWindowRules.Late })
            {
                Assert.That(UnderheartWindowRules.VentGlowScale(w, q), Is.GreaterThanOrEqualTo(UnderheartWindowRules.MinVentGlow), w + ": the heat boundary still reads in the dark");
                foreach (string region in UnderheartIds.Regions)
                {
                    if (region == q) continue;
                    Assert.That(UnderheartWindowRules.HeatScale(w, region), Is.EqualTo(1f), w + " leaves " + region + " alone");
                    Assert.That(UnderheartWindowRules.LightScale(w, region), Is.EqualTo(1f), w + " leaves " + region + " alone");
                    Assert.That(UnderheartWindowRules.AmbientScale(w, region), Is.EqualTo(1f), w + " leaves " + region + " alone");
                }
                Assert.That(UnderheartWindowRules.HeatScale(w, ""), Is.EqualTo(1f), "the surface is never touched");
            }
            Assert.That(UnderheartWindowRules.Normalize("noon"), Is.EqualTo(UnderheartWindowRules.None));
            Assert.That(UnderheartWindowRules.Normalize(null), Is.EqualTo(UnderheartWindowRules.None));
            // The window ids are exactly the descent_window choice values, so a stored decision is a window as it stands.
            Assert.That(QuestIds04E.ChoiceValues(CampaignRules04E.DescentWindowKey), Is.EquivalentTo(new[] { UnderheartWindowRules.Early, UnderheartWindowRules.Late }));
        }

        [Test]
        public void PhaseWindowsResolveFromThePhaseOrTheStoredChoice()
        {
            Assert.That(CampaignRules04E.ResolveWindow("early", "late"), Is.EqualTo("early"), "an explicit phase window wins");
            Assert.That(CampaignRules04E.ResolveWindow("late", ""), Is.EqualTo("late"));
            Assert.That(CampaignRules04E.ResolveWindow("choice", "late"), Is.EqualTo("late"));
            Assert.That(CampaignRules04E.ResolveWindow("choice", "early"), Is.EqualTo("early"));
            Assert.That(CampaignRules04E.ResolveWindow("choice", ""), Is.EqualTo(""), "no decision yet: no window");
            Assert.That(CampaignRules04E.ResolveWindow("choice", "noon"), Is.EqualTo(""));
            Assert.That(CampaignRules04E.ResolveWindow("", "late"), Is.EqualTo(""), "a phase without a window never applies one");
            Assert.That(CampaignRules04E.ResolveWindow("noon", "late"), Is.EqualTo(""));
            Assert.That(CampaignRules04E.PhaseWindowError(""), Is.Null);
            Assert.That(CampaignRules04E.PhaseWindowError("choice"), Is.Null);
            Assert.That(CampaignRules04E.PhaseWindowError("early"), Is.Null);
            Assert.That(CampaignRules04E.PhaseWindowError("late"), Is.Null);
            Assert.That(CampaignRules04E.PhaseWindowError("dusk"), Is.Not.Null);
        }

        [Test]
        public void TheValidatorChecksPhaseWindows()
        {
            List<string> read = new List<string>();
            string campaign = "{ \"Format\": 1, \"Campaigns\": [ { \"Id\": \"underheart\", \"Title\": \"Underheart and Bonds\", \"Color\": \"#FFB347\" } ], \"Quests\": [ { "
                + "\"Id\": \"uh05_t\", \"Title\": \"Test\", \"Arc\": \"underheart\", \"Campaign\": \"underheart\", \"Kind\": \"Side\", \"Order\": 150, \"Difficulty\": 2, "
                + "\"Region\": \"ash_reliquary\", \"Floor\": \"Somewhere\", \"Entrance\": \"A door\", \"Giver\": \"Cast:jax\", \"Presenter\": \"Jax Calder\", \"Summary\": \"A test.\", \"Phases\": [ "
                + "{ \"Id\": \"a\", \"Window\": \"choice\", \"Steps\": [ { \"Type\": \"Wait\", \"Seconds\": 1 } ] }, "
                + "{ \"Id\": \"b\", \"Window\": \"dusk\", \"Steps\": [ { \"Type\": \"Wait\", \"Seconds\": 1 } ] }, "
                + "{ \"Id\": \"c\", \"Window\": \"late\", \"Steps\": [ { \"Type\": \"Wait\", \"Seconds\": 1 } ] } ], \"Reward\": { \"Cash\": 100 } } ] }";
            QuestReport rep = QuestTestData.ValidateQuestJson(campaign, read);
            Assert.That(read, Is.Empty, string.Join("\n", read.ToArray()));
            Assert.That(QuestTestData.Mentions(rep.Errors, "an earlier phase must Decide 'descent_window'"), Is.True, rep.Details());
            Assert.That(QuestTestData.Mentions(rep.Errors, "Window must be 'choice' or a value of 'descent_window'"), Is.True, rep.Details());
            Assert.That(QuestTestData.Mentions(rep.Errors, "phase 'c'"), Is.False, "an explicit late window is fine: " + rep.Details());
            string guild = "{ \"Format\": 1, \"Givers\": [ { \"Guild\": \"menders\", \"At\": \"Mara Solane\", \"Name\": \"Mara\" } ], \"Quests\": [ { \"Id\": \"g\", \"Title\": \"G\", \"Arc\": \"menders\", \"Order\": 1, "
                + "\"Summary\": \"G.\", \"Phases\": [ { \"Id\": \"p\", \"Window\": \"late\", \"Steps\": [ { \"Type\": \"Wait\", \"Seconds\": 1 } ] } ], \"Reward\": { \"Cash\": 1 } } ] }";
            rep = QuestTestData.ValidateQuestJson(guild, read);
            Assert.That(QuestTestData.Mentions(rep.Errors, "Window (the descent window) is only read for campaign quests"), Is.True, rep.Details());
        }

        // ================================================================== the reliquary's heat columns

        private static List<UhHazard> ReliquaryHeat(UnderheartLayout L)
        {
            List<UhHazard> list = new List<UhHazard>();
            foreach (UhHazard h in L.Hazards)
                if (h.Cell >= 0 && h.Heat > 0f && L.Cells[h.Cell].Region == UnderheartIds.AshReliquary) list.Add(h);
            return list;
        }

        [Test]
        public void TheReliquaryHasBreathingHeatColumnsAndACoolingVent()
        {
            for (int v = 0; v < UnderheartLayout.VariantCount; v++)
            {
                UnderheartLayout L = UnderheartLayout.Create(v);
                Assert.That(UnderheartRules.Validate(L), Is.Empty, "variant " + v);
                int archive = L.CellIndex("reliquary.archive");
                List<UhHazard> heat = ReliquaryHeat(L);
                Assert.That(heat.Count, Is.GreaterThanOrEqualTo(3), "variant " + v + ": the heat columns");
                HashSet<float> phases = new HashSet<float>();
                foreach (UhHazard h in heat)
                {
                    Assert.That(h.Kind, Is.EqualTo(UhHazardKind.HotVent), h.Id + " breathes (a vent, not lava)");
                    Assert.That(h.Cell, Is.EqualTo(archive), h.Id + " stands in the colonnade hall");
                    Assert.That(h.Cycle, Is.GreaterThan(1f), h.Id + " has quiet gaps to cross in");
                    Assert.That(h.Heat, Is.EqualTo(UnderheartWindowRules.ColumnHeat), h.Id);
                    phases.Add(h.Phase);
                }
                Assert.That(phases.Count, Is.EqualTo(heat.Count), "the columns breathe in a staggered ripple, not all at once");
                bool cool = false;
                foreach (UhHazard h in L.Hazards) if (h.Kind == UhHazardKind.Cooling && h.Cell == archive) cool = true;
                Assert.That(cool, Is.True, "variant " + v + ": a blue vent before the line");
            }
        }

        /// <summary>A point just inside a cell from one of its doorways.</summary>
        private static Vector2 InsideFrom(UhPortal d, UhCell c)
        {
            float x = d.X, z = d.Z;
            if (d.FaceAlongX) z += Mathf.Sign(c.CenterZ - d.Z) * 0.8f;
            else x += Mathf.Sign(c.CenterX - d.X) * 0.8f;
            return new Vector2(x, z);
        }

        private static UhPortal DoorBetween(UnderheartLayout L, string a, string b)
        {
            int ia = L.CellIndex(a), ib = L.CellIndex(b);
            foreach (UhPortal d in L.Portals) if ((d.A == ia && d.B == ib) || (d.A == ib && d.B == ia)) return d;
            Assert.Fail("no doorway " + a + "/" + b);
            return null;
        }

        [Test]
        public void TheHeatColumnsCanBeCrossedWithoutStandingInHeat()
        {
            const float step = 0.5f, body = 0.45f;
            for (int v = 0; v < UnderheartLayout.VariantCount; v++)
            {
                UnderheartLayout L = UnderheartLayout.Create(v);
                UhCell c = L.Cell("reliquary.archive");
                List<UhHazard> heat = ReliquaryHeat(L);
                int nx = Mathf.FloorToInt((c.X1 - c.X0) / step) + 1, nz = Mathf.FloorToInt((c.Z1 - c.Z0) / step) + 1;
                bool[,] free = new bool[nx, nz];
                for (int i = 0; i < nx; i++)
                    for (int k = 0; k < nz; k++)
                    {
                        float x = c.X0 + i * step, z = c.Z0 + k * step;
                        bool ok = c.ContainsXZ(x, z, -body);
                        foreach (UhHazard h in heat) if (ok && h.ContainsXZ(x, z, body)) ok = false;
                        free[i, k] = ok;
                    }
                Func<Vector2, Vector2Int> node = p => new Vector2Int(Mathf.Clamp(Mathf.RoundToInt((p.x - c.X0) / step), 0, nx - 1), Mathf.Clamp(Mathf.RoundToInt((p.y - c.Z0) / step), 0, nz - 1));
                Vector2Int start = node(InsideFrom(DoorBetween(L, "reliquary.entryroom", "reliquary.archive"), c));
                Assert.That(free[start.x, start.y], Is.True, "variant " + v + ": the entry doorway is out of the heat");
                bool[,] seen = new bool[nx, nz];
                Queue<Vector2Int> open = new Queue<Vector2Int>();
                open.Enqueue(start);
                seen[start.x, start.y] = true;
                while (open.Count > 0)
                {
                    Vector2Int p = open.Dequeue();
                    foreach (Vector2Int d in new[] { new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1) })
                    {
                        Vector2Int n = p + d;
                        if (n.x < 0 || n.y < 0 || n.x >= nx || n.y >= nz || seen[n.x, n.y] || !free[n.x, n.y]) continue;
                        seen[n.x, n.y] = true;
                        open.Enqueue(n);
                    }
                }
                UhAnchor columns = L.Anchor("reliquary.columns");
                Vector2Int cn = node(new Vector2(columns.X, columns.Z));
                Assert.That(seen[cn.x, cn.y], Is.True, "variant " + v + ": the columns anchor without crossing a breath");
                Vector2Int rn = node(InsideFrom(DoorBetween(L, "reliquary.archive", "reliquary.records"), c));
                Assert.That(seen[rn.x, rn.y], Is.True, "variant " + v + ": the records doorway without crossing a breath");
                Vector2Int sn = node(InsideFrom(DoorBetween(L, "reliquary.archive", "reliquary.south"), c));
                Assert.That(seen[sn.x, sn.y], Is.True, "variant " + v + ": the way on to the Crown without crossing a breath");
                Vector2Int pn = node(InsideFrom(DoorBetween(L, "reliquary.archive", "reliquary.platforms"), c));
                Assert.That(seen[pn.x, pn.y], Is.True, "variant " + v + ": the platform hall without crossing a breath");
            }
        }

        [Test]
        public void MandatoryHeatIsSurvivableWithGuaranteedEquipmentInEveryWindow()
        {
            UnderheartLayout L = UnderheartLayout.Create(0);
            float budget = UnderheartWindowRules.GuaranteedHealth * 0.5f;
            float earlyWorst = 0f, lateWorst = 0f;
            foreach (UhHazard h in ReliquaryHeat(L))
            {
                // How long one breath lasts, measured from the vent itself.
                int active = 0;
                const int samples = 600;
                for (int i = 0; i < samples; i++) if (h.ActiveAt(h.Cycle * i / samples)) active++;
                float breath = h.Cycle * active / samples;
                Assert.That(breath, Is.LessThan(h.Cycle), h.Id + " rests between breaths");
                float across = 2f * h.Radius / TravelMath.WalkSpeed;
                foreach (string w in new[] { UnderheartWindowRules.None, UnderheartWindowRules.Early, UnderheartWindowRules.Late })
                {
                    float hps = h.Heat * UnderheartWindowRules.HeatScale(w, UnderheartIds.AshReliquary);
                    // Worst cases with no ward aspect, no coolant and the grace second ignored: standing through a whole
                    // breath, and walking (not jogging) straight across the grate while it breathes.
                    float stand = UnderheartWindowRules.ExposureDamage(hps, breath, 0f);
                    float walk = UnderheartWindowRules.ExposureDamage(hps, across, 0f);
                    Assert.That(stand, Is.LessThan(budget), h.Id + " " + w + ": a whole breath costs " + stand + " of " + UnderheartWindowRules.GuaranteedHealth);
                    Assert.That(walk, Is.LessThan(budget), h.Id + " " + w + ": walking across costs " + walk);
                    if (w == UnderheartWindowRules.Early) earlyWorst = Mathf.Max(earlyWorst, stand);
                    if (w == UnderheartWindowRules.Late) lateWorst = Mathf.Max(lateWorst, stand);
                }
                // With the one-second grace a jog across a grate never burns at all.
                Assert.That(UnderheartWindowRules.ExposureDamage(h.Heat * UnderheartWindowRules.LateHeatScale, 2f * h.Radius / TravelMath.JogSpeed, 1f), Is.EqualTo(0f), h.Id);
            }
            Assert.That(lateWorst, Is.GreaterThan(earlyWorst * 1.5f), "the late window is really hotter than the early one");
        }

        // ================================================================== uh05_13_crown

        [Test]
        public void TheDescentWindowChoosesTheRoute()
        {
            QuestDef q = Q("uh05_13_crown");
            QuestPhase window = Phase(q, "window");
            Assert.That(window.Checkpoint, Is.True, "a retry comes back to the window");
            Assert.That(window.ResumeAt, Is.EqualTo("Under:workshops.rest"));
            QuestStep decide = First(window, "Decide");
            Assert.That(decide.Key, Is.EqualTo(CampaignRules04E.DescentWindowKey));
            Assert.That(decide.Reask, Is.True, "an approach choice: a player who failed one way may pick the other");
            Assert.That(QuestLogic.EndsPhase(decide), Is.False, "the window is recorded, then the router reads it");
            QuestPhase router = Phase(q, "reliquary");
            Assert.That(router.Window, Is.EqualTo(CampaignRules04E.WindowChoice));
            QuestStep branch = First(router, "Branch");
            CampaignFacts f = new CampaignFacts();
            f.Choice = k => k == CampaignRules04E.DescentWindowKey ? "late" : "";
            Assert.That(CampaignRules04E.BranchTarget(branch.Options, f), Is.EqualTo("lit_route"));
            f.Choice = k => k == CampaignRules04E.DescentWindowKey ? "early" : "";
            Assert.That(CampaignRules04E.BranchTarget(branch.Options, f), Is.EqualTo("dark_route"));
            QuestPhase dark = Phase(q, "dark_route"), lit = Phase(q, "lit_route");
            Assert.That(dark.Window, Is.EqualTo(UnderheartWindowRules.Early));
            Assert.That(lit.Window, Is.EqualTo(UnderheartWindowRules.Late));
            // Both routes cross the reliquary with the companion's route sense and reach the same ending.
            QuestStep darkAssist = First(dark, "Assist"), litAssist = First(lit, "Assist");
            Assert.That(darkAssist.At, Is.EqualTo(litAssist.At));
            Assert.That(darkAssist.Seconds, Is.GreaterThan(litAssist.Seconds * 2f), "feeling for the platforms in the dark is slow; the lit line is quick");
            Assert.That(ReachFrom(q, "dark_route", null).Contains(-1), Is.True, "the early route finishes");
            Assert.That(ReachFrom(q, "lit_route", null).Contains(-1), Is.True, "the late route finishes");
            Assert.That(ReachFrom(q, "dark_route", null).Contains(QuestLogic.PhaseIndex(q, "revelation")), Is.True);
            Assert.That(ReachFrom(q, "lit_route", null).Contains(QuestLogic.PhaseIndex(q, "revelation")), Is.True);
            // Every phase from the reliquary on holds a window, so the heat and light never fall back mid-route.
            for (int i = QuestLogic.PhaseIndex(q, "reliquary"); i < q.Phases.Count; i++)
                Assert.That(q.Phases[i].Window, Is.Not.Empty, q.Phases[i].Id + " keeps the window");
            Assert.That(Phase(q, "observatory").Window, Is.Empty, "no window up at the observatory");
            Assert.That(window.Window, Is.Empty, "no window before it is chosen");
            // Old saves remember these phase ids.
            Assert.That(QuestLogic.PhaseIndex(q, "reliquary"), Is.GreaterThan(0));
            Assert.That(QuestLogic.PhaseIndex(q, "detour"), Is.GreaterThan(0));
        }

        [Test]
        public void TheEliteDetourIsLateOnlyOptionalDisclosedAndPaysOnce()
        {
            QuestDef q = Q("uh05_13_crown");
            int elite = QuestLogic.PhaseIndex(q, "elite"), detour = QuestLogic.PhaseIndex(q, "detour");
            Assert.That(ReachFrom(q, "dark_route", null).Contains(elite), Is.False, "the early route has no detour");
            Assert.That(ReachFrom(q, "dark_route", null).Contains(detour), Is.False);
            Assert.That(ReachFrom(q, "lit_route", null).Contains(elite), Is.True, "the late route offers it");
            Assert.That(ReachFrom(q, "lit_route", "elite").Contains(-1), Is.True, "the late route finishes without it (optional)");
            foreach (string id in new[] { "detour", "offer", "elite" }) Assert.That(Phase(q, id).Window, Is.EqualTo(UnderheartWindowRules.Late), id);
            // Disclosed before the choice and again at the alcove.
            QuestStep decide = First(Phase(q, "window"), "Decide");
            Assert.That(decide.Text, Does.Contain("Void Screen"), "the window choice names the detour reward");
            Assert.That(decide.Text, Does.Contain("optional"));
            QuestStep choice = First(Phase(q, "offer"), "Choice");
            Assert.That(choice.Text, Does.Contain("Void Screen"));
            Assert.That(choice.Options.Count, Is.EqualTo(2));
            // Paid once: one Claim with the stable quest token, then a flag the checkpoint reads to never offer it again.
            int claims = 0;
            foreach (QuestPhase ph in q.Phases)
                foreach (QuestStep s in ph.Steps)
                    if (s.Type == "Claim" && s.Reward == "uh_void_aspect")
                    {
                        claims++;
                        Assert.That(ph.Id, Is.EqualTo("elite"));
                        Assert.That(CampaignRules04E.ClaimToken(q.Id, s), Is.EqualTo("quest:uh05_13_crown:uh_void_aspect"));
                    }
            Assert.That(claims, Is.EqualTo(1));
            QuestPhase ep = Phase(q, "elite");
            Assert.That(ep.Steps[0].Type, Is.EqualTo("Skirmish"), "the reward follows a won fight");
            Assert.That(ep.Steps[0].Room, Is.EqualTo("reliquary.elite_detour"), "a cleared room stays clear: no second fight to farm");
            QuestStep flag = ep.Steps[ep.Steps.Count - 1];
            Assert.That(flag.Type, Is.EqualTo("Flag"));
            QuestPhase dp = Phase(q, "detour");
            Assert.That(dp.Checkpoint, Is.True);
            QuestStep gate = First(dp, "Branch");
            CampaignFacts f = new CampaignFacts();
            Assert.That(CampaignRules04E.BranchTarget(gate.Options, f), Is.EqualTo("offer"), "first visit: the offer");
            f.Flag = n => n == flag.Key;
            Assert.That(CampaignRules04E.BranchTarget(gate.Options, f), Is.EqualTo("revelation"), "a resumed run after the detour goes straight on");
            Assert.That(q.Claims, Does.Contain("uh_void_aspect"));
        }

        // ================================================================== uh05_09_ledger

        [Test]
        public void TheLedgerRouteBranchesWithoutAFightAndRejoins()
        {
            QuestDef q = Q("uh05_09_ledger");
            QuestStep decide = First(Phase(q, "route"), "Decide");
            Assert.That(decide.Key, Is.EqualTo("ledger_route"));
            Assert.That(QuestLogic.EndsPhase(decide), Is.True, "each answer leads to its own phase");
            Dictionary<string, string> to = new Dictionary<string, string>();
            foreach (ChoiceOption o in decide.Options) to[o.Value] = o.Phase;
            Assert.That(to["testimony"], Is.EqualTo("testimony"));
            Assert.That(to["terminal"], Is.EqualTo("terminal"));
            QuestPhase testimony = Phase(q, "testimony"), terminal = Phase(q, "terminal");
            Assert.That(First(testimony, "Interact").At, Is.EqualTo("Venue:office.testimony"));
            bool backRoom = false, pulled = false;
            foreach (QuestStep s in terminal.Steps)
            {
                if (s.Type == "GoTo" && s.At == "Venue:office.back_room") backRoom = true;
                if (s.Type == "Interact" && s.At == "Venue:office.terminal") pulled = true;
            }
            Assert.That(backRoom && pulled, Is.True, "the terminal route goes through the back room to the terminal");
            // The spec makes the terminal route nonlethal: neither route fights anyone.
            foreach (QuestPhase ph in new[] { testimony, terminal })
                foreach (QuestStep s in ph.Steps)
                    Assert.That(s.Type == "Skirmish" || s.Type == "Kill" || s.Type == "UBoss" || s.Type == "Spawn", Is.False, ph.Id + " stays nonlethal");
            int proof = QuestLogic.PhaseIndex(q, "proof");
            Assert.That(ReachFrom(q, "testimony", "terminal").Contains(proof), Is.True);
            Assert.That(ReachFrom(q, "terminal", "testimony").Contains(proof), Is.True);
            Assert.That(ReachFrom(q, "testimony", "terminal").Contains(-1), Is.True, "testimony completes the quest");
            Assert.That(ReachFrom(q, "terminal", "testimony").Contains(-1), Is.True, "terminal completes the quest");
            // Iris remembers how you got it; both lines lead to the same credentials.
            QuestStep branch = First(Phase(q, "proof"), "Branch");
            CampaignFacts f = new CampaignFacts();
            f.Choice = k => k == "ledger_route" ? "testimony" : "";
            Assert.That(CampaignRules04E.BranchTarget(branch.Options, f), Is.EqualTo("proof_witness"));
            f.Choice = k => k == "ledger_route" ? "terminal" : "";
            Assert.That(CampaignRules04E.BranchTarget(branch.Options, f), Is.EqualTo("proof_copy"));
            Assert.That(Phase(q, "proof_witness").Next, Is.EqualTo("credentials"));
            Assert.That(Phase(q, "proof_copy").Next, Is.EqualTo("credentials"));
            bool credentials = false;
            foreach (QuestStep s in Phase(q, "credentials").Steps) if (s.Type == "Flag" && s.Key == "ledger_credentials") credentials = true;
            Assert.That(credentials, Is.True, "both routes earn the workshops credentials");
        }

        // ================================================================== every choice is read

        /// <summary>A comparable picture of the resident slots in force for one decision ("anchor/species/count/temper/shelter;" each).</summary>
        private static string World(string key, string value)
        {
            List<ResidentSlot04E> slots = new List<ResidentSlot04E>();
            ChoiceEcology04E.EffectiveSlots(key == ChoiceEcology04E.MatronKey ? value : "", key == ChoiceEcology04E.RegentKey ? value : "", slots);
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach (ResidentSlot04E s in slots) sb.Append(s.Anchor).Append('/').Append(s.Species).Append('/').Append(s.Count).Append('/').Append((int)s.Temper).Append('/').Append(s.Sheltered).Append(';');
            return sb.ToString();
        }

        /// <summary>The title of the job a board slot posts under one decision (all quests done, so every place is open).</summary>
        private static Dictionary<string, ContractDef> _contracts;

        private static string Job(string templateId, string key, string value, int serial)
        {
            if (_contracts == null)
            {
                List<string> errors = new List<string>();
                _contracts = new Dictionary<string, ContractDef>();
                foreach (KeyValuePair<string, QuestFile> kv in QuestTestData.Load<QuestFile>("Quests", errors, new QuestJsonTrace()))
                    foreach (ContractDef c in kv.Value.Contracts) _contracts[c.Id] = c;
                Assert.That(errors, Is.Empty, string.Join("\n", errors.ToArray()));
            }
            ContractDef t;
            Assert.That(_contracts.TryGetValue(templateId, out t), Is.True, templateId + " ships");
            CampaignBoardContext ctx = new CampaignBoardContext();
            foreach (string id in Quests.Keys) ctx.Completed.Add(id);
            ctx.Choice = k => k == key ? value : "";
            ContractOffer o = CampaignContracts04E.Make(t, serial, ctx, 0);
            return o.Title + " @ " + o.Destination;
        }

        [Test]
        public void TheMatronsFateChangesTheUnderheartFromThenOn()
        {
            QuestDef q = Q("uh05_08_matron");
            QuestStep decide = First(Phase(q, "fate"), "Decide");
            Assert.That(decide.Key, Is.EqualTo(ChoiceEcology04E.MatronKey));
            Assert.That(decide.Reask, Is.False, "the fate stands once chosen (a resumed quest keeps it)");
            string before = World(ChoiceEcology04E.MatronKey, ""), contain = World(ChoiceEcology04E.MatronKey, "contain"), relocate = World(ChoiceEcology04E.MatronKey, "relocate");
            Assert.That(contain, Is.Not.EqualTo(before), "containing her changes the residents");
            Assert.That(relocate, Is.Not.EqualTo(before), "relocating her changes the residents");
            Assert.That(contain, Is.Not.EqualTo(relocate), "and the two answers differ");
            Assert.That(contain, Does.Contain(ChoiceEcology04E.MatronNest + "/" + EnemyIds04E.EmberHatchling), "contain: her nest fills again");
            Assert.That(relocate, Does.Not.Contain(ChoiceEcology04E.MatronNest), "relocate: her nest stays empty");
            Assert.That(relocate, Does.Contain(ChoiceEcology04E.DeepBrood + "/" + EnemyIds04E.EmberHatchling + "/2/"), "relocate: her brood follows her into the deep");
            Assert.That(relocate, Does.Contain(ChoiceEcology04E.HabitatCrab + "/" + EnemyIds04E.KilnCrab + "/1/" + (int)ResidentTemper04E.Default), "relocate: the habitat crab still guards its nest");
            Assert.That(contain, Does.Contain(ChoiceEcology04E.HabitatCrab + "/" + EnemyIds04E.KilnCrab + "/1/" + (int)ResidentTemper04E.Calm), "contain: the habitat crab settles");
            // The board bends too: Bram's feed runs, or rescues of her strays.
            Assert.That(Job("uhc_courier", ChoiceEcology04E.MatronKey, "contain", 3), Does.StartWith("Pen feed"));
            Assert.That(Job("uhc_courier", ChoiceEcology04E.MatronKey, "relocate", 3), Does.Not.StartWith("Pen feed"));
            Assert.That(Job("uhc_rescue", ChoiceEcology04E.MatronKey, "relocate", 3), Does.StartWith("Matron's stray"));
            Assert.That(Job("uhc_rescue", ChoiceEcology04E.MatronKey, "contain", 3), Does.Not.StartWith("Matron's stray"));
            // Bram greets by it.
            CastDef04E bram = CastRoster04E.Find(CastIds04E.GardensKeeper);
            Assert.That(OutcomeLines04E.Greeting(bram.Id, k => k == ChoiceEcology04E.MatronKey ? "contain" : "", bram.Greeting), Is.Not.EqualTo(OutcomeLines04E.Greeting(bram.Id, k => k == ChoiceEcology04E.MatronKey ? "relocate" : "", bram.Greeting)));
        }

        [Test]
        public void TheRegentsEndChangesTheRefugeTheDeepAndTheBoard()
        {
            QuestDef q = Q("uh05_15_regent");
            QuestStep decide = First(Phase(q, "end"), "Decide");
            Assert.That(decide.Key, Is.EqualTo(ChoiceEcology04E.RegentKey));
            Assert.That(decide.Reask, Is.False);
            // The deep habitat (Foundry Crown and Ash Reliquary).
            string contain = World(ChoiceEcology04E.RegentKey, "contain"), shutdown = World(ChoiceEcology04E.RegentKey, "shutdown");
            Assert.That(contain, Is.Not.EqualTo(shutdown));
            Assert.That(contain, Does.Contain(ChoiceEcology04E.FurnaceBalcony + "/" + EnemyIds04E.CinderMoth));
            Assert.That(shutdown, Does.Not.Contain(ChoiceEcology04E.FurnaceBalcony));
            Assert.That(shutdown, Does.Not.Contain(ChoiceEcology04E.CrownStaging + "/"), "shutdown: the Crown's staging refuge goes quiet");
            // The Lantern Refuge: Ilse's lines and her stall's stock.
            CastDef04E ilse = CastRoster04E.Find(CastIds04E.RefugeKeeper);
            string warmLine = OutcomeLines04E.Greeting(ilse.Id, k => k == ChoiceEcology04E.RegentKey ? "contain" : "", ilse.Greeting);
            string coldLine = OutcomeLines04E.Greeting(ilse.Id, k => k == ChoiceEcology04E.RegentKey ? "shutdown" : "", ilse.Greeting);
            Assert.That(warmLine, Is.Not.EqualTo(coldLine));
            Assert.That(warmLine, Is.Not.EqualTo(ilse.Greeting));
            List<RefugeOffer04E> warm = new List<RefugeOffer04E>(), cold = new List<RefugeOffer04E>();
            RefugeTrade04E.Offers("contain", warm);
            RefugeTrade04E.Offers("shutdown", cold);
            Assert.That(warm, Is.Not.Empty);
            Assert.That(cold, Is.Not.Empty);
            Assert.That(warm[0].Material, Is.Not.EqualTo(cold[0].Material), "the stall stocks by the outcome");
            // The board: custodian lens work or hand-valve shifts, and Ilse's endgame line.
            Assert.That(Job("uhc_lens", ChoiceEcology04E.RegentKey, "contain", 4), Does.StartWith("Custodian lens"));
            Assert.That(Job("uhc_valve_defense", ChoiceEcology04E.RegentKey, "shutdown", 4), Does.StartWith("Hand valve shift"));
            Assert.That(Job("uhc_valve_defense", ChoiceEcology04E.RegentKey, "shutdown", 4), Does.EndWith("Under:crown.valve_mech"));
            Assert.That(Job("uhc_valve_defense", ChoiceEcology04E.RegentKey, "contain", 4), Does.Not.StartWith("Hand valve shift"));
            Assert.That(CampaignContracts04E.BoardGreeting(true, "contain"), Is.Not.EqualTo(CampaignContracts04E.BoardGreeting(true, "shutdown")));
            // Odile still names the outcome at the perch in uh05_16.
            QuestStep branch = First(Phase(Q("uh05_16_answers"), "bond"), "Branch");
            CampaignFacts f = new CampaignFacts();
            f.Choice = k => k == ChoiceEcology04E.RegentKey ? "contain" : "";
            Assert.That(CampaignRules04E.BranchTarget(branch.Options, f), Is.EqualTo("bond_contain"));
            f.Choice = k => k == ChoiceEcology04E.RegentKey ? "shutdown" : "";
            Assert.That(CampaignRules04E.BranchTarget(branch.Options, f), Is.EqualTo("bond_shutdown"));
        }

        [Test]
        public void EveryCampaignChoiceChangesPlay()
        {
            // A choice changes play when its Decide sends each answer to its own phase, or a Branch or option condition
            // reads it. fusion_profile is read by code instead (PersonalGarage.ActiveProfile picks the handling profile).
            // matron_fate and regent_end are also read by code from the stored choice for the rest of the game: every
            // answer must change the Underheart's residents (ChoiceEcology04E) and post its own board variant.
            HashSet<string> routed = new HashSet<string>(), read = new HashSet<string>();
            foreach (QuestDef q in Quests.Values)
            {
                if (!q.Id.StartsWith("uh05_", StringComparison.Ordinal)) continue;
                foreach (QuestPhase ph in q.Phases)
                    foreach (QuestStep s in ph.Steps)
                    {
                        if (s.Type == "Decide" && QuestLogic.EndsPhase(s)) routed.Add(s.Key);
                        foreach (ChoiceOption o in s.Options)
                        {
                            if (string.IsNullOrEmpty(o.When)) continue;
                            foreach (string t in o.When.Split('&'))
                            {
                                string term = t.TrimStart('!');
                                if (!term.StartsWith("choice:", StringComparison.Ordinal)) continue;
                                int eq = term.IndexOf('=');
                                if (eq > 7) read.Add(term.Substring(7, eq - 7));
                            }
                        }
                    }
            }
            foreach (string key in QuestIds04E.ChoiceKeys)
            {
                if (key == "fusion_profile") continue;
                Assert.That(routed.Contains(key) || read.Contains(key), Is.True, key + " is recorded but nothing in the campaign reads it");
            }
            Assert.That(read.Contains(CampaignRules04E.DescentWindowKey), Is.True, "the descent window is read by the reliquary router");
            HashSet<string> byCode = new HashSet<string>();
            foreach (string key in new[] { ChoiceEcology04E.MatronKey, ChoiceEcology04E.RegentKey })
            {
                string undecided = World(key, "");
                HashSet<string> worlds = new HashSet<string>();
                foreach (string value in QuestIds04E.ChoiceValues(key))
                {
                    string w = World(key, value);
                    Assert.That(w, Is.Not.EqualTo(undecided), key + "=" + value + " changes the Underheart's residents");
                    Assert.That(worlds.Add(w), Is.True, key + "=" + value + " differs from the other answer");
                    bool variant = false;
                    foreach (ContractVariant04E v in CampaignContracts04E.Variants) if (v.Key == key && v.Value == value) variant = true;
                    Assert.That(variant, Is.True, key + "=" + value + " posts its own board variant");
                }
                byCode.Add(key);
            }
            Assert.That(byCode, Is.EquivalentTo(new[] { ChoiceEcology04E.MatronKey, ChoiceEcology04E.RegentKey }), "both later-ecology choices are read by code");
        }
    }
}
