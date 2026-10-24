using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>One planned regalia piece: a shard cluster on a humanoid rig bone (RigLayout index), in the bone's space at bind.</summary>
    public struct RegaliaPieceH8S
    {
        /// <summary>RigLayout bone index the piece hangs from.</summary>
        public int Bone;
        /// <summary>Which breakable part group hides it: 0 head, 1 fore limbs, 2 hind limbs, 3 chest (and shoulders, which never break).</summary>
        public int Group;
        public string Name;
        /// <summary>Cluster root in the bone's space (metres, already scaled).</summary>
        public Vector3 At;
        /// <summary>Direction the shards grow, and the surface direction they lean toward.</summary>
        public Vector3 Normal, Rake;
        public float Size, Spread;
        public int Shards;
        public uint Seed;
        public bool Socket;
    }

    /// <summary>
    /// The layout of the crystal sovereign regalia on the four classic bosses, engine-free: where each cluster sits on the
    /// humanoid rig (RigLayout bone indices, bind pose: arms down, +Y up, +Z forward), how big it is for the rig's scale,
    /// and which breakable part group hides it. BossRegaliaH8S builds the pieces from it in the game; the offline preview
    /// draws the same plan on a built boss body.
    /// </summary>
    public static class BossRegaliaLayoutH8S
    {
        public const int GroupHead = 0, GroupForeLimbs = 1, GroupHindLimbs = 2, GroupChest = 3;

        /// <summary>The boss ids that wear the regalia.</summary>
        public static bool Wears(string bossId)
        {
            return bossId == "golem_foreman" || bossId == "wraith_queen" || bossId == "mirage_djinn" || bossId == "glacier_titan";
        }

        /// <summary>Plans every piece for a boss at a rig scale (metres); an unknown boss plans nothing.</summary>
        public static void Plan(string bossId, float scale, List<RegaliaPieceH8S> into)
        {
            if (into == null || !Wears(bossId)) return;
            float s = Mathf.Max(0.6f, scale);
            uint seed = Hash(bossId) | 1u;
            bool titan = bossId == "glacier_titan";
            // The crown: clusters around the skull, the tallest at the back, leaning out and back. The Foreman and the
            // Queen already carry a crystal crown of their own (BodyBuilder), so theirs is a lower wreath behind it.
            bool wreath = bossId == "golem_foreman" || bossId == "wraith_queen";
            int crown = wreath ? 5 : 5;
            float arc = wreath ? 80f : 130f;
            for (int i = 0; i < crown; i++)
            {
                float a = Mathf.Lerp(-arc, arc, (float)i / (crown - 1)) * Mathf.Deg2Rad;
                RegaliaPieceH8S p = new RegaliaPieceH8S();
                p.Bone = RigLayout.Head;
                p.Group = GroupHead;
                p.Name = wreath ? "Wreath" : "Crown";
                p.At = new Vector3(Mathf.Sin(a) * (wreath ? 0.11f : 0.1f), wreath ? 0.06f : 0.15f, -Mathf.Cos(a) * (wreath ? 0.1f : 0.08f)) * s;
                p.Normal = new Vector3(Mathf.Sin(a), wreath ? 0.5f : 0.9f, -Mathf.Cos(a) * (wreath ? 0.9f : 0.6f)).normalized;
                p.Rake = Vector3.back;
                p.Size = s * (titan ? 0.3f : (wreath ? 0.17f : 0.24f)) * (0.75f + 0.5f * Mathf.Abs(Mathf.Cos(a * 0.5f)));
                p.Spread = p.Size * 0.4f;
                p.Shards = wreath ? 3 : 4;
                p.Seed = seed + (uint)i * 31u;
                p.Socket = true;
                into.Add(p);
            }
            // Shoulders: a heavy cluster on each, raked outward and back.
            for (int side = -1; side <= 1; side += 2)
            {
                RegaliaPieceH8S p = new RegaliaPieceH8S();
                p.Bone = side < 0 ? RigLayout.UpperArmL : RigLayout.UpperArmR;
                p.Group = GroupChest;
                p.Name = "Shoulder";
                p.At = new Vector3(side * 0.05f, 0.04f, -0.02f) * s;
                p.Normal = new Vector3(side * 0.8f, 0.7f, -0.25f).normalized;
                p.Rake = new Vector3(0f, 0.3f, -1f);
                p.Size = s * 0.2f;
                p.Spread = s * 0.08f;
                p.Shards = 4;
                p.Seed = seed + (uint)(100 + side);
                p.Socket = true;
                into.Add(p);
            }
            // Forearms (gauntlets and bracers) or, for the Titan, the shins (its knee plates).
            for (int side = -1; side <= 1; side += 2)
            {
                RegaliaPieceH8S p = new RegaliaPieceH8S();
                p.Bone = titan ? (side < 0 ? RigLayout.ShinL : RigLayout.ShinR) : (side < 0 ? RigLayout.ForearmL : RigLayout.ForearmR);
                p.Group = titan ? GroupHindLimbs : GroupForeLimbs;
                p.Name = titan ? "Shin shards" : "Gauntlet";
                p.At = new Vector3(0f, -0.12f, 0.05f) * s;
                p.Normal = new Vector3(0f, -0.2f, 1f).normalized;
                p.Rake = Vector3.up;
                p.Size = s * 0.12f;
                p.Spread = s * 0.045f;
                p.Shards = 3;
                p.Seed = seed + (uint)(200 + side);
                p.Socket = false;
                into.Add(p);
            }
            // The collar: shards rising from the top of the chest either side of the neck, and a ridge down the back.
            for (int side = -1; side <= 1; side += 2)
            {
                RegaliaPieceH8S p = new RegaliaPieceH8S();
                p.Bone = RigLayout.Chest;
                p.Group = GroupChest;
                p.Name = "Collar";
                p.At = new Vector3(side * 0.13f, 0.2f, 0.06f) * s;
                p.Normal = new Vector3(side * 0.4f, 1f, 0.15f).normalized;
                p.Rake = Vector3.back;
                p.Size = s * 0.16f;
                p.Spread = s * 0.06f;
                p.Shards = 3;
                p.Seed = seed + (uint)(300 + side);
                p.Socket = true;
                into.Add(p);
            }
            for (int i = 0; i < 3; i++)
            {
                RegaliaPieceH8S p = new RegaliaPieceH8S();
                p.Bone = RigLayout.Chest;
                p.Group = GroupChest;
                p.Name = "Ridge";
                p.At = new Vector3(0f, 0.16f - i * 0.08f, -0.13f) * s;
                p.Normal = new Vector3(0f, 0.55f, -1f).normalized;
                p.Rake = Vector3.up;
                p.Size = s * (0.14f - i * 0.02f);
                p.Spread = s * 0.04f;
                p.Shards = 3;
                p.Seed = seed + (uint)(310 + i);
                p.Socket = i == 0;
                into.Add(p);
            }
        }

        /// <summary>FNV-1a of the boss id (string.GetHashCode differs between runtimes).</summary>
        public static uint Hash(string s)
        {
            uint h = 2166136261u;
            for (int i = 0; i < s.Length; i++)
            {
                h ^= s[i];
                h *= 16777619u;
            }
            return h;
        }
    }
}
