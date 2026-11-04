#if UNITY_EDITOR
using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace NeonSyndicate.Editor
{
    public sealed partial class FantasyAcceptanceV1
    {
        private Coroutine _capture;
        private PlayerController _captureOwner;
        private PlayerCamera _captureCamera;
        private HeartPresentationFR01 _captureHeart;
        private Atmosphere _captureAtmosphere;
        private Hud _captureHud;
        private PlayerController _heldPlayer;
        private Vector3 _capturePosition;
        private Quaternion _captureRotation;
        private float _captureFov;
        private bool _capturePaused, _captureFrozen, _captureHudHidden, _captureInputBlocked, _captureHeld;
        private GameState _captureState;
        private double _captureDeadline;

        private static string CaptureProblem(bool pair)
        {
            if (!EditorApplication.isPlaying || Game.Player == null || Game.Cam == null || Game.Cam.Cam == null) return "Start ordinary Free Roam and wait for the real player camera.";
            if (QaTools.Busy) return "Stop the ordinary QA tour first. It suppresses fantasy art and cannot be used for this proof.";
            if (!pair) return null;
            if (Game.State != GameState.Playing || Game.Root == null || !Game.Root.FreeRoam || Game.Layout == null || (int)Game.Layout.Revision04O != 10) return "Resume ordinary revision-10 Free Roam before a matched pair.";
            if (Game.Cam.InCinematic || (Game.Missions != null && Game.Missions.Active != null) || (Game.Quests != null && Game.Quests.Running != null) || CombatDirector.InCombat || (Game.Wanted != null && Game.Wanted.Stars > 0)) return "Finish the active camera scene, quest, combat or pursuit before a frozen comparison. Current Game view can record an interaction without changing it.";
            if (Game.Player.InVehicle || Game.Player.IsDead || Game.Player.IsRagdolled || !Game.Player.IsGroundedPublic || Game.Player.Climbing || Game.Player.TravelState != TravelMode.None) return "Stand safely on foot for a matched pair. Elevated proof can use an existing safe overlook reached through normal play.";
            if (Game.Inner != null && Game.Inner.IsInner) return "Return from the Veil for the district comparison.";
            if (FantasyProofV1.CurrentHeartV1 == null) return "No Gothic Heart presentation owner exists in this running world.";
            return null;
        }

        private void BeginCapture(bool pair)
        {
            string problem = CaptureProblem(pair);
            if (problem != null) { _message = problem; return; }
            if (string.IsNullOrEmpty(_folder)) { _message = "Run Verify preparation first so frames have an identified proof package."; return; }
            EditorApplication.ExecuteMenuItem("Window/General/Game");
            _captureOwner = Game.Player;
            _captureDeadline = EditorApplication.timeSinceStartup + 25d;
            _capture = _captureOwner.StartCoroutine(DriveCapture(CaptureFrames(pair, ViewNames[_view])));
            _message = "Capturing actual rendered Game view. Keep the Game view visible; no native result is recorded until the PNG is saved.";
        }

        private IEnumerator DriveCapture(IEnumerator body)
        {
            try
            {
                while (true)
                {
                    bool moved = false; Exception failure = null;
                    try { moved = body.MoveNext(); } catch (Exception e) { failure = e; }
                    if (failure != null) { _message = "Native capture failed: " + SafeMessage(failure.Message); _receipt.observedLogs.Add(_message); yield break; }
                    if (!moved) yield break;
                    yield return body.Current;
                }
            }
            finally
            {
                IDisposable disposable = body as IDisposable; if (disposable != null) disposable.Dispose();
                RestoreCapture(); _capture = null; _captureOwner = null; Export(); Repaint();
            }
        }

        private IEnumerator CaptureFrames(bool pair, string view)
        {
            try
            {
                if (!pair)
                {
                    yield return new WaitForEndOfFrame();
                    SaveFrame("CURRENT_" + view, view, "current ordinary Game view");
                    _message = "Actual Game view saved. A screenshot alone does not verify the interaction or visual quality.";
                    yield break;
                }
                HoldCapture();
                PairRecord record = new PairRecord { view = view };
                _captureHeart.BeginProofComparisonV1(true);
                for (int frame = 0; frame < 4; frame++) { HoldCamera(); yield return null; }
                yield return new WaitForEndOfFrame();
                CaptureRecord before = SaveFrame("LEGACY_" + view, view, "temporary legacy comparison");
                Color32[] legacy = ReadFramePixels(before.file, out int width, out int height);
                record.legacyFile = before.file;
                record.offObserved = !before.observed.fr01.active && !before.observed.fr02a.active && before.observed.worldRewrite.active == 0 && !before.observed.surfaceWR02.active && before.observed.surfaceWR02.legacySuppressed == 0;
                _captureHeart.BeginProofComparisonV1(false);
                for (int frame = 0; frame < 4; frame++) { HoldCamera(); yield return null; }
                yield return new WaitForEndOfFrame();
                FantasyProofSnapshotV1 on = FantasyProofV1.Capture();
                string prefix = on.fr01.active && on.fr02a.active ? "FANTASY_" : "REFUSED_FANTASY_";
                CaptureRecord after = SaveFrame(prefix + view, view, "temporary fantasy comparison");
                Color32[] fantasy = ReadFramePixels(after.file, out int afterWidth, out int afterHeight);
                record.fantasyFile = after.file;
                record.sameContext = SameContext(before.observed, after.observed) && width == afterWidth && height == afterHeight;
                record.onObserved = HeartPlanFR01.FoundryViewRequestedWR02A(view) ? HeartPlanFR01.FoundryViewObservedWR02A(after.observed) : FantasyProofRulesV1.SurfaceViewObservedWR02(view, after.observed);
                if (width == afterWidth && height == afterHeight)
                {
                    ImageDiffResult difference = ImageDiff.Compare(legacy, fantasy, width, height, ImageDiff.DefaultThreshold, null);
                    record.percentChanged = difference.PercentChanged; record.meanAbsoluteDifference = difference.MeanAbsDiff;
                    record.pixelChangeObserved = difference.PercentChanged >= 1f;
                }
                record.reason = !record.sameContext ? "Frame context changed; pair is invalid." : !record.offObserved ? "Legacy OFF was requested but active fantasy geometry remained." : !record.onObserved ? "Fantasy selection did not produce active frustum candidates. Inspect actual refusal and placement counts." : !record.pixelChangeObserved ? "Less than 1 percent of pixels changed above the existing comparison threshold. The visible-change gate is BLOCKED pending a corrected native view." : "Native matched pair recorded. Pixel change is only a comparison signal; owner architectural-quality confirmation is required.";
                record.result = record.sameContext && record.offObserved && record.onObserved && record.pixelChangeObserved ? "NATIVE PAIR RECORDED, OWNER REVIEW REQUIRED" : "BLOCKED";
                _receipt.pairs.Add(record); _message = record.reason;
            }
            finally
            {
                RestoreCapture(); _capture = null; _captureOwner = null;
                Export(); Repaint();
            }
        }

        private void HoldCapture()
        {
            _captureCamera = Game.Cam; _captureAtmosphere = Game.Atmo; _captureHeart = FantasyProofV1.CurrentHeartV1;
            _captureHud = Game.Hud; _heldPlayer = Game.Player;
            _capturePosition = _captureCamera.Cam.transform.position; _captureRotation = _captureCamera.Cam.transform.rotation; _captureFov = _captureCamera.Cam.fieldOfView;
            _capturePaused = GameTime.Paused; _captureState = Game.State;
            _captureFrozen = _captureAtmosphere != null && _captureAtmosphere.Frozen;
            _captureHudHidden = _captureHud != null && _captureHud.Hidden;
            _captureInputBlocked = GInput.GameplayBlocked;
            _captureHeld = true;
            GameTime.SetPaused(true);
            GInput.GameplayBlocked = true;
            if (_captureAtmosphere != null) _captureAtmosphere.Frozen = true;
            if (_captureHud != null) _captureHud.Hidden = true;
            HoldCamera();
        }
        private void HoldCamera()
        {
            if (Game.State != _captureState || QaTools.Busy || Game.Cam != _captureCamera || FantasyProofV1.CurrentHeartV1 != _captureHeart) throw new InvalidOperationException("The ordinary session changed during capture; no valid matched pair can be claimed.");
            _captureCamera.SetCinematic(_capturePosition, _capturePosition + _captureRotation * Vector3.forward * 100f, _captureFov);
            _captureCamera.Cam.transform.SetPositionAndRotation(_capturePosition, _captureRotation);
            _captureCamera.Cam.fieldOfView = _captureFov;
        }
        private void RestoreCapture()
        {
            if (!_captureHeld) return;
            _captureHeld = false;
            bool sameSession = Game.Player == _heldPlayer && Game.Cam == _captureCamera && FantasyProofV1.CurrentHeartV1 == _captureHeart;
            try { if (_captureHeart != null) _captureHeart.EndProofComparisonV1(); } catch (Exception e) { _message = "Appearance restoration needs review: " + SafeMessage(e.Message); }
            try { if (_captureCamera != null && sameSession) _captureCamera.ClearCinematic(); } catch (Exception e) { _message = "Camera restoration needs review: " + SafeMessage(e.Message); }
            if (_captureAtmosphere != null) _captureAtmosphere.Frozen = _captureFrozen;
            if (_captureHud != null) _captureHud.Hidden = _captureHudHidden;
            // A menu/load transition may already own pause. Never overwrite its newer decision.
            if (sameSession && Game.State == _captureState) { GameTime.SetPaused(_capturePaused); GInput.GameplayBlocked = _captureInputBlocked; }
            _captureCamera = null; _captureAtmosphere = null; _captureHeart = null; _captureHud = null; _heldPlayer = null;
        }
        private void CancelCapture()
        {
            Coroutine active = _capture; PlayerController owner = _captureOwner;
            _capture = null; _captureOwner = null;
            if (active != null && owner != null) owner.StopCoroutine(active);
            RestoreCapture();
        }

        private CaptureRecord SaveFrame(string prefix, string view, string kind)
        {
            FantasyProofSnapshotV1 observed = FantasyProofV1.Capture();
            if (!observed.hasCamera || QaTools.Busy) throw new InvalidOperationException("The actual gameplay camera is not available for this proof frame.");
            Texture2D shot = null, opaque = null;
            try
            {
                shot = ScreenCapture.CaptureScreenshotAsTexture();
                if (shot == null || shot.width < 32 || shot.height < 32) throw new InvalidOperationException("No rendered Game view was captured. Keep Game view visible and retry.");
                opaque = new Texture2D(shot.width, shot.height, TextureFormat.RGB24, false);
                opaque.SetPixels32(shot.GetPixels32()); opaque.Apply(false);
                string file = prefix + "_" + _receipt.captures.Count.ToString("D3", CultureInfo.InvariantCulture) + ".png";
                File.WriteAllBytes(Path.Combine(_folder, file), opaque.EncodeToPNG());
                CaptureRecord record = new CaptureRecord { file = file, requestedView = view, kind = kind, utc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture), width = shot.width, height = shot.height, observed = observed };
                _receipt.captures.Add(record); _receipt.latest = observed;
                if (observed.freeRoam && observed.gameState == GameState.Playing.ToString()) _receipt.enteredFreeRoam = true;
                return record;
            }
            finally { if (shot != null) DestroyImmediate(shot); if (opaque != null) DestroyImmediate(opaque); }
        }
        private Color32[] ReadFramePixels(string file, out int width, out int height)
        {
            Texture2D image = new Texture2D(2, 2, TextureFormat.RGB24, false);
            try
            {
                if (!image.LoadImage(File.ReadAllBytes(Path.Combine(_folder, file)))) throw new InvalidDataException("The saved native PNG could not be decoded for comparison.");
                width = image.width; height = image.height; return image.GetPixels32();
            }
            finally { DestroyImmediate(image); }
        }
        private static bool SameContext(FantasyProofSnapshotV1 a, FantasyProofSnapshotV1 b)
        {
            return a.seed == b.seed && a.worldRevision == b.worldRevision && a.renderer == b.renderer && a.graphicsPreset == b.graphicsPreset
                && a.cameraWidth == b.cameraWidth && a.cameraHeight == b.cameraHeight && a.weather == b.weather && Mathf.Abs(a.hours - b.hours) < .0001f
                && Vector3.Distance(a.cameraPosition, b.cameraPosition) < .001f && Quaternion.Angle(a.cameraRotation, b.cameraRotation) < .01f && Mathf.Abs(a.cameraFov - b.cameraFov) < .001f
                && Vector3.Distance(a.playerPosition, b.playerPosition) < .001f && a.build.gitCommit == b.build.gitCommit && a.build.sourceFingerprint == b.build.sourceFingerprint;
        }

        private void Export()
        {
            if (string.IsNullOrEmpty(_folder) || _receipt == null) { Persist(); return; }
            try
            {
                bool pairs = true, currentCheckpoint = ReceiptMatchesCheckout();
                FantasyProofSnapshotV1 latest = EditorApplication.isPlaying && Game.Cam != null ? FantasyProofV1.Capture() : _receipt.latest;
                if (latest != null) _receipt.latest = latest;
                foreach (string view in new[] { "HOME", "STREET", "SANCTUARY", "ELEVATED", "NORTH_CIVIC", "WEST_STREET", "CAUSEWAY", "FOREST_RUINS", "ARCHIVE_CLOSEUP", "ARCHIVE_PLAYER_SCALE" })
                    if (!_receipt.pairs.Exists(p => p.view == view && p.sameContext && p.offObserved && p.onObserved && p.pixelChangeObserved && PairMatchesReceipt(p))) pairs = false;
                bool routeFrames = true;
                foreach (string view in new[] { "BASEMENT", "UNDERHEART", "GAMEPLAY", "RETURN" })
                    if (!_receipt.captures.Exists(c => c.requestedView == view && SnapshotMatchesReceipt(c.observed)
                        && c.observed.hasPlayer && c.observed.freeRoam && !c.observed.ordinaryQaBusy
                        && (view == "BASEMENT" || view == "UNDERHEART" ? !string.IsNullOrEmpty(c.observed.underheartRegion) : view != "RETURN" || string.IsNullOrEmpty(c.observed.underheartRegion)))) routeFrames = false;
                bool nativeContext = SnapshotMatchesReceipt(latest) && latest.hasPlayer && latest.freeRoam && !latest.ordinaryQaBusy;
                _receipt.nativePlaytested = currentCheckpoint && nativeContext && _receipt.enteredFreeRoam && _receipt.traversalOwnerConfirmed && _receipt.controllerQualityOwnerConfirmed && _receipt.saveVehicleOwnerConfirmed ? "NATIVE PLAYTESTED (owner-recorded checklist)" : "NOT RUN, native route checklist incomplete";
                _receipt.visibleDifferenceConfirmed = currentCheckpoint && nativeContext && pairs && _receipt.appearanceOwnerConfirmed ? "VISIBLE DIFFERENCE CONFIRMED (owner-recorded)" : "BLOCKED, required matched views or owner confirmation missing";
                _receipt.gameplayVerified = currentCheckpoint && nativeContext && _receipt.enteredFreeRoam && routeFrames && _receipt.undergroundRouteOwnerConfirmed && _receipt.interactionOwnerConfirmed ? "GAMEPLAY VERIFIED (owner-recorded interaction)" : "NOT RUN, interaction unverified";
                bool characterObserved = nativeContext && latest.playerImported && string.IsNullOrEmpty(latest.firstCharacterProblem) && latest.fr01.active && latest.fr02a.active && latest.worldRewrite.active > 0;
                bool accepted = currentCheckpoint && _receipt.enteredFreeRoam && _receipt.blockers.Count == 0 && pairs && routeFrames && _receipt.undergroundRouteOwnerConfirmed && characterObserved && _receipt.materialOwnerConfirmed && _receipt.traversalOwnerConfirmed && _receipt.controllerQualityOwnerConfirmed && _receipt.saveVehicleOwnerConfirmed && _receipt.interactionOwnerConfirmed && _receipt.appearanceOwnerConfirmed && _receipt.acceptanceOwnerConfirmed;
                _receipt.ownerAccepted = accepted ? "OWNER ACCEPTED" : "NOT RUN, owner acceptance incomplete";
                _receipt.overall = accepted ? "OWNER ACCEPTED FOR THIS CHECKPOINT" : "BLOCKED, native acceptance incomplete";
                Directory.CreateDirectory(_folder);
                File.WriteAllText(Path.Combine(_folder, "FANTASY_PROOF.json"), JsonUtility.ToJson(_receipt, true) + "\n", new UTF8Encoding(false));
                StringBuilder readme = new StringBuilder("FANTASY WORLD PROOF\n\n" + _receipt.overall + "\n");
                readme.AppendLine("Project: " + ProjectIdentity).AppendLine("Commit: " + (_receipt.build != null ? _receipt.build.gitCommit : "unknown"));
                readme.AppendLine("Source fingerprint recipe: SHA256 of sorted C#/assembly definitions, Resources shader/JSON content, package manifests, ProjectVersion and FR01/FR02A/WR01 art receipts, path plus LF-normalized UTF8 text and NUL separators. Generated proof/build stamps are excluded.");
                readme.AppendLine(_receipt.artImported).AppendLine(_receipt.nativePlaytested).AppendLine(_receipt.visibleDifferenceConfirmed).AppendLine(_receipt.gameplayVerified).AppendLine(_receipt.ownerAccepted);
                readme.AppendLine("World rewrite requires matched HOME/STREET/SANCTUARY/ELEVATED/NORTH_CIVIC/WEST_STREET views and actual BASEMENT/UNDERHEART/GAMEPLAY/RETURN frames. Labelled underground frames require an observed Underheart region; labels alone do not prove a continuous route or an encounter. The owner must confirm the activity and physical route.");
                readme.AppendLine("Actual rendered PNG frames: " + _receipt.captures.Count).AppendLine("Performance: " + _receipt.performance);
                readme.AppendLine("Controlled receipt fields avoid absolute paths, user identity and hardware identifiers; saves are stored separately. Observed Console messages are sanitized without stack traces, including known local identity tokens and obvious email addresses. Arbitrary log text and screenshots still need owner review before sharing.");
                readme.AppendLine("Save backup, if selected, is separate under ignored UserBackups/FantasyWorldProof. Nothing is automatically restored, uploaded or committed.");
                foreach (string blocker in _receipt.blockers) readme.AppendLine("BLOCKER: " + SafeMessage(blocker));
                foreach (PairRecord pair in _receipt.pairs) readme.AppendLine(pair.view + ": " + pair.result + "; " + pair.percentChanged.ToString("F3", CultureInfo.InvariantCulture) + "% changed. " + pair.reason);
                File.WriteAllText(Path.Combine(_folder, "FANTASY_PROOF_README.txt"), readme.ToString(), new UTF8Encoding(false));
                StringBuilder html = new StringBuilder("<!doctype html><meta charset=\"utf-8\"><title>Fantasy native comparison</title><style>body{font:16px sans-serif;background:#111;color:#eee;margin:24px}img{width:48%;height:auto}section{margin:32px 0}p{max-width:1000px}</style><h1>Actual Unity matched frames</h1><p>Frustum candidates and pixel changes do not prove artistic quality or completed gameplay. Required views remain blocked until observed and owner-confirmed.</p>");
                foreach (PairRecord pair in _receipt.pairs) html.Append("<section><h2>").Append(pair.view).Append("</h2><p>").Append(pair.result).Append(". ").Append(pair.percentChanged.ToString("F3", CultureInfo.InvariantCulture)).Append("% changed above threshold. ").Append(pair.reason).Append("</p><img alt=\"Legacy actual Unity\" src=\"").Append(pair.legacyFile).Append("\"><img alt=\"Fantasy requested actual Unity\" src=\"").Append(pair.fantasyFile).Append("\"></section>");
                File.WriteAllText(Path.Combine(_folder, "FANTASY_COMPARISON.html"), html.ToString(), new UTF8Encoding(false));
                Persist();
            }
            catch (Exception e) { _message = "Could not export proof: " + SafeMessage(e.Message); }
        }

        private bool ReceiptMatchesCheckout()
        {
            string sha, branch, reason;
            bool same = false;
            try { same = _receipt.build != null && GitRefReader04E.TryRead(ProjectRoot, out sha, out branch, out reason) && sha == _receipt.build.gitCommit && SourceFingerprint() == _receipt.build.sourceFingerprint; }
            catch (Exception) { same = false; }
            if (!same)
            {
                const string problem = "The checkout differs from this proof receipt. Run Verify preparation again; old screenshots and owner checkboxes do not accept a new checkpoint.";
                if (!_receipt.blockers.Contains(problem)) _receipt.blockers.Add(problem);
                _message = problem;
            }
            return same;
        }
        private bool SnapshotMatchesReceipt(FantasyProofSnapshotV1 observed)
        {
            return observed != null && observed.build != null && _receipt.build != null && observed.build.gitCommit == _receipt.build.gitCommit
                && observed.build.sourceFingerprint == _receipt.build.sourceFingerprint && observed.unityVersion == RequiredUnity && observed.worldRevision == 10;
        }
        private bool PairMatchesReceipt(PairRecord pair)
        {
            CaptureRecord before = _receipt.captures.Find(c => c.file == pair.legacyFile);
            CaptureRecord after = _receipt.captures.Find(c => c.file == pair.fantasyFile);
            if (before == null || after == null || before.observed == null || after.observed == null || _receipt.build == null) return false;
            FantasyProofSnapshotV1 a = before.observed, b = after.observed;
            return a.build.gitCommit == _receipt.build.gitCommit && b.build.gitCommit == _receipt.build.gitCommit
                && a.build.sourceFingerprint == _receipt.build.sourceFingerprint && b.build.sourceFingerprint == _receipt.build.sourceFingerprint
                && a.unityVersion == RequiredUnity && b.unityVersion == RequiredUnity && a.worldRevision == 10 && b.worldRevision == 10
                && a.seed == b.seed && (_receipt.latest == null || a.seed == _receipt.latest.seed) && SameContext(a, b);
        }
    }
}
#endif
