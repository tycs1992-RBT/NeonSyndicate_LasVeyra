using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    /// <summary>
    /// The 04E integration fixes in the GUIDANCE area: one HUD layout with no collisions in the common states (at the
    /// default HUD scale, and at larger HUD scales with the compact rules), the moved older pieces (callouts, prompts
    /// underground, the district name on foot, the 04D reward card), the choice card that never runs under its buttons,
    /// the offer's "where" line without a repeated venue name, the loaded safehouse rule, the ground search below the
    /// street, surface world events while underground and the lava loop placement. All engine-free.
    /// </summary>
    public sealed class HudLayout04ETests
    {
        private static readonly Vector2[] Screens =
        {
            new Vector2(1920f, 1080f),   // 16:9 at the default HUD scale
            new Vector2(1728f, 1080f),   // 16:10
            new Vector2(2560f, 1080f)    // 21:9
        };

        private static readonly HudState04E[] States =
        {
            HudState04E.ExploreFoot, HudState04E.FightFoot, HudState04E.Vehicle, HudState04E.Fused, HudState04E.BossFight, HudState04E.Quest
        };

        // ================================================================== HUD layout

        private static readonly bool[] Underground = { false, true };

        private static List<HudState04E> AllStates()
        {
            List<HudState04E> list = new List<HudState04E>(States);
            list.Add(HudState04E.RewardAfterFight);
            return list;
        }

        private static bool IsTransient(string name)
        {
            return System.Array.IndexOf(HudLayout04E.Transient, name) >= 0;
        }

        /// <summary>
        /// Every overlap of a 04E panel with the existing HUD (allowTransient: except the short-lived cards) and with
        /// another 04E panel, plus panels off screen, for one virtual screen size in every state, above and below ground.
        /// </summary>
        private static List<string> Problems(float W, float H, bool checkReserved, bool allowTransient)
        {
            List<string> problems = new List<string>();
            foreach (bool under in Underground)
            foreach (HudState04E state in AllStates())
            {
                List<HudBox04E> panels = HudLayout04E.Panels(state, W, H, 4, under);
                List<HudBox04E> reserved = HudLayout04E.Reserved(state, W, H, 4, under);
                string tag = Mathf.RoundToInt(W) + "x" + Mathf.RoundToInt(H) + (under ? " underground " : " ") + state + ": ";
                for (int i = 0; i < panels.Count; i++)
                {
                    HudBox04E p = panels[i];
                    Rect a = p.Rect;
                    Assert.That(p.Is04E, Is.True, p.Name);
                    if (a.xMin < -0.01f || a.yMin < -0.01f || a.xMax > W + 0.01f || a.yMax > H + 0.01f) problems.Add(tag + p.Name + " is off screen " + a);
                    if (checkReserved)
                    {
                        foreach (HudBox04E r in reserved)
                        {
                            if (allowTransient && IsTransient(r.Name)) continue;
                            if (a.Overlaps(r.Rect)) problems.Add(tag + p.Name + " " + a + " overlaps " + r.Name + " " + r.Rect);
                        }
                    }
                    for (int j = i + 1; j < panels.Count; j++)
                        if (a.Overlaps(panels[j].Rect)) problems.Add(tag + p.Name + " overlaps " + panels[j].Name);
                }
            }
            return problems;
        }

        [Test]
        public void No04EPanelOverlapsTheExistingHudInAnyCommonState()
        {
            List<string> problems = new List<string>();
            foreach (Vector2 screen in Screens) problems.AddRange(Problems(screen.x, screen.y, true, false));
            Assert.That(problems, Is.Empty, string.Join("\n", problems));
        }

        [Test]
        public void No04EPanelsOverlapEachOtherAndAllStayOnScreen()
        {
            List<string> problems = new List<string>();
            foreach (Vector2 screen in Screens) problems.AddRange(Problems(screen.x, screen.y, false, false));
            Assert.That(problems, Is.Empty, string.Join("\n", problems));
        }

        [Test]
        public void HudScaleAboveOneKeepsThe04EPanelsClear()
        {
            List<string> problems = new List<string>();
            float[] compact = { 1.05f, 1.1f, 1.15f, 1.2f, 1.25f };
            foreach (Vector2 screen in Screens)
            {
                // Up to 1.25: no overlap with the persistent HUD, only the short-lived cards may share space.
                foreach (float scale in compact)
                {
                    Vector2 v = HudLayout04E.VirtualSize(screen.x, screen.y, scale);
                    Assert.That(HudLayout04E.Compact(v.y), Is.True, "scale " + scale);
                    problems.AddRange(Problems(v.x, v.y, true, true));
                }
                // Up to the slider's 1.5 the older HUD overlaps itself; the 04E panels stay on screen and apart.
                foreach (float scale in new[] { 1.3f, 1.4f, 1.5f })
                {
                    Vector2 v = HudLayout04E.VirtualSize(screen.x, screen.y, scale);
                    problems.AddRange(Problems(v.x, v.y, false, false));
                }
            }
            Assert.That(problems, Is.Empty, string.Join("\n", problems));
        }

        [Test]
        public void TheVirtualScreenFollowsTheHudScaleLikeUIKit()
        {
            Vector2 a = HudLayout04E.VirtualSize(1920f, 1080f, 1f);
            Assert.That(a.x, Is.EqualTo(1920f).Within(0.01f));
            Assert.That(a.y, Is.EqualTo(1080f).Within(0.01f));
            Vector2 b = HudLayout04E.VirtualSize(1920f, 1080f, 1.25f);
            Assert.That(b.x, Is.EqualTo(1536f).Within(0.01f));
            Assert.That(b.y, Is.EqualTo(864f).Within(0.01f));
            Vector2 c = HudLayout04E.VirtualSize(3840f, 2160f, 1.5f);
            Assert.That(c.y, Is.EqualTo(720f).Within(0.01f), "the screen's resolution does not matter, only its aspect and the scale");
            Assert.That(HudLayout04E.Compact(1080f), Is.False);
            Assert.That(HudLayout04E.Compact(a.y), Is.False);
        }

        [Test]
        public void TheDefaultScalePlacesTheUntouchedPanelsWhereTheyWere()
        {
            const float W = 1920f, H = 1080f;
            AssertRect(HudLayout04E.RouteHint(W, H), 36f, 753.4f, 330f, 32f);
            AssertRect(HudLayout04E.BondsPanelBackground(W, H), 32f, 647.4f, 338f, 100f);
            AssertRect(HudLayout04E.BondsAbove(W, H), 32f, 507.4f, 720f, 140f);
            AssertRect(HudLayout04E.WardBox(W, H), 32f, 423.4f, 276f, 80f);
            AssertRect(HudLayout04E.RewardsChip(W, H, 490f), 24f, 389.4f, 520f, 30f);
            AssertRect(HudLayout04E.EncounterSlot(W, H), 510f, 316.4f, 900f, 118f);
            AssertRect(HudLayout04E.EcologyBox(W, H, 0), 730f, 316.4f, 460f, 54f);
            AssertRect(HudLayout04E.MechanismPanel(W, H), 740f, 316.4f, 440f, 118f);
            AssertRect(HudLayout04E.SessionChip(W, H), 850f, 16f, 580f, 58f);
            AssertRect(HudLayout04E.FusionPanel(W, H), 1500f, 600f, 380f, 128f);
            AssertRect(HudLayout04E.BottomStrip(W, H), 400f, 1044f, 1120f, 28f);
            Assert.That(HudLayout04E.BondsMessageLines(W, H), Is.EqualTo(2));
            Assert.That(HudLayout04E.RewardsChipVisible(W, H), Is.True);
            Assert.That(HudLayout04E.BondsMessageMaxWidth(W, H), Is.EqualTo(HudLayout04E.BondsMessageMaxWidth(W)));
            Assert.That(HudLayout04E.CalloutTop(H), Is.EqualTo(H * 0.6f).Within(1e-3f), "the callouts start where they always did");
            Assert.That(HudLayout04E.PromptsTop(H, false), Is.EqualTo(H * 0.74f).Within(1e-3f), "on the surface the prompts start where they always did");
        }

        private static void AssertRect(Rect r, float x, float y, float w, float h)
        {
            Assert.That(r.x, Is.EqualTo(x).Within(0.05f), "x of " + r);
            Assert.That(r.y, Is.EqualTo(y).Within(0.05f), "y of " + r);
            Assert.That(r.width, Is.EqualTo(w).Within(0.05f), "width of " + r);
            Assert.That(r.height, Is.EqualTo(h).Within(0.05f), "height of " + r);
        }

        [Test]
        public void TheThirdCombatCalloutEndsAboveTheHeatRowAtEveryScale()
        {
            foreach (Vector2 screen in Screens)
            foreach (float scale in new[] { 1f, 1.1f, 1.25f, 1.4f, 1.5f })
            {
                Vector2 v = HudLayout04E.VirtualSize(screen.x, screen.y, scale);
                Rect heat = HudLayout04E.HeatRow(v.x, v.y);
                float third = HudLayout04E.CalloutTop(v.y) + 2f * HudLayout04E.CalloutStep + HudLayout04E.CalloutHeight;
                Assert.That(HudLayout04E.CalloutStackHeight, Is.EqualTo(third - HudLayout04E.CalloutTop(v.y)).Within(1e-3f));
                Assert.That(third, Is.LessThanOrEqualTo(heat.yMin), "scale " + scale + " " + screen);
                // Underground the prompts start under the heat row; the heat row keeps above the objective block.
                Assert.That(HudLayout04E.PromptsTop(v.y, true), Is.GreaterThanOrEqualTo(heat.yMax), "prompts under the heat row");
                Assert.That(heat.yMax, Is.LessThanOrEqualTo(HudLayout04E.ObjectiveTop(v.y)), "heat row above the objective");
                // Clear of the loot card (W - 470) unless the screen is so narrow that the row is at its 300 minimum.
                if (HudLayout04E.HeatWidth(v.x) > 300f) Assert.That(heat.xMax, Is.LessThanOrEqualTo(v.x - 470f + 0.01f), "heat row clear of the loot card");
            }
        }

        [Test]
        public void The04DRewardCardNoLongerCoversTheLeftColumn()
        {
            foreach (Vector2 screen in Screens)
            {
                float W = screen.x, H = screen.y;
                Rect card = HudLayout04E.RewardCard04D(W, H);
                Rect old = new Rect(40f, H * 0.30f, 520f, 200f);
                Assert.That(old.Overlaps(HudLayout04E.RewardsChip(W, H, 490f)), Is.True, "the old place collided (the reported problem)");
                Assert.That(card.Overlaps(HudLayout04E.RewardsChip(W, H, 490f)), Is.False, "rewards chip");
                Assert.That(card.Overlaps(HudLayout04E.WardBox(W, H)), Is.False, "ward panel");
                Assert.That(card.Overlaps(HudLayout04E.BondsAbove(W, H)), Is.False, "companion messages");
                Rect slot = HudLayout04E.EncounterSlot(W, H);
                Assert.That(card.yMin, Is.EqualTo(slot.yMin).Within(0.01f), "it sits in the encounter slot rows");
                Assert.That(card.yMax, Is.EqualTo(slot.yMax).Within(0.01f));
                Assert.That(card.xMax, Is.LessThanOrEqualTo(W - 470f), "never under the loot card");
                Assert.That(card.width, Is.GreaterThanOrEqualTo(460f));
            }
        }

        [Test]
        public void TheDistrictNameOnFootClearsTheLumenTheGadgetsAndTheBottomLines()
        {
            foreach (Vector2 screen in Screens)
            foreach (float scale in new[] { 1f, 1.1f, 1.25f })
            {
                Vector2 v = HudLayout04E.VirtualSize(screen.x, screen.y, scale);
                float W = v.x, H = v.y;
                Rect chip = HudLayout04E.DistrictChipFoot(W, H);
                Rect oldPopup = new Rect(W - 640f, H - 130f, 600f, 70f);
                Rect lumenGainAndBox = new Rect(W - 310f, H - 100f, 270f, 70f);
                Assert.That(oldPopup.Overlaps(lumenGainAndBox), Is.True, "the old popup printed over the Lumen readout");
                Assert.That(chip.Overlaps(lumenGainAndBox), Is.False, "Lumen readout");
                Assert.That(chip.Overlaps(new Rect(W - 253f, H - 255f, 213f, 120f)), Is.False, "gadget slots");
                float eb = HudLayout04E.EliteBarWidth(W);
                Assert.That(chip.Overlaps(new Rect(W * 0.5f - eb * 0.5f - 3f, H - 180f, eb + 6f, 50f)), Is.False, "elite bar");
                Assert.That(chip.Overlaps(new Rect(W * 0.5f - 440f, H - 106f, 880f, 26f)), Is.False, "04D encounter line");
                Assert.That(chip.Overlaps(new Rect(W * 0.5f - 300f, H - 126f, 600f, 86f)), Is.False, "cover panel");
                Assert.That(chip.Overlaps(new Rect(W * 0.5f - 165f, H - 86f, 330f, 46f)), Is.False, "travel panel");
                Assert.That(chip.xMax, Is.EqualTo(W - 40f).Within(0.01f), "right-aligned to the HUD margin");
            }
        }

        [Test]
        public void TheLeftColumnShedsMessageLinesThenTheChipAsTheHudScaleGrows()
        {
            Vector2 s125 = HudLayout04E.VirtualSize(1920f, 1080f, 1.25f);
            Assert.That(HudLayout04E.BondsMessageLines(s125.x, s125.y), Is.EqualTo(2));
            Assert.That(HudLayout04E.RewardsChipVisible(s125.x, s125.y), Is.True);
            Assert.That(HudLayout04E.BondsMessageMaxWidth(s125.x, s125.y), Is.EqualTo(HudLayout04E.MinimapW), "compact lines wrap at the minimap's width");
            Vector2 s13 = HudLayout04E.VirtualSize(1920f, 1080f, 1.3f);
            Assert.That(HudLayout04E.BondsMessageLines(s13.x, s13.y), Is.EqualTo(1));
            Vector2 s14 = HudLayout04E.VirtualSize(1920f, 1080f, 1.4f);
            Assert.That(HudLayout04E.BondsMessageLines(s14.x, s14.y), Is.EqualTo(0));
            Assert.That(HudLayout04E.RewardsChipVisible(s14.x, s14.y), Is.True);
            Vector2 s15 = HudLayout04E.VirtualSize(1920f, 1080f, 1.5f);
            Assert.That(HudLayout04E.RewardsChipVisible(s15.x, s15.y), Is.False, "no room left: the chip hides");
            foreach (float scale in new[] { 1f, 1.1f, 1.25f, 1.3f, 1.4f })
            {
                Vector2 v = HudLayout04E.VirtualSize(1920f, 1080f, scale);
                if (HudLayout04E.RewardsChipVisible(v.x, v.y))
                    Assert.That(HudLayout04E.RewardsChip(v.x, v.y, 490f).yMin, Is.GreaterThanOrEqualTo(HudLayout04E.TopLeftFloor), "scale " + scale);
            }
        }

        [Test]
        public void TheReportedCollisionsAreGoneAt1080p()
        {
            const float W = 1920f, H = 1080f;
            // Companion panel and its brace strip sit wholly above the guidance route hint.
            Rect hint = HudLayout04E.RouteHint(W, H);
            Assert.That(HudLayout04E.BondsPanelBackground(W, H).yMax, Is.LessThanOrEqualTo(hint.yMin));
            Assert.That(HudLayout04E.BondsAbove(W, H).yMax, Is.LessThanOrEqualTo(HudLayout04E.BondsPanelBackground(W, H).yMin));
            Assert.That(hint.yMax, Is.LessThanOrEqualTo(HudLayout04E.MinimapTop(H) - 4f), "the route hint sits on the minimap frame");
            // The lead line and the quest tracker share the bottom strip, under the travel panel (795, 994, 330, 46).
            Rect strip = HudLayout04E.BottomStrip(W, H);
            Assert.That(strip.yMin, Is.GreaterThanOrEqualTo(994f + 46f));
            Assert.That(strip.yMax, Is.LessThanOrEqualTo(H));
            // The fusion panel is clear of the boss bar (510, 900, 900, 50), the tracker and the 04D line (974..1000).
            Rect fusion = HudLayout04E.FusionPanel(W, H);
            Assert.That(fusion.Overlaps(new Rect(510f, 900f, 900f, 50f)), Is.False);
            Assert.That(fusion.yMax, Is.LessThan(974f));
            // The heat row is below all three combat callouts (648..786) and the progress label (679..727), and the
            // prompts start under it underground (on the surface they keep 799).
            Rect heat = HudLayout04E.HeatRow(W, H);
            Assert.That(heat.yMin, Is.GreaterThanOrEqualTo(786f));
            Assert.That(heat.yMin, Is.GreaterThanOrEqualTo(727f));
            Assert.That(heat.yMax, Is.LessThanOrEqualTo(HudLayout04E.PromptsTop(H, true)), "and above the prompts underground");
            // The ward panel and the mechanism panel are clear of the loot pickup card (1450, 290, 430, 470+).
            Rect lootCard = new Rect(W - 470f, H * 0.5f - 250f, 430f, 580f);
            Assert.That(HudLayout04E.WardBox(W, H).Overlaps(lootCard), Is.False);
            Assert.That(HudLayout04E.MechanismPanel(W, H).Overlaps(lootCard), Is.False);
            Assert.That(HudLayout04E.EncounterSlot(W, H).Overlaps(lootCard), Is.False);
            Assert.That(HudLayout04E.RewardCard04D(W, H).Overlaps(lootCard), Is.False);
            // The ecology box is below the world event callout (106..192) and its status lines (to 270) and the banners.
            Rect eco = HudLayout04E.EcologyBox(W, H, 0);
            Assert.That(eco.yMin, Is.GreaterThanOrEqualTo(270f));
            Assert.That(eco.yMin, Is.GreaterThanOrEqualTo(H * 0.18f + 118f));
        }

        [Test]
        public void TheBossBlockFitsItsSlotAndTheSlotNeverReachesTheLootCard()
        {
            foreach (Vector2 screen in Screens)
            foreach (float scale in new[] { 1f, 1.25f })
            {
                Vector2 v = HudLayout04E.VirtualSize(screen.x, screen.y, scale);
                Rect slot = HudLayout04E.EncounterSlot(v.x, v.y);
                Rect bar = HudLayout04E.BossBar(v.x, v.y);
                Assert.That(bar.xMin, Is.GreaterThanOrEqualTo(slot.xMin));
                Assert.That(bar.xMax, Is.LessThanOrEqualTo(slot.xMax));
                // Name row (28) above the bar, status line (24), tell text (26) and its bar (6) all inside the slot.
                Assert.That(slot.yMin + 80f + 28f + 6f, Is.LessThanOrEqualTo(slot.yMax));
                Assert.That(slot.width, Is.LessThanOrEqualTo(900f));
                // Above the crosshair zone at every scale.
                Assert.That(slot.yMax, Is.LessThanOrEqualTo(v.y * 0.5f - 100f));
                if (scale <= 1f) Assert.That(slot.xMax, Is.LessThanOrEqualTo(v.x - 470f));
            }
        }

        [Test]
        public void FitFontSizeStepsDownOnlyWhenNeeded()
        {
            Assert.That(HudLayout04E.FitFontSize(500f, 1000f, 19, 14), Is.EqualTo(19));
            Assert.That(HudLayout04E.FitFontSize(1000f, 900f, 19, 14), Is.EqualTo(17));
            Assert.That(HudLayout04E.FitFontSize(5000f, 900f, 19, 14), Is.EqualTo(14), "never below the minimum");
            Assert.That(HudLayout04E.FitFontSize(0f, 900f, 19, 14), Is.EqualTo(19));
        }

        // ================================================================== choice card

        [Test]
        public void AShortChoiceTextKeepsTheClassicCard()
        {
            ChoiceCardFit04E fit = ChoiceCardLayout04E.Fit(1080f, size => 140f);
            Assert.That(fit.TextSize, Is.EqualTo(24));
            Assert.That(fit.CardHeight, Is.EqualTo(540f).Within(0.01f));
            Assert.That(fit.Scroll, Is.False);
            Assert.That(fit.AreaHeight, Is.GreaterThanOrEqualTo(fit.TextHeight));
        }

        [Test]
        public void ALongChoiceTextGrowsTheCardThenShrinksThenScrolls()
        {
            // Grows: 500 px at size 24 fits a taller card.
            ChoiceCardFit04E grow = ChoiceCardLayout04E.Fit(1080f, size => 500f);
            Assert.That(grow.TextSize, Is.EqualTo(24));
            Assert.That(grow.CardHeight, Is.GreaterThan(540f));
            Assert.That(grow.CardHeight, Is.LessThanOrEqualTo(1080f * 0.9f));
            Assert.That(grow.AreaHeight, Is.GreaterThanOrEqualTo(grow.TextHeight));
            // Shrinks: too tall at 24 and 22, fits at 20.
            ChoiceCardFit04E shrink = ChoiceCardLayout04E.Fit(1080f, size => size * 34f);
            Assert.That(shrink.TextSize, Is.EqualTo(20));
            Assert.That(shrink.Scroll, Is.False);
            Assert.That(shrink.AreaHeight, Is.GreaterThanOrEqualTo(shrink.TextHeight));
            // Scrolls: too tall even at 16 on the tallest card.
            ChoiceCardFit04E scroll = ChoiceCardLayout04E.Fit(1080f, size => 3000f);
            Assert.That(scroll.Scroll, Is.True);
            Assert.That(scroll.TextSize, Is.EqualTo(16));
            Assert.That(scroll.CardHeight, Is.EqualTo(1080f * 0.9f).Within(0.01f));
            foreach (ChoiceCardFit04E f in new[] { grow, shrink, scroll })
                Assert.That(ChoiceCardLayout04E.TitleBlock + f.AreaHeight + ChoiceCardLayout04E.ButtonBlock, Is.EqualTo(f.CardHeight).Within(0.01f), "the body area ends above the buttons");
        }

        // ================================================================== offer text

        [Test]
        public void TheWhereLineNamesTheVenueOnce()
        {
            Assert.That(GuidanceRules04E.WhereLine("Cinder Dispatch", "Cinder Dispatch, ground floor, across Veyra Avenue"), Is.EqualTo("Cinder Dispatch, ground floor, across Veyra Avenue"));
            Assert.That(GuidanceRules04E.WhereLine("The Pressure House", "Pressure House workroom, then the service level 6 m below"), Is.EqualTo("Pressure House workroom, then the service level 6 m below"));
            Assert.That(GuidanceRules04E.WhereLine("Cinder Dispatch", "ground floor"), Is.EqualTo("Cinder Dispatch, ground floor"));
            Assert.That(GuidanceRules04E.WhereLine("Lantern Refuge", ""), Is.EqualTo("Lantern Refuge"));
            Assert.That(GuidanceRules04E.WhereLine("", "upper floor"), Is.EqualTo("upper floor"));
            Assert.That(GuidanceRules04E.WhereLine("Lantern Refuge (underground)", "Lantern Refuge"), Is.EqualTo("Lantern Refuge (underground)"));
            OfferView04E v = new OfferView04E { Giver = "Nessa Vale", Location = "Cinder Dispatch", Floor = "Cinder Dispatch, ground floor, across Veyra Avenue" };
            string text = GuidanceRules04E.OfferText(v);
            Assert.That(text, Does.Contain("Cinder Dispatch, ground floor, across Veyra Avenue"));
            Assert.That(text, Does.Not.Contain("Cinder Dispatch, Cinder Dispatch"));
        }

        // ================================================================== loaded safehouse and ground search

        [Test]
        public void AnUndergroundResumeNeverBecomesTheSafehouse()
        {
            Vector3 lofts = new Vector3(-6f, 0.15f, -65.7f);
            Vector3 anchor = new Vector3(40f, -26f, -80f);
            Assert.That(SafehouseRules04E.LoadedSafehouse(lofts, anchor, true), Is.EqualTo(lofts), "surface safehouse kept");
            Assert.That(SafehouseRules04E.LoadedSafehouse(lofts, lofts, false), Is.EqualTo(lofts));
            Vector3 venueDoor = new Vector3(10f, 0.15f, 3f);
            Assert.That(SafehouseRules04E.LoadedSafehouse(lofts, venueDoor, false), Is.EqualTo(venueDoor), "no Underheart move: the resolved point, as before");
            Vector3 rest = new Vector3(38f, -18f, -70f);
            Assert.That(SafehouseRules04E.LoadedSafehouse(rest, anchor, true), Is.EqualTo(anchor), "an underground rest point follows the verified resume point");
            Vector3 bad = new Vector3(float.NaN, 0f, 0f);
            Assert.That(SafehouseRules04E.LoadedSafehouse(bad, anchor, true), Is.EqualTo(anchor));
        }

        [Test]
        public void TheHighGroundFallbackIsNeverUsedBelowTheStreet()
        {
            Assert.That(GroundRules04E.MayUseHighFallback(0f), Is.True);
            Assert.That(GroundRules04E.MayUseHighFallback(12f), Is.True, "roofs and the canyon");
            Assert.That(GroundRules04E.MayUseHighFallback(WorldBounds.UndergroundCeiling), Is.True);
            Assert.That(GroundRules04E.MayUseHighFallback(WorldBounds.UndergroundCeiling - 0.1f), Is.False);
            Assert.That(GroundRules04E.MayUseHighFallback(-26f), Is.False, "a point just outside an Underheart cell");
        }

        // ================================================================== world events

        [Test]
        public void SurfaceWorldEventsWaitWhileThePlayerIsUnderground()
        {
            Assert.That(WorldEvents.MayStartSurfaceEvent(false, false), Is.True);
            Assert.That(WorldEvents.MayStartSurfaceEvent(true, false), Is.False);
            Assert.That(WorldEvents.MayStartSurfaceEvent(true, true), Is.True, "a forced debug spawn still may");
        }

        // ================================================================== lava loop

        [Test]
        public void TheLavaLoopSitsOnTheNearestLavaChannelInRange()
        {
            List<UhHazard> hazards = new List<UhHazard>
            {
                new UhHazard { Id = "vent", Kind = UhHazardKind.HotVent, X0 = 0f, X1 = 2f, Z0 = 0f, Z1 = 2f, Y0 = -26f, Y1 = -24f },
                new UhHazard { Id = "far", Kind = UhHazardKind.Lava, X0 = 60f, X1 = 70f, Z0 = 0f, Z1 = 4f, Y0 = -26.5f, Y1 = -25.5f },
                new UhHazard { Id = "near", Kind = UhHazardKind.Lava, X0 = 10f, X1 = 20f, Z0 = -2f, Z1 = 2f, Y0 = -26.5f, Y1 = -25.5f },
                new UhHazard { Id = "pool", Kind = UhHazardKind.Lava, Round = true, X0 = -30f, X1 = -20f, Z0 = -5f, Z1 = 5f, Y0 = -26.5f, Y1 = -25.5f }
            };
            Vector3 player = new Vector3(5f, -25f, 0f);
            Assert.That(LavaLoopRules04E.Nearest(hazards, player, 34f), Is.EqualTo(2), "the vent is not lava; the near channel wins");
            Vector3 p = LavaLoopRules04E.NearestPoint(hazards[2], player);
            Assert.That(p.x, Is.EqualTo(10f).Within(1e-4f));
            Assert.That(p.z, Is.EqualTo(0f).Within(1e-4f));
            Assert.That(p.y, Is.EqualTo(-25.5f).Within(1e-4f));
            Assert.That(LavaLoopRules04E.Nearest(hazards, new Vector3(5f, -25f, 200f), 34f), Is.EqualTo(-1), "out of range");
            Vector3 round = LavaLoopRules04E.NearestPoint(hazards[3], new Vector3(-10f, -26f, 0f));
            Assert.That(round.x, Is.EqualTo(-20f).Within(1e-3f), "on the rim of the round pool");
            Vector3 inside = LavaLoopRules04E.NearestPoint(hazards[2], new Vector3(15f, -26f, 1f));
            Assert.That(inside.x, Is.EqualTo(15f).Within(1e-4f));
            Assert.That(LavaLoopRules04E.Nearest(null, player, 34f), Is.EqualTo(-1));
        }
    }
}
