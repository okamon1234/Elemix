using UnityEditor;
using UnityEngine;
namespace RogueSurvivors.Editor
{
    public sealed class MapArtImporter : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if(!assetPath.Contains("RogueSurvivors/Art/Maps/") || !assetPath.EndsWith("_v11.png"))return;
            var importer=(TextureImporter)assetImporter;importer.textureType=TextureImporterType.Default;
            bool props=assetPath.EndsWith("props_v11.png");importer.alphaIsTransparency=props;
            importer.wrapMode=props?TextureWrapMode.Clamp:TextureWrapMode.Repeat;importer.mipmapEnabled=!props;
            importer.npotScale=TextureImporterNPOTScale.None;importer.maxTextureSize=4096;importer.filterMode=FilterMode.Bilinear;importer.textureCompression=TextureImporterCompression.Uncompressed;
        }
    }
}
