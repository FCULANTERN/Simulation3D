using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

public class BackpackAndBlockSystem : MonoBehaviour
{
    [Header("UI References")]
    public GameObject buttonPrefab;         // 按鈕預製體（含 Image + TMP_Text）
    public Transform buttonParent;          // 放置按鈕的 Panel（Grid/Horizontal Layout）
    public GameObject backpackCanvas;       // 背包 UI Canvas

    [Header("Block Placement")]
    public Transform rayOrigin;             // XR 控制器射線起點
    public LayerMask placeable;      // 可放置區域 Layer
    public float maxDistance = 10f;
    public InputActionProperty triggerAction; // XR Trigger Input

    private string thumbnailPath => Path.Combine(Application.streamingAssetsPath, "ModelThumbnails");

    private string currentSelectedBlockName = "";

    void Start()
    {
        PositionCanvasInFrontOfPlayer();
        StartCoroutine(GenerateButtonsFromThumbnails());
    }

    void Update()
    {
        if (triggerAction.action.WasPressedThisFrame())
        {
            // 只要能拿就拿，不管有沒有選模型
            if (TryPickUpBlock())
            {
                return; // 成功拿到就不做放置
            }

            // 沒拿到才考慮放置
            if (!string.IsNullOrEmpty(currentSelectedBlockName))
            {
                TryPlaceBlock(currentSelectedBlockName);
            }
        }
    }

    void PositionCanvasInFrontOfPlayer()
    {
        Camera cam = Camera.main;
        if (!cam) return;

        Vector3 forward = cam.transform.forward;
        forward.y = 0;
        forward.Normalize();

        Vector3 targetPosition = cam.transform.position + forward * 2f + Vector3.up * 0.2f;
        backpackCanvas.transform.position = targetPosition;
        backpackCanvas.transform.rotation = Quaternion.LookRotation(forward);
    }

    IEnumerator GenerateButtonsFromThumbnails()
    {
        if (!Directory.Exists(thumbnailPath))
        {
            Debug.LogWarning("❌ 找不到縮圖資料夾: " + thumbnailPath);
            yield break;
        }

        string[] pngFiles = Directory.GetFiles(thumbnailPath, "*.png");

        foreach (string filePath in pngFiles)
        {
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            byte[] data = File.ReadAllBytes(filePath);
            Texture2D texture = new Texture2D(2, 2);
            if (texture.LoadImage(data))
            {
                CreateButton(fileName, texture);
            }
            else
            {
                Debug.LogWarning("❌ 無法載入圖片: " + fileName);
            }

            yield return null;
        }

        Debug.Log("✅ 所有圖片按鈕已生成");
    }

    void CreateButton(string name, Texture2D texture)
    {
        GameObject buttonGO = Instantiate(buttonPrefab, buttonParent);
        buttonGO.name = "Btn_" + name;

        Image targetImage = buttonGO.transform.Find("Image")?.GetComponent<Image>();
        if (targetImage != null)
        {
            Sprite s = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            s.name = name + "_sprite";
            targetImage.sprite = s;
            targetImage.color = Color.white;
        }

        TMP_Text label = buttonGO.GetComponentInChildren<TMP_Text>();
        if (label != null)
        {
            label.text = name;
        }

        buttonGO.GetComponent<Button>().onClick.AddListener(() =>
        {
            currentSelectedBlockName = name;
            Debug.Log("👉 已選擇模型: " + name);
            backpackCanvas.SetActive(false);
        });
    }

    void TryPlaceBlock(string modelName)
    {
        Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);
        Debug.DrawRay(ray.origin, ray.direction * maxDistance, Color.green, 1f);

        if (Physics.Raycast(ray, out var hit, maxDistance, placeable))
        {
            GameObject prefab = Resources.Load<GameObject>("BackpackModels/" + modelName);
            if (prefab != null)
            {
                Vector3 placePos = SnapToGrid(hit.point);
                GameObject placed = Instantiate(prefab, placePos, Quaternion.identity);
                placed.AddComponent<BackpackObject>(); // ✅ 標記是背包放出來的物件

                MeshFilter[] meshFilters = placed.GetComponentsInChildren<MeshFilter>(true);
                foreach (var mf in meshFilters)
                {
                    GameObject go = mf.gameObject;
                    if (go.GetComponent<Collider>() == null)
                    {
                        var collider = go.AddComponent<MeshCollider>();
                        collider.convex = true;
                        collider.isTrigger = true;
                    }
                }
            }
            else
            {
                Debug.LogWarning("❌ 無法載入 prefab: BackpackModels/" + modelName);
            }
        }
        else
        {
            Debug.Log("⚠ 沒有打到可放置區域");
        }
    }

    bool TryPickUpBlock()
    {
        Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);
        if (Physics.Raycast(ray, out var hit, maxDistance))
        {
            GameObject target = hit.collider.gameObject;

            BackpackObject marker = target.GetComponentInParent<BackpackObject>();
            if (marker != null)
            {
                Debug.Log("✅ 找到 BackpackObject，開始拿取：" + marker.gameObject.name);
                Destroy(marker.gameObject); // ❗ 記得刪掉整個父物件
                Debug.Log("🧲 拿起了：" + marker.gameObject.name);
                return true;
            }
            else
            {
                Debug.Log("❌ 找不到 BackpackObject");
            }
        }
        return false;
    }

    Vector3 SnapToGrid(Vector3 pos)
    {
        return new Vector3(
            Mathf.Round(pos.x),
            Mathf.Round(pos.y),
            Mathf.Round(pos.z)
        );
    }
}
