using UnityEngine;
using System.Collections;
using SimpleFileBrowser;

public class ImageSelector : MonoBehaviour
{
    public static string selectedImagePath;  // 讓其他腳本可讀

    public void OpenFileBrowser()
    {
        StartCoroutine(ShowLoadDialogCoroutine());
    }

    IEnumerator ShowLoadDialogCoroutine()
    {
        yield return FileBrowser.WaitForLoadDialog(
            pickMode: FileBrowser.PickMode.Files,
            allowMultiSelection: false,
            initialPath: null,
            title: "選擇圖片",
            loadButtonText: "載入"
        );

        if (FileBrowser.Success)
        {
            selectedImagePath = FileBrowser.Result[0];
            Debug.Log("✅ 選擇圖片路徑：" + selectedImagePath);
        }
    }
}
