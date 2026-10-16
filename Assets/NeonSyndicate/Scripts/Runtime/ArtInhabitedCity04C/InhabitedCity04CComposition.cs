using System;
using System.Collections.Generic;
using System.IO;

namespace NeonSyndicate
{
    [Serializable] public sealed class VenueLayout04C
    {
        public string version, id, title, notes;
        public int style;
        public bool twoStoreys;
        public VenueNode04C[] nodes;
        public VenueAnchor04C[] anchors;
        public VenueKeepout04C[] keepouts;
        public VenueLight04C[] lights;
        public VenueLabel04C[] labels;
    }
    [Serializable] public sealed class VenueNode04C
    {
        public string id, kind, source, model, role;
        public float[] position, scale, euler;
        public bool solid, far = true, interior = true;
        public int progress;
    }
    [Serializable] public sealed class VenueAnchor04C { public string id; public float[] position; public float yaw; }
    [Serializable] public sealed class VenueKeepout04C { public string id; public float[] min, max; }
    [Serializable] public sealed class VenueLight04C { public string id; public float[] position, color; public float range, intensity; }
    [Serializable] public sealed class VenueLabel04C { public string id, text; public float[] position; public float yaw, characterSize; }
    [Serializable] public sealed class VenueExpanded04C
    {
        public string version = "04C", id;
        public int style;
        public float width, depth, originalHeight, roomWidth = 16f, roomDepth = 18f, floorHeight = 4.2f, occupiedHeight;
        public bool twoStoreys;
        public VenueNode04C[] nodes;
        public VenueAnchor04C[] anchors;
        public VenueKeepout04C[] keepouts;
        public VenueLight04C[] lights;
        public VenueLabel04C[] labels;
    }

    /// <summary>Pure, deterministic shell expansion shared by runtime and source geometry review.</summary>
    public static class InhabitedCity04CComposition
    {
        public static VenueExpanded04C Expand(VenueLayout04C layout, float width, float depth, float originalHeight)
        {
            if (layout == null || layout.version != "04C" || layout.nodes == null || layout.anchors == null ||
                width < 17f || depth < 19f || width > 150f || depth > 150f || !Finite(width) || !Finite(depth) || !Finite(originalHeight))
                throw new InvalidDataException("04C venue needs a valid source and at least 17m by 19m of original parcel.");
            float top = layout.twoStoreys ? 8.6f : 5f;
            if (originalHeight < top + .4f) throw new InvalidDataException("04C occupied floors do not fit the original building height.");
            List<VenueNode04C> nodes = new List<VenueNode04C>();
            // Original lot dressing is CurbHeight + .002. A 12mm finish avoids a hidden or flickering floor.
            Box(nodes, "interior_floor", layout.id == "lofts" || layout.id == "inn" ? "timber" : "paving", 0f, -.094f, 9f, 16f, .212f, 18f);
            Box(nodes, "interior_ceiling", "stone", 0f, top - .1f, 9f, 16f, .2f, 18f);
            // Retain the original footprint around the bounded room, including its solid service wings.
            float wing = (width - 16f) * .5f;
            Box(nodes, "west_original_mass", "stone", -8f - wing * .5f, top * .5f, depth * .5f, wing, top, depth, false);
            Box(nodes, "east_original_mass", "stone", 8f + wing * .5f, top * .5f, depth * .5f, wing, top, depth, false);
            Box(nodes, "rear_original_mass", "stone", 0f, top * .5f, 18f + (depth - 18f) * .5f, 16f, top, depth - 18f, false);
            Box(nodes, "west_room_wall", "stone", -7.85f, top * .5f, 9f, .3f, top, 18f);
            Box(nodes, "east_room_wall", "stone", 7.85f, top * .5f, 9f, .3f, top, 18f);
            Box(nodes, "rear_room_wall", "stone", 0f, top * .5f, 17.85f, 16f, top, .3f);
            Front(nodes, layout.id == "garage" ? 3.2f : 1.4f, 0f, layout.twoStoreys ? 4.2f : top, true);
            if (layout.twoStoreys)
            {
                Front(nodes, 1.4f, 4.2f, top - 4.2f, false);
                Stairs(nodes);
            }
            // Upper structure retains the old solid volume and the exact original walkable roof height.
            // Its actual 04A tier/crown geometry is appended by the runtime from this same descriptor.
            nodes.Add(new VenueNode04C { id = "preserved_upper_architecture", kind = "upper04A", role = "stone", source = "04A",
                position = new[] { 0f, top, 0f }, scale = new[] { width, originalHeight - top, depth }, euler = new float[3],
                solid = true, far = true, interior = false });
            // Supported human-scale facade details remain inside the existing building setback.
            Box(nodes, "door_canopy", "roof", 0f, 3.65f, -.42f, layout.id == "garage" ? 7f : 4.6f, .2f, 1.45f, false);
            for (int side = -1; side <= 1; side += 2)
            {
                Box(nodes, "canopy_bracket_" + side, "metal", side * (layout.id == "garage" ? 3.25f : 2f), 3.22f, -.1f, .12f, .7f, .45f, false);
                if (layout.id != "garage")
                    Box(nodes, "window_sill_" + side, "trim", side * 4.2f, .95f, -.12f, 3.9f, .18f, .58f, false);
                Box(nodes, "front_pier_" + side, "trim", side * 7.7f, 2.1f, -.06f, .32f, 4.2f, .32f, false);
            }
            StreetFrontage(layout, nodes, width);
            nodes.AddRange(layout.nodes);
            if (layout.id == "clinic")
                foreach (VenueNode04C node in nodes)
                    if (node.kind != "upper04A" && node.role == "stone") node.role = "plaster";
            if (nodes.Count > 420) throw new InvalidDataException("04C venue exceeds the authored part budget.");
            HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (VenueNode04C node in nodes)
            {
                if (node == null || string.IsNullOrEmpty(node.id) || !ids.Add(node.id) || !Vector(node.position) || !Vector(node.scale) || !Vector(node.euler) ||
                    node.scale[0] <= 0f || node.scale[1] <= 0f || node.scale[2] <= 0f)
                    throw new InvalidDataException("04C contains an invalid or duplicate geometry node.");
            }
            HashSet<string> anchors = new HashSet<string>(StringComparer.Ordinal);
            foreach (VenueAnchor04C anchor in layout.anchors)
                if (anchor == null || !anchors.Add(anchor.id) || !Vector(anchor.position) || !Finite(anchor.yaw))
                    throw new InvalidDataException("04C contains an invalid or duplicate anchor.");
            string[] required = { "service", "activity", "staff", "visitor1", "visitor2", "exit" };
            foreach (string id in required) if (!anchors.Contains(id)) throw new InvalidDataException("04C required anchor missing: " + id);
            string[] activities = layout.id == "archive" ? new[] { "record1", "record2", "record3" } :
                layout.id == "observatory" ? new[] { "instrument1", "instrument2", "instrument3" } :
                layout.id == "depot" ? new[] { "record1", "defense1", "defense2", "retrieve" } :
                layout.id == "clinic" ? new[] { "record1" } : layout.id == "lofts" ? new[] { "bed", "desk" } : new string[0];
            foreach (string id in activities) if (!anchors.Contains(id)) throw new InvalidDataException("04C activity anchor missing: " + id);
            foreach (VenueAnchor04C anchor in layout.anchors)
            {
                if (anchor.id == "exit") continue;
                float[] p = anchor.position;
                if (Math.Abs(p[0]) > 7.05f || p[2] < .8f || p[2] > 17.05f || p[1] < -.01f || p[1] + 2.1f > top - .2f)
                    throw new InvalidDataException("04C anchor cannot fit the intended room capsule: " + anchor.id);
            }
            if (layout.labels == null || layout.labels.Length == 0 || layout.labels.Length > 8)
                throw new InvalidDataException("04C requires a bounded physical entrance name.");
            foreach (VenueLabel04C label in layout.labels)
                if (label == null || string.IsNullOrEmpty(label.text) || label.text.Length > 90 || !Vector(label.position) || !Finite(label.yaw) || label.characterSize < .02f || label.characterSize > .2f)
                    throw new InvalidDataException("04C physical sign source is invalid.");
            return new VenueExpanded04C { id = layout.id, style = layout.style, width = width, depth = depth, originalHeight = originalHeight,
                occupiedHeight = top, twoStoreys = layout.twoStoreys, nodes = nodes.ToArray(), anchors = layout.anchors, keepouts = layout.keepouts, lights = layout.lights, labels = layout.labels };
        }

        private static void Front(List<VenueNode04C> nodes, float halfDoor, float baseY, float height, bool openDoor)
        {
            string suffix = baseY == 0f ? "ground" : "upper";
            if (halfDoor > 2f)
            {
                // The vehicle bay has a single clear 6.4m aperture with no raised threshold.
                for (int side = -1; side <= 1; side += 2)
                    Box(nodes, "bay_jamb_" + side, "stone", side * (8f + halfDoor) * .5f, height * .5f, .15f, 8f - halfDoor, height, .3f);
                Box(nodes, "bay_lintel", "trim", 0f, 4.35f, .15f, halfDoor * 2f, 1.3f, .3f);
                return;
            }
            float[] left = { -8f, -2.2f, halfDoor, 6.2f };
            float[] right = { -6.2f, -halfDoor, 2.2f, 8f };
            for (int i = 0; i < left.Length; i++)
                Box(nodes, "front_jamb_" + suffix + i, "stone", (left[i] + right[i]) * .5f, baseY + height * .5f, .15f, right[i] - left[i], height, .3f);
            for (int side = -1; side <= 1; side += 2)
            {
                Box(nodes, "window_parapet_" + suffix + side, "stone", side * 4.2f, baseY + .5f, .15f, 4f, 1f, .3f);
                Box(nodes, "window_lintel_" + suffix + side, "stone", side * 4.2f, baseY + (3.1f + height) * .5f, .15f, 4f, height - 3.1f, .3f);
                Box(nodes, "window_guard_" + suffix + side, "metal", side * 4.2f, baseY + 1.65f, .15f, 4f, .08f, .08f);
                for (int post = -1; post <= 1; post++)
                    Box(nodes, "window_mullion_" + suffix + side + "_" + post, "metal", side * 4.2f + post * 1.3f, baseY + 2.05f, .15f, .075f, 2.1f, .08f);
            }
            if (openDoor) Box(nodes, "door_lintel", "trim", 0f, baseY + (3.2f + height) * .5f, .15f, halfDoor * 2f, height - 3.2f, .3f);
            else Box(nodes, "upper_front_center", "stone", 0f, baseY + height * .5f, .15f, halfDoor * 2f, height, .3f);
        }

        private static void StreetFrontage(VenueLayout04C layout, List<VenueNode04C> nodes, float width)
        {
            // Dress the original solid service wings as part of one frontage, keeping the real door legible.
            // These are explicitly background windows, never fake entry prompts or additional counted interiors.
            string frame = layout.id == "inn" || layout.id == "lofts" ? "timber" : "trim";
            int bays = Math.Min(6, Math.Max(0, (int)((width - 17f) / 10f)));
            for (int side = -1; side <= 1; side += 2)
            for (int bay = 0; bay < bays; bay++)
            {
                float x = side * (9.2f + bay * 4.8f);
                if (Math.Abs(x) + 1.7f > width * .5f - .25f) break;
                string id = "street_wing_" + side + "_" + bay;
                Box(nodes, id + "_recess", "glass", x, 2.02f, -.055f, 2.8f, 1.85f, .09f, false);
                for (int edge = -1; edge <= 1; edge += 2)
                {
                    Box(nodes, id + "_jamb_" + edge, frame, x + edge * 1.48f, 2.02f, -.11f, .16f, 2.1f, .24f, false);
                    Box(nodes, id + "_rail_" + edge, frame, x, 2.02f + edge * 1.01f, -.14f, 3.1f, .15f, .3f, false);
                }
                Box(nodes, id + "_sill", "trim", x, .97f, -.22f, 3.25f, .15f, .55f, false);
                if (layout.id == "inn" || layout.id == "clinic")
                    Box(nodes, id + "_awning", "linen", x, 3.2f, -.43f, 3.3f, .14f, 1.1f, false);
            }
            if (layout.id == "clinic")
            {
                Box(nodes, "clinic_cross_horizontal", "linen", 5.7f, 3.25f, -.3f, 1.15f, .28f, .12f, false);
                Box(nodes, "clinic_cross_vertical", "linen", 5.7f, 3.25f, -.3f, .28f, 1.15f, .12f, false);
            }
            else if (layout.id == "forge")
            {
                Box(nodes, "forge_supported_street_flue", "metal", -5.4f, 5.7f, -.35f, .65f, 2.6f, .6f, false);
                Box(nodes, "forge_flue_cap", "roof", -5.4f, 7.05f, -.35f, 1.05f, .16f, .9f, false);
            }
            else if (layout.id == "archive")
            {
                for (int side = -1; side <= 1; side += 2)
                    Box(nodes, "archive_entry_column_" + side, "trim", side * 1.85f, 1.75f, -.34f, .3f, 3.5f, .42f, false);
                Box(nodes, "archive_supported_header", "trim", 0f, 3.55f, -.35f, 4.4f, .2f, .7f, false);
            }
            else if (layout.id == "inn")
            {
                VenueNode04C left = Box(nodes, "diner_low_gable_left", "timber", -.95f, 3.83f, -.34f, 2.2f, .13f, .4f, false);
                VenueNode04C right = Box(nodes, "diner_low_gable_right", "timber", .95f, 3.83f, -.34f, 2.2f, .13f, .4f, false);
                left.euler[2] = 22f; right.euler[2] = -22f;
            }
        }

        private static void Stairs(List<VenueNode04C> nodes)
        {
            // The four slab rectangles leave the full stairwell empty. No hidden floor spans the route.
            Box(nodes, "upper_floor_left", "timber", -7.5f, 4.09f, 9f, 1f, .22f, 18f);
            Box(nodes, "upper_floor_main", "timber", 1.8f, 4.09f, 9f, 12.4f, .22f, 18f);
            Box(nodes, "upper_floor_front", "timber", -5.7f, 4.09f, 3f, 2.6f, .22f, 6f);
            Box(nodes, "upper_landing", "timber", -5.7f, 4.09f, 15.84f, 2.6f, .22f, 4.32f);
            for (int i = 0; i < 24; i++)
            {
                float rise = (i + 1) * .175f;
                Box(nodes, "stair_tread_" + i, "timber", -5.7f, rise * .5f, 6f + (i + .5f) * .32f, 2.4f, rise, .32f);
                if (i % 3 == 0 || i == 23)
                    for (int side = -1; side <= 1; side += 2)
                        Box(nodes, "stair_baluster_" + i + "_" + side, "metal", -5.7f + side * 1.3f, rise + .48f, 6f + (i + .5f) * .32f, .07f, .96f, .07f);
            }
            for (int side = -1; side <= 1; side += 2)
            {
                VenueNode04C rail = Box(nodes, "stair_handrail_" + side, "metal", -5.7f + side * 1.3f, 3.08f, 9.84f, .09f, .09f, 8.75f);
                rail.euler = new[] { -28.673f, 0f, 0f };
                Box(nodes, "gallery_guard_" + side, "metal", -5.7f + side * 1.35f, 5.14f, 9.84f, .08f, .08f, 7.68f);
                for (int i = 0; i < 8; i++)
                    Box(nodes, "gallery_baluster_" + side + "_" + i, "metal", -5.7f + side * 1.35f, 4.68f, 6.2f + i * 1.02f, .07f, .96f, .07f);
            }
        }

        private static VenueNode04C Box(List<VenueNode04C> nodes, string id, string role, float x, float y, float z, float w, float h, float d, bool interior = true)
        {
            VenueNode04C result = new VenueNode04C { id = id, kind = "box", role = role, position = new[] { x, y, z }, scale = new[] { w, h, d },
                euler = new float[3], solid = true, far = true, interior = interior };
            nodes.Add(result); return result;
        }
        private static bool Finite(float value) { return !float.IsNaN(value) && !float.IsInfinity(value); }
        private static bool Vector(float[] value) { return value != null && value.Length == 3 && Finite(value[0]) && Finite(value[1]) && Finite(value[2]); }
    }
}
