#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace NeonSyndicate.Editor
{
    /// <summary>
    /// Explicit, reversible Editor review of the curated H8S listening loop on Dante Rook, the exact canonical Forge actor.
    /// It verifies the motion data and its source hash, blends the loop through the original pose solver only, and ends on
    /// Revert, window close, Play-mode exit and assembly reload. Nothing is saved, enabled or approved.
    /// </summary>
    public sealed class QuietMotionReviewH8S : EditorWindow
    {
        [SerializeField] private string _report="Quiet cast motion review: NOT APPROVED, not native-tested. Start ordinary gameplay, walk to the Forge so Dante Rook is spawned at his post, then start the review.";
        [SerializeField] private bool _forceTorsoOnly;
        private NpcController _actor;
        private Vector2 _scroll;

        [MenuItem("Neon Syndicate/04OH/H8S Dante quiet listening review")]
        public static void Open(){GetWindow<QuietMotionReviewH8S>("Quiet cast review");}

        private void OnEnable(){EditorApplication.playModeStateChanged+=PlayState;AssemblyReloadEvents.beforeAssemblyReload+=Stop;}
        private void OnDisable(){Stop();EditorApplication.playModeStateChanged-=PlayState;AssemblyReloadEvents.beforeAssemblyReload-=Stop;}
        private void PlayState(PlayModeStateChange state){if(state==PlayModeStateChange.ExitingPlayMode || state==PlayModeStateChange.ExitingEditMode)Stop();}

        private void OnGUI()
        {
            _scroll=EditorGUILayout.BeginScrollView(_scroll);
            EditorGUILayout.HelpBox("Adds only the supplied listening loop to Dante Rook's existing pose. His dialogue (E / Y), Forge service, schedule, navigation, faction and damage stay with their original owners. The loop yields at once to walking, combat, gestures and directed dialogue, gives the head back to the original head-look, and keeps the arms original while he holds his hammer (the folded pose would pass through it).",MessageType.Info);
            _forceTorsoOnly=EditorGUILayout.Toggle("Torso only (arms original)",_forceTorsoOnly);
            using(new EditorGUI.DisabledScope(!EditorApplication.isPlaying))
                if(GUILayout.Button("Start quiet listening review on Dante Rook"))Begin();
            if(GUILayout.Button("Revert to original pose"))Stop();
            if(_actor!=null && _actor.Anim!=null && _actor.Anim.Pose!=null)
            {
                HumanoidPose pose=_actor.Anim.Pose;
                EditorGUILayout.LabelField("Quiet clip",string.IsNullOrEmpty(pose.QuietClipH8S)?"(yielding to an original owner)":pose.QuietClipH8S);
                EditorGUILayout.LabelField("Weight",pose.QuietWeightH8S.ToString("0.00"));
                EditorGUILayout.LabelField("Head and neck",pose.QuietHeadYieldH8S?"original head-look":"listening loop");
            }
            EditorGUILayout.TextArea(_report,GUILayout.MinHeight(110));
            EditorGUILayout.EndScrollView();
        }
        private void OnInspectorUpdate(){if(_actor!=null)Repaint();}

        private void Begin()
        {
            try
            {
                Stop();
                if(Game.Campaign==null)throw new InvalidOperationException("The campaign director is not running. Start ordinary gameplay first.");
                NpcController actor=Game.Campaign.CastActor(CastIds04E.Dante);
                if(actor==null || actor.Anim==null)throw new InvalidOperationException("Dante Rook is not spawned. Unlock him in the story and walk to the Forge; no substitute is created.");
                TextAsset asset=AssetDatabase.LoadAssetAtPath<TextAsset>(MalikReviewH8S.MotionPath);
                if(asset==null)throw new InvalidDataException("The curated H8S motion asset has not imported: "+MalikReviewH8S.MotionPath);
                MalikDataH8 data=JsonUtility.FromJson<MalikDataH8>(asset.text);string reason;
                if(!new MalikMotionH8().Valid(data,out reason))throw new InvalidDataException(reason);
                if(!File.Exists(MalikReviewH8S.SourcePath) || ImportedArtProof04OH.FileHash(MalikReviewH8S.SourcePath)!=data.sourceSha256)
                    throw new InvalidDataException("The listening loop no longer matches its recorded Blender source. Re-export and validate before review.");
                bool holdsTool=_forceTorsoOnly || HoldsTool(actor);
                if(!actor.Anim.BeginQuietReviewH8S(actor,CastIds04E.Dante,data,holdsTool,out reason))throw new InvalidOperationException(reason);
                _actor=actor;
                _report="REVIEW RUNNING on the exact canonical actor "+actor.DisplayName+" (cast id "+CastIds04E.Dante+"), clip "+QuietMotionH8S.ClipName+", source "+data.sourceSha256+", arms "+(holdsTool?"original (tool held)":"from the loop")+".\nCheck in Unity: the loop plays while he stands at his post; it stops when he walks, when you talk to him (E / Y) or when he gestures; the head turns to you; the hammer never passes through his body; the Forge still opens. Record what you saw; nothing is marked passed automatically.";
            }
            catch(Exception e){Stop();_report="Review refused safely: "+e.Message;Log.Warn(_report);}
        }
        /// <summary>A visible held costume tool (the hammer) under the actor's rig.</summary>
        private static bool HoldsTool(NpcController actor)
        {
            foreach(Renderer r in actor.GetComponentsInChildren<Renderer>(false))
                if(r!=null && r.enabled && r.gameObject.name.StartsWith("Costume Tool",StringComparison.Ordinal))return true;
            return false;
        }
        private void Stop()
        {
            if(_actor!=null && _actor.Anim!=null)_actor.Anim.EndQuietReviewH8S();
            _actor=null;
        }
    }
}
#endif
