#if UNITY_EDITOR
using System;
using UnityEditor;
namespace NeonSyndicate.Editor
{
    /// <summary>
    /// Modern import settings for delivered art whose .meta file carries no import settings at all (the repository keeps
    /// such minimal .meta files for many of the H6 to H9, H8R, H8S and candidate FBX and PNG deliveries). For those, Unity
    /// took its legacy upgrade path: FBX models came in with a Legacy rig (an Animation component the live character check
    /// refuses) and external material copies, and images came in as cube maps (owner report 2026-10-24). This applies only
    /// while the settings are missing, so an importer that already has settings, or anything outside the art folder and the
    /// tracked CrystalGothicProof materials, is never touched. The live character preparation states the same settings
    /// itself, so this only spares a fresh checkout the wrong first import.
    /// </summary>
    public sealed class ArtImportDefaultsH8S : AssetPostprocessor
    {
        private const string ArtRoot = "Assets/NeonSyndicate/Art/";
        private const string Excluded = "Assets/NeonSyndicate/Art/CrystalGothicProof/";

        private bool Applies()
        {
            return assetImporter != null && assetImporter.importSettingsMissing
                && assetPath.StartsWith(ArtRoot, StringComparison.Ordinal) && !assetPath.StartsWith(Excluded, StringComparison.Ordinal);
        }

        private void OnPreprocessTexture()
        {
            if (!Applies()) return;
            TextureImporter texture = assetImporter as TextureImporter;
            if (texture == null) return;
            // Only the shape: colour, normal and data types are set by the preparation that knows each map's role.
            texture.textureShape = TextureImporterShape.Texture2D;
        }

        private void OnPreprocessModel()
        {
            if (!Applies()) return;
            ModelImporter model = assetImporter as ModelImporter;
            if (model == null) return;
            model.animationType = ModelImporterAnimationType.Generic;
            model.importCameras = false;
            model.importLights = false;
            model.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            model.materialLocation = ModelImporterMaterialLocation.InPrefab;
        }
    }
}
#endif
