#if UNITY_EDITOR
using System;
using System.IO;
using System.Security.Cryptography;
using UnityEngine;
namespace NeonSyndicate.Editor
{
    /// <summary>
    /// Receipt fingerprint checks that survive Windows line endings (owner report 2026-10-23: the Blender Malik and Dante
    /// never went live on Tyler's Windows computer). The art receipts record each delivered file's SHA-256 as Git stores it;
    /// Git for Windows checks the receipts' text files (.json motion sets and export statistics) out with CRLF endings, so a
    /// byte-for-byte hash refused unchanged files there. A file matches its receipt when its bytes hash to the recorded value,
    /// or when it is text by Git's own test (ReceiptLineEndings) and its CR LF pairs, turned back into LF, hash to it. A Git
    /// LFS pointer, a missing file or an edited file still never matches, so the receipt keeps its full meaning.
    /// </summary>
    public static class ReceiptFileHash
    {
        /// <summary>
        /// True when the project file (a path under Assets/ or ArtSource/) matches the recorded lowercase or uppercase hex
        /// SHA-256, byte for byte or, for a text file checked out with CRLF endings, as Git stores it.
        /// </summary>
        public static bool Matches(string projectRelativePath, string expectedSha256)
        {
            if (string.IsNullOrEmpty(expectedSha256) || !ImportedArt04OH.Hex(expectedSha256, 64)) return false;
            if (string.IsNullOrEmpty(projectRelativePath) || !CinematicAcceptance04OF.RelativeBonePath(projectRelativePath)) return false;
            if (!projectRelativePath.StartsWith("Assets/", StringComparison.Ordinal) && !projectRelativePath.StartsWith("ArtSource/", StringComparison.Ordinal)) return false;
            string full = Path.Combine(Directory.GetParent(Application.dataPath).FullName, projectRelativePath);
            if (!File.Exists(full)) return false;
            byte[] data = File.ReadAllBytes(full);
            if (string.Equals(Sha256(data), expectedSha256, StringComparison.OrdinalIgnoreCase)) return true;
            if (!ReceiptLineEndings.LooksText(data, data.Length) || !ReceiptLineEndings.HasCrLf(data, data.Length)) return false;
            return string.Equals(Sha256(ReceiptLineEndings.ToLf(data, data.Length)), expectedSha256, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Lowercase hex SHA-256 of a byte array.</summary>
        public static string Sha256(byte[] data)
        {
            using (SHA256 hash = SHA256.Create())
                return BitConverter.ToString(hash.ComputeHash(data ?? new byte[0])).Replace("-", "").ToLowerInvariant();
        }
    }
}
#endif
