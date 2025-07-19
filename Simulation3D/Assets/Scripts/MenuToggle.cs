using UnityEngine;
using UnityEngine.InputSystem;

public class MenuToggle : MonoBehaviour
{
    public GameObject menuUI;
    public CameraController cameraController; // 拖入 Main Camera

    private PlayerControls controls;

    private void Awake()
    {
        controls = new PlayerControls();
        controls.UI.ToggleMenu.performed += ctx => Toggle();
    }

    private void OnEnable() => controls.UI.Enable();
    private void OnDisable() => controls.UI.Disable();

    private void Toggle()
    {
        if (menuUI == null) return;

        bool isActive = !menuUI.activeSelf;
        menuUI.SetActive(isActive);

        // 控制滑鼠游標與鎖定
        Cursor.lockState = isActive ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = isActive;

        // 停用 / 啟用鏡頭控制
        if (cameraController != null)
            cameraController.allowCameraControl = !isActive;

        if (isActive)
        {
            MoveMenuInFront();  // ← 顯示時移到面前
        }
    }

    private void MoveMenuInFront()
    {
        if (menuUI == null) return;

        Transform cam = Camera.main.transform;
        Vector3 forward = cam.forward;
        forward.y = 0;
        forward.Normalize();

        menuUI.transform.position = cam.position + forward * 1.2f + Vector3.up * 0.2f;
        menuUI.transform.LookAt(cam);
        menuUI.transform.Rotate(0, 180, 0);
    }
}
