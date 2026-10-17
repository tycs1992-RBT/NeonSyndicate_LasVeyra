using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The deep Underheart (deep frame): Pyre Gardens (floor -40, a 26 m cavern), Crucible Workshops (-55, under the
    /// refuge and canals, with the vehicle transit descent and the convoy corridor), Ash Reliquary (-72, under the
    /// gardens) and Foundry Crown (-95, a vast 35 m chamber under the workshops). Then the per-region critical paths.
    /// </summary>
    public sealed partial class UnderheartLayout
    {
        private const string G = UnderheartIds.PyreGardens;
        private const string W = UnderheartIds.CrucibleWorkshops;
        private const string Q = UnderheartIds.AshReliquary;
        private const string K = UnderheartIds.FoundryCrown;

        private void BuildGardens()
        {
            const float GF = -40f;
            AddCell("gardens.entry", G, UhCellKind.Ledge, UhTheme.Garden, -36f, 86f, -24f, 98f, -31f, 6f).Rock = true;
            AddSloped("gardens.descent", G, UhCellKind.Ramp, UhTheme.Garden, -64f, 86f, -36f, 98f, true, GF, -31f, 6f).Rock = true;
            UhCell cavern = AddCell("gardens.cavern", G, UhCellKind.Cavern, UhTheme.Garden, -124f, 50f, -64f, 140f, GF, 26f);
            cavern.Rock = true;
            UhCell ledgePath = AddCell("gardens.ledgepath", G, UhCellKind.Ledge, UhTheme.Garden, -56f, 98f, -30f, 106f, -31f, 5f);
            ledgePath.Rock = true;
            ledgePath.Optional = true;
            UhCell vista = AddCell("gardens.vistaledge", G, UhCellKind.Ledge, UhTheme.Garden, -64f, 100f, -56f, 124f, -31f, 6f);
            vista.Rock = true;
            vista.Optional = true;
            UhCell hut = AddCell("gardens.keeperhut", G, UhCellKind.Room, UhTheme.Garden, -64f, 56f, -50f, 70f, GF, 5f);
            hut.Safe = true;
            AddCell("gardens.exitpass", G, UhCellKind.Corridor, UhTheme.Garden, -50f, 58f, -30f, 66f, GF, 4f).Rock = true;
            UhCell coolA = AddCell("gardens.coolA", G, UhCellKind.Corridor, UhTheme.Garden, -136f, 100f, -124f, 112f, GF, 4.5f);
            coolA.Rock = true;
            UhCell coolB = AddCell("gardens.coolB", G, UhCellKind.Corridor, UhTheme.Garden, -136f, 66f, -124f, 78f, GF, 4.5f);
            coolB.Rock = true;
            UhCell seed = AddCell("gardens.seedchamber", G, UhCellKind.Cavern, UhTheme.Garden, -160f, 60f, -136f, 118f, GF, 12f);
            seed.Rock = true;
            AddCell("gardens.boundary", G, UhCellKind.Corridor, UhTheme.Garden, -104f, 140f, -84f, 150f, GF, 6f).Rock = true;
            UhCell lair = AddCell("gardens.matronlair", G, UhCellKind.Cavern, UhTheme.Garden, -118f, 150f, -70f, 190f, GF, 18f);
            lair.Rock = true;
            UhCell den = AddCell("gardens.den", G, UhCellKind.Cavern, UhTheme.Garden, -104f, 40f, -88f, 50f, GF, 6f);
            den.Rock = true;
            den.Optional = true;
            UhCell nest = AddCell("gardens.nest", G, UhCellKind.Cavern, UhTheme.Garden, -84f, 40f, -70f, 50f, GF, 6f);
            nest.Rock = true;
            nest.Optional = true;

            Door("canals.gardenspass", "gardens.entry", -24f, 92f, 3.5f, 3.2f);
            Door("gardens.entry", "gardens.descent", -36f, 92f, 8f, 5f);
            Door("gardens.descent", "gardens.cavern", -64f, 92f, 8f, 5f);
            Door("gardens.entry", "gardens.ledgepath", -33f, 98f, 3f, 3.2f);
            Door("gardens.ledgepath", "gardens.vistaledge", -56f, 103f, 3f, 3.2f);
            Door("gardens.vistaledge", "gardens.cavern", -64f, 112f, 20f, 5f, UhPortalKind.Window);
            Door("gardens.cavern", "gardens.keeperhut", -64f, 63f, 3f, 3.2f);
            Door("gardens.keeperhut", "gardens.exitpass", -50f, 62f, 2.4f, 3f);
            Door("gardens.cavern", "gardens.coolA", -124f, 106f, 4f, 3.4f);
            Door("gardens.cavern", "gardens.coolB", -124f, 72f, 4f, 3.4f);
            Door("gardens.coolA", "gardens.seedchamber", -136f, 106f, 4f, 3.4f);
            Door("gardens.coolB", "gardens.seedchamber", -136f, 72f, 4f, 3.4f);
            Door("gardens.cavern", "gardens.boundary", -94f, 140f, 6f, 4.5f);
            Door("gardens.boundary", "gardens.matronlair", -94f, 150f, 6f, 4.5f);
            Door("gardens.cavern", "gardens.den", -96f, 50f, 4f, 4f);
            Door("gardens.cavern", "gardens.nest", -77f, 50f, 3f, 3.4f);

            Place("gardens.entry", "gardens.entry", -28f, 92f, 270f);
            Place("gardens.caretaker", "gardens.keeperhut", -56f, 67f, 270f);
            Place("gardens.study_a", "gardens.cavern", -80f, 122f, 0f);
            Place("gardens.study_b", "gardens.cavern", -110f, 128f, 0f);
            Place("gardens.study_c", "gardens.cavern", -112f, 64f, 180f);
            Place("gardens.cool_route_a", "gardens.coolA", -130f, 106f, 270f);
            Place("gardens.cool_route_b", "gardens.coolB", -130f, 72f, 270f);
            Place("gardens.seed_core", "gardens.seedchamber", -150f, 89f, 270f);
            Place("gardens.boundary", "gardens.boundary", -94f, 143f, 0f);
            Place("gardens.basilisk", "gardens.den", -96f, 44f, 180f);
            Place("gardens.rescue_nest", "gardens.nest", -77f, 44f, 180f);
            Place("gardens.matron_gate", "gardens.boundary", -94f, 148f, 0f);
            Place("gardens.matron_arena", "gardens.matronlair", -94f, 170f, 0f);
            Place("gardens.lure_a", "gardens.matronlair", -110f, 160f, 45f);
            Place("gardens.lure_b", "gardens.matronlair", -78f, 160f, 315f);
            Place("gardens.lure_c", "gardens.matronlair", -94f, 184f, 180f);
            Place("gardens.rest", "gardens.keeperhut", -58f, 59f, 90f);
            Place("gardens.vista", "gardens.vistaledge", -59f, 112f, 270f);
            Place("gardens.habitat", "gardens.cavern", -116f, 134f, 0f);
            Place("gardens.exit_workshops", "gardens.exitpass", -34f, 62f, 90f);

            // Cooling route A: hot vents between cooled pools. Route B: warm roots and a narrow lava seep with a stone bridge.
            Vent("gardens_ventA_0", "gardens.coolA", UhHazardKind.HotVent, -127f, 103f, 1.5f, 14f, 3.8f, 0f);
            Vent("gardens_ventA_1", "gardens.coolA", UhHazardKind.HotVent, -133f, 109f, 1.5f, 14f, 3.8f, 1.6f);
            Vent("gardens_coolA_0", "gardens.coolA", UhHazardKind.Cooling, -130f, 106f, 1.1f, 0f, 0f, 0f);
            Hazard("gardens_seepB_s", "gardens.coolB", UhHazardKind.Lava, -131f, 66.5f, -129f, 70.5f, 30f, false, true);
            Hazard("gardens_seepB_n", "gardens.coolB", UhHazardKind.Lava, -131f, 73.5f, -129f, 77.5f, 30f, false, true);
            Hazard("gardens_warmB", "gardens.coolB", UhHazardKind.Warm, -135f, 67f, -133f, 77f, 0f);
            Hazard("gardens_warm_tree", "gardens.cavern", UhHazardKind.Warm, -98f, 91f, -90f, 99f, 0f);
            Vent("gardens_cool_pool", "gardens.cavern", UhHazardKind.Cooling, -108f, 84f, 3.0f, 0f, 0f, 0f);
            Hazard("gardens_lava_lair", "gardens.matronlair", UhHazardKind.Lava, -100f, 174f, -88f, 178f, 30f, false, true);
            Landmark("lm_pyre_tree", "gardens.cavern", "The Pyre Tree", -94f, GF, 95f);
        }

        private void BuildWorkshops()
        {
            const float WF = -55f;
            AddSloped("workshops.stair1", W, UhCellKind.Stair, UhTheme.Workshop, -30f, 58f, -18f, 66f, true, -40f, -46f, 3.4f);
            AddCell("workshops.landing", W, UhCellKind.Corridor, UhTheme.Workshop, -18f, 58f, -10f, 66f, -46f, 3.4f);
            AddSloped("workshops.stair2", W, UhCellKind.Stair, UhTheme.Workshop, -10f, 58f, 2f, 66f, true, -46f, -52f, 3.4f);
            AddSloped("workshops.ramp3", W, UhCellKind.Ramp, UhTheme.Workshop, 2f, 58f, 14f, 66f, true, -52f, WF, 3.4f);
            AddCell("workshops.entryhall", W, UhCellKind.Room, UhTheme.Workshop, 14f, 52f, 58f, 72f, WF, 8f);
            AddCell("workshops.foundry", W, UhCellKind.Room, UhTheme.Workshop, 14f, 16f, 50f, 52f, WF, 10f);
            AddCell("workshops.gallery", W, UhCellKind.Corridor, UhTheme.Workshop, 50f, 16f, 58f, 52f, WF, 4f);
            AddCell("workshops.store", W, UhCellKind.Room, UhTheme.Workshop, -6f, 30f, 14f, 46f, WF, 5f).Optional = true;
            AddCell("workshops.records", W, UhCellKind.Room, UhTheme.Workshop, -6f, 46f, 8f, 58f, WF, 5f).Optional = true;
            AddCell("workshops.craft", W, UhCellKind.Room, UhTheme.Workshop, -6f, 16f, 14f, 30f, WF, 5f);
            UhCell rest = AddCell("workshops.restroom", W, UhCellKind.Room, UhTheme.Workshop, -6f, 0f, 14f, 16f, WF, 4.5f);
            rest.Safe = true;
            AddCell("workshops.vaultgate", W, UhCellKind.Room, UhTheme.Workshop, 24f, 0f, 40f, 16f, WF, 6f);
            AddCell("workshops.vault", W, UhCellKind.Room, UhTheme.Workshop, 16f, -40f, 48f, 0f, WF, 14f);
            AddCell("workshops.coupler", W, UhCellKind.Room, UhTheme.Workshop, 50f, 0f, 64f, 16f, WF, 5f).Optional = true;
            AddCell("workshops.relay", W, UhCellKind.Room, UhTheme.Workshop, 64f, 0f, 78f, 16f, WF, 6f).Optional = true;
            AddCell("workshops.convoyaccess", W, UhCellKind.Corridor, UhTheme.Workshop, -6f, -52f, 4f, 0f, WF, 4f);
            AddCell("workshops.exitpass", W, UhCellKind.Corridor, UhTheme.Workshop, -24f, 2f, -6f, 10f, WF, 4f);
            // The vehicle transit line: from the canals transit tunnel (-26) down at 15 degrees to the convoy corridor (-55).
            UhCell descent = AddSloped("workshops.transitdescent", W, UhCellKind.Ramp, UhTheme.Transit, 76f, -52f, 86f, 56f, false, WF, -26f, 7f);
            descent.Vehicle = true;
            UhCell convoy = AddCell("workshops.convoy", W, UhCellKind.Corridor, UhTheme.Transit, -10f, -62f, 86f, -52f, WF, 7f);
            convoy.Vehicle = true;

            Door("gardens.exitpass", "workshops.stair1", -30f, 62f, 3.6f, 2.8f);
            Door("workshops.stair1", "workshops.landing", -18f, 62f, 3.6f, 2.8f);
            Door("workshops.landing", "workshops.stair2", -10f, 62f, 3.6f, 2.8f);
            Door("workshops.stair2", "workshops.ramp3", 2f, 62f, 3.6f, 2.8f);
            Door("workshops.ramp3", "workshops.entryhall", 14f, 62f, 4f, 3f);
            Door("workshops.entryhall", "workshops.foundry", 30f, 52f, 4f, 4f);
            Door("workshops.entryhall", "workshops.gallery", 54f, 52f, 2.4f, 3f);
            Door("workshops.gallery", "workshops.foundry", 50f, 24f, 2.4f, 3f);
            Door("workshops.foundry", "workshops.store", 14f, 38f, 3f, 3.2f);
            Door("workshops.store", "workshops.records", 2f, 46f, 2f, 2.8f);
            Door("workshops.foundry", "workshops.craft", 14f, 23f, 3f, 3.2f);
            Door("workshops.craft", "workshops.restroom", 4f, 16f, 2.4f, 2.8f);
            Door("workshops.foundry", "workshops.vaultgate", 32f, 16f, 4f, 4f);
            Door("workshops.vaultgate", "workshops.vault", 32f, 0f, 6f, 5f);
            Door("workshops.gallery", "workshops.coupler", 54f, 16f, 3f, 3f);
            Door("workshops.coupler", "workshops.relay", 64f, 8f, 3f, 3f);
            Door("workshops.restroom", "workshops.convoyaccess", -1f, 0f, 2f, 2.8f);
            Door("workshops.convoyaccess", "workshops.convoy", -1f, -52f, 1.6f, 2.6f, UhPortalKind.Walk, true);
            Door("workshops.restroom", "workshops.exitpass", -6f, 6f, 2.4f, 2.8f);
            Door("canals.transit", "workshops.transitdescent", 81f, 56f, 10f, 7f);
            Door("workshops.transitdescent", "workshops.convoy", 81f, -52f, 10f, 7f);

            Place("workshops.entry", "workshops.entryhall", 18f, 62f, 90f);
            Place("workshops.store_room", "workshops.store", 4f, 38f, 270f);
            Place("workshops.hoist", "workshops.entryhall", 55.4f, 68f, 90f);
            Place("workshops.records", "workshops.records", 1f, 52f, 270f);
            Place("workshops.vault_gate", "workshops.vaultgate", 32f, 6f, 180f);
            Place("workshops.vault_arena", "workshops.vault", 32f, -20f, 180f);
            Place("workshops.craft_point", "workshops.craft", 2f, 23f, 270f);
            Place("workshops.coupler_room", "workshops.coupler", 57f, 8f, 180f);
            Place("workshops.convoy_start", "workshops.convoy", 80f, -57f, 270f);
            Place("workshops.convoy_obstacle", "workshops.convoy", 40f, -57f, 270f);
            Place("workshops.convoy_end", "workshops.convoy", 2f, -57f, 270f);
            Place("workshops.storm_relay", "workshops.relay", 71f, 8f, 90f);
            Place("workshops.rest", "workshops.restroom", 4f, 8f, 0f);
            Place("workshops.exit_reliquary", "workshops.exitpass", -20f, 6f, 270f);

            // A slag channel across the foundry floor with two grated crossings; the furnaces glow without hurting.
            Hazard("workshops_slag_w", "workshops.foundry", UhHazardKind.Lava, 18f, 32f, 26f, 35f, 28f, false, true);
            Hazard("workshops_slag_e", "workshops.foundry", UhHazardKind.Lava, 29f, 32f, 46f, 35f, 28f, false, true);
            Hazard("workshops_warm_furnace", "workshops.foundry", UhHazardKind.Warm, 40f, 44f, 48f, 50f, 0f);
            Hazard("workshops_warm_vault", "workshops.vault", UhHazardKind.Warm, 28f, -38f, 36f, -34f, 0f);
            Landmark("lm_great_crucible", "workshops.foundry", "The Great Crucible", 32f, -48f, 42f);
        }

        private void BuildReliquary()
        {
            const float QF = -72f;
            AddSloped("reliquary.ramp1", Q, UhCellKind.Ramp, UhTheme.Archive, -60f, 2f, -24f, 10f, true, -64f, -55f, 4f);
            AddSloped("reliquary.ramp2", Q, UhCellKind.Ramp, UhTheme.Archive, -96f, 2f, -60f, 10f, true, QF, -64f, 4f);
            AddCell("reliquary.entryroom", Q, UhCellKind.Room, UhTheme.Archive, -112f, -2f, -96f, 14f, QF, 6f);
            AddCell("reliquary.archive", Q, UhCellKind.Room, UhTheme.Archive, -140f, -20f, -112f, 40f, QF, 14f);
            AddCell("reliquary.records", Q, UhCellKind.Room, UhTheme.Archive, -168f, 10f, -140f, 40f, QF, 10f);
            AddCell("reliquary.platforms", Q, UhCellKind.Room, UhTheme.Archive, -168f, -30f, -140f, 10f, QF, 14f).Optional = true;
            AddCell("reliquary.elite", Q, UhCellKind.Room, UhTheme.Archive, -196f, -30f, -168f, -6f, QF, 10f).Optional = true;
            AddCell("reliquary.gallery", Q, UhCellKind.Room, UhTheme.Archive, -112f, 24f, -96f, 40f, QF, 6f).Optional = true;
            UhCell rest = AddCell("reliquary.restroom", Q, UhCellKind.Room, UhTheme.Archive, -112f, -20f, -96f, -4f, QF, 4.5f);
            rest.Safe = true;
            AddCell("reliquary.south", Q, UhCellKind.Room, UhTheme.Archive, -140f, -44f, -112f, -20f, QF, 8f);
            AddSloped("reliquary.exitgallery", Q, UhCellKind.Ramp, UhTheme.Archive, -112f, -44f, -60f, -36f, true, QF, -82f, 4f);

            Door("workshops.exitpass", "reliquary.ramp1", -24f, 6f, 3.6f, 3f);
            Door("reliquary.ramp1", "reliquary.ramp2", -60f, 6f, 3.6f, 3f);
            Door("reliquary.ramp2", "reliquary.entryroom", -96f, 6f, 4f, 3.2f);
            Door("reliquary.entryroom", "reliquary.archive", -112f, 6f, 4f, 4f);
            Door("reliquary.archive", "reliquary.records", -140f, 25f, 4f, 4f);
            Door("reliquary.archive", "reliquary.platforms", -140f, -6f, 4f, 4f);
            Door("reliquary.platforms", "reliquary.elite", -168f, -18f, 3f, 3.4f);
            Door("reliquary.archive", "reliquary.gallery", -112f, 32f, 3f, 3.4f);
            Door("reliquary.archive", "reliquary.restroom", -112f, -12f, 2.4f, 3f);
            Door("reliquary.archive", "reliquary.south", -126f, -20f, 6f, 5f);
            Door("reliquary.south", "reliquary.exitgallery", -112f, -40f, 4f, 3.2f);

            Place("reliquary.entry", "reliquary.entryroom", -100f, 6f, 270f);
            Place("reliquary.archive_hall", "reliquary.archive", -126f, 8f, 0f);
            Place("reliquary.columns", "reliquary.archive", -126f, 26f, 0f);
            Place("reliquary.elite_detour", "reliquary.elite", -182f, -18f, 270f);
            Place("reliquary.revelation", "reliquary.records", -154f, 25f, 270f);
            Place("reliquary.platform_route", "reliquary.platforms", -146f, -2f, 270f);
            Place("reliquary.safe_route", "reliquary.platforms", -146f, -26f, 270f);
            Place("reliquary.window", "reliquary.gallery", -100f, 37f, 0f);
            Place("reliquary.surveyor", "reliquary.archive", -122f, 36f, 0f);
            Place("reliquary.rest", "reliquary.restroom", -104f, -12f, 90f);
            Place("reliquary.exit_crown", "reliquary.south", -116f, -40f, 90f);
            Hazard("reliquary_warm_ash", "reliquary.platforms", UhHazardKind.Warm, -162f, -20f, -150f, 4f, 0f);
            // The heat columns: the Crown's breath rises through three grates across the colonnade aisle, breathing in a
            // staggered ripple with walkable gaps between them and cool lanes behind the column rows. A descent window
            // (UnderheartWindowRules) runs them cooler or hotter; the blue vent before the line cools the body first.
            const float HC = UnderheartWindowRules.ColumnHeat, CY = UnderheartWindowRules.ColumnCycle;
            Vent("reliquary_heat_w", "reliquary.archive", UhHazardKind.HotVent, -131f, 16f, 1.7f, HC, CY, 0f);
            Vent("reliquary_heat_c", "reliquary.archive", UhHazardKind.HotVent, -126f, 18.5f, 1.7f, HC, CY, CY / 3f);
            Vent("reliquary_heat_e", "reliquary.archive", UhHazardKind.HotVent, -121f, 16f, 1.7f, HC, CY, CY * 2f / 3f);
            Vent("reliquary_cool_0", "reliquary.archive", UhHazardKind.Cooling, -121f, 11f, 1.1f, 0f, 0f, 0f);
            Landmark("lm_founder", "reliquary.archive", "The Founder's Colonnade", -126f, QF, 32f);
        }

        private void BuildCrown()
        {
            const float KF = -95f;
            AddCell("crown.upperpass", K, UhCellKind.Corridor, UhTheme.Foundry, -60f, -44f, -20f, -36f, -82f, 4f);
            UhCell balcony = AddCell("crown.balcony", K, UhCellKind.Ledge, UhTheme.Foundry, -20f, -50f, 0f, -30f, -82f, 5f);
            balcony.Safe = true;
            UhCell staging = AddCell("crown.staging", K, UhCellKind.Room, UhTheme.Foundry, -20f, -50f, 0f, -30f, KF, 5f);
            staging.Safe = true;
            AddCell("crown.toplanding", K, UhCellKind.Corridor, UhTheme.Foundry, -4f, -30f, 0f, -25f, -82f, 4f);
            AddSloped("crown.rampA", K, UhCellKind.Ramp, UhTheme.Foundry, -26f, -30f, -4f, -25f, true, -88.5f, -82f, 4f);
            AddCell("crown.midlanding", K, UhCellKind.Corridor, UhTheme.Foundry, -30f, -30f, -26f, -20f, -88.5f, 4f);
            AddSloped("crown.rampB", K, UhCellKind.Ramp, UhTheme.Foundry, -26f, -25f, 0f, -20f, true, -88.5f, KF, 4f);
            UhCell chamber = AddCell("crown.chamber", K, UhCellKind.Cavern, UhTheme.Foundry, 0f, -80f, 80f, 0f, KF, 35f);
            chamber.Rock = true;
            AddCell("crown.regentarena", K, UhCellKind.Cavern, UhTheme.Foundry, 20f, 0f, 60f, 30f, KF, 18f).Rock = true;
            AddCell("crown.coolingduct", K, UhCellKind.Corridor, UhTheme.Foundry, 80f, -60f, 92f, -20f, KF, 4f).Optional = true;

            Door("reliquary.exitgallery", "crown.upperpass", -60f, -40f, 4f, 3.2f);
            Door("crown.upperpass", "crown.balcony", -20f, -40f, 4f, 3.2f);
            Door("crown.balcony", "crown.toplanding", -2f, -30f, 3f, 3f);
            Door("crown.toplanding", "crown.rampA", -4f, -27.5f, 4f, 3f);
            Door("crown.rampA", "crown.midlanding", -26f, -27.5f, 4f, 3f);
            Door("crown.midlanding", "crown.rampB", -26f, -22.5f, 4f, 3f);
            Door("crown.rampB", "crown.chamber", 0f, -22.5f, 4f, 3f);
            Door("crown.balcony", "crown.chamber", 0f, -40f, 18f, 4f, UhPortalKind.Window);
            Door("crown.chamber", "crown.staging", 0f, -40f, 4f, 3.2f);
            Door("crown.chamber", "crown.regentarena", 40f, 0f, 8f, 7f);
            Door("crown.chamber", "crown.coolingduct", 80f, -56f, 3f, 3f);
            Door("crown.chamber", "crown.coolingduct", 80f, -24f, 3f, 3f);

            Place("crown.staging", "crown.staging", -10f, -42f, 90f);
            Place("crown.valve_mech", "crown.chamber", 12f, -68f, 0f);
            Place("crown.reflector_mech", "crown.chamber", 68f, -68f, 0f);
            Place("crown.escort_start", "crown.chamber", 12f, -12f, 90f);
            Place("crown.escort_end", "crown.chamber", 68f, -12f, 90f);
            Place("crown.regent_gate", "crown.chamber", 40f, -4f, 0f);
            Place("crown.regent_arena", "crown.regentarena", 40f, 16f, 0f);
            Place("crown.lift", "crown.staging", -17.4f, -46f, 270f);
            Place("crown.vista", "crown.balcony", -3f, -40f, 90f);

            // The heat source: a molten ring around the core's dais (warm beyond it), cooling vents in the duct loop.
            Hazard("crown_ring_w", "crown.chamber", UhHazardKind.Lava, 28f, -52f, 31f, -28f, 32f, false, true);
            Hazard("crown_ring_e", "crown.chamber", UhHazardKind.Lava, 49f, -52f, 52f, -28f, 32f, false, true);
            Hazard("crown_ring_s", "crown.chamber", UhHazardKind.Lava, 31f, -52f, 49f, -49f, 32f, false, true);
            Hazard("crown_warm_core", "crown.chamber", UhHazardKind.Warm, 32f, -48f, 48f, -32f, 0f);
            Vent("crown_vent_0", "crown.chamber", UhHazardKind.HotVent, 24f, -60f, 1.6f, 16f, 5f, 0f);
            Vent("crown_vent_1", "crown.chamber", UhHazardKind.HotVent, 56f, -60f, 1.6f, 16f, 5f, 2.5f);
            Vent("crown_cool_0", "crown.coolingduct", UhHazardKind.Cooling, 86f, -48f, 1.2f, 0f, 0f, 0f);
            Vent("crown_cool_1", "crown.coolingduct", UhHazardKind.Cooling, 86f, -32f, 1.2f, 0f, 0f, 0f);
            Landmark("lm_heart_furnace", "crown.chamber", "The Heart Furnace", 40f, -76f, -40f);
        }

        /// <summary>Per region: entry and exit cells (the critical path), rest anchor, landmark, next region, depth and tint.</summary>
        private void BuildRegions()
        {
            AddRegion(UnderheartIds.ServiceLevel, "service.stair", "service.tunnel3", "", "", UnderheartIds.LanternRefuge, 6f, new Color(0.75f, 0.78f, 0.82f));
            AddRegion(UnderheartIds.LanternRefuge, "refuge.vestibule", "refuge.exit", "refuge.rest", "lm_great_lantern", UnderheartIds.EmberCanals, 18f, new Color(0.45f, 0.95f, 0.85f));
            AddRegion(UnderheartIds.EmberCanals, "canals.stairA", "canals.gardenspass", "canals.rest", "lm_pressure_valve", UnderheartIds.PyreGardens, 26f, new Color(1f, 0.55f, 0.22f));
            AddRegion(UnderheartIds.PyreGardens, "gardens.entry", "gardens.exitpass", "gardens.rest", "lm_pyre_tree", UnderheartIds.CrucibleWorkshops, 40f, new Color(1f, 0.72f, 0.3f));
            AddRegion(UnderheartIds.CrucibleWorkshops, "workshops.stair1", "workshops.exitpass", "workshops.rest", "lm_great_crucible", UnderheartIds.AshReliquary, 55f, new Color(0.85f, 0.62f, 0.42f));
            AddRegion(UnderheartIds.AshReliquary, "reliquary.ramp1", "reliquary.exitgallery", "reliquary.rest", "lm_founder", UnderheartIds.FoundryCrown, 72f, new Color(0.82f, 0.84f, 0.9f));
            AddRegion(UnderheartIds.FoundryCrown, "crown.upperpass", "crown.regentarena", "crown.staging", "lm_heart_furnace", "", 95f, new Color(1f, 0.38f, 0.16f));
        }

        private void AddRegion(string id, string entry, string exit, string rest, string landmark, string next, float depth, Color tint)
        {
            Regions.Add(new UhRegionInfo { Id = id, EntryCell = CellIndex(entry), ExitCell = CellIndex(exit), Rest = rest, Landmark = landmark, Next = next, Depth = depth, Tint = tint });
            if (CellIndex(entry) < 0 || CellIndex(exit) < 0) _problems.Add("region " + id + " entry or exit cell missing");
        }
    }
}
