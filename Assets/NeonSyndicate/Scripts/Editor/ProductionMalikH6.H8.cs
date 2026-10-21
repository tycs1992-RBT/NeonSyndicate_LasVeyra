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
                EditorGUILayout.HelpBox("H8 samples the real H7 idle, walk/run, sword-ready and OathCut through the existing IK owner. No root motion, new Animator, input lock or damage events. Unsupported actions retain original posing. Native foot contact, grip, cloth, saves and visual approval remain NOT RUN until reviewed. Changing this option stops the live review; restart explicitly.",MessageType.Info);
                if(Game.Player!=null && Game.Player.Anim!=null && Game.Player.Anim.Pose!=null)
                    EditorGUILayout.LabelField("Actual sampled action",Game.Player.Anim.Pose.ReviewClipH8??"");
            }
        }
        private void BeginFullBodyH8()
        {
            if(!_h7 || !_fullBodyH8)return;
            const string path="Assets/NeonSyndicate/Art/ProductionH8/Animations/MalikFullBodyH8.json";
            TextAsset asset=AssetDatabase.LoadAssetAtPath<TextAsset>(path);
            if(asset==null)throw new InvalidDataException("Actual H8 full-body action samples have not imported.");
            MalikDataH8 data=JsonUtility.FromJson<MalikDataH8>(asset.text);string reason;
            if(!new MalikMotionH8().Valid(data,out reason))throw new InvalidDataException(reason);
            // Data is derived from exactly the versioned source; source is not copied into Assets.
            string source="ArtSource/AscensionH8/MalikVeyrH8.blend";
            if(!File.Exists(source))source="ArtSource/AscensionH7/MalikVeyrH7.blend";
            if(ImportedArtProof04OH.FileHash(source)!=data.sourceSha256)throw new InvalidDataException("Authored action source hash changed. Re-export and validate before review.");
            if(Game.Player==null || Game.Player.Anim==null || !Game.Player.Anim.BeginFullBodyReviewH8(data,out reason))throw new InvalidOperationException(reason);
        }
    }
}
#endif
