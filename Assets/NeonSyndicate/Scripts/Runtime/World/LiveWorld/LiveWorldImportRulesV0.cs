using System;

namespace NeonSyndicate
{
    /// <summary>Import corrections apply only to the shipped static art and the owner's live-world drop folder.</summary>
    public static class LiveWorldImportRulesV0
    {
        public static bool CanConfigure(string path)
        {
            if (string.IsNullOrEmpty(path) || path.IndexOf("..", StringComparison.Ordinal) >= 0) return false;
            return path.StartsWith("Assets/NeonSyndicate/Art/LiveWorld/", StringComparison.Ordinal)
                || path.StartsWith("Assets/NeonSyndicate/Art/WorldFoundationH8R/", StringComparison.Ordinal)
                || path.StartsWith("Assets/NeonSyndicate/Art/ProductionH8S/Props/", StringComparison.Ordinal);
        }

        public static bool IsPointer(string prefix)
        {
            return prefix != null && prefix.StartsWith("version https://git-lfs.github.com/spec/v1", StringComparison.Ordinal);
        }

        public static void ResetStatics() { }
    }
}
