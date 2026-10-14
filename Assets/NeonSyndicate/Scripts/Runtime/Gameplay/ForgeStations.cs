using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Builds the small props that mark progression places in the world (Forge anvils, the Menders' sign, the
    /// Frequency Order's listening crystal) and finds free ground for them next to an existing spot. Everything is
    /// procedural boxes and the shared shrine crystal mesh, so there are no assets.
    /// </summary>
    public static class ForgeStations
    {
        /// <summary>The least distance FindSpot keeps from every other interactable's prompt, in metres.</summary>
        public const float PromptGap = 2.2f;

        /// <summary>
        /// A free spot on the ground about dist metres from around: tries the preferred direction first, then seven
        /// others. Free means no world geometry in a person-sized capsule, not inside a building, off the roads, at least
        /// 2.2 m from any other interactable and close to the starting height. Returns false when nothing fits (the
        /// caller then skips the prop).
        /// </summary>
        public static bool FindSpot(Vector3 around, Vector3 prefer, float dist, out Vector3 spot)
        {
            return FindSpot(around, prefer, dist, PromptGap, around, 0f, out spot);
        }

        /// <summary>
        /// FindSpot with a chosen gap from every other interactable, and a reserved point (a prompt another system places
        /// later, for example a quest giver) that the spot keeps at least reservedRadius away from.
        /// </summary>
        public static bool FindSpot(Vector3 around, Vector3 prefer, float dist, float gap, Vector3 reserved, float reservedRadius, out Vector3 spot)
        {
            spot = around;
            Vector3 f = MathUtil.Flat(prefer);
            if (f.sqrMagnitude < 0.01f) f = Vector3.forward;
            f.Normalize();
            float baseYaw = MathUtil.YawOf(f);
            for (int i = 0; i < 8; i++)
            {
                // Preferred direction, then alternating to either side of it.
                int k = (i + 1) / 2;
                float yaw = baseYaw + (i % 2 == 1 ? k : -k) * 45f;
                Vector3 c = PedManager.Ground(around + MathUtil.DirFromYaw(yaw) * dist);
                if (Mathf.Abs(c.y - around.y) > 0.8f) continue;
                if (Physics.CheckCapsule(c + Vector3.up * 0.6f, c + Vector3.up * 1.7f, 0.45f, Layers.WorldMask, QueryTriggerInteraction.Ignore)) continue;
                if (Game.Layout != null && (Game.Layout.IsInsideBuilding(c, 0.4f) || Game.Layout.RoadAt(c, 1.5f) != null)) continue;
                if (NearInteractable(c, gap)) continue;
                if (reservedRadius > 0f && MathUtil.FlatSqrDistance(c, reserved) < reservedRadius * reservedRadius) continue;
                spot = c;
                return true;
            }
            return false;
        }

        private static bool NearInteractable(Vector3 p, float radius)
        {
            for (int i = 0; i < Interactable.All.Count; i++)
            {
                Interactable it = Interactable.All[i];
                if (it != null && MathUtil.FlatSqrDistance(it.transform.position, p) < radius * radius) return true;
            }
            return false;
        }

        /// <summary>A dark iron anvil on a block with a glowing ember bowl beside it. scale 1 is full size, 0.7 the shrine version.</summary>
        public static Transform Anvil(Vector3 pos, float yaw, float scale, Transform parent, string name)
        {
            GameObject root = new GameObject(name);
            root.transform.position = pos;
            root.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            if (parent != null) root.transform.SetParent(parent, true);
            Transform t = root.transform;
            Material iron = Mats.Solid(new Color(0.09f, 0.09f, 0.1f), 0.55f);
            Material wood = Mats.Wood;
            Material ember = Mats.Emissive(new Color(1f, 0.45f, 0.12f), 2.2f);
            float s = Mathf.Max(0.3f, scale);
            Shapes.Box("Block", t, new Vector3(0f, 0.25f * s, 0f), new Vector3(0.55f, 0.5f, 0.55f) * s, wood, false);
            Shapes.Box("Waist", t, new Vector3(0f, 0.6f * s, 0f), new Vector3(0.28f, 0.2f, 0.22f) * s, iron, false);
            Shapes.Box("Face", t, new Vector3(0f, 0.78f * s, 0f), new Vector3(0.75f, 0.16f, 0.3f) * s, iron, false);
            Shapes.Box("Horn", t, new Vector3(0.48f * s, 0.8f * s, 0f), new Vector3(0.24f, 0.1f, 0.16f) * s, iron, false);
            Shapes.Box("Glow", t, new Vector3(0f, 0.87f * s, 0f), new Vector3(0.6f, 0.02f, 0.06f) * s, ember, false);
            // Ember bowl.
            Shapes.Cyl("Bowl", t, new Vector3(-0.75f * s, 0.35f * s, 0.15f * s), new Vector3(0.45f, 0.35f, 0.45f) * s, iron, false);
            Shapes.Ball("Embers", t, new Vector3(-0.75f * s, 0.62f * s, 0.15f * s), new Vector3(0.36f, 0.16f, 0.36f) * s, ember, false);
            GameObject glow = Shapes.MeshObject("Ember Glow", Shapes.QuadMesh, new[] { Mats.Glow(new Color(1f, 0.5f, 0.15f, 0.7f)) }, t, false);
            glow.transform.localPosition = new Vector3(-0.75f * s, 0.9f * s, 0.15f * s);
            glow.transform.localScale = Vector3.one * 0.9f * s;
            glow.AddComponent<Billboard>();
            BoxCollider bc = root.AddComponent<BoxCollider>();
            bc.center = new Vector3(-0.2f * s, 0.45f * s, 0.05f * s);
            bc.size = new Vector3(1.5f, 0.9f, 0.6f) * s;
            return t;
        }

        /// <summary>A floating violet crystal over a dark plinth: where the Frequency Order listens to the Inner World.</summary>
        public static Transform ListeningCrystal(Vector3 pos, Transform parent)
        {
            GameObject root = new GameObject("Frequency Order Crystal");
            root.transform.position = pos;
            if (parent != null) root.transform.SetParent(parent, true);
            Transform t = root.transform;
            Shapes.Cyl("Plinth", t, new Vector3(0f, 0.35f, 0f), new Vector3(0.6f, 0.7f, 0.6f), Mats.Solid(new Color(0.06f, 0.05f, 0.08f), 0.8f), true);
            GameObject crystal = Shapes.MeshObject("Crystal", ShrineMeshes.Crystal, new[] { Mats.Emissive(new Color(0.62f, 0.4f, 1f), 1.8f) }, t, false);
            crystal.transform.localPosition = new Vector3(0f, 1.45f, 0f);
            crystal.transform.localScale = Vector3.one * 0.55f;
            Spinner spin = root.AddComponent<Spinner>();
            spin.Target = crystal.transform;
            spin.Speed = 35f;
            return t;
        }

        /// <summary>A green cross sign on a post: the Menders' corner.</summary>
        public static Transform MendersSign(Vector3 pos, float yaw, Transform parent)
        {
            GameObject root = new GameObject("Menders Sign");
            root.transform.position = pos;
            root.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            if (parent != null) root.transform.SetParent(parent, true);
            Transform t = root.transform;
            Material green = Mats.Emissive(new Color(0.3f, 1f, 0.6f), 1.6f);
            Shapes.Cyl("Post", t, new Vector3(0f, 1.1f, 0f), new Vector3(0.1f, 2.2f, 0.1f), Mats.DarkMetal, true);
            Shapes.Box("Cross H", t, new Vector3(0f, 2.35f, 0f), new Vector3(0.6f, 0.2f, 0.06f), green, false);
            Shapes.Box("Cross V", t, new Vector3(0f, 2.35f, 0f), new Vector3(0.2f, 0.6f, 0.06f), green, false);
            return t;
        }
    }
}
