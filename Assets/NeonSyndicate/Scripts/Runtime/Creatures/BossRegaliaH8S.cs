using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The crystal sovereign silhouette of the four classic bosses (the Shard Golem Foreman, the Mirror Wraith Queen, the
    /// Mirage Djinn and the Glacier Titan): a crown band at the top of the skull with a mirrored fan of hero shards, a
    /// mantle of shoulder horns, a collar and a spine ridge where the body has none of its own, forearm shards on the
    /// Djinn and shin shards on the Titan, in the boss's crystal family and colour style (BossRegaliaLayoutH8S.Style),
    /// laid out by the pure BossRegaliaLayoutH8S. Every piece is a plain mesh parented to a rig bone (it follows every
    /// pose and the ragdoll), on the creature surface material, on the IgnoreRaycast layer: nothing here can be hit,
    /// blocks a shot or changes a fight. A group whose breakable part (CreatureParts) breaks disappears with the part's
    /// own burst: the crown with the head part, the arm shards with the fore limb part, the shin shards with the hind
    /// limb part and the mantle with the chest part. The shards are the beasts' shards (CrystalClusterH8S), only larger.
    /// </summary>
    public sealed class BossRegaliaH8S : MonoBehaviour
    {
        private NpcController _owner;
        private readonly List<GameObject>[] _groups = new List<GameObject>[4];
        private readonly int[] _groupPart = { -1, -1, -1, -1 };
        private readonly bool[] _groupGone = new bool[4];
        private bool _resolved;
        private static readonly PartAnchor[] GroupAnchor = { PartAnchor.Head, PartAnchor.ForeLimbs, PartAnchor.HindLimbs, PartAnchor.Chest };

        /// <summary>The boss ids that wear the regalia.</summary>
        public static bool Wears(string bossId) { return BossRegaliaLayoutH8S.Wears(bossId); }

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
            for (int g = 0; g < _groups.Length; g++) _groups[g] = new List<GameObject>();
            RegaliaStyleH8S style = BossRegaliaLayoutH8S.Style(bossId);
            List<RegaliaPieceH8S> plan = new List<RegaliaPieceH8S>(24);
            BossRegaliaLayoutH8S.Plan(bossId, r.Scale, plan);
            Material mat = CreatureSurfaceH8S.Get(CreatureHideH8S.Basalt);
            List<Vector3> v = new List<Vector3>(256), n = new List<Vector3>(256);
            List<Vector2> uv = new List<Vector2>(256);
            List<int> tris = new List<int>(512);
            for (int i = 0; i < plan.Count; i++)
            {
                RegaliaPieceH8S p = plan[i];
                if (p.Bone < 0 || p.Bone >= r.Bones.Length || r.Bones[p.Bone] == null) continue;
                v.Clear(); n.Clear(); uv.Clear(); tris.Clear();
                BossRegaliaLayoutH8S.Write(p, style, Vector3.zero, v, n, uv, tris);
                MeshBuilder mb = new MeshBuilder(1);
                Push(mb, v, n, uv, tris);
                if (mb.VertexCount == 0) continue;
                GameObject go = new GameObject("H8S regalia " + p.Name);
                go.layer = Layers.IgnoreRaycast;
                go.transform.SetParent(r.Bones[p.Bone], false);
                go.AddComponent<MeshFilter>().sharedMesh = OwnedMeshes.Track(go, mb.ToMesh(go.name));
                MeshRenderer mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = mat;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                mr.receiveShadows = true;
                _groups[Mathf.Clamp(p.Group, 0, 3)].Add(go);
            }
        }

        /// <summary>
        /// Writes a shard cluster into a MeshBuilder (bone space): the socket in basalt, the shards in the family's crystal
        /// shades with lighter tips, each face flat shaded on a palette cell. The part shards (CreaturePartsRuntime) use it.
        /// </summary>
        public static void BuildCluster(MeshBuilder mb, Vector3 at, Vector3 normal, Vector3 rake, float size, float spread, int shards, uint seed, CrystalFamilyH8S family, bool socket)
        {
            List<Vector3> v = new List<Vector3>(256), n = new List<Vector3>(256);
            List<Vector2> uv = new List<Vector2>(256);
            List<int> tris = new List<int>(512);
            Vector2 basalt = BodyBuilder.Pal(ObsidianPaletteH8S.BodyRow, ObsidianPaletteH8S.Basalt);
            Vector2[] shades = new Vector2[3];
            for (int k = 0; k < 3; k++)
            {
                Vector2Int cell = ObsidianPaletteH8S.CrystalCell(family, k);
                shades[k] = BodyBuilder.Pal(cell.y, cell.x);
            }
            CrystalClusterH8S.Cluster(at, normal, rake, size, spread, shards, 0.5f, seed, socket, basalt, shades, v, n, uv, tris);
            Push(mb, v, n, uv, tris);
        }

        /// <summary>Copies flat triangles into a MeshBuilder. The faces arrive wound along their normal already (Unity's clockwise front), so the order is kept.</summary>
        private static void Push(MeshBuilder mb, List<Vector3> v, List<Vector3> n, List<Vector2> uv, List<int> tris)
        {
            for (int i = 0; i + 2 < tris.Count; i += 3)
            {
                int a = tris[i], b = tris[i + 1], c = tris[i + 2];
                mb.Triangle(0, v[a], v[b], v[c], n[a], uv[a], uv[b], uv[c]);
            }
        }

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
