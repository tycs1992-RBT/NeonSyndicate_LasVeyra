using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Puts a chassis trim (cosmetic geometry, no stats) on a personal bike under its lean pivot. Applying replaces the
    /// previous trim object, so repeated calls never stack.
    /// </summary>
    public static class ChassisTrimArt
    {
        public const string ObjectName = "Chassis Trim";

        public static void Apply(Vehicle v, string trimId, string chassisClass)
        {
            if (v == null || v.Parts == null || v.Parts.LeanRoot == null) return;
            Transform old = v.Parts.LeanRoot.Find(ObjectName);
            if (old != null) Object.Destroy(old.gameObject);
            if (string.IsNullOrEmpty(trimId) || !ChassisTrims.IsTrim(trimId)) return;
            string cls = GarageRules.IsClass(chassisClass) ? chassisClass : FusionRules.ChassisClassOf(v.Def != null ? v.Def.Name : "");
            if (string.IsNullOrEmpty(cls)) return;
            FormBlueprint bp = FusionArt.Blueprint(trimId, cls, FusionArt.UseLow, true);
            if (bp.Parts.Count == 0 || bp.VertexCount == 0) return;
            GameObject root = new GameObject(ObjectName);
            root.transform.SetParent(v.Parts.LeanRoot, false);
            Material[] palette = FusionArt.Palette(trimId);
            for (int i = 0; i < bp.Parts.Count; i++)
            {
                GameObject go = Shapes.MeshObject(bp.Parts[i].Name, FusionArt.PartMesh(bp, i), palette, root.transform, false);
                go.layer = Layers.Vehicles;
                go.transform.localPosition = bp.Parts[i].Position;
                go.transform.localRotation = bp.Parts[i].Rotation;
            }
        }
    }
}
