#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NeonSyndicate.Editor
{
    /// <summary>Explicit native proof workflow. Selecting a button never certifies the visual or gameplay gates.</summary>
    public sealed partial class FantasyAcceptanceV1 : EditorWindow
    {
        private const string SessionKey = "NSLV.FantasyAcceptanceV1.";
        private static string[] ViewNames { get { return new[] { "HOME", "STREET", "SANCTUARY", "ELEVATED", "NORTH_CIVIC", "WEST_STREET", "BASEMENT", "UNDERHEART", "GAMEPLAY", "RETURN", "CAUSEWAY", "FOREST_RUINS", "ARCHIVE_CLOSEUP", "ARCHIVE_PLAYER_SCALE" }; } }
        [Serializable] private sealed class Receipt
        {
            public int schemaVersion = 3;
            public BasiliskNativeReviewWR02B basiliskWR02B = new BasiliskNativeReviewWR02B();
            public string capturedUtc = "", sourceImplemented = "SOURCE IMPLEMENTED", artImported = "NOT RUN", nativePlaytested = "NOT RUN", visibleDifferenceConfirmed = "NOT RUN", gameplayVerified = "NOT RUN", ownerAccepted = "NOT RUN", overall = "BLOCKED";
            public string observationScope = "Actual native observations only when this tool ran in Unity. Logs are the last 100 messages observed since this window opened, not the complete earlier Console history. Frustum counts are not occlusion or artistic proof. Pixel differences are not visual-quality acceptance.";
            public string meshInspectionScope = "Required FR01/FR02A/WR01 receipt meshes require readable UV/normal validation. Optional frozen H8R reuse may be readable only by Unity's renderer; its mesh reference, vertex count and materials are checked, with UV/normal introspection omitted when not readable. Its original importer is not changed.";
            public FantasyBuildStampV1 build;
            public bool undergroundRouteOwnerConfirmed;
            public bool preflightExecuted, enteredFreeRoam, saveBackupCreated, materialOwnerConfirmed, traversalOwnerConfirmed, controllerQualityOwnerConfirmed, saveVehicleOwnerConfirmed, interactionOwnerConfirmed, appearanceOwnerConfirmed, acceptanceOwnerConfirmed;
            public int hydratedArtFiles, importedModels;
            public List<string> blockers = new List<string>();
            public List<string> observedLogs = new List<string>();
            public List<CaptureRecord> captures = new List<CaptureRecord>();
            public List<PairRecord> pairs = new List<PairRecord>();
            public FantasyProofSnapshotV1 latest;
            public string performance = "NOT RUN. Frozen screenshot timing does not establish gameplay performance.";
        }
        [Serializable] private sealed class CaptureRecord
        {
            public string file = "", requestedView = "", kind = "", utc = "", provenance = "Actual Unity rendered Game view, captured after WaitForEndOfFrame.";
            public int width, height;
            public FantasyProofSnapshotV1 observed;
        }
        [Serializable] private sealed class PairRecord
        {
            public string view = "", legacyFile = "", fantasyFile = "", result = "BLOCKED", reason = "";
            public bool sameContext, offObserved, onObserved, pixelChangeObserved;
            public float percentChanged, meanAbsoluteDifference;
            public string acceptance = "Owner visual-quality confirmation is still required.";
        }

        [SerializeField] private Receipt _receipt = new Receipt();
        [SerializeField] private string _folder = "", _message = "Run Verify preparation before starting native review.";
        [SerializeField] private bool _backupSaves = true;
        [SerializeField] private int _view;
        private Vector2 _scroll;
        private bool _awaitSession, _requestIssued, _checksOpen;
        private double _sessionDeadline;

        [MenuItem("Neon Syndicate/Acceptance/Verify Fantasy World", false, 3400)]
        public static void Open()
        {
            FantasyAcceptanceV1 window = GetWindow<FantasyAcceptanceV1>("Fantasy World Proof");
            window.minSize = new Vector2(540f, 520f); window.Show();
        }
        public static void OpenFromCommandLine() { Open(); }
        public static void VerifyFromCommandLine()
        {
            FantasyAcceptanceV1 window = CreateInstance<FantasyAcceptanceV1>();
            try { window.Verify(); if (Application.isBatchMode) EditorApplication.Exit(window._receipt.blockers.Count == 0 ? 0 : 2); }
            finally { DestroyImmediate(window); }
        }

        private void OnEnable()
        {
            string stored = SessionState.GetString(SessionKey + "Receipt", "");
            if (stored.Length > 0) { try { _receipt = JsonUtility.FromJson<Receipt>(stored) ?? new Receipt(); } catch (Exception) { _receipt = new Receipt(); } }
            _folder = SessionState.GetString(SessionKey + "Folder", _folder);
            _awaitSession = SessionState.GetBool(SessionKey + "Await", false);
            _requestIssued = SessionState.GetBool(SessionKey + "Issued", false);
            _sessionDeadline = EditorApplication.timeSinceStartup + 180d;
            Application.logMessageReceived += ObserveLog;
            EditorApplication.update += Tick;
            EditorApplication.playModeStateChanged += PlayChanged;
        }
        private void OnDisable()
        {
            CancelCapture(); CloseBasiliskWR02B(); Persist();
            Application.logMessageReceived -= ObserveLog;
            EditorApplication.update -= Tick;
            EditorApplication.playModeStateChanged -= PlayChanged;
        }
        private void Persist()
        {
            SessionState.SetString(SessionKey + "Receipt", JsonUtility.ToJson(_receipt));
            SessionState.SetString(SessionKey + "Folder", _folder);
            SessionState.SetBool(SessionKey + "Await", _awaitSession);
            SessionState.SetBool(SessionKey + "Issued", _requestIssued);
        }
        private void ObserveLog(string message, string stack, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert && type != LogType.Warning) return;
            if (_receipt == null) return;
            _receipt.observedLogs.Add(type + ": " + SafeMessage(message));
            while (_receipt.observedLogs.Count > 100) _receipt.observedLogs.RemoveAt(0);
        }
        private void PlayChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode || state == PlayModeStateChange.ExitingEditMode) { CancelCapture(); Persist(); }
        }
        private void Tick()
        {
            if (_capture != null && EditorApplication.timeSinceStartup > _captureDeadline) { _message = "Capture timed out. Keep the Game view visible, then retry."; CancelCapture(); Export(); }
            if (!_awaitSession || !EditorApplication.isPlaying) return;
            if (EditorApplication.timeSinceStartup > _sessionDeadline)
            { _awaitSession = false; _message = "BLOCKED: the ordinary Free Roam session did not become ready within three minutes. Retain the first Console error."; Export(); return; }
            if (!_requestIssued && Game.State == GameState.Title && Game.Root != null)
            {
                string reason;
                _requestIssued = VisibleRealityV0.PlayFantasyWorld(out reason);
                _message = SafeMessage(reason);
                if (!_requestIssued) { _awaitSession = false; _receipt.blockers.Add(_message); Export(); }
                Persist();
            }
            if (_requestIssued && Game.State == GameState.Playing && Game.Root != null && Game.Root.FreeRoam && Game.Player != null)
            {
                _awaitSession = false; _receipt.enteredFreeRoam = true;
                _receipt.latest = FantasyProofV1.Capture();
                _message = "Ordinary Free Roam observed. Check the on-screen actual activation result. Walk normally; select each required view and capture its matched pair here.";
                Export(); Repaint();
            }
        }

        private void Verify()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
            { _message = "Stop Play and wait for compilation/import to finish before verification."; return; }
            _receipt = new Receipt { capturedUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture), preflightExecuted = true };
            _folder = Path.Combine(ProjectRoot, "UserCaptures/FantasyWorldProof", DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff", CultureInfo.InvariantCulture));
            Directory.CreateDirectory(_folder);
            _receipt.build = PrepareBuildStamp();
            if (!string.IsNullOrEmpty(_receipt.build.problem)) _receipt.blockers.Add(_receipt.build.problem);
            if (!File.Exists(Path.Combine(ProjectRoot, "CLAUDE.md")) || !File.Exists(Path.Combine(ProjectRoot, "ProjectSettings/ProjectVersion.txt"))) _receipt.blockers.Add("This does not look like the existing game project. Open the repository from GitHub Desktop.");
            if (EditorUtility.scriptCompilationFailed) _receipt.blockers.Add("Unity reports script compilation errors. Resolve the first red Console error before Play.");
            int hydrated = 0;
            Dictionary<string, string> checkedPaths = new Dictionary<string, string>(StringComparer.Ordinal);
            CheckArtReceipt("Assets/NeonSyndicate/Art/HeartFR01/manifest.json", _receipt.blockers, ref hydrated, checkedPaths);
            CheckArtReceipt("Assets/NeonSyndicate/Art/HeartFR02A/manifest.json", _receipt.blockers, ref hydrated, checkedPaths);
            CheckArtReceipt("Assets/NeonSyndicate/Art/HeartWR01/manifest.json", _receipt.blockers, ref hydrated, checkedPaths);
            CheckArtReceipt("Assets/NeonSyndicate/Art/HeartWR02/manifest.json", _receipt.blockers, ref hydrated, checkedPaths);
            CheckArtReceipt("Assets/NeonSyndicate/Art/FoundryWR02A/manifest.json", _receipt.blockers, ref hydrated, checkedPaths);
            CheckArtReceipt("Assets/NeonSyndicate/Art/FoundryWR02A/EnvironmentWR02B/manifest.json", _receipt.blockers, ref hydrated, checkedPaths);
            _receipt.hydratedArtFiles = hydrated;
            try
            {
                LiveCastSetup.Rebuild();
                HeartSetupFR01.Prepare(true); HeartSetupFR01.PrepareLiving(true); HeartSetupFR01.PrepareRewrite(true); HeartSetupFR01.PrepareSurfaceWR02(true); HeartSetupFR01.PrepareFoundryWR02A(true); HeartSetupFR01.PrepareCompositionWR02B(true);
                _receipt.importedModels = ValidateCatalog("Assets/NeonSyndicate/Resources/HeartPreparedFR01/Catalog.asset", false, _receipt.blockers)
                    + ValidateCatalog("Assets/NeonSyndicate/Resources/HeartPreparedFR02A/Catalog.asset", true, _receipt.blockers)
                    + ValidateCatalog("Assets/NeonSyndicate/Resources/HeartPreparedWR01/Catalog.asset", false, _receipt.blockers, true)
                    + ValidateCatalog("Assets/NeonSyndicate/Resources/HeartPreparedWR02/Catalog.asset", false, _receipt.blockers, false, true)
                    + ValidateCatalog("Assets/NeonSyndicate/Resources/HeartPreparedWR02A/Catalog.asset", false, _receipt.blockers, false, false, true)
                    + ValidateCatalog("Assets/NeonSyndicate/Resources/HeartPreparedWR02B/Catalog.asset", false, _receipt.blockers, false, false, false, true);
                ValidateCharacters(_receipt.blockers);
                _receipt.build = PrepareBuildStamp();
                if (!string.IsNullOrEmpty(_receipt.build.problem) && !_receipt.blockers.Contains(_receipt.build.problem)) _receipt.blockers.Add(_receipt.build.problem);
            }
            catch (Exception e) { _receipt.blockers.Add(SafeMessage(e.Message)); }
            _receipt.artImported = _receipt.blockers.Count == 0 ? "ART IMPORTED" : "BLOCKED";
            _message = _receipt.blockers.Count == 0 ? "Preparation checked in this native Editor. Gameplay, actual visibility and character appearance are still unverified. Begin Native Review next."
                : "FANTASY WORLD PREFLIGHT BLOCKED: " + _receipt.blockers[0];
            Export(); Repaint();
        }
        private static void ValidateCharacters(List<string> blockers)
        {
            LiveCastRegistry registry = AssetDatabase.LoadAssetAtPath<LiveCastRegistry>(LiveCastRegistry.AssetPath);
            if (registry == null || !registry.HasPlayerRow) { blockers.Add("Malik has no prepared imported player row. The game must retain the procedural fallback; imported appearance remains unverified."); return; }
            foreach (LiveCastRow row in registry.Rows)
            {
                if (row == null || row.Model == null || row.Model.Prefab == null) { blockers.Add("A prepared character prefab is missing."); continue; }
                foreach (Renderer renderer in row.Model.Prefab.GetComponentsInChildren<Renderer>(true))
                    foreach (Material material in renderer.sharedMaterials)
                        if (material == null || material.shader == null || !material.shader.isSupported || (material.GetTag("LiveCastAlbedo", false, "") == "required" && material.mainTexture == null))
                            blockers.Add("Required character shader or albedo missing: " + row.Label + "/" + renderer.name);
            }
        }

        private void BeginReview()
        {
            if (!_receipt.preflightExecuted || EditorApplication.isCompiling || EditorApplication.isUpdating) { _message = "Run Verify preparation first and wait for imports."; return; }
            if (EditorApplication.isPlaying)
            {
                if (Game.State != GameState.Title) { _message = "Return to Title before beginning a fresh fantasy review. Current Game view capture remains available in the running session."; return; }
                _awaitSession = true; _requestIssued = false; _sessionDeadline = EditorApplication.timeSinceStartup + 180d; Persist(); return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) { _message = "Review cancelled; the open scene was preserved."; return; }
            if (_backupSaves) BackupSaves();
            global::NeonSyndicate.EditorTools.NeonSyndicateSetup.RunSetup(false);
            EditorSceneManager.OpenScene(global::NeonSyndicate.EditorTools.NeonSyndicateSetup.ScenePath, OpenSceneMode.Single);
            _awaitSession = true; _requestIssued = false; _sessionDeadline = EditorApplication.timeSinceStartup + 180d;
            _message = "Starting the existing game. Native session has not been observed yet."; Persist();
            EditorApplication.isPlaying = true;
        }
        private void BackupSaves()
        {
            string folder = Path.Combine(ProjectRoot, "UserBackups/FantasyWorldProof", Path.GetFileName(_folder), DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff", CultureInfo.InvariantCulture));
            Directory.CreateDirectory(folder);
            foreach (int revision in new[] { 0, 2, 3, 4, 5, 6, 7, 8, 9, 10 })
                for (int slot = 0; slot <= SaveSystem.Slots; slot++)
                {
                    string path = SaveSystem.PathFor(slot, revision);
                    foreach (string source in new[] { path, SaveFileTransaction.BackupPath(path), SaveFileTransaction.TempPath(path) })
                        if (File.Exists(source)) File.Copy(source, Path.Combine(folder, Path.GetFileName(source)), false);
                }
            _receipt.saveBackupCreated = true;
        }

        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            try
            {
                EditorGUILayout.LabelField("VERIFY FANTASY WORLD", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(_message, _receipt.blockers.Count > 0 ? MessageType.Warning : MessageType.Info);
                EditorGUILayout.LabelField("Project", ProjectIdentity);
                if (_receipt.build != null) { EditorGUILayout.LabelField("Prepared commit", _receipt.build.gitCommit); EditorGUILayout.LabelField("Dirty checkout", !_receipt.build.dirtyKnown ? "Unknown; Git SHA and source fingerprint are available separately" : _receipt.build.dirty ? "Local changes present" : "No tracked local changes"); }
                EditorGUILayout.LabelField("Unity", Application.unityVersion + " (required " + RequiredUnity + ")");
                using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating))
                    if (GUILayout.Button("1. Verify preparation and source identity", GUILayout.Height(32))) Verify();
                _backupSaves = EditorGUILayout.ToggleLeft("Copy existing saves to private UserBackups before review", _backupSaves);
                EditorGUILayout.HelpBox("Review uses ordinary Free Roam, whose existing autosave behavior still applies. Backup copies are separate from the exportable proof package. No save is restored or migrated automatically.", MessageType.None);
                using (new EditorGUI.DisabledScope(!_receipt.preflightExecuted || _capture != null))
                    if (GUILayout.Button("2. Begin native review: PLAY FANTASY WORLD", GUILayout.Height(36))) { try { BeginReview(); } catch (Exception e) { _message = SafeMessage(e.Message); Export(); } }
                _view = EditorGUILayout.Popup("Current player view", _view, ViewNames);
                EditorGUILayout.HelpBox("Walk to HOME, NORTH_CIVIC, WEST_STREET, SANCTUARY and ELEVATED views using normal controls. Capture BASEMENT, UNDERHEART, GAMEPLAY and RETURN as you complete the existing Pressure House activity. This tool does not teleport the player. Keep Game view visible during capture. Pair capture briefly freezes the current ordinary view and hides HUD, then restores it.", MessageType.None);
                using (new EditorGUI.DisabledScope(!EditorApplication.isPlaying || _capture != null))
                {
                    if (GUILayout.Button("Capture current rendered Game view")) BeginCapture(false);
                    if (GUILayout.Button("Capture matched OFF / ON pair at this view")) BeginCapture(true);
                }
                if (_capture != null && GUILayout.Button("Cancel capture and restore")) { CancelCapture(); _message = "Capture stopped. The proof runner requested restoration of its captured session state; inspect the current Game view and any retained restoration warning."; Export(); }
                DrawBasiliskWR02B();
                _checksOpen = EditorGUILayout.Foldout(_checksOpen, "Record owner-observed native results", true);
                if (_checksOpen)
                {
                    EditorGUILayout.HelpBox("Check only actions actually performed in this running build. Starting Play or taking a screenshot does not complete these checks.", MessageType.Warning);
                    _receipt.materialOwnerConfirmed = EditorGUILayout.ToggleLeft("I inspected Malik and important NPC skin, clothing, hair and eyes", _receipt.materialOwnerConfirmed);
                    _receipt.traversalOwnerConfirmed = EditorGUILayout.ToggleLeft("I tested walking, sprint, dodge, melee, doors, stairs and camera", _receipt.traversalOwnerConfirmed);
                    _receipt.controllerQualityOwnerConfirmed = EditorGUILayout.ToggleLeft("I tested keyboard, controller, Medium and Low quality", _receipt.controllerQualityOwnerConfirmed);
                    _receipt.saveVehicleOwnerConfirmed = EditorGUILayout.ToggleLeft("I checked original vehicle/companion routes and save/load", _receipt.saveVehicleOwnerConfirmed);
                    _receipt.interactionOwnerConfirmed = EditorGUILayout.ToggleLeft("I completed a meaningful existing interaction on the route", _receipt.interactionOwnerConfirmed);
                    _receipt.undergroundRouteOwnerConfirmed = EditorGUILayout.ToggleLeft("I walked the physical basement, completed a legal existing objective and returned", _receipt.undergroundRouteOwnerConfirmed);
                    _receipt.appearanceOwnerConfirmed = EditorGUILayout.ToggleLeft("All required matched views show substantial fantasy architecture changes", _receipt.appearanceOwnerConfirmed);
                    _receipt.acceptanceOwnerConfirmed = EditorGUILayout.ToggleLeft("I accept this native result for this exact checkpoint", _receipt.acceptanceOwnerConfirmed);
                }
                if (GUILayout.Button("3. Export JSON, screenshot comparison and readable results")) Export();
                if (GUILayout.Button("Open local proof package") && Directory.Exists(_folder)) EditorUtility.RevealInFinder(_folder);
                EditorGUILayout.LabelField("Native gameplay", _receipt.nativePlaytested);
                EditorGUILayout.LabelField("Visible difference", _receipt.visibleDifferenceConfirmed);
                EditorGUILayout.LabelField("Gameplay interaction", _receipt.gameplayVerified);
                EditorGUILayout.LabelField("Owner acceptance", _receipt.ownerAccepted);
                EditorGUILayout.LabelField("Actual PNG frames", _receipt.captures.Count.ToString(CultureInfo.InvariantCulture));
                foreach (string blocker in _receipt.blockers) EditorGUILayout.HelpBox(blocker, MessageType.Warning);
            }
            finally { EditorGUILayout.EndScrollView(); }
        }
    }
}
#endif
