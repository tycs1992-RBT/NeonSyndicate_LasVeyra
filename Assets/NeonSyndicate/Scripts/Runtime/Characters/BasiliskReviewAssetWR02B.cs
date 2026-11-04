using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>Disabled review data prepared by the existing Heart importer. Never an active imported-model registry.</summary>
    public sealed class BasiliskReviewAssetWR02B : ScriptableObject
    {
        public ImportedModel04OD Model;
        public AnimationClip[] PreviewClips;
        public string AssetFingerprint = "", Status = "Native candidate preparation NOT RUN.";
    }
}
