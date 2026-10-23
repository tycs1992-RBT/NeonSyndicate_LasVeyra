#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace NeonSyndicate.Editor
{
    /// <summary>
    /// Explicit, reversible VISUAL PREVIEW of the H8S Forge gallery furnishings in the real Forge. In Play mode it finds the
    /// one original "Forge (Dante Rook's forge)" station, fits the delivered Anvil inside that station's own BoxCollider
    /// (every rendered corner keeps ForgeGalleryFitH8S.BoxMargin from every face) and hides only the four primitive anvil
    /// renderers the new visual replaces. It places the Workbench and BookStand on the real Forge room floor where
    /// GalleryPlanH8S and the physics probes find them clear of colliders, interaction radii and the entrance sight lines.
    /// Every new object sits on IgnoreRaycast with no collider, rigidbody or script; the original collider, station,
    /// interaction, layers and services are never touched. End, a failure part-way, leaving Play mode, closing the window
    /// or an assembly reload restores the recorded renderer states and removes every new object and material.
    /// </summary>
    public sealed class ForgeGalleryReviewH8S : EditorWindow
    {
        private const string PropRoot = "Assets/NeonSyndicate/Art/ProductionH8S/Props/";
        private const string StationName = "Forge (Dante Rook's forge)";
        private const string PreviewName = "H8S VISUAL PREVIEW Forge gallery furnishings (reversible, no collision)";
        private const int MaxPhysicalTries = 60;
        private const float LargeColliderArea = 30f;

        [Serializable] private sealed class Receipt { public string license, creator; public bool commercialUse, publicSourceApproved, enabled; public Entry[] files; }
        [Serializable] private sealed class Entry { public string path, sha256; public long bytes; }

        private GameObject _preview;
        private BoxCollider _station;
        private CityLayout _layout;
        private readonly List<Renderer> _hidden = new List<Renderer>();
        private readonly List<bool> _hiddenWasEnabled = new List<bool>();
        private readonly List<Material> _materials = new List<Material>();
        private readonly Vector3[] _corners = new Vector3[8];
        private string _report = "VISUAL PREVIEW only, native Unity acceptance NOT RUN. Use Neon Syndicate > 04OH > H8S Forge gallery furnishings review in Play mode.";
        private Vector2 _scroll;

        /// <summary>Opens the review window and starts the preview (in Edit mode it only verifies the receipt and prepares texture import settings).</summary>
        [MenuItem("Neon Syndicate/04OH/H8S Forge gallery furnishings review", false, 3200)]
        public static void Open()
        {
            ForgeGalleryReviewH8S window = GetWindow<ForgeGalleryReviewH8S>("H8S Forge gallery");
            window.Begin();
        }

        /// <summary>Ends every running Forge gallery preview and restores the original Forge art.</summary>
        [MenuItem("Neon Syndicate/04OH/H8S End Forge gallery furnishings review", false, 3201)]
        public static void EndFromMenu()
        {
            ForgeGalleryReviewH8S[] open = UnityEngine.Resources.FindObjectsOfTypeAll<ForgeGalleryReviewH8S>();
            if (open.Length == 0) Log.Info("H8S Forge gallery review: nothing is running.");
            foreach (ForgeGalleryReviewH8S window in open)
                window.Stop("Ended from the menu. The original Forge art is restored and every preview object is removed.");
        }

        private void OnEnable()
        {
            EditorApplication.playModeStateChanged += PlayState;
            AssemblyReloadEvents.beforeAssemblyReload += BeforeReload;
            EditorApplication.update += CheckSession;
        }

        private void OnDisable()
        {
            Stop("The review window closed. The original Forge art is restored.");
            EditorApplication.playModeStateChanged -= PlayState;
            AssemblyReloadEvents.beforeAssemblyReload -= BeforeReload;
            EditorApplication.update -= CheckSession;
        }

        private void PlayState(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode || state == PlayModeStateChange.ExitingEditMode)
                Stop("Play mode is changing. The original Forge art is restored.");
        }

        private void BeforeReload()
        {
            Stop("Scripts are reloading. The original Forge art is restored.");
        }

        private void CheckSession()
        {
            if (_preview == null && _hidden.Count == 0 && _materials.Count == 0) return;
            if (!EditorApplication.isPlaying || _station == null || _preview == null || Game.Layout != _layout)
                Stop("The Forge station or the world changed, so the preview ended and the original art was restored.");
        }

        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            EditorGUILayout.HelpBox("VISUAL PREVIEW of the delivered H8S Forge furnishings, disabled and unapproved. The original Forge collider, station, E/Y interaction, services, quests and saves stay authoritative. New visuals have no collision and sit on IgnoreRaycast. Appearance, camera and feel need your check in Unity.", MessageType.Info);
            using (new EditorGUI.DisabledScope(!EditorApplication.isPlaying))
                if (GUILayout.Button("Start or restart the Forge gallery preview")) Begin();
            if (GUILayout.Button("End and restore the original Forge art")) Stop("Ended from the window. The original Forge art is restored.");
            EditorGUILayout.TextArea(_report, GUILayout.MinHeight(260));
            EditorGUILayout.EndScrollView();
        }

        private void Begin()
        {
            Stop(null);
            StringBuilder log = new StringBuilder();
            try
            {
                ReadReceipt();
                PrepareTextures(log);
                if (!EditorApplication.isPlaying)
                {
                    _report = "Edit mode: the 11 receipt hashes match and the texture import settings are ready." + log +
                        "\nEnter Play mode, reach the Forge in any world (revision 10 Gothic Civic H5 for the route), then run this menu again to place the VISUAL PREVIEW.";
                    return;
                }
                if (Game.Layout == null || Game.Crafting == null) throw new InvalidOperationException("Start a game first: the world and the Forge are not built yet.");
                _station = FindStation();
                _layout = Game.Layout;
                _preview = new GameObject(PreviewName);
                PlaceAnvil(log);
                PlaceGallery(log);
                Layers.SetRecursive(_preview, Layers.IgnoreRaycast);
                if (_preview.GetComponentsInChildren<Collider>(true).Length != 0 || _preview.GetComponentsInChildren<Rigidbody>(true).Length != 0 ||
                    _preview.GetComponentsInChildren<MonoBehaviour>(true).Length != 0)
                    throw new InvalidOperationException("A preview object gained physics or a script; refused.");
                _report = "VISUAL PREVIEW RUNNING in the real Forge (world revision " + (int)_layout.Revision04O + "). No collider, station, interaction, layer, quest or save was changed." + log +
                    "\n\nCheck in Unity (NOT RUN here): the anvil sits inside the old collider from every side; E or gamepad Y still opens Dante Rook's Forge at the same distance; the camera passes the new pieces without bumping; the furnishings read well and do not hide Dante from the door; frame rate. Use the End menu or this window to restore.";
                Log.Info("H8S Forge gallery review started (VISUAL PREVIEW):" + log);
            }
            catch (Exception e)
            {
                Stop(null);
                _report = "Review refused safely and everything was restored: " + e.Message + (log.Length > 0 ? "\nProgress before the refusal:" + log : "");
                Log.Warn("H8S Forge gallery review refused: " + e.Message);
            }
            Repaint();
        }

        private void Stop(string why)
        {
            bool active = _preview != null || _hidden.Count > 0 || _materials.Count > 0;
            for (int i = 0; i < _hidden.Count; i++)
                if (_hidden[i] != null) _hidden[i].enabled = _hiddenWasEnabled[i];
            _hidden.Clear();
            _hiddenWasEnabled.Clear();
            if (_preview != null) DestroyImmediate(_preview);
            _preview = null;
            for (int i = 0; i < _materials.Count; i++)
                if (_materials[i] != null) DestroyImmediate(_materials[i]);
            _materials.Clear();
            _station = null;
            _layout = null;
            if (!active || why == null) return;
            _report = why + "\n\nLast preview:\n" + _report;
            Log.Info("H8S Forge gallery review: " + why);
            Repaint();
        }

        // ------------------------------------------------------------------ receipt, textures, materials, models

        private static string ProjectPath(string assetPath)
        {
            return Path.Combine(Directory.GetParent(Application.dataPath).FullName, assetPath);
        }

        private static void ReadReceipt()
        {
            string path = PropRoot + "candidate.json";
            if (!File.Exists(ProjectPath(path))) throw new FileNotFoundException("Missing the props receipt: " + path);
            Receipt r = JsonUtility.FromJson<Receipt>(File.ReadAllText(ProjectPath(path)));
            if (r == null || r.license != "CC0-1.0" || !r.commercialUse || !r.publicSourceApproved || r.enabled || r.files == null || r.files.Length != 11)
                throw new InvalidDataException("The props receipt does not record the 11 admitted CC0 files with enabled false.");
            foreach (Entry e in r.files)
                if (e == null || string.IsNullOrEmpty(e.path) || !e.path.StartsWith(PropRoot, StringComparison.Ordinal) || !ImportedArt04OH.Hex(e.sha256, 64) ||
                    ImportedArtProof04OH.FileHash(e.path) != e.sha256)
                    throw new InvalidDataException("A delivered prop file differs from its receipt (an LFS pointer instead of the real file?): " + (e != null ? e.path : "missing entry"));
        }

        private static void PrepareTextures(StringBuilder log)
        {
            for (int i = 0; i < 2; i++)
            {
                string role = i == 0 ? "Metal" : "Furniture";
                PropTexture(role + "_BaseColor.png", false, false, log);
                PropTexture(role + "_Normal.png", true, true, log);
                PropTexture(role + "_MetallicSmoothness.png", false, true, log);
            }
        }

        /// <summary>Loads a prop texture after making its importer match the delivery: base colour sRGB, normal maps and packed channels linear.</summary>
        private static Texture2D PropTexture(string file, bool normal, bool linear, StringBuilder log)
        {
            string path = PropRoot + file;
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new FileNotFoundException("Wait for the actual texture import, then retry: " + path);
            TextureImporterType kind = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            bool srgb = !normal && !linear;
            if (importer.textureType != kind || importer.sRGBTexture != srgb)
            {
                importer.textureType = kind;
                importer.sRGBTexture = srgb;
                importer.SaveAndReimport();
                if (log != null) log.Append("\nTexture import setting corrected in its .meta: ").Append(file).Append(normal ? " as a normal map" : srgb ? " as sRGB colour" : " as linear packed data");
            }
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture == null) throw new InvalidDataException("The texture did not import: " + path);
            return texture;
        }

        /// <summary>One runtime material per texture set through the original Lit shader owner (Built-in Standard or URP Lit). Never saved as an asset.</summary>
        private Material Surface(string role)
        {
            string name = "H8S VISUAL PREVIEW " + role;
            for (int i = 0; i < _materials.Count; i++)
                if (_materials[i] != null && _materials[i].name == name) return _materials[i];
            Shader shader = Mats.StandardShader;
            if (shader == null) throw new InvalidOperationException("The original supported Lit shader is unavailable.");
            bool urp = RenderPath.IsURP;
            Material m = new Material(shader) { name = name };
            _materials.Add(m);
            m.SetColor(urp ? "_BaseColor" : "_Color", Color.white);
            m.SetTexture(urp ? "_BaseMap" : "_MainTex", PropTexture(role + "_BaseColor.png", false, false, null));
            m.SetTexture("_BumpMap", PropTexture(role + "_Normal.png", true, true, null));
            m.SetFloat("_BumpScale", 1f);
            m.EnableKeyword("_NORMALMAP");
            // Packed delivery: metallic in R (from ORM blue), smoothness in A (255 minus ORM green). Occlusion is not bound:
            // both pipelines read occlusion from green, which holds roughness in the delivered ORM.
            m.SetTexture("_MetallicGlossMap", PropTexture(role + "_MetallicSmoothness.png", false, true, null));
            m.EnableKeyword(urp ? "_METALLICSPECGLOSSMAP" : "_METALLICGLOSSMAP");
            m.SetFloat("_Metallic", 1f);
            m.SetFloat("_SmoothnessTextureChannel", 0f);
            if (urp) m.SetFloat("_Smoothness", 1f);
            else
            {
                m.SetFloat("_GlossMapScale", 1f);
                m.SetFloat("_Glossiness", 1f);
            }
            return m;
        }

        private static string RoleOf(string material)
        {
            if (material.IndexOf("Furniture", StringComparison.Ordinal) >= 0) return "Furniture";
            if (material.IndexOf("Metal", StringComparison.Ordinal) >= 0) return "Metal";
            throw new InvalidDataException("Unrecorded prop material \"" + material + "\"; refused.");
        }

        /// <summary>A visual-only copy of a delivered prop under a new holder, with the preview materials and no animation, physics or scripts.</summary>
        private GameObject Prop(string name, out GameObject holder)
        {
            string path = PropRoot + name + ".fbx";
            ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null) throw new FileNotFoundException("Wait for the actual model import, then retry: " + path);
            if (importer.addCollider) throw new InvalidDataException(name + ".fbx imports with generated colliders; the preview must add no collision.");
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (source == null) throw new InvalidDataException("The model did not import: " + path);
            foreach (Component c in source.GetComponentsInChildren<Component>(true))
                if (c == null || !ImportedArt04OH.VisualComponent(c.GetType())) throw new InvalidDataException(name + ".fbx carries a non-visual component; refused.");
            holder = new GameObject("VISUAL PREVIEW " + name);
            holder.transform.SetParent(_preview.transform, false);
            GameObject visual = (GameObject)Instantiate(source, holder.transform, false);
            visual.name = name + " (delivered H8S prop, not approved)";
            foreach (Animator animator in visual.GetComponentsInChildren<Animator>(true))
            {
                animator.enabled = false;
                animator.applyRootMotion = false;
            }
            foreach (Renderer renderer in visual.GetComponentsInChildren<Renderer>(true))
            {
                Material[] slots = renderer.sharedMaterials;
                for (int i = 0; i < slots.Length; i++) slots[i] = Surface(RoleOf(slots[i] != null ? slots[i].name : ""));
                renderer.sharedMaterials = slots;
            }
            Layers.SetRecursive(holder, Layers.IgnoreRaycast);
            return visual;
        }

        /// <summary>The rendered bounds (eight mesh-bound corners per mesh) of everything under root, in frame's local space.</summary>
        private bool LocalBounds(GameObject root, Transform frame, out Vector3 min, out Vector3 max)
        {
            min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            bool any = false;
            foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh == null) continue;
                Bounds b = filter.sharedMesh.bounds;
                ForgeGalleryFitH8S.Corners(b.min, b.max, _corners);
                for (int i = 0; i < 8; i++)
                {
                    Vector3 p = frame.InverseTransformPoint(filter.transform.TransformPoint(_corners[i]));
                    min = Vector3.Min(min, p);
                    max = Vector3.Max(max, p);
                    any = true;
                }
            }
            return any;
        }

        private List<Vector3> RenderedCorners(GameObject root, Transform frame)
        {
            List<Vector3> corners = new List<Vector3>();
            foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh == null) continue;
                Bounds b = filter.sharedMesh.bounds;
                ForgeGalleryFitH8S.Corners(b.min, b.max, _corners);
                for (int i = 0; i < 8; i++) corners.Add(frame.InverseTransformPoint(filter.transform.TransformPoint(_corners[i])));
            }
            return corners;
        }

        // ------------------------------------------------------------------ the original station and the anvil

        private static BoxCollider FindStation()
        {
            List<BoxCollider> named = new List<BoxCollider>();
            foreach (BoxCollider c in UnityEngine.Object.FindObjectsByType<BoxCollider>(FindObjectsSortMode.None))
                if (c != null && c.gameObject.name == StationName) named.Add(c);
            if (named.Count == 0)
                throw new InvalidOperationException("The original \"" + StationName + "\" station with its BoxCollider is not in this world (Dante Rook's forge was not placed yet). Nothing was changed.");
            if (named.Count > 1)
                throw new InvalidOperationException("Found " + named.Count + " objects named \"" + StationName + "\" with a BoxCollider, so the original station is ambiguous. Nothing was changed.");
            BoxCollider station = named[0];
            if (Game.Crafting == null || !station.transform.IsChildOf(Game.Crafting.transform))
                throw new InvalidOperationException("The \"" + StationName + "\" collider is not owned by the Forge (CraftingSystem). Nothing was changed.");
            if (!station.enabled || station.isTrigger) throw new InvalidOperationException("The original station collider is not an enabled solid collider. Nothing was changed.");
            Vector3 s = station.transform.lossyScale;
            if (s.x <= 0f || Mathf.Abs(s.x - s.y) > 1e-3f || Mathf.Abs(s.x - s.z) > 1e-3f)
                throw new InvalidOperationException("The original station is not uniformly scaled, so a fitted copy cannot be proved inside it. Nothing was changed.");
            return station;
        }

        /// <summary>The one direct child of the station with this name and its renderer; null when optional and absent.</summary>
        private static Renderer Piece(Transform station, string name, bool required)
        {
            Renderer found = null;
            for (int i = 0; i < station.childCount; i++)
            {
                Transform t = station.GetChild(i);
                if (t.name != name) continue;
                if (found != null) throw new InvalidOperationException("The original Forge art has two \"" + name + "\" pieces; refused.");
                found = t.GetComponent<Renderer>();
                if (found == null) throw new InvalidOperationException("The original Forge piece \"" + name + "\" has no renderer; refused.");
            }
            if (found == null && required)
                throw new InvalidOperationException("The original Forge anvil piece \"" + name + "\" is missing, so the replacement cannot be matched safely. Nothing was changed.");
            return found;
        }

        private void PlaceAnvil(StringBuilder log)
        {
            Transform frame = _station.transform;
            Renderer block = Piece(frame, "Block", true);
            Renderer[] replaced = { Piece(frame, "Waist", true), Piece(frame, "Face", true), Piece(frame, "Horn", true), Piece(frame, "Glow", true) };
            Renderer bowl = Piece(frame, "Bowl", false);
            Renderer embers = Piece(frame, "Embers", false);
            Vector3 blockMin, blockMax;
            if (!LocalBounds(block.gameObject, frame, out blockMin, out blockMax)) throw new InvalidOperationException("The original support block has no mesh.");
            float supportTop = blockMax.y;
            Vector2 supportCenter = new Vector2((blockMin.x + blockMax.x) * 0.5f, (blockMin.z + blockMax.z) * 0.5f);

            GameObject holder;
            GameObject visual = Prop("Anvil", out holder);
            float stationScale = frame.lossyScale.x;
            holder.transform.SetPositionAndRotation(frame.position, frame.rotation);
            holder.transform.localScale = Vector3.one * stationScale;
            Vector3 localMin, localMax;
            if (!LocalBounds(visual, frame, out localMin, out localMax)) throw new InvalidDataException("The Anvil has no rendered mesh.");
            Vector3 native = localMax - localMin, source = ForgeGalleryFitH8S.SourceSize("Anvil");
            if (!ForgeGalleryFitH8S.NativeSizeMatches(native, source, ForgeGalleryFitH8S.NativeSizeTolerance))
                throw new InvalidDataException("Native scale check failed: the Anvil imported at " + native.ToString("F3") + " m but the delivered source is " + source.ToString("F3") +
                    " m. Check the model's Scale Factor and Convert Units. Nothing was hidden.");

            ForgeFitH8S fit = ForgeGalleryFitH8S.Fit(localMin, localMax, _station.center, _station.size, supportTop, supportCenter, ForgeGalleryFitH8S.BoxMargin,
                ForgeGalleryFitH8S.MinAnvilScale, ForgeGalleryFitH8S.MaxAnvilScale, ForgeGalleryFitH8S.MaxSlide, true);
            if (!fit.Ok) throw new InvalidOperationException("The Anvil cannot fit inside the original Forge collider: " + fit.Reason + " Nothing was hidden.");
            KeepClear(bowl, fit, frame);
            KeepClear(embers, fit, frame);

            holder.transform.SetPositionAndRotation(frame.TransformPoint(fit.Pivot), frame.rotation * Quaternion.Euler(0f, 90f * fit.QuarterTurns, 0f));
            holder.transform.localScale = Vector3.one * (stationScale * fit.Scale);
            List<Vector3> corners = RenderedCorners(visual, frame);
            int outside = ForgeGalleryFitH8S.FirstOutside(corners, _station.center, _station.size, ForgeGalleryFitH8S.BoxMargin - 0.0005f);
            if (corners.Count == 0 || outside >= 0)
                throw new InvalidOperationException("After placement a rendered Anvil corner lies outside the original collider margin" + (outside >= 0 ? " at " + corners[outside].ToString("F3") : "") + ". Nothing was hidden.");

            // Proven inside: only now hide the replaced primitive pieces, recording each exact state first.
            for (int i = 0; i < replaced.Length; i++)
            {
                _hidden.Add(replaced[i]);
                _hiddenWasEnabled.Add(replaced[i].enabled);
                replaced[i].enabled = false;
            }
            Vector3 boxMin = _station.center - _station.size * 0.5f, boxMax = _station.center + _station.size * 0.5f;
            Vector3 cMin = corners[0], cMax = corners[0];
            for (int i = 1; i < corners.Count; i++)
            {
                cMin = Vector3.Min(cMin, corners[i]);
                cMax = Vector3.Max(cMax, corners[i]);
            }
            log.Append("\nAnvil: native size ").Append(native.ToString("F3")).Append(" m verified against the delivered source; scale ").Append(fit.Scale.ToString("0.0000"))
                .Append(", turned ").Append(90 * fit.QuarterTurns).Append(" degrees (horn to +x like the original), pivot ").Append(fit.Pivot.ToString("F3"))
                .Append(" in station space, resting on the original block top at ").Append(supportTop.ToString("0.000")).Append(" m. ").Append(fit.Reason)
                .Append("\nEvery one of ").Append(corners.Count).Append(" rendered corners is inside the original BoxCollider (centre ").Append(_station.center.ToString("F3"))
                .Append(", size ").Append(_station.size.ToString("F3")).Append(") with clearances: left ").Append((cMin.x - boxMin.x).ToString("0.000"))
                .Append(", right ").Append((boxMax.x - cMax.x).ToString("0.000")).Append(", bottom ").Append((cMin.y - boxMin.y).ToString("0.000"))
                .Append(", top ").Append((boxMax.y - cMax.y).ToString("0.000")).Append(", back ").Append((cMin.z - boxMin.z).ToString("0.000"))
                .Append(", front ").Append((boxMax.z - cMax.z).ToString("0.000")).Append(" m.")
                .Append("\nHidden original renderers (restored on End): Waist, Face, Horn, Glow. Kept visible: Block (support), Bowl, Embers, Ember Glow.");
        }

        /// <summary>Refuses when the fitted anvil would cut into an original piece that stays visible.</summary>
        private void KeepClear(Renderer piece, ForgeFitH8S fit, Transform frame)
        {
            if (piece == null) return;
            Vector3 min, max;
            if (!LocalBounds(piece.gameObject, frame, out min, out max)) return;
            float depth = ForgeGalleryFitH8S.OverlapDepth(fit.Min, fit.Max, min, max);
            if (depth > ForgeGalleryFitH8S.TouchTolerance)
                throw new InvalidOperationException("The fitted Anvil would cut " + depth.ToString("0.000") + " m into the original " + piece.name + ". Nothing was hidden.");
        }

        // ------------------------------------------------------------------ the gallery furnishings

        private void PlaceGallery(StringBuilder log)
        {
            VenueBinding04C forge = Game.Inhabited != null && Game.Inhabited.IsOperational ? Game.Inhabited.FindVenue("forge") : null;
            if (forge == null || !forge.Available || forge.View == null || forge.View.Composition == null)
            {
                log.Append("\nGallery furnishings skipped: Dante Rook's Crystal Forge room is not loaded, so there is no gallery floor.");
                return;
            }
            VenueView04C view = forge.View;
            if (!view.Contains(_station.transform.position))
            {
                log.Append("\nGallery furnishings skipped: the Forge station stands outside the Forge room (its outdoor fallback placement).");
                return;
            }
            GalleryRulesH8S rules = Rules(view, log);
            PlaceFurnishing("Workbench", rules, view.transform, log);
            PlaceFurnishing("BookStand", rules, view.transform, log);
        }

        /// <summary>Everything the furnishings keep clear of, measured in the live Forge room (venue-local metres).</summary>
        private GalleryRulesH8S Rules(VenueView04C view, StringBuilder log)
        {
            Transform room = view.transform;
            VenueExpanded04C composition = view.Composition;
            GalleryRulesH8S rules = new GalleryRulesH8S();
            // The same occupied extents VenueView04C.Contains uses.
            float half = composition.roomWidth * 0.5f - 0.3f;
            rules.Room = new PlanRectH8S(new Vector2(-half, 0.15f), new Vector2(half, composition.roomDepth - 0.3f), "room");
            rules.FloorY = view.LocalAnchor("service").y;
            int keepouts = 0, anchors = 0, interactions = 0, colliders = 0;
            if (composition.keepouts != null)
                foreach (VenueKeepout04C k in composition.keepouts)
                {
                    if (k == null || k.min == null || k.max == null || k.min.Length < 3 || k.max.Length < 3) continue;
                    rules.Blocked.Add(new PlanRectH8S(new Vector2(k.min[0], k.min[2]), new Vector2(k.max[0], k.max[2]), "keepout " + k.id).Expanded(GalleryPlanH8S.KeepoutMargin));
                    keepouts++;
                }
            if (composition.anchors != null)
                foreach (VenueAnchor04C a in composition.anchors)
                {
                    if (a == null || a.position == null || a.position.Length < 3 || a.id == "exit") continue;
                    rules.Circles.Add(new PlanCircleH8S(new Vector2(a.position[0], a.position[2]), GalleryPlanH8S.AnchorRadius, "anchor " + a.id));
                    anchors++;
                }
            for (int i = 0; i < Interactable.All.Count; i++)
            {
                Interactable it = Interactable.All[i];
                if (it == null || !it.isActiveAndEnabled) continue;
                Vector3 local = room.InverseTransformPoint(it.transform.position);
                if (Mathf.Abs(local.y - rules.FloorY) > it.HeightTolerance + 0.5f) continue;
                if (GalleryPlanH8S.RectDistance(rules.Room, new Vector2(local.x, local.z)) > it.Radius + GalleryPlanH8S.InteractMargin) continue;
                rules.Circles.Add(new PlanCircleH8S(new Vector2(local.x, local.z), it.Radius + GalleryPlanH8S.InteractMargin, "interaction " + it.name));
                interactions++;
            }
            rules.Blocked.Add(PlanOf(_station.center, _station.size, _station.transform, room, "Forge station collider").Expanded(GalleryPlanH8S.StationMargin));
            int deferred = 0;
            Vector3 centre = room.TransformPoint(new Vector3(0f, rules.FloorY + 1.1f, composition.roomDepth * 0.5f));
            Vector3 halfExtents = new Vector3(composition.roomWidth * 0.5f, 0.95f, composition.roomDepth * 0.5f);
            foreach (Collider c in Physics.OverlapBox(centre, halfExtents, room.rotation, Layers.WorldMask | (1 << Layers.Debris) | (1 << Layers.Vehicles), QueryTriggerInteraction.Ignore))
            {
                if (c == null || c == _station || (_preview != null && c.transform.IsChildOf(_preview.transform))) continue;
                Bounds b = c.bounds;
                PlanRectH8S plan = PlanOf(Vector3.zero, b.size, null, room, "collider " + c.name, b.center);
                // A building-sized combined collider (the gothic service shell is one MeshCollider) would blanket the room as
                // a box, so it is left to the exact per-spot Physics.CheckBox instead.
                if (plan.Size.x * plan.Size.y > LargeColliderArea)
                {
                    deferred++;
                    continue;
                }
                rules.Blocked.Add(plan.Expanded(GalleryPlanH8S.ColliderMargin));
                colliders++;
            }
            Vector3 entrance = view.LocalAnchor("exit");
            string giverSource;
            Vector3 giver = GiverLocal(view, out giverSource);
            Vector3 station = room.InverseTransformPoint(_station.transform.position);
            Vector2 door = new Vector2(entrance.x, entrance.z);
            rules.Paths.Add(new PlanPathH8S(door, new Vector2(giver.x, giver.z), GalleryPlanH8S.PathHalfWidth, "walk entrance to giver"));
            rules.Paths.Add(new PlanPathH8S(door, new Vector2(station.x, station.z), GalleryPlanH8S.PathHalfWidth, "walk entrance to Forge station"));
            Vector3 eye = entrance + Vector3.up * 1.6f;
            rules.Sights.Add(new SightLineH8S(eye, giver + Vector3.up * 1.65f, GalleryPlanH8S.SightClearance, "sight entrance to giver head"));
            rules.Sights.Add(new SightLineH8S(eye, giver + Vector3.up * 1.15f, GalleryPlanH8S.SightClearance, "sight entrance to giver chest"));
            rules.Sights.Add(new SightLineH8S(eye, giver + Vector3.up * 0.5f, GalleryPlanH8S.SightClearance, "sight entrance to giver knees"));
            rules.Sights.Add(new SightLineH8S(eye, station + Vector3.up * 1f, GalleryPlanH8S.SightClearance, "sight entrance to Forge station"));
            rules.PreferNear = new Vector2(station.x, station.z);
            bool sightClear = !Physics.Linecast(room.TransformPoint(eye), room.TransformPoint(giver + Vector3.up * 1.65f), Layers.WorldMask, QueryTriggerInteraction.Ignore);
            log.Append("\nGallery rules in the live Forge room: ").Append(keepouts).Append(" keepouts, ").Append(anchors).Append(" anchors, ").Append(interactions)
                .Append(" interaction radii, ").Append(colliders).Append(" existing collider footprints (").Append(deferred)
                .Append(" building-sized colliders left to the exact per-spot physics check), the station collider, walk and sight lines from the entrance (Venue:forge.exit) to ")
                .Append(giverSource).Append(" at ").Append(giver.ToString("F2")).Append(". Existing sight line entrance to giver: ")
                .Append(sightClear ? "clear." : "already blocked by existing geometry (not caused by this preview).");
            return rules;
        }

        /// <summary>The plan rectangle (room-local) covering a box given in another transform's space, or a world box when space is null.</summary>
        private PlanRectH8S PlanOf(Vector3 center, Vector3 size, Transform space, Transform room, string label, Vector3 worldCenter = default(Vector3))
        {
            Vector3 min = space != null ? center - size * 0.5f : worldCenter - size * 0.5f;
            Vector3 max = space != null ? center + size * 0.5f : worldCenter + size * 0.5f;
            ForgeGalleryFitH8S.Corners(min, max, _corners);
            Vector2 lo = new Vector2(float.MaxValue, float.MaxValue), hi = new Vector2(float.MinValue, float.MinValue);
            for (int i = 0; i < 8; i++)
            {
                Vector3 world = space != null ? space.TransformPoint(_corners[i]) : _corners[i];
                Vector3 local = room.InverseTransformPoint(world);
                lo = Vector2.Min(lo, new Vector2(local.x, local.z));
                hi = Vector2.Max(hi, new Vector2(local.x, local.z));
            }
            return new PlanRectH8S(lo, hi, label);
        }

        /// <summary>Where the Forge giver stands, room-local: Dante Rook's live actor, else his post, else the room's staff anchor.</summary>
        private static Vector3 GiverLocal(VenueView04C view, out string source)
        {
            Transform room = view.transform;
            if (Game.Campaign != null)
            {
                NpcController actor = Game.Campaign.CastActor(CastIds04E.Dante);
                if (actor != null && view.Contains(actor.transform.position))
                {
                    source = "Dante Rook's live actor";
                    return room.InverseTransformPoint(actor.transform.position);
                }
                Vector3 post;
                float yaw;
                if (Game.Campaign.CastPost(CastIds04E.Dante, out post, out yaw) && view.Contains(post))
                {
                    source = "Dante Rook's post";
                    return room.InverseTransformPoint(post);
                }
            }
            source = "the Forge staff anchor";
            return view.LocalAnchor("staff");
        }

        private void PlaceFurnishing(string name, GalleryRulesH8S rules, Transform room, StringBuilder log)
        {
            GameObject holder;
            GameObject visual = Prop(name, out holder);
            holder.transform.SetPositionAndRotation(room.position, room.rotation);
            holder.transform.localScale = Vector3.one;
            Vector3 min, max;
            if (!LocalBounds(visual, room, out min, out max))
            {
                DestroyImmediate(holder);
                log.Append("\n").Append(name).Append(" skipped: no rendered mesh.");
                return;
            }
            Vector3 native = max - min, source = ForgeGalleryFitH8S.SourceSize(name);
            if (!ForgeGalleryFitH8S.NativeSizeMatches(native, source, ForgeGalleryFitH8S.NativeSizeTolerance))
            {
                DestroyImmediate(holder);
                log.Append("\n").Append(name).Append(" skipped: native scale check failed (imported ").Append(native.ToString("F3")).Append(" m, delivered ").Append(source.ToString("F3")).Append(" m).");
                return;
            }
            List<GallerySpotH8S> spots = GalleryPlanH8S.Spots(new Vector2(native.x, native.z), native.y, rules, GalleryPlanH8S.GridStep);
            string lastWhy = spots.Count == 0 ? "no planned spot passes the rules" : "";
            int tried = 0;
            for (int s = 0; s < spots.Count && tried < MaxPhysicalTries; s++)
            {
                GallerySpotH8S spot = spots[s];
                tried++;
                float floor;
                string why = PhysicalCheck(room, spot.Footprint, rules.FloorY, native.y, out floor);
                if (why != null)
                {
                    lastWhy = why;
                    continue;
                }
                Vector3 pivot = GalleryPlanH8S.PivotFor(spot.Center, floor, min, max, spot.QuarterTurns);
                holder.transform.SetPositionAndRotation(room.TransformPoint(pivot), room.rotation * Quaternion.Euler(0f, 90f * spot.QuarterTurns, 0f));
                Vector3 placedMin, placedMax;
                LocalBounds(visual, room, out placedMin, out placedMax);
                if (placedMin.x < spot.Footprint.Min.x - 0.02f || placedMin.z < spot.Footprint.Min.y - 0.02f || placedMax.x > spot.Footprint.Max.x + 0.02f ||
                    placedMax.z > spot.Footprint.Max.y + 0.02f || Mathf.Abs(placedMin.y - floor) > 0.02f)
                {
                    lastWhy = "the rendered bounds did not land on the planned footprint";
                    continue;
                }
                PlanRectH8S taken = spot.Footprint.Expanded(GalleryPlanH8S.ItemGap);
                taken.Label = "placed " + name;
                rules.Blocked.Add(taken);
                log.Append("\n").Append(name).Append(": placed against the ").Append(spot.Wall).Append(" at room-local ").Append(spot.Center.ToString("F2"))
                    .Append(", turned ").Append(90 * spot.QuarterTurns).Append(" degrees, on the existing floor at ").Append(floor.ToString("0.000"))
                    .Append(" m (spot ").Append(s + 1).Append(" of ").Append(spots.Count).Append(" planned; ").Append(tried - 1).Append(" refused by the physical probes). VISUAL PREVIEW, no collider.");
                return;
            }
            DestroyImmediate(holder);
            log.Append("\n").Append(name).Append(" skipped: no safe gallery spot after ").Append(tried).Append(" physical checks (").Append(lastWhy).Append(").");
        }

        /// <summary>Null when the footprint stands on the existing level room floor and no existing collider occupies its volume, otherwise why not.</summary>
        private static string PhysicalCheck(Transform room, PlanRectH8S footprint, float floorY, float height, out float floor)
        {
            floor = float.MinValue;
            const float inset = 0.08f;
            for (int i = 0; i < 5; i++)
            {
                float x = i == 4 ? footprint.Center.x : (i & 1) == 0 ? footprint.Min.x + inset : footprint.Max.x - inset;
                float z = i == 4 ? footprint.Center.y : (i & 2) == 0 ? footprint.Min.y + inset : footprint.Max.y - inset;
                RaycastHit hit;
                if (!Physics.Raycast(room.TransformPoint(new Vector3(x, floorY + 1f, z)), -room.up, out hit, 1.6f, Layers.WorldMask, QueryTriggerInteraction.Ignore))
                    return "no existing floor under a footprint corner";
                Vector3 local = room.InverseTransformPoint(hit.point);
                if (Mathf.Abs(local.y - floorY) > 0.06f) return "something other than the room floor lies under a footprint corner";
                if (Vector3.Dot(hit.normal, room.up) < 0.95f) return "the floor under a footprint corner is not level";
                floor = Mathf.Max(floor, local.y);
            }
            Vector2 c = footprint.Center, h = footprint.Size * 0.5f;
            Vector3 centre = room.TransformPoint(new Vector3(c.x, floor + height * 0.5f + 0.05f, c.y));
            Vector3 half = new Vector3(Mathf.Max(0.01f, h.x - 0.03f), Mathf.Max(0.01f, height * 0.5f - 0.03f), Mathf.Max(0.01f, h.y - 0.03f));
            if (Physics.CheckBox(centre, half, room.rotation, ~((1 << Layers.IgnoreRaycast) | (1 << Layers.Minimap)), QueryTriggerInteraction.Ignore))
                return "an existing collider (furniture, wall, character or vehicle) occupies the footprint";
            return null;
        }
    }
}
#endif
