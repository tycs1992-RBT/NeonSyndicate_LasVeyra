using UnityEngine;

namespace NeonSyndicate
{
    public static partial class GothicRealmGeometry04OB
    {
        /// <summary>A closed irregular mineral envelope with a genuinely hollow bore and two open mouths.</summary>
        private static void MineralGrotto(MeshBuilder stone, MeshBuilder mineral, Building building)
        {
            RectF f = building.Footprint;
            Vector3 origin = new Vector3(f.CenterX, CityLayout.CurbHeight, f.Z0);
            float half = f.Width * .385f, thickness = f.Width * .10f;
            const float spring = 2.7f, rise = 6.2f;
            const int rings = 6, segments = 12;
            for (int z = 0; z < rings; z++) for (int i = 0; i < segments; i++)
            {
                Vector3[] inner = new Vector3[4], outer = new Vector3[4];
                int[] angles = { i, i + 1, i + 1, i };
                int[] rows = { z, z, z + 1, z + 1 };
                for (int n = 0; n < 4; n++)
                {
                    float theta = angles[n] * Mathf.PI / segments, along = rows[n] * f.Depth / rings;
                    float rough = Mathf.Sin(theta) * Mathf.Sin(along * .65f + theta * 3f) * .27f;
                    inner[n] = new Vector3(-Mathf.Cos(theta) * half, spring + Mathf.Sin(theta) * rise + rough, along);
                    outer[n] = inner[n] + new Vector3(-Mathf.Cos(theta) * thickness, Mathf.Sin(theta) * thickness, 0f);
                }
                Vector3[] p = { inner[0], inner[1], inner[2], inner[3], outer[0], outer[1], outer[2], outer[3] };
                for (int n = 0; n < p.Length; n++) p[n] += origin;
                Hex(stone, p);
            }
            for (int side = -1; side <= 1; side += 2)
            {
                Box(stone, origin, 0f, new Vector3(side * (half + thickness * .5f), spring * .5f, f.Depth * .5f), new Vector3(thickness, spring, f.Depth));
                for (int i = 0; i < 3; i++)
                {
                    Rock(stone, origin + new Vector3(side * (half + .5f), 0f, 2f + i * 6f), new Vector3(3f, 7.5f + i, 4.4f), building.Seed + i);
                    Crystal(mineral, origin + new Vector3(side * 7.4f, 0f, 4f + i * 4.5f), new Vector3(1.8f, 3f + i * .8f, 1.8f), side * .07f, building.Seed + i * 17);
                }
            }
        }

        /// <summary>Conservative surface navigation for the grotto: the central ten metres remain a real passage.</summary>
        public static bool BlocksGround04OB(Building building, Vector3 point, float margin)
        {
            if (!building.Footprint.Inset(-margin).Contains(point)) return false;
            if (building.GothicFamily04OB != 8) return true;
            float half = 5f - Mathf.Max(0f, margin);
            return half <= 0f || Mathf.Abs(point.x - building.Footprint.CenterX) > half;
        }
    }
}
