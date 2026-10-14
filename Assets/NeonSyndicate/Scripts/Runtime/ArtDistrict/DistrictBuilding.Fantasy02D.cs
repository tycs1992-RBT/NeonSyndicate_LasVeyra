using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>Integrated art pass for existing buildings. All parts join the original material-batched LODs.</summary>
    public sealed partial class DistrictBuilding
    {
        private readonly CrystalCityStyle _fantasyStyle;

        private void CrystalPart(string name, string material, Vector3 world, Quaternion rotation, Vector3 scale, bool far = true)
        {
            Material mat = CrystalCity02DMaterials.Get(_fantasyStyle, material);
            if (mat == null) return;
            Matrix4x4 placement = Matrix4x4.TRS(world - _origin, rotation, scale);
            Mesh near = CrystalCity02DGeometry.Get(name, Settings.Graphics == GraphicsPreset.Low);
            if (near != null) Add(_near, mat, new CombineInstance { mesh = near, transform = placement });
            if (!far) return;
            Mesh distant = CrystalCity02DGeometry.Get(name, true);
            if (distant != null) Add(_far, mat, new CombineInstance { mesh = distant, transform = placement });
        }

        private void FacadeRelief02D(Vector3 face, Quaternion rotation, float width, float height, int tier)
        {
            if (height < 7f || width < 6f) return;
            float ornamentY = Mathf.Clamp(height * .63f, 4.6f, height - 1.5f);
            float scale = Mathf.Clamp(width * .075f, .8f, 1.8f);
            Vector3 medallion = face + rotation * new Vector3(0f, ornamentY, .37f);
            CrystalPart("orbit", "bronze", medallion, rotation * Quaternion.Euler(90f,0f,0f), Vector3.one * scale);
            CrystalPart("rosette", "glass", medallion + rotation * Vector3.forward * .025f,
                rotation, Vector3.one * scale, false);

            // Readable at walking height, without placing geometry in a doorway.
            if (tier == 0)
                for (int s = -1; s <= 1; s += 2)
                {
                    Vector3 p = face + rotation * new Vector3(width * .32f * s, 3.7f, .25f);
                    CrystalPart("lantern", "bronze", p, rotation, Vector3.one * .6f);
                    CrystalPart("shard", "crystal", p + Vector3.up * .12f, rotation, new Vector3(.6f,.6f,.6f));
                }
            if (tier == 0)
            {
                // A low planting at a wall edge, away from the central threshold.
                Vector3 pocket = face + rotation * new Vector3(width * .39f, .17f, .12f);
                if (_fantasyStyle == CrystalCityStyle.VerdantReach)
                    CrystalPart("fern", "foliage", pocket, rotation, Vector3.one * .85f, false);
                else if (_fantasyStyle == CrystalCityStyle.DawnstoneCourt)
                    CrystalPart("bloom", "crystalWarm", pocket, rotation, Vector3.one * .6f, false);
                else CrystalPart("shard", "crystalViolet", pocket, rotation, new Vector3(.45f,.95f,.45f), false);
            }
            if (_fantasyStyle == CrystalCityStyle.DawnstoneCourt && height > 10f)
            {
                Vector3 rib = face + rotation * new Vector3(-width * .36f, height * .30f, .26f);
                CrystalPart("buttress", "sandstone", rib, rotation, new Vector3(.65f,1.05f,.65f));
            }
            if (_fantasyStyle == CrystalCityStyle.VerdantReach)
            {
                // Drapery starts above head height and stays close to the existing facade.
                for (int s = -1; s <= 1; s += 2)
                {
                    Vector3 p = face + rotation * new Vector3(width * .28f * s, height - .3f, .35f);
                    CrystalPart("vine", "foliage", p, rotation, new Vector3(1.5f, Mathf.Min(1.7f, (height - 3f) / 2.6f), 1f), false);
                }
            }
        }

        private void LandmarkRoof02D(RectF roof, float y)
        {
            float span = Mathf.Min(roof.Width, roof.Depth);
            if (span < 10f) return;
            float r = Mathf.Clamp(span * .19f, 2f, 5.2f);
            Vector3 center = roof.Center(y + .16f);
            Quaternion yaw = Quaternion.Euler(0f, _building.Seed % 360, 0f);
            if (_fantasyStyle == CrystalCityStyle.AmethystWard)
            {
                CrystalPart("crown", "bronze", center, yaw, new Vector3(r, r * .8f, r));
                Vector3 jewel = center + Vector3.up * r * .35f;
                CrystalPart("shard", "crystalViolet", jewel, yaw * Quaternion.Euler(0f,0f,-8f), new Vector3(r * 1.3f,r * 2.4f,r * 1.3f));
                CrystalPart("orbit", "bronze", center + Vector3.up * r * 1.35f,
                    yaw * Quaternion.Euler(58f,0f,25f), Vector3.one * r * 1.1f);
            }
            else if (_fantasyStyle == CrystalCityStyle.DawnstoneCourt)
            {
                CrystalPart("crown", "sandstone", center, yaw, new Vector3(r,r * 1.25f,r));
                Vector3 ring = center + Vector3.up * r * 2f;
                CrystalPart("orbit", "bronze", ring, yaw * Quaternion.Euler(90f,0f,0f), Vector3.one * r * .9f);
                CrystalPart("rosette", "glass", ring, yaw, Vector3.one * r * .8f);
                for (int i = 0; i < 3; i++)
                {
                    Vector3 p = center + yaw * new Vector3((i - 1) * r * .52f,r * .65f,0f);
                    CrystalPart("shard", "crystalWarm", p, yaw, new Vector3(r * .55f,r * (i == 1 ? 1.45f : 1f),r * .55f));
                }
            }
            else
            {
                float treeScale = Mathf.Clamp(span / 24f, .85f, 1.35f);
                CrystalPart("trunk", "bark", center, yaw, Vector3.one * treeScale);
                CrystalPart("leaves", "foliage", center, yaw, Vector3.one * treeScale);
                CrystalPart("bloom", "crystal", center + Vector3.up * 4.1f * treeScale, yaw, Vector3.one * 1.6f);
                for (int i = 0; i < 3; i++)
                {
                    float angle = i * 120f * Mathf.Deg2Rad;
                    Vector3 p = center + new Vector3(Mathf.Cos(angle) * r * .65f,0f,Mathf.Sin(angle) * r * .65f);
                    CrystalPart("fern", "foliage", p, Quaternion.Euler(0f,i * 120f,0f), Vector3.one * 1.3f);
                }
            }

            // Sparse suspended relics are above roofs, never floating in traffic lanes.
            if ((_building.Seed & 7) == 0 && span > 16f)
            {
                Vector3 p = center + Vector3.up * (r * 3f + 4f);
                CrystalPart("relic", "basalt", p, yaw, Vector3.one * r * .65f);
                CrystalPart("shard", "crystal", p + Vector3.up * .2f, yaw, new Vector3(r * .5f,r,r * .5f));
            }
        }
    }
}
