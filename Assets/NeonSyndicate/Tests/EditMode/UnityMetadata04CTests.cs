using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
#endif

namespace NeonSyndicate.Tests
{
    /// <summary>Guard metadata format and verify actual Editor import and source inclusion.</summary>
    public sealed class UnityMetadata04CTests
    {
        [TestCase("180937b2c4b745e4bb42615b272b6be5e", false)]
        [TestCase("18b37ff5a738427e9f634318eb79508f1", false)]
        [TestCase("0000000000000000f0000000000000000", false)]
        [TestCase("0000000000000000f000000000000000", true)]
        [TestCase("0123456789abcdef0123456789abcdef", true)]
        [TestCase("0123456789abcdef0123456789abcdeg", false)]
        public void GuidFormatRequiresExactlyThirtyTwoHexCharacters(string value, bool expected)
        {
            Assert.That(Regex.IsMatch(value, @"\A[0-9a-fA-F]{32}\z"), Is.EqualTo(expected));
        }

#if UNITY_EDITOR
        [Test, Category("NeedsEngine")]
        public void ProjectMetadataIsValidUniqueAndComplete()
        {
            HashSet<string> guids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string path in Directory.GetFileSystemEntries(Application.dataPath, "*", SearchOption.AllDirectories))
            {
                string normalized = path.Replace('\\', '/');
                if (normalized.Contains("/_Recovery/") || normalized.EndsWith("/_Recovery", StringComparison.Ordinal)) continue;
                if (!path.EndsWith(".meta", StringComparison.Ordinal))
                {
                    Assert.That(File.Exists(path + ".meta"), Is.True, "Missing sidecar: " + path);
                    continue;
                }
                MatchCollection values = Regex.Matches(File.ReadAllText(path), @"^guid:\s*(\S+)\s*$", RegexOptions.Multiline);
                Assert.That(values.Count, Is.EqualTo(1), path);
                string guid = values[0].Groups[1].Value;
                Assert.That(Regex.IsMatch(guid, @"\A[0-9a-fA-F]{32}\z"), Is.True, path);
                Assert.That(guids.Add(guid), Is.True, "Duplicate GUID: " + path);
                string asset = path.Substring(0, path.Length - 5);
                Assert.That(File.Exists(asset) || Directory.Exists(asset), Is.True, "Orphan metadata: " + path);
            }
        }

        [Test, Category("NeedsEngine")]
        public void TerritoryPlacementIsImportedAndIncludedExactlyOnce()
        {
            const string path = "Assets/NeonSyndicate/Scripts/Runtime/Gameplay/Territories/TerritoryPlacement.cs";
            Assert.That(AssetDatabase.LoadAssetAtPath<MonoScript>(path), Is.Not.Null, "Script must be an imported MonoScript asset.");
            string guid = AssetDatabase.AssetPathToGUID(path);
            Assert.That(Regex.IsMatch(guid, @"\A[0-9a-fA-F]{32}\z"), Is.True);
            Assert.That(AssetDatabase.GUIDToAssetPath(guid), Is.EqualTo(path));
            int sourceCount = 0;
            foreach (UnityEditor.Compilation.Assembly assembly in CompilationPipeline.GetAssemblies(AssembliesType.Player))
            {
                if (assembly.name != "NeonSyndicate.Runtime") continue;
                foreach (string source in assembly.sourceFiles)
                    if (Regex.IsMatch(File.ReadAllText(source), @"\bclass\s+TerritoryPlacement\b")) sourceCount++;
            }
            Assert.That(sourceCount, Is.EqualTo(1), "Actual runtime compilation input must include exactly one placement type.");
            Assert.That(typeof(TerritoryPlacement).Assembly.GetType("NeonSyndicate.TerritoryPlacement"), Is.EqualTo(typeof(TerritoryPlacement)));
        }

        [Test, Category("NeedsEngine")]
        public void Authored04AMaterialsImportTheirIntendedShader()
        {
            string folder = Path.Combine(Application.dataPath, "NeonSyndicate/Resources/VisualRebirth04A/Materials");
            string[] paths = Directory.GetFiles(folder, "*.mat");
            Assert.That(paths.Length, Is.EqualTo(56));
            foreach (string absolute in paths)
            {
                string path = "Assets" + absolute.Substring(Application.dataPath.Length).Replace('\\', '/');
                Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
                Assert.That(material, Is.Not.Null, path);
                Assert.That(material.shader, Is.Not.Null, path);
                Assert.That(material.shader.name, Is.EqualTo("Standard"), path);
            }
        }
#endif
    }
}
