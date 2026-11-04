using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    [Serializable]
    public sealed class BasiliskNativeReviewWR02B
    {
        public string sourceFingerprint = "", assetFingerprint = "", bodyFingerprint = "", unityVersion = "", scope = "NOT RUN";
        public bool imported, bindValidated, idle, walk, turning, attack, impact, death, feet, skin, materials, ownerMotionApproved;
        public bool AllowsReview(string source, string asset, string body)
        {
            return source != null && asset != null && body != null && source.Length == 64 && asset.Length == 64 && body.Length == 64 && sourceFingerprint == source && assetFingerprint == asset && bodyFingerprint == body && unityVersion == "6000.3.16f1" &&
                scope == "ACTUAL NATIVE ISOLATED MOTION REVIEW" && imported && bindValidated && idle && walk && turning && attack && impact && death && feet && skin && materials && ownerMotionApproved;
        }
    }

    /// <summary>Pure contract checks for the existing basilisk pose. No spawning, movement, damage or reward ownership.</summary>
    public static class BasiliskSkinRulesWR02B
    {
        public const string SpeciesId = "crystal_basilisk";
        public const int BoneCount = 32;

        public static bool Mapping(string species, CreatureSkeleton skeleton, IList<ImportedBone04OD> mapping, IList<string> targetNames, IList<int> targetParents, out string reason)
        {
            reason = "Existing crystal_basilisk 32-bone hierarchy required.";
            if (species != SpeciesId || skeleton == null || skeleton.Count != BoneCount || targetNames == null || targetParents == null || targetNames.Count != BoneCount || targetParents.Count != BoneCount) return false;
            if (!ImportedModelRegistry04OD.ValidMapping(BoneCount, mapping, 1, out reason)) return false;
            HashSet<string> names = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < BoneCount; i++)
            {
                if (targetNames[i] != skeleton.Names[i] || !names.Add(targetNames[i]) || targetParents[i] != skeleton.Parents[i] || targetParents[i] >= i || targetParents[i] < -1)
                { reason = "Imported bone name, parent or parent-first order differs at " + i; return false; }
            }
            reason = "Complete original species hierarchy mapped. Native bind, skin and motion acceptance remains separate.";
            return true;
        }

        /// <summary>Original broken-part visuals win until the imported asset has independently accepted part masks.</summary>
        public static bool KeepCandidate(bool ownerPresent, bool rigPresent, bool ragdolled, bool hidden, PartEffect broken, int brokenCount = 0)
        { return ownerPresent && rigPresent && !ragdolled && !hidden && broken == PartEffect.None && brokenCount == 0; }

        /// <summary>Retain imported bind-local scale while copying only the original owner's local dissolve/stretch.</summary>
        public static Vector3 LocalScale(Vector3 importedBind, Vector3 originalPose)
        { return Vector3.Scale(importedBind, originalPose); }
    }
}
