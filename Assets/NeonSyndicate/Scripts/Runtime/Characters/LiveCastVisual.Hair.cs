using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// LiveCastVisual, hair, beards and headwear on study bodies (owner direction in chat 2026-10-23: swap NPCs and enemies
    /// for realistic bodies). The first fresh clone of a body measures its skull from the skin vertices weighted to the
    /// Head bone at bind (LiveSkullMeasure) and the kit (LiveHairKit) builds each style, beard and headpiece once on that
    /// skull. Every clone carries three slot renderers under its Head bone, aligned with the character frame, and each
    /// person who binds the body receives the meshes their look, role and (for the cast) costume ask for, with the colours
    /// in the same property blocks as the skin and coat tints. Nothing here touches the body's own renderers.
    /// </summary>
    public sealed partial class LiveCastVisual
    {
        private const string HairSlotName = "Live hair", BeardSlotName = "Live beard", HeadSlotName = "Live headwear";
        private LiveHairKit _kit;
        private MeshFilter _hairFilter, _beardFilter, _headFilter;
        private int _hairRenderer = -1, _beardRenderer = -1, _headRenderer = -1;
        private bool _headClothTextured;

        /// <summary>
        /// The kit of a prefab: measured once per session on the first fresh clone (at rest, before any pose copy). A head
        /// that cannot be measured leaves the body bare and says why once.
        /// </summary>
        private LiveHairKit EnsureKit(ImportedModel04OD model, Transform head, Transform chest)
        {
            LiveHairKit kit = LiveHairKits.Find(model.Prefab);
            if (kit != null) return kit;
            LiveSkullFit fit;
            try { fit = MeasureSkull(head, chest); }
            catch (Exception e)
            {
                fit = new LiveSkullFit();
                fit.Reason = e.Message;
            }
            kit = new LiveHairKit(model.Prefab, fit);
            LiveHairKits.Add(kit);
            if (fit.Valid)
                Log.Info("Live hair: " + Label + " skull measured (crown " + fit.Crown.ToString("0.000") + " m above the head bone, brows " + fit.BrowTop.ToString("0.000") + ", ears " + fit.EarBottom.ToString("0.000") + " to " + fit.EarTop.ToString("0.000") + ", chin " + fit.Chin.ToString("0.000") + "). Hair, beards and headwear are fitted to it.");
            else
                Log.Warn("Live hair: " + Label + " gets no hair, beard or headwear (" + fit.Reason + "). The body still draws.");
            return kit;
        }

        /// <summary>
        /// The skull in the head frame from the clone's skinned meshes: skin vertices weighted to the Head bone, the brows
        /// and eyes by their materials, and every coat, collar and armour vertex as cover. Positions go through the bind
        /// pose into head-bone space and then into the character's frame, so the result does not depend on how the
        /// imported bone happens to be rolled.
        /// </summary>
        private LiveSkullFit MeasureSkull(Transform head, Transform chest)
        {
            // Once per body kind and session, so plain lists are fine here.
            List<Vector3> skull = new List<Vector3>(2048), brows = new List<Vector3>(512), eyes = new List<Vector3>(512), cover = new List<Vector3>(8192);
            Quaternion align = Quaternion.Inverse(_visual.transform.rotation) * head.rotation;
            foreach (SkinnedMeshRenderer skin in _visual.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                Mesh mesh = skin.sharedMesh;
                if (mesh == null || !mesh.isReadable) continue;
                Material[] materials = skin.sharedMaterials;
                int subs = mesh.subMeshCount;
                LiveCrowdSurface[] surfaces = new LiveCrowdSurface[subs];
                bool any = false;
                for (int s = 0; s < subs; s++)
                {
                    surfaces[s] = s < materials.Length && materials[s] != null ? LiveCrowdRules.Surface(materials[s].name) : LiveCrowdSurface.Unknown;
                    if (surfaces[s] != LiveCrowdSurface.Unknown) any = true;
                }
                if (!any) continue;
                Transform[] bones = skin.bones;
                int headIndex = Array.IndexOf(bones, head);
                Matrix4x4[] bindposes = mesh.bindposes;
                Matrix4x4 toHead = headIndex >= 0 && headIndex < bindposes.Length ? bindposes[headIndex] : head.worldToLocalMatrix * skin.transform.localToWorldMatrix;
                Vector3[] vertices = mesh.vertices;
                float[] headWeight = headIndex >= 0 ? HeadWeights(mesh, headIndex, vertices.Length) : null;
                byte[] vertexSub = new byte[vertices.Length];
                for (int i = 0; i < vertexSub.Length; i++) vertexSub[i] = 255;
                for (int s = 0; s < subs && s < 255; s++)
                {
                    int[] triangles = mesh.GetTriangles(s);
                    for (int i = 0; i < triangles.Length; i++)
                        if (vertexSub[triangles[i]] == 255) vertexSub[triangles[i]] = (byte)s;
                }
                for (int i = 0; i < vertices.Length; i++)
                {
                    if (vertexSub[i] == 255) continue;
                    LiveCrowdSurface surface = surfaces[vertexSub[i]];
                    if (surface == LiveCrowdSurface.Unknown) continue;
                    float w = headWeight != null ? headWeight[i] : 0f;
                    Vector3 p = align * toHead.MultiplyPoint3x4(vertices[i]);
                    switch (surface)
                    {
                        case LiveCrowdSurface.Skin: if (w >= 0.5f) skull.Add(p); break;
                        case LiveCrowdSurface.Brows: if (w >= 0.3f) brows.Add(p); break;
                        case LiveCrowdSurface.Eyes: if (w >= 0.3f) eyes.Add(p); break;
                        case LiveCrowdSurface.Wool:
                        case LiveCrowdSurface.Leather:
                        case LiveCrowdSurface.Steel:
                        case LiveCrowdSurface.Bronze:
                            cover.Add(p);
                            break;
                    }
                }
            }
            return LiveSkullMeasure.Measure(skull, brows, eyes, cover, NeckRise(head, chest));
        }

        /// <summary>
        /// How much higher the head sits above the chest in play than at the study's bind: the procedural neck (chest to
        /// head at Height 1) minus the imported one. A coat weighted to the chest ends up that much lower on the head.
        /// </summary>
        private static float NeckRise(Transform head, Transform chest)
        {
            if (head == null || chest == null) return 0f;
            Appearance unit = new Appearance();
            unit.Height = 1f;
            unit.Bulk = 1f;
            RigLayout layout = RigLayout.From(unit);
            float procedural = layout.World[RigLayout.Head].y - layout.World[RigLayout.Chest].y;
            float imported = Vector3.Distance(head.position, chest.position);
            return Mathf.Clamp(procedural - imported, 0f, 0.12f);
        }

        /// <summary>The Head bone's share of every vertex (complete influences; the four-weight table when a mesh has no more).</summary>
        private static float[] HeadWeights(Mesh mesh, int headIndex, int vertexCount)
        {
            float[] weights = new float[vertexCount];
            byte[] counts = mesh.GetBonesPerVertex().ToArray();
            if (counts.Length == vertexCount)
            {
                BoneWeight1[] all = mesh.GetAllBoneWeights().ToArray();
                int cursor = 0;
                for (int i = 0; i < vertexCount; i++)
                {
                    int c = counts[i];
                    for (int j = 0; j < c && cursor + j < all.Length; j++)
                        if (all[cursor + j].boneIndex == headIndex) weights[i] += all[cursor + j].weight;
                    cursor += c;
                }
                return weights;
            }
            BoneWeight[] four = mesh.boneWeights;
            for (int i = 0; i < vertexCount && i < four.Length; i++)
            {
                BoneWeight w = four[i];
                if (w.boneIndex0 == headIndex) weights[i] += w.weight0;
                if (w.boneIndex1 == headIndex) weights[i] += w.weight1;
                if (w.boneIndex2 == headIndex) weights[i] += w.weight2;
                if (w.boneIndex3 == headIndex) weights[i] += w.weight3;
            }
            return weights;
        }

        /// <summary>
        /// Creates the three slot renderers under the clone's Head bone on a fresh clone, aligned with the character frame
        /// so the kit's head-frame meshes drop in unchanged. Must run before the renderers are cached, so the slots take
        /// part in presentation, tints and the hit flash like every other renderer of the body. False when the strand
        /// materials are unavailable (the body then draws bare).
        /// </summary>
        private bool CreateHeadSlots(Transform head)
        {
            Material[] hair = LiveHairMaterials.HairSet(), beard = LiveHairMaterials.BeardSet();
            if (hair == null || beard == null || head == null) return false;
            Quaternion toCharacter = Quaternion.Inverse(Quaternion.Inverse(_visual.transform.rotation) * head.rotation);
            Material cloth = null, metal = null, trim = null;
            foreach (Renderer r in _visual.GetComponentsInChildren<Renderer>(true))
                foreach (Material m in r.sharedMaterials)
                {
                    if (m == null) continue;
                    switch (LiveCrowdRules.Surface(m.name))
                    {
                        case LiveCrowdSurface.Wool: if (cloth == null) cloth = m; break;
                        case LiveCrowdSurface.Steel: if (metal == null) metal = m; break;
                        case LiveCrowdSurface.Bronze: if (trim == null) trim = m; break;
                    }
                }
            if (cloth == null) cloth = LiveHairMaterials.Base;
            if (metal == null) metal = Mats.DarkMetal;
            if (trim == null) trim = Mats.Metal;
            _headClothTextured = cloth.mainTexture != null;
            _hairFilter = Slot(head, HairSlotName, toCharacter, hair);
            _beardFilter = Slot(head, BeardSlotName, toCharacter, beard);
            _headFilter = Slot(head, HeadSlotName, toCharacter, new[] { cloth, metal, trim, trim });
            return true;
        }

        private MeshFilter Slot(Transform head, string name, Quaternion localRotation, Material[] materials)
        {
            GameObject go = new GameObject(name);
            go.layer = Layers.IgnoreRaycast;
            go.transform.SetParent(head, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = localRotation;
            go.transform.localScale = Vector3.one;
            MeshFilter filter = go.AddComponent<MeshFilter>();
            MeshRenderer renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = materials;
            renderer.enabled = false;
            renderer.receiveShadows = true;
            return filter;
        }

        /// <summary>Finds the slot filters on a pooled clone and their indices in the cached renderer table.</summary>
        private void FindHeadSlots()
        {
            _hairFilter = _beardFilter = _headFilter = null;
            _hairRenderer = _beardRenderer = _headRenderer = -1;
            if (_renderers == null) return;
            for (int r = 0; r < _renderers.Length; r++)
            {
                Renderer renderer = _renderers[r];
                if (renderer == null) continue;
                if (renderer.name == HairSlotName) { _hairRenderer = r; _hairFilter = renderer.GetComponent<MeshFilter>(); }
                else if (renderer.name == BeardSlotName) { _beardRenderer = r; _beardFilter = renderer.GetComponent<MeshFilter>(); }
                else if (renderer.name == HeadSlotName)
                {
                    _headRenderer = r;
                    _headFilter = renderer.GetComponent<MeshFilter>();
                    Material[] materials = renderer.sharedMaterials;
                    _headClothTextured = materials.Length > 0 && materials[0] != null && materials[0].mainTexture != null;
                }
            }
        }

        /// <summary>The surfaces of the slot materials (the body's surfaces come from the material names).</summary>
        private void SlotSurfaces(LiveCrowdSurface[] surfaces)
        {
            if (_hairRenderer >= 0) Set(surfaces, _hairRenderer, LiveCrowdSurface.Hair, LiveCrowdSurface.Hair, LiveCrowdSurface.HairTips, LiveCrowdSurface.Beard);
            if (_beardRenderer >= 0) Set(surfaces, _beardRenderer, LiveCrowdSurface.Beard, LiveCrowdSurface.Beard);
            if (_headRenderer >= 0) Set(surfaces, _headRenderer, LiveCrowdSurface.HeadCloth, LiveCrowdSurface.HeadMetal, LiveCrowdSurface.HeadTrim, LiveCrowdSurface.HeadGlow);
        }

        private void Set(LiveCrowdSurface[] surfaces, int renderer, params LiveCrowdSurface[] values)
        {
            int start = _materialStart[renderer], count = _materialCount[renderer];
            for (int m = 0; m < count && m < values.Length && start + m < surfaces.Length; m++) surfaces[start + m] = values[m];
        }

        /// <summary>
        /// Dresses one person's head: the style under the headpiece their role and look (or costume) ask for, the beard,
        /// the meshes from the kit, which slots show, and the colours and the visor glow in the tint and emission tables.
        /// A missing kit or material leaves the slots hidden.
        /// </summary>
        private void DressHead(NpcRole role, Appearance look, Color gangColor, bool hasGang, CastDef04E cast, LiveCrowdSurface[] surfaces, bool[] shown, Color[] tints, Color[] emission)
        {
            Mesh hairMesh = null, beardMesh = null, headMesh = null;
            LiveHeadTint tint = new LiveHeadTint();
            if (_kit != null && _kit.Fit != null && _kit.Fit.Valid)
            {
                LiveHeadpiece piece = cast != null ? LiveHairRules.CastHeadpiece(cast.Costume.Head, look) : LiveHairRules.Headpiece(role, look);
                LiveHairCover cover = LiveHairRules.Cover(piece);
                HairStyle style = LiveHairRules.Style(look, cover);
                LiveBeard beard = LiveHairRules.Beard(look);
                tint = cast != null ? LiveHairRules.CastColors(look, cast.Costume.Main, cast.Costume.Accent, piece) : LiveHairRules.Colors(role, look, gangColor, hasGang, piece);
                try
                {
                    hairMesh = _kit.Hair(style, piece);
                    beardMesh = _kit.Beard(beard);
                    headMesh = _kit.Headpiece(piece);
                }
                catch (Exception e)
                {
                    LiveCrowd.NoteHairProblem(Label, e.Message);
                    hairMesh = beardMesh = headMesh = null;
                }
            }
            Wear(_hairFilter, _hairRenderer, hairMesh, shown);
            Wear(_beardFilter, _beardRenderer, beardMesh, shown);
            Wear(_headFilter, _headRenderer, headMesh, shown);
            if (surfaces == null || tints == null) return;
            for (int i = 0; i < surfaces.Length && i < tints.Length; i++)
            {
                Color c;
                switch (surfaces[i])
                {
                    case LiveCrowdSurface.Hair: c = tint.Hair; break;
                    case LiveCrowdSurface.HairTips: c = tint.Tips; break;
                    case LiveCrowdSurface.Beard: c = tint.Beard; break;
                    // The coat's wool map is dark: the cloth colour is a multiplier that lands the map on it, as the coat's tint is.
                    case LiveCrowdSurface.HeadCloth: c = _headClothTextured ? LiveCrowdRules.ClothMultiplier(tint.HeadCloth) : tint.HeadCloth; break;
                    case LiveCrowdSurface.HeadMetal: c = tint.HeadMetal; break;
                    case LiveCrowdSurface.HeadTrim: c = tint.HeadTrim; break;
                    case LiveCrowdSurface.HeadGlow:
                        // Lit parts: a dark version of the glow colour under a faint emission (the visor slit, lenses, stones).
                        c = tint.HeadGlow == Color.black ? tint.HeadTrim : tint.HeadGlow * 0.6f;
                        if (emission != null && i < emission.Length) emission[i] = tint.HeadGlow;
                        break;
                    default: continue;
                }
                // The base cap sits a shade under the strands, so the volume reads.
                if (surfaces[i] == LiveCrowdSurface.Hair && _hairRenderer >= 0 && i == _materialStart[_hairRenderer]) c *= 0.8f;
                c.a = 1f;
                tints[i] = c;
            }
        }

        /// <summary>
        /// A named cast member on a study body (LiveCastRow.CastStudy): the cast palette from their procedural costume
        /// colours (LiveCrowdRules.CastTint), the study's armour hidden (a cast member is a plain role), and the head from
        /// their look and costume. Runs once per binding on the plain live path, where there is no pooled puppet.
        /// </summary>
        private void PrepareCastLook()
        {
            CastDef04E def = CastRoster04E.Find(_row.CastId);
            Appearance look = _rig.Look;
            Color main = def != null ? def.Costume.Main : new Color(0.2f, 0.22f, 0.26f), accent = def != null ? def.Costume.Accent : new Color(0.7f, 0.55f, 0.3f);
            LiveCrowdTint tint = LiveCrowdRules.CastTint(look, main, accent);
            FindHeadSlots();
            LiveCrowdSurface[] surfaces = Surfaces();
            SlotSurfaces(surfaces);
            bool[] armour = ArmourFlags();
            Color[] baseColor = BaseColors(_renderers, _materialStart, _emission.Length);
            int total = _emission.Length;
            _tints = new Color[total];
            for (int i = 0; i < total; i++)
            {
                Color c;
                switch (surfaces[i])
                {
                    case LiveCrowdSurface.Skin: c = tint.Skin; break;
                    case LiveCrowdSurface.Brows: c = tint.Hair; break;
                    case LiveCrowdSurface.Wool: c = tint.Cloth; break;
                    case LiveCrowdSurface.Leather: c = tint.Leather; break;
                    case LiveCrowdSurface.Bronze: c = tint.Bronze; break;
                    default: c = Color.white; break;
                }
                _tints[i] = Multiply(baseColor[i], c);
            }
            _partShown = new bool[_renderers.Length];
            for (int r = 0; r < _renderers.Length; r++) _partShown[r] = LiveCrowdRules.PartShown(armour[r], NpcRole.Civilian);
            DressHead(NpcRole.Civilian, look, Color.black, false, def, surfaces, _partShown, _tints, _emission);
        }

        /// <summary>
        /// Scales a study body to this person's height (LiveCrowdRules.HeightScale) from the clone at rest: the rig's head
        /// height over the clone's. Runs after the skull measurement and the bind offsets, which do not depend on it; the
        /// head slots and the skinned pieces scale with the root, and the bones stay pinned to the person's rig.
        /// </summary>
        private void ScaleToHeight(ImportedModel04OD model)
        {
            if (_rig == null || _rig.Layout == null || _visual == null || _target == null) return;
            Transform head = _target[RigLayout.Head];
            if (head == null) return;
            float study = (head.position.y - _visual.transform.position.y) / Mathf.Max(0.0001f, _visual.transform.lossyScale.y);
            float scale = LiveCrowdRules.HeightScale(_rig.Layout.World[RigLayout.Head].y, study);
            _visual.transform.localScale = Vector3.one * (model.Scale * scale);
        }

        private static void Wear(MeshFilter filter, int renderer, Mesh mesh, bool[] shown)
        {
            if (filter != null) filter.sharedMesh = mesh;
            if (shown != null && renderer >= 0 && renderer < shown.Length) shown[renderer] = mesh != null;
        }

        /// <summary>A material's own colour times a tint: textured materials are white and keep the tint, solid ones keep their colour under it.</summary>
        internal static Color Multiply(Color baseColor, Color tint)
        {
            return new Color(baseColor.r * tint.r, baseColor.g * tint.g, baseColor.b * tint.b, 1f);
        }

        /// <summary>Each material's own colour, read once per clone (the property block replaces it, so the tint must carry it).</summary>
        internal static Color[] BaseColors(Renderer[] renderers, int[] materialStart, int total)
        {
            Color[] colors = new Color[total];
            for (int i = 0; i < total; i++) colors[i] = Color.white;
            for (int r = 0; r < renderers.Length; r++)
            {
                if (renderers[r] == null) continue;
                Material[] materials = renderers[r].sharedMaterials;
                for (int m = 0; m < materials.Length && materialStart[r] + m < total; m++)
                {
                    Material material = materials[m];
                    if (material == null) continue;
                    // A material with a texture carries its look in the map; a solid material carries it in its colour.
                    bool textured = material.HasProperty("_MainTex") && material.mainTexture != null;
                    if (!textured && material.HasProperty("_BaseMap") && material.GetTexture("_BaseMap") != null) textured = true;
                    colors[materialStart[r] + m] = textured ? Color.white : material.color;
                }
            }
            return colors;
        }
    }
}
