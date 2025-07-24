using UnityEngine;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    public GameObject backpackCanvas;
    public GameObject generationCanvas;
    public GameObject mainMenuCanvas;

    public Button backpackButton;
    public Button generateButton;
    public XRInventoryManager inventoryManager;

    void Start()
    {
        if (backpackButton != null)
            backpackButton.onClick.AddListener(OpenBackpack);

        if (generateButton != null)
            generateButton.onClick.AddListener(OpenGenerator);

        // 初始狀態：只開啟主選單
        backpackCanvas.SetActive(false);
        generationCanvas.SetActive(false);
    }

    void OpenBackpack()
    {
        backpackCanvas.SetActive(true);
        generationCanvas.SetActive(false);
        mainMenuCanvas.SetActive(false); // 關閉主選單

        MoveCanvasToFront(backpackCanvas);
    }

    void OpenGenerator()
    {
        generationCanvas.SetActive(true);
        backpackCanvas.SetActive(false);
        mainMenuCanvas.SetActive(false); // 關閉主選單

        MoveCanvasToFront(generationCanvas);
    }

    void MoveCanvasToFront(GameObject canvas)
    {
        Transform cam = Camera.main.transform;
        Vector3 forward = cam.forward;
        forward.y = 0;
        forward.Normalize();

        float distance = 2.0f;
        Vector3 targetPosition = cam.position + forward * distance + Vector3.up * 0.2f;

        canvas.transform.position = targetPosition;
        canvas.transform.rotation = Quaternion.LookRotation(canvas.transform.position - cam.position);
    }

}
