using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The upper Underheart: the Pressure House stair and service level (fixed in the venue frame), the service tunnel,
    /// the refuge vestibule, then the Lantern Refuge and the Ember Canals in the deep frame. Floors: venue floor 0.15,
    /// service level -6.0 (6.15 m below the venue floor, ceilings under -2.2 so the street slab is never touched),
    /// refuge -18, canals -26.
    /// </summary>
    public sealed partial class UnderheartLayout
    {
        private const string S = UnderheartIds.ServiceLevel;
        private const string R = UnderheartIds.LanternRefuge;
        private const string C = UnderheartIds.EmberCanals;

        /// <summary>Venue frame: the stair down from the Pressure House floor, the service level and the service tunnel.</summary>
        private void BuildFixed()
        {
            const float SF = -6.0f;   // service floor
            const float SH = 3.4f;    // service headroom (ceiling -2.6, slab top -2.2)
            float top = CityLayout.CurbHeight;
            // The stair: 31 risers of 0.198 m over 11 m (0.355 m treads) inside the venue's floor opening.
            AddSloped("service.stair", S, UhCellKind.Stair, UhTheme.Service, VenueIds04E.StairX0, VenueIds04E.StairTopZ, VenueIds04E.StairX1, VenueIds04E.StairBottomZ,
                false, top, top - VenueIds04E.StairDrop, 3.2f);
            UhCell hall = AddCell("service.hall", S, UhCellKind.Corridor, UhTheme.Service, 1f, VenueIds04E.StairBottomZ, 11f, 40f, SF, SH);
            AddCell("service.construct", S, UhCellKind.Room, UhTheme.Service, -15f, 18f, 1f, 34f, SF, SH);
            AddCell("service.panel", S, UhCellKind.Room, UhTheme.Service, 11f, 18f, 16f, 24f, SF, 3.0f).Optional = true;
            AddCell("service.ventroom", S, UhCellKind.Room, UhTheme.Service, 11f, 30f, 17f, 38f, SF, 3.0f).Optional = true;
            AddCell("service.nook", S, UhCellKind.Room, UhTheme.Service, -6f, 34f, 1f, 40f, SF, 3.0f).Optional = true;
            AddCell("service.tunneltop", S, UhCellKind.Room, UhTheme.Service, 1f, 40f, 11f, 48f, SF, SH);
            UhCell hollow = AddCell("service.hollow", S, UhCellKind.Cavern, UhTheme.Service, 11f, 40f, 25f, 54f, SF, SH);
            hollow.Rock = true;
            hollow.Optional = true;
            // The service tunnel down to the refuge: stair, landing, ramp, landing, stair (12 m in 36 m).
            AddSloped("service.tunnel1", S, UhCellKind.Stair, UhTheme.Service, 4f, 48f, 8f, 55.5f, false, SF, SF - 4f, SH);
            AddCell("service.landing1", S, UhCellKind.Corridor, UhTheme.Service, 4f, 55.5f, 8f, 59.5f, SF - 4f, SH);
            AddSloped("service.tunnel2", S, UhCellKind.Ramp, UhTheme.Service, 4f, 59.5f, 8f, 72.5f, false, SF - 4f, SF - 8f, SH).Rock = true;
            AddCell("service.landing2", S, UhCellKind.Corridor, UhTheme.Service, 4f, 72.5f, 8f, 76.5f, SF - 8f, SH).Rock = true;
            AddSloped("service.tunnel3", S, UhCellKind.Stair, UhTheme.Service, 4f, 76.5f, 8f, 84f, false, SF - 8f, SF - 12f, SH).Rock = true;
            // The vestibule belongs to the refuge but is square and fixed, so every deep heading attaches to one of its faces.
            UhCell vest = AddCell("refuge.vestibule", R, UhCellKind.Room, UhTheme.Transit, PivotX - VestibuleHalf, PivotZ - VestibuleHalf, PivotX + VestibuleHalf, PivotZ + VestibuleHalf, -18f, 5f);
            vest.Safe = true;

            Door("service.stair", "service.hall", 5.7f, VenueIds04E.StairBottomZ, 2.8f, 3.0f);
            Door("service.hall", "service.construct", 1f, 26f, 3f, 2.8f);
            Door("service.hall", "service.panel", 11f, 21f, 1.6f, 2.4f);
            Door("service.hall", "service.ventroom", 11f, 34f, 1.6f, 2.4f);
            Door("service.hall", "service.nook", 1f, 37f, 2f, 2.4f);
            Door("service.hall", "service.tunneltop", 6f, 40f, 6f, 3.0f);
            Door("service.tunneltop", "service.hollow", 11f, 44f, 2.4f, 2.6f);
            Door("service.tunneltop", "service.tunnel1", 6f, 48f, 4f, 3.0f);
            Door("service.tunnel1", "service.landing1", 6f, 55.5f, 4f, 3.0f);
            Door("service.landing1", "service.tunnel2", 6f, 59.5f, 4f, 3.0f);
            Door("service.tunnel2", "service.landing2", 6f, 72.5f, 4f, 3.0f);
            Door("service.landing2", "service.tunnel3", 6f, 76.5f, 4f, 3.0f);
            Door("service.tunnel3", "refuge.vestibule", 6f, 84f, 4f, 3.0f);

            Place("service.stair_bottom", "service.hall", 5.7f, 15.4f, 0f);
            Place("service.construct_room", "service.construct", -7f, 26f, 90f);
            Place("service.service_panel", "service.panel", 14.6f, 21f, 90f);
            Place("service.vent", "service.ventroom", 14.5f, 34f, 90f);
            Place("service.juveniles", "service.nook", -3f, 37f, 270f);
            Place("service.gate", "service.hall", 6f, 38.6f, 0f);
            Place("service.clamp_hollow", "service.hollow", 18f, 47f, 90f);
            Place("service.clamp_a", "service.hollow", 15f, 50.5f, 135f);
            Place("service.clamp_b", "service.hollow", 21f, 50.5f, 225f);
            Place("service.clamp_c", "service.hollow", 18f, 43f, 0f);
            Place("service.tunnel_top", "service.tunneltop", 6f, 45.5f, 0f);
            // The refuge lift's top door on the service hall's east wall, beside the stair.
            LiftShortcut("sc_refuge_lift", "Refuge lift", "refuge.lift", "service.hall", 8.4f, 17.5f, 90f);
            if (hall == null) _problems.Add("service hall missing");
        }

        /// <summary>Lantern Refuge (floor -18): a reclaimed transit hall with alcoves for residents, fitting and rest.</summary>
        private void BuildRefuge()
        {
            const float RF = -18f;
            UhCell corr = AddCell("refuge.corridor", R, UhCellKind.Corridor, UhTheme.Transit, -2f, 6f, 2f, 14f, RF, 4f);
            UhCell hall = AddCell("refuge.hall", R, UhCellKind.Room, UhTheme.Transit, -9f, 14f, 9f, 62f, RF, 9f);
            UhCell camp = AddCell("refuge.camp", R, UhCellKind.Room, UhTheme.Transit, -19f, 22f, -9f, 42f, RF, 5f);
            UhCell works = AddCell("refuge.works", R, UhCellKind.Room, UhTheme.Transit, 9f, 18f, 19f, 34f, RF, 5f);
            UhCell lifts = AddCell("refuge.liftbay", R, UhCellKind.Room, UhTheme.Transit, 9f, 40f, 17f, 50f, RF, 5f);
            UhCell rest = AddCell("refuge.restroom", R, UhCellKind.Room, UhTheme.Transit, -17f, 46f, -9f, 56f, RF, 4f);
            UhCell exit = AddCell("refuge.exit", R, UhCellKind.Corridor, UhTheme.Transit, -2f, 62f, 2f, 68f, RF, 4f);
            corr.Safe = hall.Safe = camp.Safe = works.Safe = lifts.Safe = rest.Safe = exit.Safe = true;
            camp.Optional = true;
            rest.Optional = true;

            Door("refuge.vestibule", "refuge.corridor", 0f, 6f, 3f, 3.2f);
            Door("refuge.corridor", "refuge.hall", 0f, 14f, 4f, 3.6f);
            Door("refuge.hall", "refuge.camp", -9f, 32f, 4f, 3.4f);
            Door("refuge.hall", "refuge.works", 9f, 26f, 4f, 3.4f);
            Door("refuge.hall", "refuge.liftbay", 9f, 45f, 3f, 3.2f);
            Door("refuge.hall", "refuge.restroom", -9f, 51f, 3f, 3.0f);
            Door("refuge.hall", "refuge.exit", 0f, 62f, 3f, 3.2f);

            Place("refuge.entry", "refuge.vestibule", 0f, 2f, 0f);
            Place("refuge.caretaker", "refuge.works", 14f, 30f, 270f);
            Place("refuge.fitting", "refuge.works", 16f, 21.5f, 90f);
            Place("refuge.ledger", "refuge.works", 11.5f, 20f, 180f);
            Place("refuge.rest", "refuge.restroom", -13f, 52f, 90f);
            Place("refuge.map_board", "refuge.hall", 6.5f, 57f, 90f);
            Place("refuge.lift", "refuge.liftbay", 14.4f, 42f, 90f);
            Place("refuge.sample_drop", "refuge.hall", -6f, 20f, 270f);
            Place("refuge.exit_canals", "refuge.exit", 0f, 65f, 0f);
            Place("refuge.residents", "refuge.camp", -14f, 32f, 90f);
            Landmark("lm_great_lantern", "refuge.hall", "The Great Lantern", 0f, -12f, 38f);
            // The crown lift's top door shares the lift bay (the last return shortcut lands at the refuge).
            LiftShortcut("sc_crown_lift", "Crown lift", "crown.lift", "refuge.liftbay", 14.4f, 48f, 90f);
        }

        /// <summary>Ember Canals (floor -26): basalt channel hall with a lava trench and catwalks, the hot assist route, the
        /// valve room, the gated stair back to the refuge, the maintenance hut and the broad transit tunnel.</summary>
        private void BuildCanals()
        {
            const float CF = -26f;
            AddSloped("canals.stairA", C, UhCellKind.Stair, UhTheme.Basalt, -2.5f, 68f, 2.5f, 75.5f, false, -18f, -22f, 3.4f);
            AddCell("canals.landing", C, UhCellKind.Corridor, UhTheme.Basalt, -2.5f, 75.5f, 2.5f, 77.5f, -22f, 3.4f);
            AddSloped("canals.stairB", C, UhCellKind.Stair, UhTheme.Basalt, -2.5f, 77.5f, 2.5f, 85f, false, -22f, CF, 3.4f);
            UhCell entry = AddCell("canals.entry", C, UhCellKind.Room, UhTheme.Basalt, -8f, 85f, 8f, 99f, CF, 8f);
            entry.Rock = true;
            UhCell channel = AddCell("canals.channel", C, UhCellKind.Cavern, UhTheme.Basalt, 8f, 84f, 64f, 104f, CF, 11f);
            channel.Rock = true;
            UhCell cache = AddCell("canals.cache", C, UhCellKind.Cavern, UhTheme.Basalt, 14f, 104f, 26f, 114f, CF, 5f);
            cache.Rock = true;
            cache.Optional = true;
            AddCell("canals.salvage", C, UhCellKind.Room, UhTheme.Basalt, 34f, 104f, 48f, 116f, CF, 6f).Optional = true;
            UhCell hut = AddCell("canals.hut", C, UhCellKind.Room, UhTheme.Basalt, 64f, 84f, 76f, 96f, CF, 4.5f);
            hut.Safe = true;
            UhCell hot = AddCell("canals.hotway", C, UhCellKind.Corridor, UhTheme.Basalt, 54f, 48f, 62f, 84f, CF, 6f);
            hot.Rock = true;
            UhCell pocket = AddCell("canals.coolpocket", C, UhCellKind.Room, UhTheme.Basalt, 62f, 62f, 68f, 70f, CF, 3.5f);
            pocket.Rock = true;
            pocket.Optional = true;
            AddCell("canals.ante", C, UhCellKind.Room, UhTheme.Basalt, 46f, 40f, 54f, 52f, CF, 5f);
            UhCell valve = AddCell("canals.valveroom", C, UhCellKind.Room, UhTheme.Basalt, 23f, 22f, 46f, 48f, CF, 13f);
            valve.Rock = true;
            AddCell("canals.sctop", C, UhCellKind.Corridor, UhTheme.Basalt, 19f, 25f, 23f, 27f, -18f, 3.4f);
            AddSloped("canals.scstair", C, UhCellKind.Stair, UhTheme.Basalt, 19f, 27f, 23f, 39f, false, -18f, CF, 3.4f);
            AddCell("canals.scbottom", C, UhCellKind.Corridor, UhTheme.Basalt, 19f, 39f, 23f, 41f, CF, 3.4f);
            UhCell transit = AddCell("canals.transit", C, UhCellKind.Corridor, UhTheme.Transit, 76f, 56f, 86f, 136f, CF, 7f);
            transit.Vehicle = true;
            UhCell pass = AddSloped("canals.gardenspass", C, UhCellKind.Ramp, UhTheme.Basalt, -24f, 88f, -8f, 96f, true, -31f, CF, 4f);
            pass.Rock = true;

            Door("refuge.exit", "canals.stairA", 0f, 68f, 3f, 3.0f);
            Door("canals.stairA", "canals.landing", 0f, 75.5f, 4f, 3.0f);
            Door("canals.landing", "canals.stairB", 0f, 77.5f, 4f, 3.0f);
            Door("canals.stairB", "canals.entry", 0f, 85f, 4f, 3.0f);
            Door("canals.entry", "canals.channel", 8f, 92f, 10f, 6f);
            Door("canals.channel", "canals.cache", 20f, 104f, 1.8f, 2.6f);
            Door("canals.channel", "canals.salvage", 41f, 104f, 3f, 3.2f);
            Door("canals.channel", "canals.hut", 64f, 90f, 2.4f, 2.8f);
            Door("canals.channel", "canals.hotway", 58f, 84f, 4f, 4f);
            Door("canals.hotway", "canals.coolpocket", 62f, 66f, 2f, 2.6f);
            Door("canals.hotway", "canals.ante", 54f, 50f, 3f, 3.2f);
            Door("canals.ante", "canals.valveroom", 46f, 44f, 4f, 4f);
            Door("refuge.works", "canals.sctop", 19f, 26f, 2f, 2.8f);
            Door("canals.sctop", "canals.scstair", 21f, 27f, 3.6f, 2.8f);
            Door("canals.scstair", "canals.scbottom", 21f, 39f, 3.6f, 2.8f);
            UhPortal gate = Door("canals.scbottom", "canals.valveroom", 23f, 40f, 2f, 2.8f);
            Door("canals.hut", "canals.transit", 76f, 92f, 1.6f, 2.6f, UhPortalKind.Walk, true);
            Door("canals.entry", "canals.gardenspass", -8f, 92f, 3.5f, 3.2f);

            Place("canals.entry", "canals.entry", 0f, 88f, 0f);
            Place("canals.crossing", "canals.channel", 22f, 94f, 0f);
            Place("canals.assist_route", "canals.hotway", 58f, 76f, 180f);
            Place("canals.valve_room", "canals.valveroom", 34.5f, 36f, 180f);
            Place("canals.pressure_valve", "canals.valveroom", 34.5f, 26f, 180f);
            Place("canals.gate", "canals.valveroom", 25.5f, 40f, 270f);
            Place("canals.sample_vent", "canals.valveroom", 43f, 25f, 135f);
            Place("canals.actuator", "canals.channel", 44f, 101f, 0f);
            Place("canals.transit", "canals.transit", 81f, 100f, 0f);
            Place("canals.portal_bottom", "canals.transit", 81f, 132f, 0f);
            Place("canals.salvage_crew", "canals.salvage", 41f, 110f, 0f);
            Place("canals.branch_cache", "canals.cache", 20f, 110f, 0f);
            Place("canals.rest", "canals.hut", 68f, 88f, 90f);
            Place("canals.cool_pocket", "canals.coolpocket", 65f, 66f, 90f);
            Place("canals.exit_gardens", "canals.entry", -6f, 92f, 270f);

            // The lava trench down the channel, split by two catwalks (the coolant crossing and the east catwalk).
            Hazard("canals_lava_w", "canals.channel", UhHazardKind.Lava, 14f, 92f, 21f, 96f, 34f, false, true);
            Hazard("canals_lava_m", "canals.channel", UhHazardKind.Lava, 23f, 92f, 49f, 96f, 34f, false, true);
            Hazard("canals_lava_e", "canals.channel", UhHazardKind.Lava, 51f, 92f, 60f, 96f, 34f, false, true);
            // The hot assist route: vents breathing out of step, blue cooling vents between them marking the safe line.
            float[,] vents = { { 56.0f, 80f }, { 60.0f, 74f }, { 56.0f, 68f }, { 60.0f, 62f }, { 56.0f, 56f } };
            for (int i = 0; i < vents.GetLength(0); i++)
                Vent("canals_vent_" + i, "canals.hotway", UhHazardKind.HotVent, vents[i, 0], vents[i, 1], 1.7f, 16f, 4.2f, i * 0.9f);
            float[,] cool = { { 60.0f, 78f }, { 56.5f, 71.5f }, { 60.0f, 65f }, { 56.5f, 59f } };
            for (int i = 0; i < cool.GetLength(0); i++)
                Vent("canals_cool_" + i, "canals.hotway", UhHazardKind.Cooling, cool[i, 0], cool[i, 1], 1.0f, 0f, 0f, 0f);
            Hazard("canals_warm_valve", "canals.valveroom", UhHazardKind.Warm, 30f, 23f, 39f, 27f, 0f);
            Hazard("canals_warm_cache", "canals.cache", UhHazardKind.Warm, 22f, 110f, 25f, 113f, 0f);
            Vent("canals_cool_pocket", "canals.coolpocket", UhHazardKind.Cooling, 66f, 66f, 1.0f, 0f, 0f, 0f);
            Landmark("lm_pressure_valve", "canals.valveroom", "The Pressure Valve", 34.5f, -20f, 23.5f);

            GateShortcut("sc_canals_gate", "Canal return gate", "canals.gate", gate);
            LiftShortcut("sc_workshops_hoist", "Workshop hoist", "workshops.hoist", "canals.hut", 71f, 93.4f, 0f);
        }
    }
}
