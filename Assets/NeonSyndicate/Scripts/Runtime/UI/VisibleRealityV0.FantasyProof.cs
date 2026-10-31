using UnityEngine;

namespace NeonSyndicate
{
    public sealed partial class VisibleRealityV0
    {
        private FantasyProofSnapshotV1 _fantasyProofSnapshotV1;
        private float _fantasyProofNextV1;
        private Vector2 _fantasyProofScrollV1;

        private void RegisterFantasyProofV1()
        { DebugPages.Register(new MenuPage { Title = "Fantasy proof", Order = 58, Draw = DrawFantasyProofV1 }); }

        private void DrawFantasyProofV1(Rect r)
        {
            if (_fantasyProofSnapshotV1 == null || Time.unscaledTime >= _fantasyProofNextV1)
            { _fantasyProofSnapshotV1 = FantasyProofV1.Capture(); _fantasyProofNextV1 = Time.unscaledTime + 1f; }
            FantasyProofSnapshotV1 proof = _fantasyProofSnapshotV1;
            if (Event.current != null && Event.current.type == EventType.Repaint && Pad.Connected && Mathf.Abs(Pad.RightStick.y) > .2f)
                _fantasyProofScrollV1.y = Mathf.Clamp(_fantasyProofScrollV1.y - Pad.RightStick.y * 700f * Time.unscaledDeltaTime, 0, Mathf.Max(0, 3600 - r.height));
            _fantasyProofScrollV1 = GUI.BeginScrollView(r, _fantasyProofScrollV1, new Rect(0, 0, r.width - 24, 3600), false, true);
            try
            {
                Rect row = new Rect(0, 0, r.width - 24, 3600); float y = 0;
                Line(row, ref y, "FANTASY WORLD: ACTUAL RUNNING PROJECT", UIKit.Gold, 32f);
                Line(row, ref y, proof.summary, UIKit.Gold, 92f);
                Line(row, ref y, "Branch: " + proof.build.gitBranch + "\nCommit: " + proof.build.gitCommit + (!proof.build.dirtyKnown ? " (working tree status unknown)" : proof.build.dirty ? " (local changes)" : " (clean)") + "\nProject: " + proof.build.projectIdentity, Color.white, 64f);
                Line(row, ref y, "Source fingerprint: " + proof.build.sourceFingerprint, Color.white, 44f);
                Line(row, ref y, "Unity " + proof.unityVersion + "; revision " + proof.worldRevision + "; seed " + proof.seed + "; " + proof.renderer + "; " + proof.graphicsPreset + "; " + proof.gameState + "; Free Roam " + proof.freeRoam, Color.white, 58f);
                Line(row, ref y, "Player " + proof.playerPosition.ToString("F1") + "; camera " + proof.cameraWidth + "x" + proof.cameraHeight + "; ordinary QA busy " + proof.ordinaryQaBusy, Color.white, 44f);
                DrawProofLayerV1(row, ref y, proof.fr01);
                DrawProofLayerV1(row, ref y, proof.fr02a);
                DrawWorldRewriteProofWR01(row, ref y, proof.worldRewrite);
                Line(row, ref y, "Underheart region: " + proof.underheartRegion, Color.white, 44f);
                Line(row, ref y, proof.liveCastBinding + "; nearby imported " + proof.nearbyImportedCharacters + ", fallback " + proof.nearbyFallbackCharacters + "; character appearance: " + proof.characterAppearance, Color.white, 58f);
                Line(row, ref y, "Current required material problem: " + (proof.firstCharacterProblem.Length > 0 ? proof.firstCharacterProblem : "none observed in the bound Malik/Dante materials"), UIKit.Gold, 72f);
                Line(row, ref y, "First retained LiveCast reason: " + proof.retainedLiveCastProblem, UIKit.Gold, 72f);
                Line(row, ref y, proof.visibilityMethod, Color.white, 110f);
                Line(row, ref y, "Stop Play, then Neon Syndicate > Acceptance > Verify Fantasy World for checkout validation, native review and screenshot evidence. A selection or renderer count does not certify a visible transformation.", Color.white, 100f);
            }
            finally { GUI.EndScrollView(); }
        }

        private static void DrawWorldRewriteProofWR01(Rect row, ref float y, FantasyRewriteProofWR01 rewrite)
        {
            Line(row, ref y, "WORLD REWRITE: selected " + rewrite.selected + "; prepared " + rewrite.prepared + "; policy " + rewrite.policyActive, UIKit.Cyan, 48f);
            Line(row, ref y, "Planned " + rewrite.planned + "; admitted " + rewrite.admitted + "; observed active " + rewrite.active + "; legacy groups suppressed " + rewrite.legacyGroupsSuppressed + "; refused " + rewrite.refused, Color.white, 58f);
            Line(row, ref y, "Imported civic crown renderers " + rewrite.importedCrownRenderers + "; imported crown frustum candidates " + rewrite.importedCrownFrustumRenderers + ". Retained physical witness surfaces cannot substitute for imported art evidence.", Color.white, 66f);
            Line(row, ref y, "Imported skyline renderers " + rewrite.importedSkylineRenderers + "; imported skyline frustum candidates " + rewrite.importedSkylineFrustumRenderers, Color.white, 58f);
            Line(row, ref y, "Active renderer components " + rewrite.activeRenderers + "; near player " + rewrite.nearPlayerRenderers + "; camera-frustum candidates " + rewrite.inCameraFrustumRenderers + ". Original underground streaming can hide admitted cells.", Color.white, 66f);
            foreach (FantasyRewriteRecordWR01 record in rewrite.records)
                if (!record.admitted) Line(row, ref y, record.stableId + " / " + record.family + ": " + record.reason, UIKit.Gold, 76f);
            Line(row, ref y, rewrite.status, Color.white, 90f);
        }

        private static void DrawProofLayerV1(Rect row, ref float y, FantasyLayerProofV1 layer)
        {
            Line(row, ref y, layer.name + ": prepared " + layer.prepared + ", valid imported models " + layer.preparedModels + ", selected " + layer.selected + ", policy " + layer.policyActive + ", observed active " + layer.active, UIKit.Cyan, 48f);
            Line(row, ref y, "Instantiated module roots " + layer.instantiatedObjects + "; active roots " + layer.activeObjects + "; active renderer components " + layer.activeRenderers + "; near player " + layer.nearPlayerRenderers + "; camera-frustum candidates " + layer.inCameraFrustumRenderers, Color.white, 70f);
            Line(row, ref y, "Placement refusals " + layer.refusedPlacements + " (native " + layer.nativeRefusedPlacements + "); activation refusals " + layer.activationRefusals + ". First placement/preparation refusal: " + layer.firstRefusal + "\nFirst native refusal: " + layer.firstNativeRefusal + "\nFirst activation refusal: " + layer.firstActivationRefusal, UIKit.Gold, 130f);
            Line(row, ref y, layer.status, Color.white, 94f);
        }
    }
}
