using UnityEngine;
using UnityEditor;
using System.IO;

public class GenerateModelThumbnails : EditorWindow
{
    private static string modelsPath = "Assets/Resources/BackpackModels";
    private static string savePath = "Assets/StreamingAssets/ModelThumbnails";

    [MenuItem("Tools/Generate GLB Model Thumbnails")]
    static void GenerateThumbnails()
    {
        if (!Directory.Exists(savePath))
        {
            Directory.CreateDirectory(savePath);
        }

        string[] modelGuids = AssetDatabase.FindAssets("t:GameObject", new[] { modelsPath });

        foreach (string guid in modelGuids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            GameObject modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);

            if (modelPrefab == null) continue;

            Texture2D thumbnail = AssetPreview.GetAssetPreview(modelPrefab);
            if (thumbnail == null)
            {
                Debug.LogWarning($"尚未生成預覽圖: {assetPath}，請稍後再試");
                continue;
            }

            byte[] pngData = thumbnail.EncodeToPNG();
            string modelName = Path.GetFileNameWithoutExtension(assetPath);
            string outputFile = Path.Combine(savePath, modelName + ".png");
            File.WriteAllBytes(outputFile, pngData);
            Debug.Log($"✅ 已寫入縮圖: {outputFile}");

            // ➤ 強制匯入資產並轉換為 Sprite
            string relativePath = outputFile.Replace(Application.dataPath, "Assets");
            AssetDatabase.ImportAsset(relativePath, ImportAssetOptions.ForceUpdate);

            TextureImporter importer = AssetImporter.GetAtPath(relativePath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.alphaIsTransparency = true;
                importer.alphaSource = TextureImporterAlphaSource.FromInput;
                importer.mipmapEnabled = false;
                importer.isReadable = true;
                importer.SaveAndReimport();

                Debug.Log($"🖼 設為 Sprite 並重新導入: {relativePath}");
            }
        }

        AssetDatabase.Refresh();
        Debug.Log("🎉 完成所有縮圖產生與匯入！");
    }
}
