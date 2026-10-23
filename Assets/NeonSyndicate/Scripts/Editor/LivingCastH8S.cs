#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace NeonSyndicate.Editor
{
    /// <summary>
    /// Explicit, reversible Editor review of Dante Rook's supplied H8S visual on the live canonical actor, through the
    /// original ImportedVisual04OD adapter. Outside Play mode the review menu verifies the receipt, the shared textures and
    /// the source blend, applies complete-weight import settings and prepares a disabled review prefab (generated, never
    /// committed). In Play mode it binds that prefab to Game.Campaign.CastActor("dante"); no substitute is ever spawned.
    /// Unlimited skin weights are raised only while a review runs and the prior value is restored on End, on any failure
    /// part-way through, when Play mode changes and before an assembly reload. Nothing is enabled, approved or saved.
    /// The only statics are this Editor session's fields, cleared on every one of those exits.
    /// </summary>
    [InitializeOnLoad]
    public static partial class LivingCastH8S
    {
        private const string ReviewMenu = "Neon Syndicate/04OH/H8S Dante living cast review";
        private const string EndMenu = "Neon Syndicate/04OH/End H8S Dante living cast review";

        private static NpcController _actor;
        private static int _session;
        private static bool _qualityOwned;
        private static SkinWeights _priorWeights;

        static LivingCastH8S()
        {
            EditorApplication.playModeStateChanged += PlayState;
            AssemblyReloadEvents.beforeAssemblyReload += BeforeReload;
        }

        /// <summary>Outside Play mode: verify and prepare the disabled review copy. In Play mode: bind it to the live Dante.</summary>
        [MenuItem(ReviewMenu)]
        public static void Review()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isPlaying)
            {
                Log.Warn("H8S Dante living cast review: wait until Play mode has started, then choose the menu again.");
                return;
            }
            if (EditorApplication.isPlaying) Begin();
            else PrepareOrVerify();
        }

        /// <summary>Ends a running review and restores the procedural body, costume, cadence and skin weights.</summary>
        [MenuItem(EndMenu)]
        public static void End()
        {
            bool running = _actor != null;
            Stop();
            if (!running) Log.Info("H8S Dante living cast review: no review was running. Skin weights are at the project value.");
        }

        private static void PlayState(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode || state == PlayModeStateChange.ExitingEditMode || state == PlayModeStateChange.EnteredEditMode) Stop();
        }

        private static void BeforeReload()
        {
            // Private adapter fields do not survive a reload, so the binding must be undone while it can still restore.
            Stop();
        }

        private static void Begin()
        {
            Stop();
            int token = ++_session;
            try
            {
                Receipt receipt = ReadReceipt();
                VerifySharedTextures();
                VerifySource();
                GameObject prepared = AssetDatabase.LoadAssetAtPath<GameObject>(DanteReviewH8S.PreparedPrefab);
                if (prepared == null) throw new InvalidOperationException("No review copy yet. Stop Play mode, choose \"" + ReviewMenu + "\" once to prepare it, then return.");
                string summary;
                ImportedModel04OD row = ReviewRow(prepared, receipt, out summary);
                string reason;
                if (!DanteReviewH8S.CanReview(row, true, out reason)) throw new InvalidDataException(reason);
                CampaignDirector campaign = Game.Campaign;
                if (campaign == null || Game.Player == null) throw new InvalidOperationException("Start the existing world normally (Gothic Civic H5, revision 10) with living Malik first.");
                NpcController actor = campaign.CastActor(CastIds04E.Dante);
                if (actor == null)
                {
                    string why;
                    campaign.CastAvailable(CastIds04E.Dante, out why);
                    throw new InvalidOperationException("Dante Rook is not standing in the world now. " + why);
                }
                Vector3 post;
                float yaw;
                bool bound = campaign.CastPost(CastIds04E.Dante, out post, out yaw);
                bool hasCamera = Game.Cam != null && Game.Cam.Cam != null;
                Vector3 camera = hasCamera ? Game.Cam.Cam.transform.position : Vector3.zero;
                if (!LivingCastPresenceH8S.CanEnter(bound, post, actor.transform.position, hasCamera, camera, true, Game.PlayerPosition))
                    throw new InvalidOperationException("Walk to Dante at his Forge post (within " + LivingCastPresenceH8S.ViewEnter + " m) and wait until he stands at his station, then choose the menu again.");
                string proportions = Proportions(prepared, actor);
                // Explicit Editor-only live inspection scope. No project asset or persistent setting is written.
                _priorWeights = QualitySettings.skinWeights;
                QualitySettings.skinWeights = SkinWeights.Unlimited;
                _qualityOwned = true;
                if (!ImportedVisual04OD.BeginNpcReviewH8S(actor, row, () => Ended(token), out reason)) throw new InvalidOperationException(reason);
                _actor = actor;
                string quiet = StartQuietH8S(actor);
                Log.Info("H8S Dante living cast review RUNNING on the canonical actor. Asset " + row.ArtProof.AssetId + " / " + row.ArtProof.LicenseId
                    + " / export SHA-256 " + row.ArtProof.ExportSha256 + " / visual dependencies " + row.ArtProof.VisualDependencyHash + ". " + summary
                    + " Hidden duplicated costume layers: " + ImportedVisual04OD.NpcReviewCostumeLayersH8S(actor) + " (apron bib, apron skirt and belt); the hammer stays. "
                    + proportions + " " + quiet
                    + " Candidate stays DISABLED and NOT APPROVED. Dialogue, quest giving, the Forge service, navigation and damage stay with the original owners."
                    + " Walk away past " + LivingCastPresenceH8S.ViewExit + " m and the procedural body returns; come back and the visual returns. Use \"" + EndMenu + "\" to finish.");
            }
            catch (Exception e)
            {
                if (token == _session) _actor = null;
                RestoreQuality();
                Log.Warn("H8S Dante living cast review refused safely, the procedural Dante Rook stays: " + e.Message);
            }
        }

        /// <summary>Adds the curated H8S listening loop to the same review through the original pose owner. It is optional:
        /// a refusal is reported and the visual review continues. Arms stay original while the hammer is held.</summary>
        private static string StartQuietH8S(NpcController actor)
        {
            try
            {
                TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(MalikReviewH8S.MotionPath);
                if (asset == null) return "Quiet listening loop: not started (the curated motion asset has not imported).";
                MalikDataH8 data = JsonUtility.FromJson<MalikDataH8>(asset.text);
                string reason;
                if (!new MalikMotionH8().Valid(data, out reason)) return "Quiet listening loop: refused (" + reason + ").";
                if (ProjectFileHashH8S.Sha256(MalikReviewH8S.SourcePath) != data.sourceSha256) return "Quiet listening loop: refused (its recorded Blender source changed).";
                if (!actor.Anim.BeginQuietReviewH8S(actor, CastIds04E.Dante, data, true, out reason)) return "Quiet listening loop: refused (" + reason + ").";
                return "Quiet listening loop: RUNNING, torso only while the hammer is held; it yields to walking, talking, gestures and head-look.";
            }
            catch (Exception e)
            {
                return "Quiet listening loop: refused safely (" + e.Message + ").";
            }
        }

        /// <summary>The adapter's cleanup for one session. A late cleanup from an older session changes nothing.</summary>
        private static void Ended(int token)
        {
            if (token != _session) return;
            _actor = null;
            RestoreQuality();
            Log.Info("H8S Dante living cast review closed (ended, or the actor despawned, fell, died or skin quality changed). The procedural Dante Rook, his apron, belt and animation cadence are restored.");
        }

        private static void Stop()
        {
            NpcController actor = _actor;
            _actor = null;
            if (EditorApplication.isPlaying && actor != null) ImportedVisual04OD.EndNpcReviewH8S(actor);
            _session++;
            RestoreQuality();
        }

        private static void RestoreQuality()
        {
            if (!_qualityOwned) return;
            _qualityOwned = false;
            // Respect a value the owner chose meanwhile; only undo this review's own change.
            if (QualitySettings.skinWeights == SkinWeights.Unlimited) QualitySettings.skinWeights = _priorWeights;
        }
    }
}
#endif
