using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DynamicBackpackLoader : MonoBehaviour
{
    public GameObject buttonPrefab;          // 按鈕預製體（BackpackItemButton）
    public Transform buttonParent;           // ScrollView Content 或任意 UI 容器
    public GameObject backpackPanel;         // 背包 UI 面板
    public XRInventoryManager inventoryManager; // 控制模型放置邏輯

    private string thumbnailPath => Path.Combine(Application.streamingAssetsPath, "ModelThumbnails");

    void Start()
    {
        LoadAllGLBModels();
    }

    void LoadAllGLBModels()
    {
        GameObject[] models = Resources.LoadAll<GameObject>("BackpackModels");
        foreach (GameObject model in models)
        {
            GameObject newButton = Instantiate(buttonPrefab, buttonParent);

            // 設定按鈕文字
            TMP_Text label = newButton.GetComponentInChildren<TMP_Text>();
            if (label != null)
                label.text = model.name;

            // 載入縮圖
            StartCoroutine(LoadThumbnail(newButton, model.name));

            // 點擊事件註冊
            int index = inventoryManager.RegisterModel(model);
            newButton.GetComponent<Button>().onClick.AddListener(() =>
            {
                inventoryManager.SetCurrentPrefab(index);
                backpackPanel.SetActive(false);
            });
        }
    }

    IEnumerator LoadThumbnail(GameObject button, string modelName)
    {
        string path = Path.Combine(thumbnailPath, modelName + ".png");

        if (!File.Exists(path))
        {
            Debug.LogWarning("❌ 找不到縮圖: " + path);
            yield break;
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        // Android 需要使用 UnityWebRequest 存取 StreamingAssets
        using (UnityEngine.Networking.UnityWebRequest request = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(path))
        {
            yield return request.SendWebRequest();
            if (request.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                Debug.LogWarning("⚠ Android載入圖片失敗: " + request.error);
                yield break;
            }

            Texture2D tex = ((DownloadHandlerTexture)request.downloadHandler).texture;
            ApplyThumbnail(button, SpriteFromTexture(tex), path);
        }
#else
        byte[] imageData = File.ReadAllBytes(path);
        Texture2D texture = new Texture2D(2, 2);
        if (texture.LoadImage(imageData))
        {
            ApplyThumbnail(button, SpriteFromTexture(texture), path);
        }
        else
        {
            Debug.LogWarning("❌ 圖片無法轉換: " + path);
        }
#endif
    }

    void ApplyThumbnail(GameObject button, Sprite sprite, string path)
    {
        // 從子物件中找名字為 "Image" 的 Image 組件
        Image[] images = button.GetComponentsInChildren<Image>(true);
        Image targetImage = null;

        foreach (var img in images)
        {
            if (img.gameObject.name == "Image")
            {
                targetImage = img;
                break;
            }
        }

        if (targetImage != null)
        {
            targetImage.sprite = sprite;
            targetImage.color = Color.white;
            Debug.Log("✅ 成功載入縮圖: " + path);
        }
        else
        {
            Debug.LogWarning("⚠ 按鈕中沒有 Image 子物件");
        }
    }

    Sprite SpriteFromTexture(Texture2D tex)
    {
        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
    }
}
