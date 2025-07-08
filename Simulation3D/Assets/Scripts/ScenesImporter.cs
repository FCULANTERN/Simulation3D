using UnityEngine;
using System.IO;
using System.Threading.Tasks;
using GLTFast;

public class ScenesImporter : MonoBehaviour
{
    [Header("GLB 資料夾 (StreamingAssets 下)")]
    public string folderName = "DownloadedModels";

    [Header("自動套用的材質 (Vertex Color)")]
    public Material vertexColorMaterial;

    [Header("Transform 設定")]
    public Vector3 position = new Vector3(-1.3f, 0f, -1.6f);
    public Vector3 rotation = new Vector3(180f, 90f, 90f);
    public Vector3 scale = Vector3.one;

    /// <summary>
    /// UI 按鈕呼叫的入口方法
    /// </summary>
    public void ImportGlbModelsFromButton()
    {
        string fullPath = Path.Combine(Application.streamingAssetsPath, folderName);
        Debug.Log("📂 嘗試載入資料夾：" + fullPath);

        if (!Directory.Exists(fullPath))
        {
            Debug.LogWarning("❌ 找不到資料夾：" + fullPath);
            return;
        }

        string[] glbFiles = Directory.GetFiles(fullPath, "*.glb");

        if (glbFiles.Length == 0)
        {
            Debug.Log("⚠ 沒有找到任何 .glb 檔案");
            return;
        }

        foreach (string glbPath in glbFiles)
        {
            Debug.Log("📦 找到 GLB 檔案：" + glbPath);
            _ = LoadAndApply(glbPath);
        }
    }

    /// <summary>
    /// 非同步載入 glb 並套用 Transform 與材質
    /// </summary>
    private async Task LoadAndApply(string path)
    {
        try
        {
            Debug.Log("🔄 開始載入：" + path);

            // ✅ 先宣告 gltf
            GltfImport gltf = new GltfImport();

            // ✅ 使用 file:// URI
            string uri = new System.Uri(path).AbsoluteUri;

            bool success = await gltf.Load(uri);

            if (!success)
            {
                Debug.LogError("❌ glTFast 載入失敗：" + path);
                return;
            }

            // ✅ 再宣告 root
            GameObject root = new GameObject(Path.GetFileNameWithoutExtension(path));

            // ✅ 正確使用 async Instantiate
            bool instantiated = await gltf.InstantiateMainSceneAsync(root.transform);
            if (!instantiated)
            {
                Debug.LogError("❌ InstantiateMainSceneAsync() 失敗");
                return;
            }

            // 設定 Transform
            root.transform.position = position;
            root.transform.eulerAngles = rotation;
            root.transform.localScale = scale;

            // 套用材質
            if (vertexColorMaterial != null)
            {
                MeshRenderer[] renderers = root.GetComponentsInChildren<MeshRenderer>(true);
                foreach (var mr in renderers)
                {
                    mr.material = vertexColorMaterial;
                }
                Debug.Log($"✅ 已套用材質至 {renderers.Length} 個 MeshRenderer：{root.name}");
            }
            else
            {
                Debug.LogWarning("⚠ 未指定 vertexColorMaterial");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("🔥 匯入流程發生錯誤：" + ex.Message + "\n" + ex.StackTrace);
        }
    }

}
