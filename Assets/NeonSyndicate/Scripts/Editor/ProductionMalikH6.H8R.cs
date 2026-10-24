#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
namespace NeonSyndicate.Editor
{
    public sealed partial class ProductionMalikH6
    {
        [SerializeField] private bool _h8r;
        [MenuItem("Neon Syndicate/04OH/H8R Selected Malik gameplay review")]
        public static void OpenH8R()
        {
            ProductionMalikH6 window=GetWindow<ProductionMalikH6>("Malik art review");
            window.SelectVersion(true);window._h8r=true;window._fullBodyH8=true;
            window._prepared=AssetDatabase.LoadAssetAtPath<GameObject>(window.Prepared);
            window._report="Owner-selected human, fitted wardrobe and three LODs. Appearance NOT APPROVED. Prepare in EditMode, then start Gothic Civic H5 normally. This explicit review reuses the original H8 pose, IK and recoil authority. Revert restores fallback. Native validation is not inferred from preparation.";
        }
    }
}
#endif
