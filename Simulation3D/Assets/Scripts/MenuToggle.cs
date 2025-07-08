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
    }
}
