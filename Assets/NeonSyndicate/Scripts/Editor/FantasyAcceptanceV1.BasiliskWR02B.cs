#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NeonSyndicate.Editor
{
    public sealed partial class FantasyAcceptanceV1
    {
        private PreviewRenderUtility _basiliskPreviewWR02B;
        private GameObject _basiliskPreviewRootWR02B, _basiliskPoseRootWR02B, _basiliskGroundWR02B;
        private Mesh _basiliskGroundMeshWR02B;
        private Transform[] _basiliskOriginalBonesWR02B, _basiliskTargetBonesWR02B;
        private Quaternion[] _basiliskImportedBindWR02B;
        private CreaturePose _basiliskPoseWR02B;
        private BasiliskReviewAssetWR02B _basiliskProfileWR02B;
        private Creature _basiliskReviewOwnerWR02B;
        private readonly float[] _basiliskSeenWR02B = new float[7];
        private int _basiliskModeWR02B, _basiliskAttackWR02B;
        private float _basiliskClockWR02B;
        private float[] _basiliskAttacksSeenWR02B;
        private double _basiliskPreviousWR02B;
        private bool _basiliskOpenWR02B;
        private int _basiliskLodWR02B;
        private readonly float[] _basiliskLodsSeenWR02B = new float[3];
        private string _basiliskMessageWR02B = "Native motion review NOT RUN. Original creatures retained.";
        private static string[] BasiliskModesWR02B { get { return new[] { "Idle", "Walk", "Turning", "Attack", "Impact", "Death", "Sloped ground" }; } }

        private void CloseBasiliskWR02B()
        {
            if (_basiliskReviewOwnerWR02B != null) ImportedVisual04OD.EndBasiliskNativeReviewWR02B(_basiliskReviewOwnerWR02B);
            _basiliskReviewOwnerWR02B = null;
            if (_basiliskPreviewWR02B != null) { _basiliskPreviewWR02B.Cleanup(); _basiliskPreviewWR02B = null; }
            if (_basiliskPoseRootWR02B != null) DestroyImmediate(_basiliskPoseRootWR02B);
            if (_basiliskGroundMeshWR02B != null) DestroyImmediate(_basiliskGroundMeshWR02B);
            _basiliskGroundMeshWR02B = null; _basiliskGroundWR02B = null;
            _basiliskPoseRootWR02B = null; _basiliskPreviewRootWR02B = null; _basiliskPoseWR02B = null;
        }
        private void StartBasiliskWR02B()
        {
            CloseBasiliskWR02B(); Array.Clear(_basiliskLodsSeenWR02B, 0, 3); Array.Clear(_basiliskSeenWR02B, 0, _basiliskSeenWR02B.Length);
            _receipt.basiliskWR02B = new BasiliskNativeReviewWR02B();
            try
            {
                if (_receipt.build == null || SourceFingerprint() != _receipt.build.sourceFingerprint || Application.unityVersion != "6000.3.16f1") throw new InvalidOperationException("Verify this exact source in Unity 6000.3.16f1 first.");
                _basiliskProfileWR02B = UnityEditor.AssetDatabase.LoadAssetAtPath<BasiliskReviewAssetWR02B>("Assets/NeonSyndicate/Resources/HeartPreparedWR02B/BasiliskReview.asset");
                ImportedModel04OD model = _basiliskProfileWR02B != null ? _basiliskProfileWR02B.Model : null;
                if (model == null || model.Enabled || model.Prefab == null) throw new InvalidOperationException("Prepare the disabled review candidate first.");
                foreach (Component component in model.Prefab.GetComponentsInChildren<Component>(true)) if (component == null || !(component is Transform || component is SkinnedMeshRenderer || component is LODGroup)) throw new InvalidOperationException("Review prefab contains nonvisual authority.");
                Creature owner = Selection.activeGameObject != null ? Selection.activeGameObject.GetComponentInParent<Creature>() : null;
                CreatureBody body = owner != null && owner.Ready && owner.Species.Id == BasiliskSkinRulesWR02B.SpeciesId && owner.CRig != null ? owner.CRig.Body : CreatureBuilder.Build(CreatureLooks.For(BasiliskSkinRulesWR02B.SpeciesId, 61724), 0);
                CreatureSkeleton skeleton = body.Skeleton; _basiliskPoseWR02B = new CreaturePose(body);
                _basiliskPreviewWR02B = new PreviewRenderUtility();
                _basiliskPreviewRootWR02B = Instantiate(model.Prefab); _basiliskPreviewRootWR02B.hideFlags = HideFlags.HideAndDontSave;
                _basiliskPreviewWR02B.AddSingleGO(_basiliskPreviewRootWR02B);
                LODGroup group = _basiliskPreviewRootWR02B.GetComponent<LODGroup>(); if (group != null) group.ForceLOD(0);
                _basiliskGroundWR02B = new GameObject("Isolated mathematical slope witness, no collision") { hideFlags = HideFlags.HideAndDontSave };
                _basiliskGroundMeshWR02B = new Mesh { name = "Preview ground witness" };
                _basiliskGroundMeshWR02B.vertices = new[] { new Vector3(-7, 0, -7), new Vector3(-7, 0, 7), new Vector3(7, 0, 7), new Vector3(7, 0, -7) };
                _basiliskGroundMeshWR02B.triangles = new[] { 0, 1, 2, 0, 2, 3 }; _basiliskGroundMeshWR02B.uv = new[] { Vector2.zero, Vector2.up * 7, Vector2.one * 7, Vector2.right * 7 }; _basiliskGroundMeshWR02B.RecalculateNormals(); _basiliskGroundMeshWR02B.RecalculateBounds();
                _basiliskGroundWR02B.AddComponent<MeshFilter>().sharedMesh = _basiliskGroundMeshWR02B;
                _basiliskGroundWR02B.AddComponent<MeshRenderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/NeonSyndicate/Resources/HeartPreparedWR02/CausewayStone.mat");
                _basiliskPreviewWR02B.AddSingleGO(_basiliskGroundWR02B);
                _basiliskPoseRootWR02B = new GameObject("Isolated original pose math only") { hideFlags = HideFlags.HideAndDontSave };
                _basiliskPreviewWR02B.AddSingleGO(_basiliskPoseRootWR02B);
                _basiliskOriginalBonesWR02B = new Transform[skeleton.Count]; _basiliskTargetBonesWR02B = new Transform[skeleton.Count]; _basiliskImportedBindWR02B = new Quaternion[skeleton.Count];
                List<string> names = new List<string>(); List<int> parents = new List<int>();
                for (int i = 0; i < skeleton.Count; i++)
                {
                    Transform bone = new GameObject(skeleton.Names[i]).transform; bone.SetParent(skeleton.Parents[i] < 0 ? _basiliskPoseRootWR02B.transform : _basiliskOriginalBonesWR02B[skeleton.Parents[i]], false); bone.localPosition = skeleton.LocalPositions[i]; _basiliskOriginalBonesWR02B[i] = bone;
                    Transform target = _basiliskPreviewRootWR02B.transform.Find(model.Bones[i].TargetPath); if (target == null) throw new InvalidOperationException("Missing mapped preview bone."); _basiliskTargetBonesWR02B[i] = target; names.Add(target.name);
                    int parent = -1; for (Transform p = target.parent; p != null; p = p.parent) { int index = Array.IndexOf(_basiliskTargetBonesWR02B, p); if (index >= 0) { parent = index; break; } } parents.Add(parent);
                    bool found = false; Matrix4x4 bind = Matrix4x4.identity;
                    foreach (SkinnedMeshRenderer skin in _basiliskPreviewRootWR02B.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                    {
                        if (skin.sharedMesh == null || skin.sharedMesh.bindposes.Length != skin.bones.Length) throw new InvalidOperationException("Native skin bind table invalid.");
                        int index = Array.IndexOf(skin.bones, target); if (index < 0) continue;
                        Matrix4x4 actual = skin.transform.localToWorldMatrix * skin.sharedMesh.bindposes[index].inverse;
                        if (found && (actual.GetColumn(3) - bind.GetColumn(3)).sqrMagnitude > .000004f) throw new InvalidOperationException("Conflicting native LOD bind matrices."); bind = actual; found = true;
                        foreach (Material material in skin.sharedMaterials) if (material == null || material.shader == null || !material.shader.isSupported || material.mainTexture == null) throw new InvalidOperationException("Native candidate shader or albedo missing.");
                    }
                    CreatureSkeleton canonical = CreatureBuilder.Build(CreatureLooks.For(BasiliskSkinRulesWR02B.SpeciesId, 61724), 0).Skeleton;
                    if (!found || (_basiliskPreviewRootWR02B.transform.InverseTransformPoint(bind.GetColumn(3)) - canonical.ModelPosition(i)).sqrMagnitude > .000009f) throw new InvalidOperationException("Native bind differs from authored canonical body.");
                    _basiliskImportedBindWR02B[i] = bind.rotation;
                }
                string reason; if (!BasiliskSkinRulesWR02B.Mapping(BasiliskSkinRulesWR02B.SpeciesId, skeleton, model.Bones, names, parents, out reason)) throw new InvalidOperationException(reason);
                _receipt.basiliskWR02B = new BasiliskNativeReviewWR02B { sourceFingerprint = _receipt.build.sourceFingerprint, assetFingerprint = _basiliskProfileWR02B.AssetFingerprint, bodyFingerprint = ImportedVisual04OD.BasiliskBodyFingerprintWR02B(body), unityVersion = Application.unityVersion, scope = "ACTUAL NATIVE ISOLATED MOTION REVIEW", imported = true, bindValidated = true };
                _basiliskAttacksSeenWR02B = new float[CreatureAttackDb.For(BasiliskSkinRulesWR02B.SpeciesId).Length];
                _basiliskClockWR02B = 0; _basiliskPreviousWR02B = EditorApplication.timeSinceStartup;
                _basiliskMessageWR02B = "Isolated native preview uses original CreaturePose and actual attack timings. No actor, AI, hitbox, reward or scene authority is spawned. Check each motion only after observing it.";
            }
            catch (Exception error) { CloseBasiliskWR02B(); _basiliskMessageWR02B = "REFUSED: " + error.Message; }
        }
        private void DrawBasiliskWR02B()
        {
            _basiliskOpenWR02B = EditorGUILayout.Foldout(_basiliskOpenWR02B, "WR02-B isolated basilisk motion review", true); if (!_basiliskOpenWR02B) return;
            EditorGUILayout.HelpBox(_basiliskMessageWR02B, MessageType.Info);
            if (GUILayout.Button("Start isolated original-pose review (selected existing body, or canonical seed 61724)")) StartBasiliskWR02B();
            if (_basiliskPreviewWR02B == null) return;
            int mode = EditorGUILayout.Popup("Motion", _basiliskModeWR02B, BasiliskModesWR02B); if (mode != _basiliskModeWR02B) { _basiliskModeWR02B = mode; _basiliskClockWR02B = 0; _basiliskPoseWR02B.Reset(Vector3.zero, 0); }
            CreatureAttack[] attacks = CreatureAttackDb.For(BasiliskSkinRulesWR02B.SpeciesId); string[] attackNames = new string[attacks.Length]; for (int i = 0; i < attacks.Length; i++) attackNames[i] = attacks[i].Name;
            _basiliskAttackWR02B = EditorGUILayout.Popup("Original attack", _basiliskAttackWR02B, attackNames); _basiliskAttackWR02B = Mathf.Clamp(_basiliskAttackWR02B, 0, attacks.Length - 1);
            _basiliskLodWR02B = EditorGUILayout.Popup("Mesh LOD", _basiliskLodWR02B, new[] { "LOD0", "LOD1", "LOD2" });
            LODGroup lodGroup = _basiliskPreviewRootWR02B.GetComponent<LODGroup>(); if (lodGroup != null) lodGroup.ForceLOD(_basiliskLodWR02B);
            Rect rect = GUILayoutUtility.GetRect(320, 260, GUILayout.ExpandWidth(true));
            if (Event.current.type == EventType.Repaint)
            {
                float dt = Mathf.Clamp((float)(EditorApplication.timeSinceStartup - _basiliskPreviousWR02B), .001f, .05f); _basiliskPreviousWR02B = EditorApplication.timeSinceStartup; _basiliskClockWR02B += dt;
                CreaturePoseInput input = CreaturePoseInput.Idle(Vector3.zero, 0); input.Time = _basiliskClockWR02B;
                if (_basiliskModeWR02B == 1 || _basiliskModeWR02B == 6) { input.RootPosition = Vector3.forward * _basiliskClockWR02B * 2; input.Velocity = Vector3.forward * 2; }
                if (_basiliskModeWR02B == 2) { input.Yaw = _basiliskClockWR02B * 45; input.YawRate = 45; }
                if (_basiliskModeWR02B == 3) { CreatureAttack attack = attacks[_basiliskAttackWR02B]; AttackTiming timing = CreatureAttackDb.Timing(attack, 1, Mathf.Max(.05f, _basiliskPoseWR02B.Body.Height)); input.Action = attack.Action; input.ActionT = timing.ActionT(_basiliskClockWR02B % (timing.Total + .7f)); input.Windup = timing.WindupFrac; input.Strike = timing.StrikeFrac; }
                if (_basiliskModeWR02B == 4) input.Hurt = (_basiliskClockWR02B % 2) < .2f ? 1 : 0;
                if (_basiliskModeWR02B == 5) { input.Action = CreatureAction.Death; input.ActionT = Mathf.Clamp01(_basiliskClockWR02B / 2); }
                if (_basiliskModeWR02B == 6) { input.GroundHeight = (x, z) => z * .12f; input.RootPosition.y = input.RootPosition.z * .12f; }
                _basiliskPreviewRootWR02B.transform.SetPositionAndRotation(input.RootPosition, Quaternion.Euler(0, input.Yaw, 0));
                _basiliskGroundWR02B.transform.SetPositionAndRotation(input.RootPosition, Quaternion.Euler(_basiliskModeWR02B == 6 ? -Mathf.Atan(.12f) * Mathf.Rad2Deg : 0, 0, 0));
                _basiliskPoseWR02B.Solve(ref input, dt);
                _basiliskPoseRootWR02B.transform.SetPositionAndRotation(input.RootPosition, Quaternion.Euler(0, input.Yaw, 0));
                for (int i = 0; i < _basiliskOriginalBonesWR02B.Length; i++) { _basiliskOriginalBonesWR02B[i].localPosition = _basiliskPoseWR02B.LocalPositions[i]; _basiliskOriginalBonesWR02B[i].localRotation = _basiliskPoseWR02B.LocalRotations[i]; _basiliskTargetBonesWR02B[i].SetPositionAndRotation(_basiliskOriginalBonesWR02B[i].position, _basiliskOriginalBonesWR02B[i].rotation * _basiliskImportedBindWR02B[i]); }
                _basiliskPreviewWR02B.camera.transform.position = input.RootPosition + new Vector3(7, 4, 8); _basiliskPreviewWR02B.camera.transform.LookAt(input.RootPosition + Vector3.up); _basiliskPreviewWR02B.camera.nearClipPlane = .05f; _basiliskPreviewWR02B.camera.farClipPlane = 100;
                _basiliskPreviewWR02B.lights[0].intensity = 1.1f; _basiliskPreviewWR02B.lights[0].transform.rotation = Quaternion.Euler(45, 35, 0); _basiliskPreviewWR02B.ambientColor = new Color(.3f, .3f, .35f);
                _basiliskPreviewWR02B.BeginPreview(rect, GUIStyle.none); _basiliskPreviewWR02B.camera.Render(); Texture image = _basiliskPreviewWR02B.EndPreview(); GUI.DrawTexture(rect, image, ScaleMode.ScaleToFit); _basiliskSeenWR02B[_basiliskModeWR02B] += dt;
                _basiliskLodsSeenWR02B[_basiliskLodWR02B] += dt;
                if (_basiliskModeWR02B == 3) _basiliskAttacksSeenWR02B[_basiliskAttackWR02B] += dt;
            }
            BasiliskNativeReviewWR02B review = _receipt.basiliskWR02B;
            using (new EditorGUI.DisabledScope(_basiliskSeenWR02B[_basiliskModeWR02B] < 3))
            {
                if (_basiliskModeWR02B == 0) review.idle = EditorGUILayout.ToggleLeft("Observed idle is acceptable", review.idle);
                if (_basiliskModeWR02B == 1) review.walk = EditorGUILayout.ToggleLeft("Observed movement and feet are acceptable", review.walk);
                if (_basiliskModeWR02B == 2) review.turning = EditorGUILayout.ToggleLeft("Observed turning is acceptable", review.turning);
                if (_basiliskModeWR02B == 3 && Array.TrueForAll(_basiliskAttacksSeenWR02B, seconds => seconds >= 6)) review.attack = EditorGUILayout.ToggleLeft("I inspected ALL original attack windup, strike and recovery phases", review.attack);
                if (_basiliskModeWR02B == 4) review.impact = EditorGUILayout.ToggleLeft("Observed impact reaction is acceptable", review.impact);
                if (_basiliskModeWR02B == 5) review.death = EditorGUILayout.ToggleLeft("Observed death pose is acceptable", review.death);
                if (_basiliskModeWR02B == 6) review.feet = EditorGUILayout.ToggleLeft("Observed slope contact is acceptable; live collision still requires review", review.feet);
            }
            using (new EditorGUI.DisabledScope(!Array.TrueForAll(_basiliskLodsSeenWR02B, seconds => seconds >= 3))) review.skin = EditorGUILayout.ToggleLeft("I inspected native skin deformation at joints and all LODs", review.skin); review.materials = EditorGUILayout.ToggleLeft("I inspected native material and crystal readability", review.materials); review.ownerMotionApproved = EditorGUILayout.ToggleLeft("I approve an explicit temporary review on this exact existing body", review.ownerMotionApproved);
            if (GUILayout.Button("Review selected existing legal basilisk in Free Roam"))
            {
                Creature owner = Selection.activeGameObject != null ? Selection.activeGameObject.GetComponentInParent<Creature>() : null; string reason;
                if (SourceFingerprint() != review.sourceFingerprint) _basiliskMessageWR02B = "Source changed. Repeat verification and isolated review.";
                else if (_basiliskReviewOwnerWR02B != null) _basiliskMessageWR02B = "Close and restore the current live review before reviewing another creature.";
                else if (ImportedVisual04OD.BeginBasiliskNativeReviewWR02B(owner, _basiliskProfileWR02B.Model, review, review.sourceFingerprint, _basiliskProfileWR02B.AssetFingerprint, out reason)) { _basiliskReviewOwnerWR02B = owner; _basiliskMessageWR02B = reason; } else _basiliskMessageWR02B = reason;
            }
            if (GUILayout.Button("Close review and restore original creature")) CloseBasiliskWR02B();
            Repaint();
        }
    }
}
#endif
