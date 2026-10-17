using UnityEngine;
using UnityEngine.Rendering;

namespace NeonSyndicate
{
    /// <summary>
    /// The honest stand-in when a cast member's body cannot be built: a small message terminal at their post, labeled
    /// "MESSAGE TERMINAL" with the person's name and "not present". It carries the same marker and talk prompt, so a
    /// non-combat handoff (the offer card) still works, and the STATUS page counts it as a terminal, never as a person.
    /// A post, an angled screen with a lit face and a sign; one collider so nobody walks through it. Runtime only.
    /// </summary>
    public static class MessageTerminal04E
    {
        public static GameObject Build(Vector3 pos, float yaw, string personName, Transform parent)
        {
            GameObject root = new GameObject("Message Terminal 04E " + personName);
            root.transform.SetParent(parent, false);
            root.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
            Material body = Mats.Solid(new Color(0.16f, 0.18f, 0.22f), 0.45f);
            Material trim = Mats.Solid(new Color(0.85f, 0.65f, 0.25f), 0.6f);
            Material screen = Mats.Emissive(new Color(0.35f, 0.85f, 1f), 1.4f);
            MeshBuilder b = new MeshBuilder(3);
            b.Box(0, new Vector3(0f, 0.05f, 0f), new Vector3(0.62f, 0.1f, 0.46f), UVMode.Unit);
            b.Box(0, new Vector3(0f, 0.62f, -0.04f), new Vector3(0.16f, 1.05f, 0.14f), UVMode.Unit);
            Quaternion tilt = QMath.AX(-28f);
            b.Box(0, new Vector3(0f, 1.2f, 0.02f), new Vector3(0.56f, 0.4f, 0.06f), tilt, UVMode.Unit);
            b.Box(2, new Vector3(0f, 1.2f, 0f) + tilt * new Vector3(0f, 0f, 0.034f), new Vector3(0.48f, 0.32f, 0.01f), tilt, UVMode.Unit);
            b.Box(0, new Vector3(0f, 1.41f, -0.07f), new Vector3(0.1f, 0.12f, 0.06f), UVMode.Unit);
            b.Box(0, new Vector3(0f, 1.54f, -0.06f), new Vector3(0.66f, 0.17f, 0.03f), UVMode.Unit);
            b.Box(1, new Vector3(0f, 1.45f, -0.06f), new Vector3(0.68f, 0.015f, 0.035f), UVMode.Unit);
            b.Box(1, new Vector3(0f, 0.12f, 0.2f), new Vector3(0.58f, 0.03f, 0.04f), UVMode.Unit);
            GameObject mesh = new GameObject("Terminal Body");
            mesh.transform.SetParent(root.transform, false);
            mesh.AddComponent<MeshFilter>().sharedMesh = OwnedMeshes.Track(mesh, b.ToMesh("Message Terminal 04E"));
            MeshRenderer mr = mesh.AddComponent<MeshRenderer>();
            mr.sharedMaterials = new[] { body, trim, screen };
            mr.shadowCastingMode = ShadowCastingMode.On;
            BoxCollider col = mesh.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.7f, 0f);
            col.size = new Vector3(0.6f, 1.4f, 0.45f);
            mesh.layer = Layers.Debris;
            Label(root.transform, "MESSAGE TERMINAL", new Vector3(0f, 1.54f, -0.04f), 0.045f, new Color(1f, 0.82f, 0.35f));
            Label(root.transform, personName + " (not present)", new Vector3(0f, 1.224f, 0.046f), 0.03f, new Color(0.85f, 0.95f, 1f));
            return root;
        }

        private static void Label(Transform parent, string text, Vector3 local, float size, Color color)
        {
            GameObject go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = local;
            // A TextMesh reads from its local -Z side; the screen faces +Z (toward the viewer) tilted up 28 degrees, so the
            // text turns around and tilts with it.
            go.transform.localRotation = Quaternion.Euler(local.y < 1.4f ? 28f : 0f, 180f, 0f);
            TextMesh tm = go.AddComponent<TextMesh>();
            tm.font = Mats.UIFont;
            tm.text = text;
            tm.fontSize = 64;
            tm.characterSize = size;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = color;
            MeshRenderer r = go.GetComponent<MeshRenderer>();
            if (tm.font != null) r.sharedMaterial = tm.font.material;
            r.shadowCastingMode = ShadowCastingMode.Off;
        }
    }
}
