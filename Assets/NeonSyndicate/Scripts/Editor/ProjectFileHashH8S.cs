#if UNITY_EDITOR
using System;
using System.IO;
using System.Security.Cryptography;
using UnityEngine;
namespace NeonSyndicate.Editor
{
    /// <summary>
    /// SHA256 of an artist source or asset file named by a project-relative path. The original receipt hash
    /// (ImportedArtProof04OH.FileHash) only accepts paths under Assets/, so it returned an empty string for the Blender sources
    /// under ArtSource/ and the H8, H8R and H8S full-body reviews could never match their recorded source hash. This helper
    /// accepts exactly the two project folders that hold reviewed art.
    /// </summary>
    public static class ProjectFileHashH8S
    {
        /// <summary>Lowercase hex SHA256, or an empty string for an unsafe path, a folder outside Assets/ and ArtSource/,
        /// or a missing file.</summary>
        public static string Sha256(string projectRelativePath)
        {
            if(string.IsNullOrEmpty(projectRelativePath) || !CinematicAcceptance04OF.RelativeBonePath(projectRelativePath))return "";
            if(!projectRelativePath.StartsWith("Assets/",StringComparison.Ordinal) && !projectRelativePath.StartsWith("ArtSource/",StringComparison.Ordinal))return "";
            string full=Path.Combine(Directory.GetParent(Application.dataPath).FullName,projectRelativePath);
            if(!File.Exists(full))return "";
            using(SHA256 hash=SHA256.Create())using(FileStream stream=File.OpenRead(full))
                return BitConverter.ToString(hash.ComputeHash(stream)).Replace("-","").ToLowerInvariant();
        }
    }
}
#endif
