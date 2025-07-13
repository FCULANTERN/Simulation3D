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
    public Vector3 position = new Vector3(10f, 0f, 10f);
    public Vector3 rotation = Vector3.zero;
    public Vector3 scale = Vector3.one;

    private GameObject baseFloor;

    void Start()
    {
        CreateBaseFloor();
    }

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

    private void CreateBaseFloor()
    {
        baseFloor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        baseFloor.name = "TemporaryBaseFloor";
        baseFloor.transform.position = new Vector3(0, 0, 0);
        baseFloor.transform.localScale = new Vector3(10, 1, 10);

        // 🔁 設定圖層為 "Placeable"
        baseFloor.layer = LayerMask.NameToLayer("Placeable");

        var collider = baseFloor.GetComponent<Collider>();
        if (collider != null)
        {
            collider.isTrigger = false;
            var physicsMaterial = new PhysicsMaterial
            {
                dynamicFriction = 0.6f,
                staticFriction = 0.6f,
                bounciness = 0,
                frictionCombine = PhysicsMaterialCombine.Multiply,
                bounceCombine = PhysicsMaterialCombine.Minimum
            };
            collider.material = physicsMaterial;
        }

        var rb = baseFloor.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
    }

    private async Task LoadAndApply(string path)
    {
        try
        {
            Debug.Log("🔄 開始載入：" + path);

            GltfImport gltf = new GltfImport();
            string uri = new System.Uri(path).AbsoluteUri;
            bool success = await gltf.Load(uri);

            if (!success)
            {
                Debug.LogError("❌ glTFast 載入失敗：" + path);
                return;
            }

            GameObject root = new GameObject(Path.GetFileNameWithoutExtension(path));
            bool instantiated = await gltf.InstantiateMainSceneAsync(root.transform);

            if (!instantiated)
            {
                Debug.LogError("❌ InstantiateMainSceneAsync() 失敗");
                return;
            }

            SetLayerRecursively(root, LayerMask.NameToLayer("Placeable"));

            root.transform.position = position;
            root.transform.eulerAngles = new Vector3(-90f, 0f, 0f);
            root.transform.localScale = scale;

            if (vertexColorMaterial != null)
            {
                MeshRenderer[] renderers = root.GetComponentsInChildren<MeshRenderer>(true);
                foreach (var mr in renderers)
                {
                    mr.material = vertexColorMaterial;
                }
                Debug.Log($"✅ 已套用材質至 {renderers.Length} 個 MeshRenderer：{root.name}");
            }

            MeshFilter[] meshFilters = root.GetComponentsInChildren<MeshFilter>(true);
            foreach (var mf in meshFilters)
            {
                if (mf.gameObject.GetComponent<Collider>() == null)
                {
                    var meshCollider = mf.gameObject.AddComponent<MeshCollider>();
                    meshCollider.convex = false;
                    meshCollider.isTrigger = false;
                }
            }

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
    
    private void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }
}
