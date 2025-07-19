using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class XRInventoryManager : MonoBehaviour
{
    [Header("UI 與放置設定")]
    public GameObject inventoryUI; // 指向 Canvas
    public Transform rayOrigin;    // XR 控制器 Transform
    public GameObject[] availablePrefabs;

    [Header("控制器按鈕")]
    public InputActionProperty toggleInventoryAction; // 開關背包按鈕 (如 Menu/Grip)
    public InputActionProperty triggerAction;         // 放置按鈕 (如 Trigger)

    private GameObject currentPrefab; // 當前選擇的放置物件
    private bool isInventoryVisible = false;

    private void MoveCanvasToHead()
    {
        Transform head = Camera.main.transform;
        Vector3 forward = head.forward;
        forward.y = 0; // 保持水平
        forward.Normalize();

        Vector3 canvasPosition = head.position + forward * 1f + Vector3.up * 0.2f; // 微微上提
        inventoryUI.transform.position = canvasPosition;

        // 讓 UI 面向玩家
        inventoryUI.transform.LookAt(head);
        inventoryUI.transform.Rotate(0, 180f, 0);
    }

    void Update()
    {
        if (toggleInventoryAction.action.WasPressedThisFrame())
        {
            isInventoryVisible = !isInventoryVisible;
            inventoryUI.SetActive(isInventoryVisible);

            if (isInventoryVisible)
            MoveCanvasToHead(); // ← 顯示時移動到玩家前方
        }

        if (triggerAction.action.WasPressedThisFrame() && currentPrefab != null && !isInventoryVisible)
        {
            Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);
            if (Physics.Raycast(ray, out var hit, 20f))
            {
                Vector3 placePos = SnapToGrid(hit.point);
                Instantiate(currentPrefab, placePos, Quaternion.identity);
            }
        }
    }

    public void SetCurrentPrefab(int index)
    {
        if (index >= 0 && index < availablePrefabs.Length)
        {
            currentPrefab = availablePrefabs[index];
            Debug.Log("✅ 選擇了物品：" + currentPrefab.name);
            isInventoryVisible = false;
            inventoryUI.SetActive(false);
        }
    }

    Vector3 SnapToGrid(Vector3 pos)
    {
        return new Vector3(Mathf.Round(pos.x), Mathf.Round(pos.y), Mathf.Round(pos.z));
    }
}
