using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The crystal sovereign silhouette of the four classic bosses (the Shard Golem Foreman, the Mirror Wraith Queen, the
    /// Mirage Djinn and the Glacier Titan): a crown of shards around the skull, shoulder and collar clusters, forearm or
    /// shin shards and a ridge down the back, in the boss's crystal family (ObsidianLookH8S.BossFamily). Every piece is
    /// a plain mesh parented to a rig bone (it follows every pose and the ragdoll), on the creature surface material, on
    /// the IgnoreRaycast layer: nothing here can be hit, blocks a shot or changes a fight. A group whose breakable part
    /// (CreatureParts) breaks disappears with the part's own burst: the crown with the head part, the arm shards with the
    /// fore limb part, the shin shards with the hind limb part and the collar with the chest part. Shapes come from
    /// CrystalClusterH8S, so the bosses wear the same shards as the beasts, only larger.
    /// </summary>
    public sealed class BossRegaliaH8S : MonoBehaviour
    {
        private NpcController _owner;
        private readonly List<GameObject>[] _groups = new List<GameObject>[4];
        private readonly int[] _groupPart = { -1, -1, -1, -1 };
        private readonly bool[] _groupGone = new bool[4];
        private static readonly PartAnchor[] GroupAnchor = { PartAnchor.Head, PartAnchor.ForeLimbs, PartAnchor.HindLimbs, PartAnchor.Chest };

        /// <summary>The boss ids that wear the regalia.</summary>
        public static bool Wears(string bossId)
        {
            return bossId == "golem_foreman" || bossId == "wraith_queen" || bossId == "mirage_djinn" || bossId == "glacier_titan";
        }

        /// <summary>Attaches the regalia to a freshly spawned boss (CreatureDirector.SpawnBossBody); any failure leaves the boss as it was.</summary>
        public static void Attach(NpcController npc, string bossId)
        {
            if (npc == null || npc.Rig == null || !Wears(bossId)) return;
            try
            {
                npc.gameObject.AddComponent<BossRegaliaH8S>().Bind(npc, bossId);
            }
            catch (Exception e)
            {
                Log.Exception("Boss regalia " + bossId, e);
            }
        }

        private void Bind(NpcController npc, string bossId)
        {
            _owner = npc;
            HumanoidRig r = npc.Rig;
            float s = Mathf.Max(0.6f, r.Scale);
            CrystalFamilyH8S family = ObsidianLookH8S.BossFamily(bossId);
            uint seed = Hash(bossId) | 1u;
            for (int g = 0; g < _groups.Length; g++) _groups[g] = new List<GameObject>();
            // The crown: five clusters around the top of the skull, the tallest at the back, leaning out and back.
            if (r.Head != null)
            {
                int crown = bossId == "wraith_queen" ? 7 : 5;
                for (int i = 0; i < crown; i++)
                {
                    float a = Mathf.Lerp(-130f, 130f, crown == 1 ? 0.5f : (float)i / (crown - 1)) * Mathf.Deg2Rad;
                    Vector3 dir = new Vector3(Mathf.Sin(a), 0.9f, -Mathf.Cos(a) * 0.6f).normalized;
                    Vector3 at = new Vector3(Mathf.Sin(a) * 0.1f, 0.15f, -Mathf.Cos(a) * 0.08f) * s;
                    float size = s * (bossId == "glacier_titan" ? 0.3f : 0.24f) * (0.75f + 0.5f * Mathf.Abs(Mathf.Cos(a * 0.5f)));
                    Piece(0, r.Head, "Crown", at, dir, Vector3.back, size, size * 0.4f, 4, seed + (uint)i * 31u, family, true);
                }
            }
            // Shoulders: a heavy cluster on each, raked outward and back.
            Shoulder(r.UpperArmL, -1, s, seed + 101u, family);
            Shoulder(r.UpperArmR, 1, s, seed + 102u, family);
            // Forearms (gauntlets and bracers) or, for the Titan, the shins (its knee plates).
            if (bossId == "glacier_titan")
            {
                Limb(2, r.ShinL, s, seed + 201u, family);
                Limb(2, r.ShinR, s, seed + 202u, family);
            }
            else
            {
                Limb(1, r.ForearmL, s, seed + 211u, family);
                Limb(1, r.ForearmR, s, seed + 212u, family);
            }
            // The collar: shards rising from the top of the chest either side of the neck, and a ridge down the back.
            if (r.Chest != null)
            {
                for (int side = -1; side <= 1; side += 2)
                    Piece(3, r.Chest, "Collar", new Vector3(side * 0.13f, 0.12f, 0.06f) * s, new Vector3(side * 0.4f, 1f, 0.15f).normalized, Vector3.back, s * 0.16f, s * 0.06f, 3, seed + (uint)(300 + side), family, true);
                for (int i = 0; i < 3; i++)
                    Piece(3, r.Chest, "Ridge", new Vector3(0f, 0.1f - i * 0.08f, -0.13f) * s, new Vector3(0f, 0.55f, -1f).normalized, Vector3.up, s * (0.14f - i * 0.02f), s * 0.04f, 3, seed + (uint)(310 + i), family, i == 0);
            }
        }

        private void Shoulder(Transform bone, int side, float s, uint seed, CrystalFamilyH8S family)
        {
            if (bone == null) return;
            Piece(3, bone, "Shoulder", new Vector3(side * 0.05f, 0.04f, -0.02f) * s, new Vector3(side * 0.8f, 0.7f, -0.25f).normalized, new Vector3(0f, 0.3f, -1f), s * 0.2f, s * 0.08f, 4, seed, family, true);
        }

        private void Limb(int group, Transform bone, float s, uint seed, CrystalFamilyH8S family)
        {
            if (bone == null) return;
            Piece(group, bone, group == 2 ? "Shin shards" : "Gauntlet", new Vector3(0f, -0.12f, 0.05f) * s, new Vector3(0f, -0.2f, 1f).normalized, Vector3.up, s * 0.12f, s * 0.045f, 3, seed, family, false);
        }

        /// <summary>One cluster piece under a bone: a mesh built in the bone's space from a planned cluster.</summary>
        private void Piece(int group, Transform bone, string name, Vector3 at, Vector3 normal, Vector3 rake, float size, float spread, int shards, uint seed, CrystalFamilyH8S family, bool socket)
        {
            MeshBuilder mb = new MeshBuilder(1);
            BuildCluster(mb, at, normal, rake, size, spread, shards, seed, family, socket);
            if (mb.VertexCount == 0) return;
            GameObject go = new GameObject("H8S regalia " + name);
            go.layer = Layers.IgnoreRaycast;
            go.transform.SetParent(bone, false);
            go.AddComponent<MeshFilter>().sharedMesh = OwnedMeshes.Track(go, mb.ToMesh(go.name));
            MeshRenderer mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = CreatureSurfaceH8S.Get(CreatureHideH8S.Basalt);
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            mr.receiveShadows = true;
            _groups[group].Add(go);
        }

        /// <summary>
        /// Writes a shard cluster into a MeshBuilder (bone space): the socket in basalt, the shards in the family's crystal
        /// shades with lighter tips, each face flat shaded on a palette cell. Shared with the part shards.
        /// </summary>
        public static void BuildCluster(MeshBuilder mb, Vector3 at, Vector3 normal, Vector3 rake, float size, float spread, int shards, uint seed, CrystalFamilyH8S family, bool socket)
        {
            Vector3 z = normal.normalized;
            if (z.sqrMagnitude < 0.5f) z = Vector3.up;
            Vector3 x = rake - z * Vector3.Dot(rake, z);
            if (x.sqrMagnitude < 1e-8f) x = Vector3.Cross(z, Mathf.Abs(z.y) < 0.9f ? Vector3.up : Vector3.right);
            x.Normalize();
            Vector3 y = Vector3.Cross(z, x);
            CrystalPrismH8S[] prisms = new CrystalPrismH8S[12];
            int count = CrystalClusterH8S.Plan(seed, shards, size, spread, 0.5f, prisms);
            List<Vector3> v = new List<Vector3>(256), n = new List<Vector3>(256);
            List<int> kind = new List<int>(256), tris = new List<int>(512);
            Vector2 basalt = BodyBuilder.Pal(ObsidianPaletteH8S.BodyRow, ObsidianPaletteH8S.Basalt);
            Vector3 origin = at - z * (size * 0.04f);
            if (socket)
            {
                CrystalClusterH8S.Socket(seed ^ 0x5bd1e995u, 7, spread * 1.25f + size * 0.12f, size * 0.1f, v, n, kind, tris);
                Emit(mb, v, n, kind, tris, origin, x, y, z, basalt, basalt, basalt);
            }
            for (int i = 0; i < count; i++)
            {
                v.Clear(); n.Clear(); kind.Clear(); tris.Clear();
                CrystalPrismH8S p = CrystalClusterH8S.Place(prisms[i], origin, x, y, z);
                CrystalClusterH8S.Faces(p, seed + (uint)(i * 7919), 0.14f, v, n, kind, tris);
                Vector2Int body = ObsidianPaletteH8S.CrystalCell(family, p.Shade), tip = ObsidianPaletteH8S.CrystalCell(family, Mathf.Min(2, p.Shade + 1));
                Emit(mb, v, n, kind, tris, Vector3.zero, Vector3.right, Vector3.up, Vector3.forward, BodyBuilder.Pal(body.y, body.x), BodyBuilder.Pal(tip.y, tip.x), basalt);
            }
        }

        /// <summary>FNV-1a of the boss id (string.GetHashCode differs between runtimes).</summary>
        private static uint Hash(string s)
        {
            uint h = 2166136261u;
            for (int i = 0; i < s.Length; i++)
            {
                h ^= s[i];
                h *= 16777619u;
            }
            return h;
        }

        private static void Emit(MeshBuilder mb, List<Vector3> v, List<Vector3> n, List<int> kind, List<int> tris, Vector3 origin, Vector3 x, Vector3 y, Vector3 z, Vector2 bodyUV, Vector2 tipUV, Vector2 socketUV)
        {
            for (int i = 0; i + 2 < tris.Count; i += 3)
            {
                int a = tris[i], b = tris[i + 1], c = tris[i + 2];
                Vector3 pa = origin + x * v[a].x + y * v[a].y + z * v[a].z;
                Vector3 pb = origin + x * v[b].x + y * v[b].y + z * v[b].z;
                Vector3 pc = origin + x * v[c].x + y * v[c].y + z * v[c].z;
                Vector3 fn = (x * n[a].x + y * n[a].y + z * n[a].z).normalized;
                int k = kind[a];
                Vector2 uv = k == 1 ? tipUV : (k == 2 ? socketUV : bodyUV);
                // The faces arrive wound along their normal already (Unity's clockwise front), so the order is kept.
                mb.Triangle(0, pa, pb, pc, fn, uv, uv, uv);
            }
        }

        private bool _resolved;

        private void LateUpdate()
        {
            if (_owner == null || _owner.Health == null || _owner.Health.Parts == null || _owner.Health.Parts.Book == null) return;
            CreaturePartBook book = _owner.Health.Parts.Book;
            if (!_resolved)
            {
                // The parts may be attached after the regalia: resolve the groups once they exist.
                _resolved = true;
                for (int g = 0; g < _groups.Length; g++) _groupPart[g] = book.IndexOf(GroupAnchor[g]);
            }
            for (int g = 0; g < _groups.Length; g++)
            {
                if (_groupGone[g] || _groupPart[g] < 0 || !book.Broken(_groupPart[g])) continue;
                _groupGone[g] = true;
                List<GameObject> pieces = _groups[g];
                for (int i = 0; i < pieces.Count; i++) if (pieces[i] != null) pieces[i].SetActive(false);
            }
        }
    }
}
