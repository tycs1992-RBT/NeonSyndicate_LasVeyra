using System;
using System.Text.RegularExpressions;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>Written from the local checkout in Editor preparation. No machine or user path is retained.</summary>
    [Serializable]
    public sealed class FantasyBuildStampV1
    {
        public int schemaVersion = 1;
        public string projectIdentity = "", gitBranch = "", gitCommit = "", sourceFingerprint = "";
        public bool dirty, dirtyKnown;
        public string unityVersion = "", requiredUnityVersion = "6000.3.16f1", preparedUtc = "", problem = "";
    }

    /// <summary>Observed native components, not a claim of visual quality, occlusion or artistic acceptance.</summary>
    [Serializable]
    public sealed class FantasyLayerProofV1
    {
        public string name = "", status = "", firstRefusal = "", firstNativeRefusal = "", firstActivationRefusal = "";
        public bool prepared, selected, active, policyActive, gatewayStaged;
        public int preparedModels, instantiatedObjects, activeObjects, activeRenderers;
        public int nearPlayerRenderers, inCameraFrustumRenderers, refusedPlacements, nativeRefusedPlacements, activationRefusals;
        public int stagedHouses, stagedCourtPieces;
    }

    /// <summary>A snapshot taken only by running code in the actual Editor or player.</summary>
    [Serializable]
    public sealed class FantasyProofSnapshotV1
    {
        public int schemaVersion = 2;
        public FantasyBuildStampV1 build = new FantasyBuildStampV1();
        public string capturedUtc = "", unityVersion = "", renderer = "", graphicsPreset = "", gameState = "";
        public int worldRevision = -1, seed;
        public bool freeRoam, hasPlayer, playerImported, dantePresent, danteImported, hasCamera, ordinaryQaBusy;
        public Vector3 playerPosition, cameraPosition;
        public Quaternion cameraRotation;
        public int cameraWidth, cameraHeight, nearbyImportedCharacters, nearbyFallbackCharacters;
        public float cameraFov, hours;
        public string weather = "", liveCastBinding = "", firstCharacterProblem = "", retainedLiveCastProblem = "";
        public string characterAppearance = "UNVERIFIED", summary = "BLOCKED: no native snapshot.";
        public string visibilityMethod = "Active means enabled renderer components on active objects. Frustum candidates also match the gameplay camera culling mask and frustum. LOD draw selection, occlusion and artistic quality are not proven by these counts.";
        public FantasyLayerProofV1 fr01 = new FantasyLayerProofV1 { name = "FR01" };
        public FantasyRewriteProofWR01 worldRewrite = new FantasyRewriteProofWR01();
        public FantasySurfaceProofWR02 surfaceWR02 = new FantasySurfaceProofWR02();
        public string underheartRegion = "";
        public FantasyLayerProofV1 fr02a = new FantasyLayerProofV1 { name = "FR02A" };
    }

    /// <summary>Pure verdict and privacy helpers shared by native receipts and offline regression tests.</summary>
    public static class FantasyProofRulesV1
    {
        public static void ResetStatics() { }

        public static bool Selection(bool persistent, bool preview, bool sessionSelection, bool proof, bool legacy)
        { return (proof && !legacy) || (preview ? sessionSelection : persistent); }

        /// <summary>No computer paths, account names or stack traces belong in an exportable proof receipt.</summary>
        public static string SafeText(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            string safe = Regex.Replace(text, @"[A-Za-z]:[\\/][^\r\n]*", "[local path removed]");
            safe = Regex.Replace(safe, @"\\\\[^\r\n]*", "[local path removed]");
            safe = Regex.Replace(safe, @"(?<![A-Za-z0-9:/])/(?!/)[^\r\n]*", "[local path removed]");
            safe = Regex.Replace(safe, @"~[\\/][^\r\n]*", "[local path removed]");
            return safe.Length > 1200 ? safe.Substring(0, 1200) : safe;
        }

        /// <summary>New surface views need their own native renderer evidence. Original skyline views keep their previous crown gate.</summary>
        public static bool SurfaceViewObservedWR02(string view, FantasyProofSnapshotV1 proof)
        {
            if (proof == null || !proof.fr01.active || !proof.fr02a.active || !proof.worldRewrite.selected || proof.worldRewrite.active < 1 ||
                !proof.surfaceWR02.selected || !proof.surfaceWR02.prepared || !proof.surfaceWR02.active || proof.surfaceWR02.refused > 0) return false;
            if (view == "CAUSEWAY") return proof.surfaceWR02.causewayFrustumRenderers > 0;
            if (view == "FOREST_RUINS") return proof.surfaceWR02.canopyFrustumRenderers > 0;
            return proof.worldRewrite.importedCrownFrustumRenderers + proof.worldRewrite.importedSkylineFrustumRenderers > 0;
        }

        /// <summary>A code or selection observation can never promote the owner's visual or gameplay acceptance.</summary>
        public static string Summary(FantasyProofSnapshotV1 proof)
        {
            if (proof == null) return "BLOCKED: no native snapshot.";
            if (proof.build == null || !GitRefReader04E.IsSha(proof.build.gitCommit))
                return "BLOCKED: the running project has no valid checkout stamp. Stop Play and Verify Fantasy World.";
            if (!string.IsNullOrEmpty(proof.build.problem)) return "BLOCKED: " + SafeText(proof.build.problem);
            if (proof.unityVersion != proof.build.requiredUnityVersion) return "BLOCKED: the running Unity version does not match this project requirement.";
            if (proof.worldRevision != 10) return "FANTASY WORLD COULD NOT LOAD: this is not revision 10.";
            if (!proof.fr01.prepared || !proof.fr02a.prepared) return "FANTASY WORLD COULD NOT LOAD: required imported district models or materials are missing.";
            if (!string.IsNullOrEmpty(proof.firstCharacterProblem)) return "FANTASY WORLD PARTIAL: " + SafeText(proof.firstCharacterProblem);
            if (!proof.hasPlayer) return "FANTASY WORLD PARTIAL: Malik has not spawned in this session.";
            if (!proof.playerImported) return "FANTASY WORLD PARTIAL: Malik uses the procedural fallback. Character appearance is UNVERIFIED; read the retained LiveCast reason.";
            if (proof.dantePresent && !proof.danteImported) return "FANTASY WORLD PARTIAL: Dante uses the procedural fallback. His imported appearance is UNVERIFIED.";
            if (proof.fr02a.stagedHouses == 0) return "FANTASY WORLD PARTIAL: " + (proof.fr01.gatewayStaged ? "gateway staged" : "gateway not staged") + ", but 0 houses admitted.";
            if (!proof.fr01.selected || !proof.fr02a.selected) return "FANTASY WORLD OFF: use PLAY FANTASY WORLD for an explicit preview session.";
            if (!proof.fr01.active || !proof.fr02a.active) return "FANTASY WORLD PARTIAL: selected, but one or both appearance layers are inactive. Read the actual refusal below.";
            if (proof.worldRewrite.selected && !proof.worldRewrite.prepared) return "FANTASY WORLD PARTIAL: WR01 civic assets are not prepared. Original civic presentation retained.";
            if (proof.surfaceWR02.selected && (!proof.surfaceWR02.prepared || !proof.surfaceWR02.active || proof.surfaceWR02.refused > 0)) return "FANTASY WORLD PARTIAL: WR02 surfaces or canopy refused. Read exact source owners and reasons in the surface receipt.";
            if (proof.worldRewrite.selected && proof.worldRewrite.refused > 0) return "FANTASY WORLD PARTIAL: one or more structural replacements were refused. Read stable IDs and exact reasons in World rewrite.";
            if (!proof.fr01.gatewayStaged) return "FANTASY WORLD PARTIAL: houses active, but gateway admission failed.";
            if (!proof.hasCamera) return "FANTASY WORLD PARTIAL: no gameplay camera is available.";
            if (proof.fr01.inCameraFrustumRenderers + proof.fr02a.inCameraFrustumRenderers + proof.worldRewrite.importedCrownFrustumRenderers + proof.worldRewrite.importedSkylineFrustumRenderers == 0)
                return "FANTASY WORLD PARTIAL: active art is outside this camera frustum. Follow the Living Streets route.";
            return "Native components active with art in the camera frustum. Visible difference and gameplay still require the matched screenshots and owner review.";
        }
    }

    /// <summary>Read-only native truth. It owns neither gameplay state nor screenshot or acceptance authority.</summary>
    public static class FantasyProofV1
    {
        /// <summary>Read-only access for the separate Editor assembly; the original layout field stays internal.</summary>
        public static HeartPresentationFR01 CurrentHeartV1 { get { return Game.Layout != null ? Game.Layout.HeartArtFR01 : null; } }
        private static FantasyBuildStampV1 _stamp;
        public static void ResetStatics() { _stamp = null; FantasyProofRulesV1.ResetStatics(); }
        public static FantasyBuildStampV1 BuildStamp
        {
            get
            {
                if (_stamp != null) return _stamp;
                try
                {
                    TextAsset asset = Resources.Load<TextAsset>("FantasyBuildV1");
                    _stamp = asset != null ? JsonUtility.FromJson<FantasyBuildStampV1>(asset.text) : null;
                }
                catch (Exception e) { _stamp = new FantasyBuildStampV1 { problem = FantasyProofRulesV1.SafeText(e.Message) }; }
                if (_stamp == null) _stamp = new FantasyBuildStampV1 { problem = "No checkout stamp was prepared in this project. Stop Play and choose Acceptance > Verify Fantasy World." };
                return _stamp;
            }
        }

        /// <summary>Call from the actual game only. Counting renderer components does not manufacture a gameplay frame.</summary>
        public static FantasyProofSnapshotV1 Capture(Camera camera = null)
        {
            if (camera == null && Game.Cam != null) camera = Game.Cam.Cam;
            FantasyProofSnapshotV1 proof = new FantasyProofSnapshotV1
            {
                build = BuildStamp, capturedUtc = DateTime.UtcNow.ToString("o"), unityVersion = Application.unityVersion,
                renderer = RenderPath.IsURP ? "URP" : "Built-in", graphicsPreset = Settings.Graphics.ToString(),
                gameState = Game.State.ToString(), worldRevision = Game.Layout != null ? (int)Game.Layout.Revision04O : -1,
                seed = Game.Seed, freeRoam = Game.Root != null && Game.Root.FreeRoam, hasPlayer = Game.Player != null,
                playerPosition = Game.PlayerPosition, hasCamera = camera != null && camera.enabled && camera.gameObject.activeInHierarchy, ordinaryQaBusy = QaTools.Busy,
                hours = Game.Atmo != null ? Game.Atmo.Hours : -1,
                weather = Game.Atmo != null ? Game.Atmo.Current.ToString() : "Unavailable"
            };
            if (camera != null)
            {
                proof.cameraPosition = camera.transform.position; proof.cameraRotation = camera.transform.rotation;
                proof.cameraWidth = camera.pixelWidth; proof.cameraHeight = camera.pixelHeight; proof.cameraFov = camera.fieldOfView;
            }
            HeartPresentationFR01 heart = Game.Layout != null ? Game.Layout.HeartArtFR01 : null;
            if (heart != null) { heart.FillProofV1(proof, camera); heart.FillRewriteProofWR01(proof, camera); }
            proof.underheartRegion = Game.Underheart != null ? Game.Underheart.RegionAt(Game.PlayerPosition) : "";
            LiveCastRegistry registry = LiveCastRegistry.Loaded;
            proof.playerImported = Game.Player != null && LiveCastVisual.IsLive(Game.Player);
            CharacterBase dante = Game.Campaign != null ? Game.Campaign.CastActor(CastIds04E.Dante) : null;
            proof.dantePresent = dante != null; proof.danteImported = dante != null && LiveCastVisual.IsLive(dante);
            proof.liveCastBinding = Game.Player == null ? "Malik not spawned" : proof.playerImported ? "Malik imported visual bound" : "Malik procedural fallback";
            proof.liveCastBinding += dante == null ? "; Dante not spawned" : proof.danteImported ? "; Dante imported visual bound" : "; Dante procedural fallback";
            if (registry == null) proof.liveCastBinding += "; prepared registry unavailable";
            proof.firstCharacterProblem = InspectCharacterV1(Game.Player);
            if (proof.firstCharacterProblem.Length == 0 && Game.Campaign != null)
                proof.firstCharacterProblem = InspectCharacterV1(dante);
            if (Game.Player != null) foreach (NpcController npc in NpcController.All)
            {
                if (npc == null || npc.CreatureSpecies != null || (npc.transform.position - Game.PlayerPosition).sqrMagnitude > 60f * 60f) continue;
                if (LiveCastVisual.IsLive(npc)) proof.nearbyImportedCharacters++; else proof.nearbyFallbackCharacters++;
                if (proof.firstCharacterProblem.Length == 0) proof.firstCharacterProblem = InspectCharacterV1(npc);
            }
            proof.retainedLiveCastProblem = LiveCastNotice.Problems.Count > 0 ? FantasyProofRulesV1.SafeText(LiveCastNotice.Problems[0]) : "";
            proof.summary = FantasyProofRulesV1.Summary(proof);
            return proof;
        }

        private static string InspectCharacterV1(CharacterBase actor)
        {
            if (actor == null || !LiveCastVisual.IsLive(actor)) return "";
            foreach (Renderer renderer in actor.GetComponentsInChildren<Renderer>(true))
            {
                if (!renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
                foreach (Material material in renderer.sharedMaterials)
                {
                    if (material == null) return "An active imported character has an empty material slot.";
                    if (material.shader == null || !material.shader.isSupported) return "Character shader missing or unsupported: " + FantasyProofRulesV1.SafeText(material.name);
                    if (material.GetTag("LiveCastAlbedo", false, "") == "required" && material.mainTexture == null)
                        return "Character required albedo missing: " + FantasyProofRulesV1.SafeText(material.name);
                }
            }
            return "";
        }
    }
}
