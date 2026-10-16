using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The projectile bodies (all +Z forward, no colliders, about 200 triangles at most): Shardwake's bright mineral
    /// shard with a short fading refractive tail, the smaller barbed fan shard, Gyre Crescent's faceted disc (spinning,
    /// the same shape as the one that leaves the cradle, drawn larger), Sunderheart's orb with a dense glowing core under
    /// a cracked shell and three orbiting pebbles, and Mirage Choir's slim cross-bladed guided shard. Plus the Mirrorwake
    /// focus marker, one hovering mirror facet.
    /// </summary>
    public static partial class CrystalFocusModels
    {
        /// <summary>A shot body as plain data (no Unity objects).</summary>
        public static CrystalModelGeometry BuildShotGeometry(CrystalShotKind kind)
        {
            switch (kind)
            {
                case CrystalShotKind.FanShard: return BuildFanShard();
                case CrystalShotKind.Disc: return BuildDiscShot();
                case CrystalShotKind.Orb: return BuildOrbShot();
                case CrystalShotKind.ChoirShard: return BuildChoirShard();
                default: return BuildShardShot();
            }
        }

        private static CrystalModelGeometry BuildShardShot()
        {
            CrystalModelGeometry m = new CrystalModelGeometry();
            MeshBuilder b = m.Body.Mesh;
            m.Body.Shadows = false;
            Shard(b, SubCore, new Vector3(0f, 0f, -0.13f), new Vector3(0f, 0f, 0.17f), 0.024f, 8, 0.58f, 22.5f);
            Shard(b, SubActive, new Vector3(0.012f, 0.006f, -0.16f), new Vector3(0.016f, 0.009f, -0.06f), 0.007f, 4, 0.5f, 45f);
            Shard(b, SubActive, new Vector3(-0.011f, -0.008f, -0.15f), new Vector3(-0.015f, -0.011f, -0.07f), 0.006f, 4, 0.5f, 45f);
            CrystalPartGeometry tail = m.Add("Tail", CrystalPartRole.Glow, 0, Vector3.zero);
            tail.Additive = true;
            tail.Shadows = false;
            Tail(tail.Mesh, SubStructure, 0.7f, 0.055f, -0.08f);
            GlowStar(tail.Mesh, SubStructure, new Vector3(0f, 0f, 0.02f), 0.08f);
            return m;
        }

        private static CrystalModelGeometry BuildFanShard()
        {
            CrystalModelGeometry m = new CrystalModelGeometry();
            MeshBuilder b = m.Body.Mesh;
            m.Body.Shadows = false;
            Shard(b, SubCore, new Vector3(0f, 0f, -0.09f), new Vector3(0f, 0f, 0.13f), 0.02f, 6, 0.62f, 0f, 0.6f);
            for (int s = -1; s <= 1; s += 2)
                Fin(b, SubActive, new Vector3(0.01f * s, 0f, 0.02f), new Vector3(0.006f * s, 0f, -0.07f), new Vector3(0.034f * s, 0f, -0.09f), 0.004f);
            CrystalPartGeometry tail = m.Add("Tail", CrystalPartRole.Glow, 0, Vector3.zero);
            tail.Additive = true;
            tail.Shadows = false;
            Tail(tail.Mesh, SubStructure, 0.42f, 0.045f, -0.06f);
            return m;
        }

        private static CrystalModelGeometry BuildDiscShot()
        {
            CrystalModelGeometry m = new CrystalModelGeometry();
            m.Body.Additive = true;
            m.Body.Shadows = false;
            // A soft glow ring in the disc's plane, under the spinning disc.
            Vector2 a = new Vector2(0f, 0f), bb = new Vector2(1f, 0f), c = new Vector2(1f, 1f), d = new Vector2(0f, 1f);
            const float h = 0.16f;
            GlowCard(m.Body.Mesh, SubStructure, new Vector3(0f, -h, -h), new Vector3(0f, -h, h), new Vector3(0f, h, h), new Vector3(0f, h, -h), a, bb, c, d);
            CrystalPartGeometry disc = m.Add("Disc", CrystalPartRole.Disc, 0, Vector3.zero);
            disc.Axis = Vector3.right;
            disc.Shadows = false;
            GyreDisc(disc.Mesh, 0.095f, 0.0105f, 9);
            return m;
        }

        private static CrystalModelGeometry BuildOrbShot()
        {
            CrystalModelGeometry m = new CrystalModelGeometry();
            m.Body.Shadows = false;
            // The body itself is the rotating shell; it is a Core part so the rig tumbles it.
            m.Body.Additive = true;
            GlowStar(m.Body.Mesh, SubStructure, Vector3.zero, 0.3f);
            CrystalPartGeometry core = m.Add("Core", CrystalPartRole.Core, 0, Vector3.zero);
            core.Shadows = false;
            CrackedOrb(core.Mesh, SubStructure, SubCore, Vector3.zero, 0.14f, 4, 7, 0.16f, 0.82f);
            for (int i = 0; i < 3; i++)
            {
                float ang = i * 120f * Mathf.Deg2Rad;
                Vector3 off = Nrm(new Vector3(Mathf.Cos(ang), Mathf.Sin(ang) * 0.8f, Mathf.Sin(ang) * 0.35f)) * 0.22f;
                CrystalPartGeometry peb = m.Add("Pebble " + i, CrystalPartRole.Pebble, i, off);
                peb.Aux = Vector3.zero;
                peb.Shadows = false;
                Orb(peb.Mesh, SubCrystal, Vector3.zero, new Vector3(0.028f, 0.024f, 0.034f), 3, 4, QMath.AZ(i * 40f));
            }
            return m;
        }

        private static CrystalModelGeometry BuildChoirShard()
        {
            CrystalModelGeometry m = new CrystalModelGeometry();
            MeshBuilder b = m.Body.Mesh;
            m.Body.Shadows = false;
            // Two crossed blades and a bright core spine.
            Plate4(b, SubActive, SubActive, new Vector3(0f, 0f, -0.1f), new Vector3(0.026f, 0f, 0.0f), new Vector3(0f, 0f, 0.16f), new Vector3(-0.026f, 0f, 0.0f), 0.004f);
            Plate4(b, SubActive, SubActive, new Vector3(0f, 0f, -0.08f), new Vector3(0f, 0.016f, 0.01f), new Vector3(0f, 0f, 0.13f), new Vector3(0f, -0.016f, 0.01f), 0.004f);
            Shard(b, SubCore, new Vector3(0f, 0f, -0.06f), new Vector3(0f, 0f, 0.12f), 0.009f, 5, 0.45f, 18f);
            CrystalPartGeometry tail = m.Add("Tail", CrystalPartRole.Glow, 0, Vector3.zero);
            tail.Additive = true;
            tail.Shadows = false;
            Tail(tail.Mesh, SubStructure, 0.38f, 0.04f, -0.07f);
            return m;
        }

        /// <summary>The Mirrorwake focus marker as plain data: one mirror facet facing +Z over a soft glow card.</summary>
        public static CrystalModelGeometry BuildMarkerGeometry()
        {
            CrystalModelGeometry m = new CrystalModelGeometry();
            m.Body.Additive = true;
            m.Body.Shadows = false;
            Vector2 a = new Vector2(0f, 0f), bb = new Vector2(1f, 0f), c = new Vector2(1f, 1f), d = new Vector2(0f, 1f);
            const float h = 0.34f;
            GlowCard(m.Body.Mesh, SubStructure, new Vector3(-h, -h, -0.02f), new Vector3(h, -h, -0.02f), new Vector3(h, h, -0.02f), new Vector3(-h, h, -0.02f), a, bb, c, d);
            CrystalPartGeometry facet = m.Add("Facet", CrystalPartRole.Facet, 0, Vector3.zero);
            facet.Shadows = false;
            MirrorFacet(facet.Mesh, 0.15f, 0.21f, 0.1f, 0.014f);
            // A bright core stud at the facet's heart, so the focus still reads when seen edge-on.
            Gem(facet.Mesh, SubCore, new Vector3(0f, 0.01f, 0f), Vector3.forward, 0.022f, 0.016f, 6, 0f, Vector3.up);
            return m;
        }
    }
}
