using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    /// <summary>
    /// The 04E integration fixes in the GUIDANCE area: one HUD layout with no collisions in the common states, the choice
    /// card that never runs under its buttons, the offer's "where" line without a repeated venue name, the loaded
    /// safehouse rule, the ground search below the street, surface world events while underground and the lava loop
    /// placement. All engine-free.
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

        [Test]
        public void No04EPanelOverlapsTheExistingHudInAnyCommonState()
        {
            List<string> problems = new List<string>();
            foreach (Vector2 screen in Screens)
            foreach (HudState04E state in States)
            {
                List<HudBox04E> panels = HudLayout04E.Panels(state, screen.x, screen.y, 4);
                List<HudBox04E> reserved = HudLayout04E.Reserved(state, screen.x, screen.y, 4);
                foreach (HudBox04E p in panels)
                {
                    Assert.That(p.Is04E, Is.True, p.Name);
                    foreach (HudBox04E r in reserved)
                        if (p.Rect.Overlaps(r.Rect)) problems.Add(screen.x + "x" + screen.y + " " + state + ": " + p.Name + " " + p.Rect + " overlaps " + r.Name + " " + r.Rect);
                }
            }
            Assert.That(problems, Is.Empty, string.Join("\n", problems));
        }

        [Test]
        public void No04EPanelsOverlapEachOtherAndAllStayOnScreen()
        {
            List<string> problems = new List<string>();
            foreach (Vector2 screen in Screens)
            foreach (HudState04E state in States)
            {
                List<HudBox04E> panels = HudLayout04E.Panels(state, screen.x, screen.y, 4);
                for (int i = 0; i < panels.Count; i++)
                {
                    Rect a = panels[i].Rect;
                    if (a.xMin < 0f || a.yMin < 0f || a.xMax > screen.x || a.yMax > screen.y) problems.Add(state + " " + panels[i].Name + " is off screen " + a);
                    for (int j = i + 1; j < panels.Count; j++)
                        if (a.Overlaps(panels[j].Rect)) problems.Add(screen.x + " " + state + ": " + panels[i].Name + " overlaps " + panels[j].Name);
                }
            }
            Assert.That(problems, Is.Empty, string.Join("\n", problems));
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
            // The heat row is below the combat callouts (648..742 for two) and the progress label (679..727).
            Rect heat = HudLayout04E.HeatRow(W, H);
            Assert.That(heat.yMin, Is.GreaterThanOrEqualTo(742f));
            Assert.That(heat.yMin, Is.GreaterThanOrEqualTo(727f));
            Assert.That(heat.yMax, Is.LessThanOrEqualTo(H * 0.74f), "and above the prompts");
            // The ward panel and the mechanism panel are clear of the loot pickup card (1450, 290, 430, 470+).
            Rect lootCard = new Rect(W - 470f, H * 0.5f - 250f, 430f, 580f);
            Assert.That(HudLayout04E.WardBox(W, H).Overlaps(lootCard), Is.False);
            Assert.That(HudLayout04E.MechanismPanel(W, H).Overlaps(lootCard), Is.False);
            Assert.That(HudLayout04E.EncounterSlot(W, H).Overlaps(lootCard), Is.False);
            // The ecology box is below the world event callout (106..192) and its status lines (to 270) and the banners.
            Rect eco = HudLayout04E.EcologyBox(W, H, 0);
            Assert.That(eco.yMin, Is.GreaterThanOrEqualTo(270f));
            Assert.That(eco.yMin, Is.GreaterThanOrEqualTo(H * 0.18f + 118f));
        }

        [Test]
        public void TheBossBlockFitsItsSlotAndTheSlotNeverReachesTheLootCard()
        {
            foreach (Vector2 screen in Screens)
            {
                Rect slot = HudLayout04E.EncounterSlot(screen.x, screen.y);
                Rect bar = HudLayout04E.BossBar(screen.x, screen.y);
                Assert.That(bar.xMin, Is.GreaterThanOrEqualTo(slot.xMin));
                Assert.That(bar.xMax, Is.LessThanOrEqualTo(slot.xMax));
                // Name row (28) above the bar, status line (24), tell text (26) and its bar (6) all inside the slot.
                Assert.That(slot.yMin + 80f + 28f + 6f, Is.LessThanOrEqualTo(slot.yMax));
                Assert.That(slot.xMax, Is.LessThanOrEqualTo(screen.x - 470f));
                Assert.That(slot.width, Is.LessThanOrEqualTo(900f));
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
