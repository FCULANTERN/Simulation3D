using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;

public class DynamicBackpackLoader : MonoBehaviour
{
    public GameObject buttonPrefab;  // 預製的按鈕 (Resources/UI/BackpackItemButton)
    public Transform buttonParent;   // ScrollView Content
    public GameObject backpackPanel; // 要顯示的背包面板
    public XRInventoryManager inventoryManager; // 參考放置腳本（選物品）

    void Start()
    {
        LoadAllGLBModels();
    }

    void LoadAllGLBModels()
    {
        Object[] glbObjects = Resources.LoadAll("BackpackModels", typeof(GameObject));
        foreach (Object obj in glbObjects)
        {
            GameObject model = obj as GameObject;
            if (model != null)
            {
                GameObject newButton = Instantiate(buttonPrefab, buttonParent);
                newButton.GetComponentInChildren<TMP_Text>().text = model.name;

                int index = inventoryManager.RegisterModel(model); // 把模型加入放置列表
                newButton.GetComponent<Button>().onClick.AddListener(() =>
                {
                    inventoryManager.SetCurrentPrefab(index);
                    backpackPanel.SetActive(false); // 選擇完關閉背包
                });
            }
        }
    }
}
