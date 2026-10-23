#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace NeonSyndicate.Editor
{
    public sealed partial class ProductionMalikH6
    {
        [SerializeField] private bool _fullBodyH8;
        [MenuItem("Neon Syndicate/04OH/H8 Living Hero gameplay review")]
        public static void OpenH8()
        {ProductionMalikH6 window=GetWindow<ProductionMalikH6>("Malik art review");window.SelectVersion(true);window._fullBodyH8=true;}
        private void DrawFullBodyH8()
        {
            if(!_h7)return;
            bool was=_fullBodyH8;_fullBodyH8=EditorGUILayout.Toggle("H8 authored full-body review",_fullBodyH8);
            if(was!=_fullBodyH8 && EditorApplication.isPlaying)Stop();
            if(_fullBodyH8)
            {
                _authoredAccents=false;
                EditorGUILayout.HelpBox("H8 samples the real H7 idle, walk/run, sword-ready and OathCut, plus H8 dodge, cast, aim and guardian cooperation through the existing IK owner. No root motion, new Animator, input lock or damage events. Unsupported actions retain original posing. Native foot contact, grip, cloth, saves and visual approval remain NOT RUN until reviewed. Changing this option stops the live review; restart explicitly.",MessageType.Info);
                if(Game.Player!=null && Game.Player.Anim!=null && Game.Player.Anim.Pose!=null)
                    EditorGUILayout.LabelField("Actual sampled action",Game.Player.Anim.Pose.ReviewClipH8??"");
                DrawLandingH8S();
            }
        }
        private void PrepareH8Clips()
        {
            if(!_fullBodyH8 || _h8s)return;
            const string path="Assets/NeonSyndicate/Art/ProductionH8/Animations/MalikActionsH8.fbx";
            ModelImporter importer=AssetImporter.GetAtPath(path) as ModelImporter;
            if(importer==null)throw new InvalidDataException("H8 action FBX is missing. Pull the actual LFS assets before preparing.");
            importer.importAnimation=true;importer.animationType=ModelImporterAnimationType.Generic;importer.animationCompression=ModelImporterAnimationCompression.Off;importer.optimizeGameObjects=false;importer.preserveHierarchy=true;
            ModelImporterClipAnimation[] settings=importer.defaultClipAnimations;
            foreach(ModelImporterClipAnimation clip in settings){clip.loopTime=clip.name.Contains("IdleBreath") || clip.name.Contains("H7_Walk") || clip.name.Contains("H7_Run") || clip.name.Contains("SwordReady") || clip.name.Contains("FireAim");clip.lockRootRotation=true;clip.lockRootHeightY=true;clip.lockRootPositionXZ=true;}
            if(settings.Length>0)importer.clipAnimations=settings;importer.SaveAndReimport();int count=0;
            foreach(UnityEngine.Object item in AssetDatabase.LoadAllAssetsAtPath(path))
            {AnimationClip clip=item as AnimationClip;if(clip==null || clip.name.StartsWith("__preview__",StringComparison.Ordinal))continue;if(clip.events.Length!=0)throw new InvalidDataException("Authored H8 clips cannot carry gameplay events.");count++;}
            if(count!=14)throw new InvalidDataException("Expected fourteen real H7/H8 clips. Inspect the native Animation tab and keep fallback.");
        }
        private void BeginFullBodyH8()
        {
            if(!_h7 || !_fullBodyH8)return;
            string path=_h8s?MalikReviewH8S.MotionPath:_h8r?MalikReviewH8R.Root+"Animations/MalikFullBodyH8R.json":"Assets/NeonSyndicate/Art/ProductionH8/Animations/MalikFullBodyH8.json";
            TextAsset asset=AssetDatabase.LoadAssetAtPath<TextAsset>(path);
            if(asset==null)throw new InvalidDataException("Actual H8 full-body action samples have not imported.");
            MalikDataH8 data=JsonUtility.FromJson<MalikDataH8>(asset.text);string reason;
            if(!new MalikMotionH8().Valid(data,out reason))throw new InvalidDataException(reason);
            // Data is derived from exactly the versioned source; source is not copied into Assets.
            string source=_h8s?MalikReviewH8S.SourcePath:_h8r?"ArtSource/AscensionH8R/MalikVeyrH8R.blend":"ArtSource/AscensionH8/MalikVeyrH8.blend";
            if(!File.Exists(source))source="ArtSource/AscensionH7/MalikVeyrH7.blend";
            if(ImportedArtProof04OH.FileHash(source)!=data.sourceSha256)throw new InvalidDataException("Authored action source hash changed. Re-export and validate before review.");
            if(Game.Player==null || Game.Player.Anim==null || !Game.Player.Anim.BeginFullBodyReviewH8(data,out reason))throw new InvalidOperationException(reason);
            ApplyLandingH8S();
        }
    }
}
#endif
