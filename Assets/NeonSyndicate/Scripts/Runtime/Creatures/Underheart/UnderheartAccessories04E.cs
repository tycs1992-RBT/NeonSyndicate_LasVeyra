using UnityEngine;
using UnityEngine.Rendering;

namespace NeonSyndicate
{
    /// <summary>
    /// The live handles of one creature's 04E accessory pieces: the glowing renderers a role pulses as its tell, and the
    /// moving parts (the construct's weld arm, the sentinel's scanner fan and clamps). Glow goes through a property block
    /// on the shared materials (no material copies).
    /// </summary>
    public sealed class AccessoryRig04E : MonoBehaviour
    {
        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

        public MeshRenderer[] Glowing = new MeshRenderer[0];
        public Color GlowColor = Color.white;
        /// <summary>The construct's weld arm root (unfolds while mending).</summary>
        public Transform Arm;
        /// <summary>The tip of the weld arm or the sentinel's clamp emitter (beams and tethers start here).</summary>
        public Transform Tip;
        /// <summary>The sentinel's scanner lens (its weak point).</summary>
        public Transform Lens;
        /// <summary>The sentinel's scan fan (shown only while it sweeps).</summary>
        public Transform Fan;
        public MeshRenderer FanRenderer;

        private MaterialPropertyBlock _mpb;
        private float _sent = -1f;
        private float _fanSent = -1f;
        private Color _fanColor;

        /// <summary>Sets the glow of every glowing piece (1 is the resting glow; a tell ramps it up).</summary>
        public void SetGlow(float k)
        {
            if (Mathf.Abs(k - _sent) < 0.02f) return;
            _sent = k;
            if (_mpb == null) _mpb = new MaterialPropertyBlock();
            Color c = GlowColor * Mathf.Max(0f, k) * 2.2f;
            for (int i = 0; i < Glowing.Length; i++)
            {
                MeshRenderer r = Glowing[i];
                if (r == null) continue;
                r.GetPropertyBlock(_mpb);
                _mpb.SetColor(EmissionId, c);
                r.SetPropertyBlock(_mpb);
            }
        }

        /// <summary>Shows the scan fan with a colour and an opening angle (0 hides it).</summary>
        public void SetFan(bool on, Color color, float yawDegrees, float length)
        {
            if (Fan == null || FanRenderer == null) return;
            if (FanRenderer.enabled != on) FanRenderer.enabled = on;
            if (!on) return;
            Fan.localRotation = QMath.AY(yawDegrees);
            Fan.localScale = new Vector3(length, 1f, length);
            if (_fanColor == color && Mathf.Abs(_fanSent - length) < 0.01f) return;
            _fanColor = color;
            _fanSent = length;
            if (_mpb == null) _mpb = new MaterialPropertyBlock();
            FanRenderer.GetPropertyBlock(_mpb);
            _mpb.SetColor("_TintColor", color);
            _mpb.SetColor("_BaseColor", color);
            _mpb.SetColor("_Color", color);
            FanRenderer.SetPropertyBlock(_mpb);
        }
    }

    /// <summary>
    /// Procedural accessory pieces that give the 04E roles their own silhouettes on top of their bodies: the furnace
    /// caster's caged crucible and twin chimney stacks, the repair construct's jointed weld arm and coolant dome, the kiln
    /// crab's pincers, and the extraction sentinel's scanner hood and lens, clamp arms and harvest tank. Meshes and
    /// materials are built once per session and shared by every creature (MeshBuilder, two submeshes: metal and glow);
    /// each piece is a child of a bone, so it follows the pose. No colliders: the body's own hit volumes take the hits.
    /// </summary>
    public static class UnderheartAccessories04E
    {
        private static Mesh _crucible, _chimney, _weldArm, _dome, _pincer, _hood, _clamp, _tank, _fan;
        private static Material _metal, _darkMetal, _ember, _cyan, _amber, _fanMat;

        public static void ResetStatics()
        {
            _crucible = _chimney = _weldArm = _dome = _pincer = _hood = _clamp = _tank = _fan = null;
            _metal = _darkMetal = _ember = _cyan = _amber = _fanMat = null;
        }

        private static void EnsureMaterials()
        {
            if (_metal == null) _metal = Mats.Metal;
            if (_darkMetal == null) _darkMetal = Mats.DarkMetal;
            if (_ember == null) _ember = Mats.Emissive(new Color(1f, 0.42f, 0.1f), 2.2f);
            if (_cyan == null) _cyan = Mats.Emissive(new Color(0.3f, 0.9f, 1f), 2f);
            if (_amber == null) _amber = Mats.Emissive(new Color(1f, 0.72f, 0.2f), 2f);
            if (_fanMat == null) _fanMat = Mats.Additive("Sentinel Scan Fan", new Color(1f, 0.7f, 0.2f, 0.5f), UIKit.Circle);
        }

        // ================================================================== meshes

        private static Mesh Crucible
        {
            get
            {
                if (_crucible != null) return _crucible;
                MeshBuilder b = new MeshBuilder(2);
                b.FixedUV = new Vector2(0.5f, 0.5f);
                // A heavy ring base, eight cage bars around a glowing core, a lid ring.
                b.Cylinder(0, new Vector3(0f, 0f, 0f), 0.36f, 0.32f, 0.1f, 10, QMath.Identity, true, UVMode.Fixed);
                for (int i = 0; i < 8; i++)
                {
                    float a = i * Mathf.PI * 0.25f;
                    Vector3 o = new Vector3(Mathf.Cos(a) * 0.3f, 0.08f, Mathf.Sin(a) * 0.3f);
                    b.Box(0, o + Vector3.up * 0.22f, new Vector3(0.045f, 0.44f, 0.045f), QMath.AY(-a * Mathf.Rad2Deg), UVMode.Fixed);
                }
                b.Cylinder(0, new Vector3(0f, 0.5f, 0f), 0.33f, 0.22f, 0.08f, 10, QMath.Identity, true, UVMode.Fixed);
                b.Sphere(1, new Vector3(0f, 0.28f, 0f), new Vector3(0.22f, 0.22f, 0.22f), 6, 10, QMath.Identity);
                _crucible = b.ToMesh("Furnace Crucible");
                return _crucible;
            }
        }

        private static Mesh Chimney
        {
            get
            {
                if (_chimney != null) return _chimney;
                MeshBuilder b = new MeshBuilder(2);
                b.FixedUV = new Vector2(0.5f, 0.5f);
                b.Cylinder(0, Vector3.zero, 0.09f, 0.07f, 0.55f, 8, QMath.Identity, false, UVMode.Fixed);
                b.Cylinder(0, new Vector3(0f, 0.5f, 0f), 0.11f, 0.11f, 0.06f, 8, QMath.Identity, false, UVMode.Fixed);
                b.Cylinder(1, new Vector3(0f, 0.55f, 0f), 0.075f, 0.06f, 0.05f, 8, QMath.Identity, true, UVMode.Fixed);
                _chimney = b.ToMesh("Furnace Chimney");
                return _chimney;
            }
        }

        private static Mesh WeldArm
        {
            get
            {
                if (_weldArm != null) return _weldArm;
                MeshBuilder b = new MeshBuilder(2);
                b.FixedUV = new Vector2(0.5f, 0.5f);
                // Shoulder hub, upper boom forward and up, forearm forward and down, a nozzle with a glowing tip (local +Z).
                b.Sphere(0, Vector3.zero, new Vector3(0.09f, 0.09f, 0.09f), 4, 8, QMath.Identity);
                Vector3 elbow = new Vector3(0f, 0.22f, 0.26f), wrist = new Vector3(0f, 0.08f, 0.5f);
                Boom(b, Vector3.zero, elbow, 0.045f);
                b.Sphere(0, elbow, new Vector3(0.06f, 0.06f, 0.06f), 4, 8, QMath.Identity);
                Boom(b, elbow, wrist, 0.035f);
                b.Cylinder(0, wrist, 0.05f, 0.025f, 0.1f, 8, QMath.FromTo(Vector3.up, Vector3.forward), false, UVMode.Fixed);
                b.Sphere(1, wrist + Vector3.forward * 0.11f, new Vector3(0.035f, 0.035f, 0.035f), 4, 8, QMath.Identity);
                _weldArm = b.ToMesh("Weld Arm");
                return _weldArm;
            }
        }

        private static Mesh Dome
        {
            get
            {
                if (_dome != null) return _dome;
                MeshBuilder b = new MeshBuilder(2);
                b.FixedUV = new Vector2(0.5f, 0.5f);
                b.Sphere(0, Vector3.zero, new Vector3(0.24f, 0.14f, 0.24f), 5, 10, QMath.Identity);
                b.Cylinder(1, new Vector3(0f, -0.02f, 0f), 0.25f, 0.25f, 0.04f, 12, QMath.Identity, false, UVMode.Fixed);
                b.Cylinder(0, new Vector3(0f, 0.1f, 0f), 0.03f, 0.02f, 0.18f, 6, QMath.Identity, true, UVMode.Fixed);
                b.Sphere(1, new Vector3(0f, 0.3f, 0f), new Vector3(0.04f, 0.04f, 0.04f), 4, 6, QMath.Identity);
                _dome = b.ToMesh("Coolant Dome");
                return _dome;
            }
        }

        private static Mesh Pincer
        {
            get
            {
                if (_pincer != null) return _pincer;
                MeshBuilder b = new MeshBuilder(2);
                b.FixedUV = new Vector2(0.5f, 0.5f);
                // A thick palm and two curved jaws pointing forward (local +Z), the upper one larger.
                b.Sphere(0, Vector3.zero, new Vector3(0.09f, 0.07f, 0.11f), 4, 8, QMath.Identity);
                b.Wedge(0, new Vector3(0f, 0.03f, 0.13f), 0.09f, 0.2f, 0.06f, QMath.AX(-12f), UVMode.Fixed);
                b.Wedge(0, new Vector3(0f, -0.03f, 0.1f), 0.07f, 0.15f, 0.045f, QMath.AX(190f), UVMode.Fixed);
                b.Sphere(1, new Vector3(0f, 0.06f, 0.02f), new Vector3(0.025f, 0.02f, 0.04f), 3, 6, QMath.Identity);
                _pincer = b.ToMesh("Kiln Pincer");
                return _pincer;
            }
        }

        private static Mesh Hood
        {
            get
            {
                if (_hood != null) return _hood;
                MeshBuilder b = new MeshBuilder(2);
                b.FixedUV = new Vector2(0.5f, 0.5f);
                // A boxy hood over the head with a round lens in front (local +Z) and two sensor fins.
                b.Box(0, new Vector3(0f, 0.04f, 0.02f), new Vector3(0.3f, 0.2f, 0.3f), QMath.Identity, UVMode.Fixed);
                b.Box(0, new Vector3(0f, 0.17f, -0.02f), new Vector3(0.06f, 0.08f, 0.26f), QMath.Identity, UVMode.Fixed);
                b.Box(0, new Vector3(0.17f, 0.06f, -0.04f), new Vector3(0.04f, 0.14f, 0.18f), QMath.AZ(-12f), UVMode.Fixed);
                b.Box(0, new Vector3(-0.17f, 0.06f, -0.04f), new Vector3(0.04f, 0.14f, 0.18f), QMath.AZ(12f), UVMode.Fixed);
                b.Cylinder(0, new Vector3(0f, 0.03f, 0.17f), 0.085f, 0.085f, 0.04f, 12, QMath.FromTo(Vector3.up, Vector3.forward), false, UVMode.Fixed);
                b.Cylinder(1, new Vector3(0f, 0.03f, 0.205f), 0.07f, 0.05f, 0.02f, 12, QMath.FromTo(Vector3.up, Vector3.forward), true, UVMode.Fixed);
                _hood = b.ToMesh("Sentinel Hood");
                return _hood;
            }
        }

        private static Mesh Clamp
        {
            get
            {
                if (_clamp != null) return _clamp;
                MeshBuilder b = new MeshBuilder(2);
                b.FixedUV = new Vector2(0.5f, 0.5f);
                // A cuff around the forearm, a piston and two hooked clamp jaws reaching past the hand (local -Y is down the arm).
                b.Cylinder(0, new Vector3(0f, -0.05f, 0f), 0.085f, 0.075f, 0.18f, 8, QMath.AX(180f), true, UVMode.Fixed);
                b.Cylinder(0, new Vector3(0f, -0.2f, 0.06f), 0.025f, 0.025f, 0.2f, 6, QMath.AX(180f), true, UVMode.Fixed);
                b.Wedge(0, new Vector3(0.05f, -0.42f, 0.03f), 0.04f, 0.16f, 0.05f, QMath.AX(90f), UVMode.Fixed);
                b.Wedge(0, new Vector3(-0.05f, -0.42f, 0.03f), 0.04f, 0.16f, 0.05f, QMath.AX(90f), UVMode.Fixed);
                b.Sphere(1, new Vector3(0f, -0.12f, 0.08f), new Vector3(0.025f, 0.025f, 0.025f), 3, 6, QMath.Identity);
                _clamp = b.ToMesh("Sentinel Clamp");
                return _clamp;
            }
        }

        private static Mesh Tank
        {
            get
            {
                if (_tank != null) return _tank;
                MeshBuilder b = new MeshBuilder(2);
                b.FixedUV = new Vector2(0.5f, 0.5f);
                // An upright harvest tank on the back with a glowing amber window band and two feed pipes.
                b.Cylinder(0, new Vector3(0f, -0.25f, 0f), 0.17f, 0.17f, 0.5f, 10, QMath.Identity, true, UVMode.Fixed);
                b.Cylinder(1, new Vector3(0f, -0.08f, 0f), 0.175f, 0.175f, 0.14f, 10, QMath.Identity, false, UVMode.Fixed);
                b.Cylinder(0, new Vector3(0.1f, 0.25f, 0.05f), 0.025f, 0.025f, 0.22f, 6, QMath.AX(35f), false, UVMode.Fixed);
                b.Cylinder(0, new Vector3(-0.1f, 0.25f, 0.05f), 0.025f, 0.025f, 0.22f, 6, QMath.AX(35f), false, UVMode.Fixed);
                _tank = b.ToMesh("Sentinel Tank");
                return _tank;
            }
        }

        /// <summary>A flat fan of 50 degrees along +Z, unit length (scaled by range), UV centre at the apex.</summary>
        private static Mesh Fan
        {
            get
            {
                if (_fan != null) return _fan;
                MeshBuilder b = new MeshBuilder(1);
                const int n = 10;
                for (int i = 0; i < n; i++)
                {
                    float a0 = Mathf.Lerp(-25f, 25f, i / (float)n) * Mathf.Deg2Rad;
                    float a1 = Mathf.Lerp(-25f, 25f, (i + 1) / (float)n) * Mathf.Deg2Rad;
                    Vector3 p0 = new Vector3(Mathf.Sin(a0), 0f, Mathf.Cos(a0));
                    Vector3 p1 = new Vector3(Mathf.Sin(a1), 0f, Mathf.Cos(a1));
                    Vector2 c = new Vector2(0.5f, 0.5f);
                    Vector2 u0 = new Vector2(0.5f + p0.x * 0.5f, 0.5f + p0.z * 0.5f), u1 = new Vector2(0.5f + p1.x * 0.5f, 0.5f + p1.z * 0.5f);
                    b.Triangle(0, Vector3.zero, p1, p0, Vector3.up, c, u1, u0);
                    b.Triangle(0, Vector3.zero, p0, p1, Vector3.down, c, u0, u1);
                }
                _fan = b.ToMesh("Scan Fan");
                return _fan;
            }
        }

        private static void Boom(MeshBuilder b, Vector3 from, Vector3 to, float radius)
        {
            Vector3 d = to - from;
            float len = d.magnitude;
            if (len < 1e-4f) return;
            b.Cylinder(0, from, radius, radius * 0.85f, len, 6, QMath.FromTo(Vector3.up, d / len), false, UVMode.Fixed);
        }

        // ================================================================== attaching

        private static MeshRenderer Piece(string name, Mesh mesh, Material body, Material glow, Transform parent, Vector3 localPos, Quaternion localRot, float scale)
        {
            GameObject go = Shapes.MeshObject(name, mesh, new[] { body, glow }, parent, false);
            go.layer = parent.gameObject.layer;
            go.transform.localPosition = localPos;
            go.transform.localRotation = localRot;
            go.transform.localScale = Vector3.one * scale;
            MeshRenderer r = go.GetComponent<MeshRenderer>();
            r.shadowCastingMode = ShadowCastingMode.Off;
            return r;
        }

        private static Transform Bone(Creature c, int index)
        {
            if (c == null || c.CRig == null || index < 0 || index >= c.CRig.Bones.Length) return null;
            return c.CRig.Bones[index];
        }

        /// <summary>The furnace caster's crucible (on its back) and two chimney stacks behind it.</summary>
        public static AccessoryRig04E AttachCaster(Creature c)
        {
            if (c == null || c.CRig == null) return null;
            EnsureMaterials();
            CreatureSkeleton sk = c.CRig.Body.Skeleton;
            Transform chest = Bone(c, sk.Chest) ?? c.transform;
            Transform back = Bone(c, sk.Spine.Length > 0 ? sk.Spine[sk.Spine.Length / 2] : sk.Pelvis) ?? chest;
            float h = Mathf.Max(0.4f, c.Size);
            AccessoryRig04E rig = c.gameObject.AddComponent<AccessoryRig04E>();
            rig.GlowColor = new Color(1f, 0.42f, 0.1f);
            MeshRenderer crucible = Piece("Crucible", Crucible, _darkMetal, _ember, back, new Vector3(0f, 0.2f * h, 0f), QMath.Identity, h * 0.95f);
            MeshRenderer stackL = Piece("Chimney L", Chimney, _darkMetal, _ember, back, new Vector3(0.18f * h, 0.16f * h, -0.25f * h), QMath.AX(-18f), h * 0.9f);
            MeshRenderer stackR = Piece("Chimney R", Chimney, _darkMetal, _ember, back, new Vector3(-0.18f * h, 0.16f * h, -0.25f * h), QMath.AX(-18f), h * 0.9f);
            rig.Glowing = new[] { crucible, stackL, stackR };
            rig.Tip = crucible.transform;
            rig.SetGlow(1f);
            return rig;
        }

        /// <summary>The repair construct's weld arm (folded under the chest) and coolant dome on its back.</summary>
        public static AccessoryRig04E AttachConstruct(Creature c)
        {
            if (c == null || c.CRig == null) return null;
            EnsureMaterials();
            CreatureSkeleton sk = c.CRig.Body.Skeleton;
            Transform chest = Bone(c, sk.Chest) ?? c.transform;
            Transform back = Bone(c, sk.Pelvis) ?? chest;
            float h = Mathf.Max(0.4f, c.Size);
            AccessoryRig04E rig = c.gameObject.AddComponent<AccessoryRig04E>();
            rig.GlowColor = new Color(0.3f, 0.9f, 1f);
            MeshRenderer arm = Piece("Weld Arm", WeldArm, _metal, _cyan, chest, new Vector3(0.08f * h, -0.08f * h, 0.12f * h), QMath.AX(30f), h * 0.85f);
            MeshRenderer dome = Piece("Coolant Dome", Dome, _metal, _cyan, back, new Vector3(0f, 0.16f * h, 0f), QMath.Identity, h * 0.9f);
            GameObject tip = new GameObject("Weld Tip");
            tip.transform.SetParent(arm.transform, false);
            tip.transform.localPosition = new Vector3(0f, 0.08f, 0.62f);
            rig.Arm = arm.transform;
            rig.Tip = tip.transform;
            rig.Glowing = new[] { arm, dome };
            rig.SetGlow(1f);
            return rig;
        }

        /// <summary>The kiln crab's two pincers on the lower bones of its front legs.</summary>
        public static AccessoryRig04E AttachCrab(Creature c)
        {
            if (c == null || c.CRig == null) return null;
            EnsureMaterials();
            CreatureSkeleton sk = c.CRig.Body.Skeleton;
            float h = Mathf.Max(0.25f, c.Size);
            AccessoryRig04E rig = c.gameObject.AddComponent<AccessoryRig04E>();
            rig.GlowColor = new Color(1f, 0.55f, 0.15f);
            System.Collections.Generic.List<MeshRenderer> glow = new System.Collections.Generic.List<MeshRenderer>();
            for (int i = 0; i < sk.Legs.Length; i++)
            {
                LegChain l = sk.Legs[i];
                if (!l.Front) continue;
                Transform lower = Bone(c, l.Lower);
                if (lower == null) continue;
                float side = l.Side < 0 ? -1f : 1f;
                glow.Add(Piece(side < 0f ? "Pincer L" : "Pincer R", Pincer, _darkMetal, _amber, lower, new Vector3(0.05f * side * h, 0f, 0.12f * h), QMath.AY(side * -14f), h * 2.6f));
            }
            rig.Glowing = glow.ToArray();
            rig.SetGlow(0.6f);
            return rig;
        }

        /// <summary>The extraction sentinel's scanner hood and lens, the scan fan, clamp arms on both forearms and the tank.</summary>
        public static AccessoryRig04E AttachSentinel(NpcController n)
        {
            if (n == null || n.Rig == null) return null;
            EnsureMaterials();
            HumanoidRig r = n.Rig;
            float s = Mathf.Max(0.5f, r.Scale);
            AccessoryRig04E rig = n.gameObject.AddComponent<AccessoryRig04E>();
            rig.GlowColor = new Color(1f, 0.72f, 0.2f);
            System.Collections.Generic.List<MeshRenderer> glow = new System.Collections.Generic.List<MeshRenderer>();
            if (r.Head != null)
            {
                MeshRenderer hood = Piece("Scanner Hood", Hood, _darkMetal, _amber, r.Head, new Vector3(0f, 0.06f, 0.02f), QMath.Identity, s);
                glow.Add(hood);
                GameObject lens = new GameObject("Scanner Lens");
                lens.transform.SetParent(hood.transform, false);
                lens.transform.localPosition = new Vector3(0f, 0.03f, 0.22f);
                rig.Lens = lens.transform;
                GameObject fan = Shapes.MeshObject("Scan Fan", Fan, new[] { _fanMat }, lens.transform, false);
                fan.layer = n.gameObject.layer;
                MeshRenderer fr = fan.GetComponent<MeshRenderer>();
                fr.shadowCastingMode = ShadowCastingMode.Off;
                fr.receiveShadows = false;
                fr.enabled = false;
                rig.Fan = fan.transform;
                rig.FanRenderer = fr;
                rig.Tip = lens.transform;
            }
            if (r.ForearmL != null) glow.Add(Piece("Clamp L", Clamp, _metal, _amber, r.ForearmL, Vector3.zero, QMath.Identity, s));
            if (r.ForearmR != null) glow.Add(Piece("Clamp R", Clamp, _metal, _amber, r.ForearmR, Vector3.zero, QMath.Identity, s));
            if (r.Chest != null) glow.Add(Piece("Harvest Tank", Tank, _darkMetal, _amber, r.Chest, new Vector3(0f, 0.08f, -0.24f), QMath.Identity, s));
            rig.Glowing = glow.ToArray();
            rig.SetGlow(1f);
            return rig;
        }
    }
}
