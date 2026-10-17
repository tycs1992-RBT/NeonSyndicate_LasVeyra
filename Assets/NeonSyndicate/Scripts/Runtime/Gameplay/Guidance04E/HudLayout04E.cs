using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>The common play states the HUD layout is checked in (the EditMode layout test walks every one).</summary>
    public enum HudState04E
    {
        /// <summary>On foot, exploring (Free Roam, between quests).</summary>
        ExploreFoot = 0,
        /// <summary>On foot in a fight (callouts, enemy bars, cover, loot cards, a finite encounter).</summary>
        FightFoot = 1,
        /// <summary>Riding or driving (speedometer, district popups, the fusion prompt).</summary>
        Vehicle = 2,
        /// <summary>Fused with the companion on the personal chassis.</summary>
        Fused = 3,
        /// <summary>An Underheart boss fight on foot (always inside a campaign quest).</summary>
        BossFight = 4,
        /// <summary>On foot during a quest (objective, tracker, a Choir mechanism).</summary>
        Quest = 5,
        /// <summary>On foot just after a fight cleared, with the 04D reward card up (any of the foot elements may still show).</summary>
        RewardAfterFight = 6
    }

    /// <summary>A named HUD rectangle: an existing element the 04E panels keep clear of, or a 04E panel.</summary>
    public struct HudBox04E
    {
        public string Name;
        public Rect Rect;
        /// <summary>True for the panels this layout places; false for existing HUD elements mirrored here.</summary>
        public bool Is04E;

        public HudBox04E(string name, Rect rect, bool is04E)
        {
            Name = name;
            Rect = rect;
            Is04E = is04E;
        }
    }

    /// <summary>
    /// One HUD layout for every panel 04E adds, and for the older HUD pieces it had to move (the combat callout stack,
    /// the contextual prompts underground, the district name on foot, the 04D reward card), in UIKit's 1080p reference
    /// space: W and H are UIKit.Width and UIKit.Height, 1920 x 1080 on a 16:9 screen at the default HUD scale. A HUD
    /// scale above 1 shrinks that space (1.25 gives 1536 x 864 on 16:9); then the compact rules apply: the companion
    /// messages drop to one line or none and wrap at the minimap's width, the rewards chip hides when the left column
    /// has no room for it, the encounter slot (boss block, ecology box, Choir panel, 04D reward card) rises to sit just
    /// above the crosshair zone and steps right of the left column, the heat row narrows to stay clear of the loot card
    /// and keeps above the objective, the callouts rise above the heat row, and the session chip moves under the banners
    /// when the top strip is too narrow. Each panel's draw code reads its rectangle here. Reserved() mirrors the existing
    /// HUD (Hud.cs, HudCombat.cs and the older overlays) per common state, and HudLayout04ETests checks that no 04E panel
    /// overlaps another one or an existing element in any common state on 16:9, 16:10 and 21:9 at the default scale; at
    /// HUD scales up to 1.25 it allows only the transient cards listed in Transient; up to 1.5 it checks that the 04E
    /// panels stay on screen and never overlap each other. Engine-free: Rect and Mathf only. Docs/Underheart04E/GUIDANCE.md
    /// ("HUD layout") lists the same rectangles in plain words.
    /// </summary>
    public static class HudLayout04E
    {
        /// <summary>UIKit's reference height: the virtual screen is this tall at the default HUD scale.</summary>
        public const float RefHeight = 1080f;

        /// <summary>True when the HUD scale setting is above 1 (the virtual screen is shorter than 1080): compact rules apply.</summary>
        public static bool Compact(float H) { return H < RefHeight - 1f; }

        /// <summary>UIKit.Width and UIKit.Height for a screen size and a HUD scale setting (the same formula as UIKit.Scale).</summary>
        public static Vector2 VirtualSize(float screenW, float screenH, float hudScale)
        {
            float s = Mathf.Max(0.4f, screenH / RefHeight * hudScale);
            return new Vector2(screenW / s, screenH / s);
        }

        // ================================================================== existing HUD geometry (mirrors Hud.DrawMinimap)

        public const float MinimapX = 36f;
        public const float MinimapW = 330f;

        /// <summary>The minimap's height (the map texture is 512 x 336).</summary>
        public static float MinimapH { get { return MinimapW * 336f / 512f; } }

        /// <summary>The top edge of the minimap picture (its dark frame starts 4 px higher).</summary>
        public static float MinimapTop(float H) { return H - MinimapH - 70f; }

        /// <summary>The elite and creature boss bar's width at the bottom centre (Hud.DrawBossBar).</summary>
        public static float EliteBarWidth(float W) { return Mathf.Min(900f, W * 0.55f); }

        /// <summary>The top of the objective and subtitle block at the bottom centre (Hud.DrawMessages).</summary>
        public static float ObjectiveTop(float H) { return H - 208f; }

        // ================================================================== left column: minimap, route hint, companion, ward

        /// <summary>The left column never climbs above this (under the vitals, the tonic row and the Forge brew timers).</summary>
        public const float TopLeftFloor = 166f;

        /// <summary>Guidance: the up or down hint with the floor or region label, sitting on the minimap's frame.</summary>
        public static Rect RouteHint(float W, float H) { return new Rect(MinimapX, MinimapTop(H) - 40f, MinimapW, 32f); }

        public const float BondsPanelHeight = 92f;

        /// <summary>The companion panel's content box (its dark background reaches 4 px around it), 10 px above the route hint.</summary>
        public static Rect BondsPanel(float W, float H)
        {
            Rect hint = RouteHint(W, H);
            return new Rect(MinimapX, hint.y - 10f - BondsPanelHeight, MinimapW, BondsPanelHeight);
        }

        /// <summary>The companion panel with its background.</summary>
        public static Rect BondsPanelBackground(float W, float H)
        {
            Rect p = BondsPanel(W, H);
            return new Rect(p.x - 4f, p.y - 4f, p.width + 8f, p.height + 8f);
        }

        /// <summary>Companion messages stack upward from here when no companion is active.</summary>
        public static float BondsIdleMessagesBottom(float W, float H) { return BondsPanel(W, H).yMax + 4f; }

        /// <summary>The widest a companion message line gets (BondSystem.Hud): 720, less on narrow screens so a long line
        /// never reaches the crosshair zone (resonance burst, crystal labels) left of the screen centre.</summary>
        public static float BondsMessageMaxWidth(float W) { return Mathf.Clamp(W * 0.5f - 248f, 330f, 720f); }

        /// <summary>The same, with the compact rule: above HUD scale 1 the lines wrap at the minimap's width.</summary>
        public static float BondsMessageMaxWidth(float W, float H) { return Compact(H) ? MinimapW : BondsMessageMaxWidth(W); }

        /// <summary>One companion message (a long line wraps onto two rows) with its gap.</summary>
        public const float BondsMessageRow = 44f;
        /// <summary>The brace strip (30 px and its gap) and the control hint (22 px) above the panel.</summary>
        public const float BondsBraceAndHint = 34f + 22f;
        public const int BondsMessageLinesMax = 2;

        public const float WardBoxWidth = 276f;
        public const float WardBoxHeight = 80f;
        public const float RewardsChipHeight = 30f;

        /// <summary>The top of the rewards chip when the companion messages reserve `lines` lines.</summary>
        private static float ChipTop(float W, float H, int lines)
        {
            float aboveTop = BondsPanel(W, H).y - BondsBraceAndHint - lines * BondsMessageRow;
            return aboveTop - 4f - WardBoxHeight - 4f - RewardsChipHeight;
        }

        /// <summary>
        /// How many companion message lines the column reserves above the panel: two at the default scale, fewer when the
        /// whole left column (messages, ward panel, rewards chip) would otherwise climb above TopLeftFloor. BondSystem.Hud
        /// draws at most this many.
        /// </summary>
        public static int BondsMessageLines(float W, float H)
        {
            for (int lines = BondsMessageLinesMax; lines > 0; lines--)
                if (ChipTop(W, H, lines) >= TopLeftFloor) return lines;
            return 0;
        }

        /// <summary>
        /// Everything the companion HUD can draw above its panel in the worst case: the brace strip (30 px), the control
        /// hint (22 px) and the reserved message lines (44 px each, two at the default scale).
        /// </summary>
        public static Rect BondsAbove(float W, float H)
        {
            Rect p = BondsPanel(W, H);
            float top = p.y - BondsBraceAndHint - BondsMessageLines(W, H) * BondsMessageRow;
            return new Rect(MinimapX - 4f, top, BondsMessageMaxWidth(W, H) + 8f, p.y - 4f - top);
        }

        /// <summary>
        /// Builds: the ward panel (background box) in the left column, directly above everything the companion HUD can
        /// draw. The right-hand loot pickup card, the contextual site hints and the gadget slots never reach it.
        /// </summary>
        public static Rect WardBox(float W, float H)
        {
            Rect above = BondsAbove(W, H);
            return new Rect(MinimapX - 4f, above.y - 4f - WardBoxHeight, WardBoxWidth, WardBoxHeight);
        }

        /// <summary>Builds: the "rewards waiting" chip (out of combat), just above the ward panel.</summary>
        public static Rect RewardsChip(float W, float H, float textWidth)
        {
            return new Rect(24f, WardBox(W, H).y - 4f - RewardsChipHeight, Mathf.Clamp(textWidth + 30f, 120f, 520f), RewardsChipHeight);
        }

        /// <summary>False when even without companion message lines the chip would climb above TopLeftFloor (a large HUD
        /// scale): it then stays hidden; the reward banner, BUILD > REWARDS and the ledgers still list what is waiting.</summary>
        public static bool RewardsChipVisible(float W, float H) { return ChipTop(W, H, BondsMessageLines(W, H)) >= TopLeftFloor; }

        /// <summary>Gadgets: the gear score and city tier on one line under the minimap frame.</summary>
        public static Rect GearScoreLine(float W, float H) { return new Rect(MinimapX, H - 34f, MinimapW, 24f); }

        /// <summary>
        /// The right edge of the left-column panels (companion messages, ward panel and, when withChip, the rewards chip)
        /// that share the vertical span top..top+height; 0 when none does.
        /// </summary>
        public static float LeftColumnRight(float W, float H, float top, float height, bool withChip)
        {
            float right = 0f;
            Rect a = BondsAbove(W, H);
            if (a.yMin < top + height && a.yMax > top) right = Mathf.Max(right, a.xMax);
            Rect w = WardBox(W, H);
            if (w.yMin < top + height && w.yMax > top) right = Mathf.Max(right, w.xMax);
            if (withChip && RewardsChipVisible(W, H))
            {
                Rect c = RewardsChip(W, H, 520f);
                if (c.yMin < top + height && c.yMax > top) right = Mathf.Max(right, c.xMax);
            }
            return right;
        }

        /// <summary>The x of a centred panel, stepped right of the left column when they share rows (only at large HUD scales).</summary>
        public static float CentredX(float W, float H, float width, float top, float height, bool withChip)
        {
            float x = W * 0.5f - width * 0.5f;
            float right = LeftColumnRight(W, H, top, height, withChip);
            return right > 0f ? Mathf.Max(x, right + 8f) : x;
        }

        // ================================================================== bottom strip, centre and right

        /// <summary>
        /// The one-line strip at the bottom edge for the guidance lead line (no mission running) and the quest tracker (a
        /// quest running); the two are never shown together. Clear of the minimap, the travel and cover panels above it
        /// and the Lumen corner.
        /// </summary>
        public static Rect BottomStrip(float W, float H) { return new Rect(400f, H - 36f, Mathf.Max(200f, W - 800f), 28f); }

        public const float HeatRowHeight = 34f;

        /// <summary>The heat row's width: 480, narrower on small virtual screens so it never reaches the loot card (W - 470).</summary>
        public static float HeatWidth(float W) { return Mathf.Clamp(2f * (W * 0.5f - 476f), 300f, 480f); }

        /// <summary>The heat row's top: the old top of the prompt column, kept above the objective block on short screens.</summary>
        public static float HeatTop(float H) { return Mathf.Min(H * 0.74f, ObjectiveTop(H) - HeatRowHeight - 4f); }

        /// <summary>
        /// Underheart: the heat readout row (heat and the cooled timer share it), at the top of the prompt column, below
        /// the full combat callout stack (three lines) and the progress label. Underground the contextual prompts start
        /// under it (PromptsTop), so the two never print over each other.
        /// </summary>
        public static Rect HeatRow(float W, float H)
        {
            float w = HeatWidth(W);
            return new Rect(W * 0.5f - w * 0.5f, HeatTop(H), w, HeatRowHeight);
        }

        /// <summary>Where the contextual prompts start (Hud.DrawPrompts): under the heat row while the player is underground.</summary>
        public static float PromptsTop(float H, bool underground) { return underground ? HeatTop(H) + HeatRowHeight + 6f : H * 0.74f; }

        /// <summary>The combat callouts (PARRY, a boss's phase or window line): at most three lines, 44 px apart, 50 px tall.</summary>
        public const int MaxCallouts = 3;
        public const float CalloutStep = 44f;
        public const float CalloutHeight = 50f;

        /// <summary>The whole callout stack with three lines.</summary>
        public static float CalloutStackHeight { get { return (MaxCallouts - 1) * CalloutStep + CalloutHeight; } }

        /// <summary>The first callout's top (HudCombat.DrawCallouts): H * 0.6 as before, raised on short screens so the third
        /// line still ends above the heat row.</summary>
        public static float CalloutTop(float H) { return Mathf.Min(H * 0.6f, HeatTop(H) - CalloutStackHeight - 2f); }

        public const float FusionWidth = 380f;
        public const float FusionHeight = 128f;

        /// <summary>
        /// Fusion: the vehicle panel in the right column above the vehicle Lumen readout and the district and vehicle
        /// name popups, below the money and weapon block, clear of the contextual site hints and the bottom-centre boss
        /// bar, objective and encounter lines.
        /// </summary>
        public static Rect FusionPanel(float W, float H) { return new Rect(W - 40f - FusionWidth, H - 480f, FusionWidth, FusionHeight); }

        /// <summary>Fusion: the toast line's box above the panel (two lines at most).</summary>
        public static Rect FusionToastBox(float W, float H)
        {
            Rect p = FusionPanel(W, H);
            return new Rect(p.x, p.y - 56f, p.width, 50f);
        }

        public const float DistrictChipHeight = 28f;

        /// <summary>The on-foot district chip's width: 380, narrower on small screens so it stays right of the 04D encounter line.</summary>
        public static float DistrictChipWidth(float W) { return Mathf.Clamp(W - 40f - (W * 0.5f + 444f), 200f, 380f); }

        /// <summary>
        /// The district name on foot (Hud.DrawPopups): one line, right-aligned, in the band between the gadget slots (to
        /// H - 135) and the Lumen readout (from H - 100), right of the elite bar, the cover panel and the 04D encounter
        /// line. It used to print two large lines over the Lumen readout. It is skipped while an objective or a subtitle
        /// is on screen. In a vehicle the district popup keeps its place above the speedometer.
        /// </summary>
        public static Rect DistrictChipFoot(float W, float H)
        {
            float w = DistrictChipWidth(W);
            return new Rect(W - 40f - w, H - 130f, w, DistrictChipHeight);
        }

        // ================================================================== encounter slot (top centre, under the banners)

        public const float EncounterHeight = 118f;

        /// <summary>The encounter slot's top edge: just under the banner area, raised on short screens to end above the
        /// crosshair zone (from H / 2 - 100).</summary>
        public static float EncounterTop(float H) { return Mathf.Min(H * 0.18f + 122f, H * 0.5f - 104f - EncounterHeight); }

        /// <summary>The encounter slot's width: at most 900, and never reaching the right-hand loot card (W - 470).</summary>
        public static float EncounterWidth(float W) { return Mathf.Clamp(W - 960f, 460f, 900f); }

        /// <summary>
        /// The shared slot for the one finite encounter a quest step runs at a time: the Underheart boss block (bar,
        /// mechanic or boundary line, the move winding up), the ecology encounter box or a Choir mechanism panel.
        /// </summary>
        public static Rect EncounterSlot(float W, float H)
        {
            float w = EncounterWidth(W);
            float top = EncounterTop(H);
            return new Rect(CentredX(W, H, w, top, EncounterHeight, false), top, w, EncounterHeight);
        }

        /// <summary>Ecology: the encounter box of the index-th running skirmish near the player (one in practice).</summary>
        public static Rect EcologyBox(float W, float H, int index)
        {
            float top = EncounterTop(H) + index * 60f;
            return new Rect(CentredX(W, H, 460f, top, 54f, false), top, 460f, 54f);
        }

        /// <summary>Bosses: the Choir mechanism panel.</summary>
        public static Rect MechanismPanel(float W, float H)
        {
            float top = EncounterTop(H);
            return new Rect(CentredX(W, H, 440f, top, EncounterHeight, true), top, 440f, EncounterHeight);
        }

        /// <summary>Bosses: the boss health bar (name and phase row above it, the status line and the tell below).</summary>
        public static Rect BossBar(float W, float H)
        {
            Rect slot = EncounterSlot(W, H);
            return new Rect(slot.x, slot.y + 34f, slot.width, 14f);
        }

        /// <summary>The 04D reward card's width: like the slot, and narrow enough to clear the left column's rewards chip.</summary>
        public static float RewardCardWidth(float W) { return Mathf.Clamp(Mathf.Min(EncounterWidth(W), W - 1104f), 460f, 900f); }

        /// <summary>
        /// The 04D reward card (a signature or contract reward after a fight clears) in the encounter slot under the
        /// banners. It used to sit in the left column (40, H * 0.3, 520, 200) over the rewards chip, the ward panel and the
        /// companion messages. It waits while a 04E encounter panel holds the slot.
        /// </summary>
        public static Rect RewardCard04D(float W, float H)
        {
            float w = RewardCardWidth(W);
            float top = EncounterTop(H);
            return new Rect(CentredX(W, H, w, top, EncounterHeight, true), top, w, EncounterHeight);
        }

        // ================================================================== session chip (top, beside the vitals)

        /// <summary>
        /// Guidance: the session chip ("Underheart and Bonds 04E", a few seconds at session start), centred in the free span
        /// between the longest health bar (to x 806) and the money and weapon block (from W - 440). When that span is
        /// narrower than 320 (a large HUD scale) it sits in the empty encounter slot under the banners instead.
        /// </summary>
        public static Rect SessionChip(float W, float H)
        {
            float left = 812f, right = W - 452f;
            if (right - left >= 320f)
            {
                float w = Mathf.Min(580f, right - left);
                float cx = (left + right) * 0.5f;
                return new Rect(cx - w * 0.5f, 16f, w, 58f);
            }
            float cw = Mathf.Clamp((W - 482f - W * 0.5f) * 2f, 240f, 580f);
            float top = EncounterTop(H);
            return new Rect(CentredX(W, H, cw, top, 58f, true), top, cw, 58f);
        }

        // ================================================================== text fitting

        /// <summary>The largest font size (from `max` down to `min`) whose width, scaled from the width measured at `max`, fits.</summary>
        public static int FitFontSize(float widthAtMax, float available, int max, int min)
        {
            if (widthAtMax <= available || widthAtMax <= 0.01f) return max;
            int size = Mathf.FloorToInt(max * available / widthAtMax);
            return Mathf.Clamp(size, min, max);
        }

        // ================================================================== the layout model (tests and docs)

        /// <summary>
        /// Existing HUD elements that are short-lived cards or popups (banners, world event callouts, toasts, the loot
        /// cards, site and place hints, the progress label, brew timers, vehicle popups, mission timers). At HUD scales
        /// above 1 the test lets a 04E panel share space with these; never at the default scale.
        /// </summary>
        public static readonly string[] Transient =
        {
            "banner", "world_events", "faction.toasts", "bestiary.toasts", "loot.pickup_card", "gear.loot_popups", "site_hints",
            "inhabited.place_hint", "lootworld.third_eye", "progress", "forge.brews", "district_and_vehicle_name", "radio",
            "mission.timer_counter"
        };

        /// <summary>The existing HUD elements visible in a common state (worst case: underground, with the heat row up).</summary>
        public static List<HudBox04E> Reserved(HudState04E s, float W, float H, int maxTonics)
        {
            return Reserved(s, W, H, maxTonics, true);
        }

        /// <summary>The existing HUD elements visible in a common state (bounds of what they draw, at worst common size).</summary>
        public static List<HudBox04E> Reserved(HudState04E s, float W, float H, int maxTonics, bool underground)
        {
            List<HudBox04E> list = new List<HudBox04E>(40);
            // Always: the vitals (longest bars), the money and weapon block, the minimap with its clock and power line, banners.
            Add(list, "vitals.health", 37f, 31f, 766f, 22f);
            Add(list, "vitals.stamina", 37f, 57f, 646f, 16f);
            Add(list, "vitals.energy", 37f, 77f, 646f, 14f);
            Add(list, "vitals.tonics", 37f, 104f, 40f + maxTonics * 22f + 8f + 140f - 37f, 26f);
            Add(list, "top_right.money_weapon", W - 440f, 30f, 400f, 166f);
            Add(list, "minimap", 32f, MinimapTop(H) - 4f, 338f, MinimapH + 34f);
            Add(list, "banner", W * 0.15f, H * 0.18f, W * 0.7f, 118f);
            bool foot = s != HudState04E.Vehicle && s != HudState04E.Fused;
            bool reward = s == HudState04E.RewardAfterFight;
            bool fight = s == HudState04E.FightFoot || s == HudState04E.BossFight || reward;
            bool quest = s == HudState04E.Quest || s == HudState04E.BossFight || reward;
            if (foot)
            {
                Add(list, "lumen", W - 310f, H - 100f, 270f, 70f);
                Add(list, "lumen.echo", W - 300f, H - 28f, 260f, 24f);
                Add(list, "prompts", W * 0.5f - 380f, PromptsTop(H, underground), 760f, 106f);
                Add(list, "gadget.slots", W - 253f, H - 255f, 213f, 120f);
                Add(list, "loot.pickup_card", W - 470f, H * 0.5f - 250f, 430f, 470f + 3f * 26f + 32f);
                Add(list, "forge.brews", 40f, 138f, 600f, 24f);
            }
            else
            {
                Add(list, "speedometer", W - 340f, H - 130f, 300f, 94f);
                Add(list, "lumen.vehicle", W - 310f, H - 240f, 270f, 70f);
                Add(list, "lumen.echo.vehicle", W - 300f, H - 168f, 260f, 24f);
                Add(list, "district_and_vehicle_name", W - 480f, H - 286f, 440f, 116f);
                Add(list, "prompts.vehicle", W * 0.5f - 380f, H * 0.74f, 760f, 50f);
                Add(list, "radio", W * 0.5f - 500f, 30f, 1000f, 70f);
            }
            if (s == HudState04E.ExploreFoot || reward)
            {
                Add(list, "travel", W * 0.5f - 165f, H - 86f, 330f, 46f);
                Add(list, "inhabited.place_hint", W * 0.5f - 280f, 90f, 560f, 74f);
                Add(list, "lootworld.third_eye", W * 0.5f - 210f, 118f, 420f, 70f);
            }
            if (s == HudState04E.ExploreFoot || s == HudState04E.Vehicle || s == HudState04E.Fused || reward)
            {
                // Surface world events (status lines up to 840 wide) and the relay, stronghold and discovery hints.
                Add(list, "world_events", W * 0.5f - 420f, 106f, 840f, 164f);
                Add(list, "site_hints", W - 450f, H - 348f, 420f, 140f);
            }
            if (fight || s == HudState04E.Quest)
            {
                // Three callout lines at most (HudCombat keeps the newest three).
                Add(list, "combat.callouts", W * 0.5f - 250f, CalloutTop(H), 500f, CalloutStackHeight);
                Add(list, "progress", W * 0.5f - 300f, H * 0.66f - 34f, 600f, 48f);
                Add(list, "crosshair.zone", W * 0.5f - 200f, H * 0.5f - 100f, 400f, 210f);
                Add(list, "gear.loot_popups", W - 470f, H * 0.36f, 430f, 226f);
                Add(list, "bestiary.toasts", W - 560f, 210f, 520f, 92f);
                Add(list, "faction.toasts", 40f, 166f, 480f, 112f);
                float eb = EliteBarWidth(W);
                Add(list, "elite_boss_bar", W * 0.5f - eb * 0.5f - 3f, H - 180f, eb + 6f, 50f);
                Add(list, "cover", W * 0.5f - 300f, H - 126f, 600f, 86f);
                Add(list, "encounter_04d", W * 0.5f - 440f, H - 106f, 880f, 26f);
                Add(list, "travel", W * 0.5f - 165f, H - 86f, 330f, 46f);
            }
            if (quest || s == HudState04E.Vehicle || s == HudState04E.Fused)
            {
                Add(list, "objective_and_subtitle", W * 0.5f - 440f, ObjectiveTop(H), 880f, 128f);
                Add(list, "mission.timer_counter", W - 340f, 202f, 300f, 90f);
            }
            return list;
        }

        /// <summary>The 04E panels visible in a common state (worst case: underground, with the heat row up).</summary>
        public static List<HudBox04E> Panels(HudState04E s, float W, float H, int maxTonics)
        {
            return Panels(s, W, H, maxTonics, true);
        }

        /// <summary>The 04E panels (and the moved older pieces this layout now owns) visible in a common state.</summary>
        public static List<HudBox04E> Panels(HudState04E s, float W, float H, int maxTonics, bool underground)
        {
            List<HudBox04E> list = new List<HudBox04E>(16);
            bool foot = s != HudState04E.Vehicle && s != HudState04E.Fused;
            list.Add(new HudBox04E("guidance.route_hint", RouteHint(W, H), true));
            list.Add(new HudBox04E("bonds.panel", BondsPanelBackground(W, H), true));
            list.Add(new HudBox04E("bonds.above", BondsAbove(W, H), true));
            list.Add(new HudBox04E("guidance.bottom_strip", BottomStrip(W, H), true));
            list.Add(new HudBox04E("gadgets.gear_score", GearScoreLine(W, H), true));
            if (foot)
            {
                list.Add(new HudBox04E("builds.ward", WardBox(W, H), true));
                if (underground) list.Add(new HudBox04E("underheart.heat", HeatRow(W, H), true));
            }
            else
            {
                list.Add(new HudBox04E("fusion.panel", FusionPanel(W, H), true));
                list.Add(new HudBox04E("fusion.toast", FusionToastBox(W, H), true));
            }
            // The rewards chip shows out of combat only (and hides when a large HUD scale leaves it no room).
            if (s != HudState04E.FightFoot && s != HudState04E.BossFight && RewardsChipVisible(W, H))
                list.Add(new HudBox04E("builds.rewards_chip", RewardsChip(W, H, 520f), true));
            if (s == HudState04E.ExploreFoot) list.Add(new HudBox04E("guidance.session_chip", SessionChip(W, H), true));
            // The district name on foot: skipped while an objective or subtitle shows (quest states).
            if (s == HudState04E.ExploreFoot || s == HudState04E.FightFoot) list.Add(new HudBox04E("hud.district_foot", DistrictChipFoot(W, H), true));
            if (s == HudState04E.FightFoot) list.Add(new HudBox04E("ecology.encounter_box", EcologyBox(W, H, 0), true));
            if (s == HudState04E.Quest) list.Add(new HudBox04E("bosses.mechanism", MechanismPanel(W, H), true));
            if (s == HudState04E.BossFight) list.Add(new HudBox04E("bosses.boss_block", EncounterSlot(W, H), true));
            if (s == HudState04E.RewardAfterFight) list.Add(new HudBox04E("arsenal.reward_card_04d", RewardCard04D(W, H), true));
            return list;
        }

        private static void Add(List<HudBox04E> list, string name, float x, float y, float w, float h)
        {
            list.Add(new HudBox04E(name, new Rect(x, y, w, h), false));
        }
    }
}
