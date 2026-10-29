using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>A material section wholly inside one existing Forge box volume.</summary>
    public struct ForgeSurfaceSectionWR02C3
    {
        public Vector3 Center, Size;
        public string Surface;
        public ForgeSurfaceSectionWR02C3(Vector3 center, Vector3 size, string surface)
        { Center = center; Size = size; Surface = surface; }
    }

    public static partial class HeartPlanFR01
    {
        /// <summary>Only the four original solid room surfaces are eligible; no entrance or service is replaced.</summary>
        public static bool ForgeSurfaceSourceWR02C3(string id, Vector3 position, Vector3 size, Vector3 euler)
        {
            Vector3 expectedPosition, expectedSize;
            switch (id)
            {
                case "interior_floor": expectedPosition = new Vector3(0f, -.094f, 9f); expectedSize = new Vector3(16f, .212f, 18f); break;
                case "west_room_wall": expectedPosition = new Vector3(-7.85f, 2.5f, 9f); expectedSize = new Vector3(.3f, 5f, 18f); break;
                case "east_room_wall": expectedPosition = new Vector3(7.85f, 2.5f, 9f); expectedSize = new Vector3(.3f, 5f, 18f); break;
                case "rear_room_wall": expectedPosition = new Vector3(0f, 2.5f, 17.85f); expectedSize = new Vector3(16f, 5f, .3f); break;
                default: return false;
            }
            return MathUtil.IsFinite(position) && MathUtil.IsFinite(size) && MathUtil.IsFinite(euler) &&
                (position - expectedPosition).sqrMagnitude < .000001f && (size - expectedSize).sqrMagnitude < .000001f && euler.sqrMagnitude < .000001f;
        }

        /// <summary>Partition the same solid box into stone, dark framing and a stone route to the original anvil.</summary>
        public static List<ForgeSurfaceSectionWR02C3> ForgeSurfacePlanWR02C3(string id, Vector3 size)
        {
            List<ForgeSurfaceSectionWR02C3> sections = new List<ForgeSurfaceSectionWR02C3>();
            Vector3 position;
            if (id == "interior_floor") position = new Vector3(0f, -.094f, 9f);
            else if (id == "west_room_wall") position = new Vector3(-7.85f, 2.5f, 9f);
            else if (id == "east_room_wall") position = new Vector3(7.85f, 2.5f, 9f);
            else if (id == "rear_room_wall") position = new Vector3(0f, 2.5f, 17.85f);
            else return sections;
            if (!ForgeSurfaceSourceWR02C3(id, position, size, Vector3.zero)) return sections;
            if (id == "interior_floor")
            {
                float[] xs = { -8f, -5.75f, -1.5f, 1.5f, 8f };
                float[] zs = { -9f, -2.2f, .6f, 9f };
                for (int x = 0; x < xs.Length - 1; x++) for (int z = 0; z < zs.Length - 1; z++)
                {
                    bool mainWalk = x == 2 && z < 2;
                    bool forgeWalk = x == 1 && z == 1;
                    string surface = mainWalk || forgeWalk ? "CarvedTrim" : "Masonry";
                    sections.Add(new ForgeSurfaceSectionWR02C3(new Vector3((xs[x] + xs[x + 1]) * .5f, 0f, (zs[z] + zs[z + 1]) * .5f),
                        new Vector3(xs[x + 1] - xs[x], size.y, zs[z + 1] - zs[z]), surface));
                }
                return sections;
            }
            bool rear = id == "rear_room_wall";
            float length = rear ? size.x : size.z;
            int bays = rear ? 5 : 6;
            float pitch = length / bays, rib = .20f;
            sections.Add(new ForgeSurfaceSectionWR02C3(new Vector3(0f, -1.875f, 0f), new Vector3(size.x, 1.25f, size.z), "Masonry"));
            sections.Add(new ForgeSurfaceSectionWR02C3(new Vector3(0f, 2.25f, 0f), new Vector3(size.x, .5f, size.z), "SlateTimber"));
            for (int i = 0; i < bays; i++)
            {
                float edge = -length * .5f + i * pitch;
                Vector3 ribCenter = rear ? new Vector3(edge + rib * .5f, .375f, 0f) : new Vector3(0f, .375f, edge + rib * .5f);
                Vector3 ribSize = rear ? new Vector3(rib, 3.25f, size.z) : new Vector3(size.x, 3.25f, rib);
                Vector3 bayCenter = rear ? new Vector3(edge + rib + (pitch - rib) * .5f, .375f, 0f) : new Vector3(0f, .375f, edge + rib + (pitch - rib) * .5f);
                Vector3 baySize = rear ? new Vector3(pitch - rib, 3.25f, size.z) : new Vector3(size.x, 3.25f, pitch - rib);
                sections.Add(new ForgeSurfaceSectionWR02C3(ribCenter, ribSize, "SlateTimber"));
                sections.Add(new ForgeSurfaceSectionWR02C3(bayCenter, baySize, "CarvedTrim"));
            }
            return sections;
        }
    }
}
