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
    public Vector3 position = new Vector3(10f, 0f, 10f); // 例如設到遠一點
    public Vector3 rotation = Vector3.zero;
    public Vector3 scale = Vector3.one;

    private GameObject baseFloor; // 追蹤基礎地板

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

    void Start()
    {
        CreateBaseFloor();
    }

    private void CreateBaseFloor()
    {
        // 建立臨時基礎地板
        baseFloor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        baseFloor.name = "TemporaryBaseFloor";
        baseFloor.transform.position = new Vector3(0, 0, 0);
        baseFloor.transform.localScale = new Vector3(10, 1, 10);

        if (vertexColorMaterial != null)
        {
            baseFloor.GetComponent<MeshRenderer>().material = vertexColorMaterial;
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
            // 讓地板從 Z 軸轉到 Y+（X 軸轉 -90 度）
            root.transform.eulerAngles = new Vector3(-90f, 0f, 0f);
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

            // 載入後自動加上 MeshCollider
            MeshFilter[] meshFilters = root.GetComponentsInChildren<MeshFilter>(true);
            foreach (var mf in meshFilters)
            {
                if (mf.gameObject.GetComponent<Collider>() == null)
                {
                    var collider = mf.gameObject.AddComponent<MeshCollider>();
                    collider.convex = false;
                    collider.isTrigger = false; // 確保不是 trigger
                }
            }

            // 模型載入完成後，移除基礎地板
            if (baseFloor != null)
            {
                Destroy(baseFloor);
                baseFloor = null;
                Debug.Log("✅ 已移除臨時基礎地板");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("🔥 匯入流程發生錯誤：" + ex.Message + "\n" + ex.StackTrace);
        }
    }

}
