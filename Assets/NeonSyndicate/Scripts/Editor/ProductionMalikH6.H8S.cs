#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
namespace NeonSyndicate.Editor
{
    public sealed partial class ProductionMalikH6
    {
        [SerializeField] private bool _h8s;
        [SerializeField] private bool _landingH8S=true;
        [MenuItem("Neon Syndicate/04OH/H8S Selected Malik living motion review")]
        public static void OpenH8S()
        {
            ProductionMalikH6 window=GetWindow<ProductionMalikH6>("Malik art review");
            window.SelectVersion(true);window._h8r=true;window._h8s=true;window._fullBodyH8=true;
            window._prepared=AssetDatabase.LoadAssetAtPath<GameObject>(window.Prepared);
            window._report="H8S derivative of the owner-selected human: continuous rolled collar, three LODs and the curated seventeen-action set (schema 2). Appearance NOT APPROVED. Prepare in EditMode, then start Gothic Civic H5 normally and request the reversible review. It reuses the original H8 pose, IK, recoil and controller authority and adds the authored landing after real air time. Revert restores the fallback. Native validation is not inferred from preparation.";
        }
        /// <summary>Review switch for the authored landing (H8S only). It changes the live review at once.</summary>
        private void DrawLandingH8S()
        {
            if(!_h8s)return;
            bool was=_landingH8S;
            _landingH8S=EditorGUILayout.Toggle("H8S landing after real air time",_landingH8S);
            if(was!=_landingH8S)ApplyLandingH8S();
            EditorGUILayout.HelpBox("A landing starts only on the grounded edge after at least "+LandingTriggerH8S.MinAirTime.ToString("0.00")+" s in the air with no travel power, combat move, guard, dodge, reload, aim or gesture during the flight. Short flights use the reviewed H8 dip; flights from "+LandingTriggerH8S.HeavyAirTime.ToString("0.00")+" s use the supplied heavy landing from its touchdown frame, with the pelvis lowered so the planted foot stays on the ground (the supplied action has no root translation). It blends in over "+LandingTriggerH8S.BlendIn.ToString("0.00")+" s and out over "+LandingTriggerH8S.BlendOut.ToString("0.00")+" s, lightens while you keep moving and stops at once on any unsafe state. The controller, feet, collision and camera stay original.",MessageType.Info);
            if(Game.Player!=null && Game.Player.Anim!=null && Game.Player.Anim.Pose!=null)
            {
                LandingTriggerH8S landing=Game.Player.Anim.Pose.LandingH8S;
                EditorGUILayout.LabelField("Landing",landing.Active?(landing.Heavy?"heavy, ":"small, ")+(landing.Progress*100f).ToString("0")+"%":"idle (air "+landing.AirTime.ToString("0.00")+" s)");
            }
        }
        /// <summary>Pushes the landing switch into the live review pose, if a review is running.</summary>
        private void ApplyLandingH8S()
        {
            if(Game.Player!=null && Game.Player.Anim!=null && Game.Player.Anim.Pose!=null)Game.Player.Anim.Pose.ReviewLandingH8S=_h8s && _landingH8S;
        }
    }
}
#endif
