using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>The same 04A architecture at two explicit mesh budgets for selected neighborhood landmarks.</summary>
    public sealed partial class DistrictBuilding
    {
        private readonly bool _rebirth04A;
        private readonly int _style04A;
        public bool Uses04A { get { return _rebirth04A; } }

        private void AddTier04A(RectF footprint, float bottom, float top, int tier, bool last)
        {
            for (int level = 0; level < 2; level++)
            {
                bool detailed = level == 0;
                Dictionary<Material, MeshBuilder> geometry = new Dictionary<Material, MeshBuilder>();
                System.Func<Material,MeshBuilder> get = delegate(Material material)
                {
                    MeshBuilder builder;
                    if (!geometry.TryGetValue(material,out builder))
                    {
                        builder = new MeshBuilder(1);
                        geometry.Add(material,builder);
                    }
                    return builder;
                };
                VisualRebirth04AArchitecture.Tier(_building,footprint,bottom,top,tier,_style04A,detailed,get);
                if (last) VisualRebirth04AArchitecture.Crown(_building,footprint,top,_style04A,detailed,get);
                foreach (KeyValuePair<Material,MeshBuilder> pair in geometry)
                {
                    if (pair.Value.VertexCount == 0) continue;
                    Mesh mesh = pair.Value.ToMesh("04A " + (detailed ? "architectural detail" : "distant silhouette"));
                    _temporary.Add(mesh);
                    Add(detailed ? _near : _far,pair.Key,new CombineInstance
                    {
                        mesh = mesh,
                        transform = Matrix4x4.Translate(-_origin)
                    });
                }
            }
            if (tier == 0)
            {
                Vector3 focus = DistrictArchitecture.ViewFocus(_building);
                Vector3 delta = focus-footprint.Center(0f);
                int side = Mathf.Abs(delta.x) > Mathf.Abs(delta.z) ? (delta.x > 0f ? 1 : 3) : (delta.z > 0f ? 0 : 2);
                _beacon = FaceCenter(footprint,bottom,side) + Vector3.up * Mathf.Min(3.5f,top-bottom-.2f);
            }
            if (last && footprint.Width >= 14f && footprint.Depth >= 14f && top < 100f)
            {
                _hasDrakeSpot = true;
                _drakeSpot = footprint.Center(top + .12f);
            }
        }
    }
}
