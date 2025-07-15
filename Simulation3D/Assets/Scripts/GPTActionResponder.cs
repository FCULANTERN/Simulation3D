using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;
using System.Text.RegularExpressions;

public class GPTActionResponder : MonoBehaviour
{
    [Header("Azure OpenAI 設定")]
    public string apiKey = "請填入你的金鑰";
    public string endpoint = "https://你的資源名稱.openai.azure.com/";
    public string deploymentName = "gpt35";
    public string apiVersion = "2024-03-01-preview";

    [Header("TTS API")]
    public string ttsApiUrl = "http://localhost:5001/speak";

    [TextArea(2, 5)]
    public string userPrompt = "我們一起跳舞吧！";

    [Header("動畫控制器")]
    public MultiCharacterController characterController;

    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // 測試用
        //StartCoroutine(CallGPT(userPrompt));
    }

    public IEnumerator CallGPT(string prompt)
    {
        string url = $"{endpoint}openai/deployments/{deploymentName}/chat/completions?api-version={apiVersion}";

        string systemInstruction = "請用以下格式回答我：你要說的話 + *你會做的動作*。" +
        "不需要解讀語意，請隨機從這些動作中選一個：跳舞、揮手、鼓掌、敬禮、打招呼。" +
        "例如：太好了，我來了！ *敬禮*";

        string json = $@"
        {{
            ""messages"": [
                {{""role"": ""system"", ""content"": ""{systemInstruction}""}},
                {{""role"": ""user"", ""content"": ""{prompt}""}}
            ],
            ""temperature"": 0.7
        }}";

        byte[] body = Encoding.UTF8.GetBytes(json);

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(body);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("api-key", apiKey);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string rawJson = request.downloadHandler.text;
                string reply = ExtractGPTReply(rawJson);
                string action = ExtractAction(reply);
                string message = RemoveActionTag(reply);

                Debug.Log($"🗣 回應文字 👉 {message}");
                Debug.Log($"🎭 動作提示 👉 {action}");

                PlayAnimation(action);
                StartCoroutine(CallTTS(message));
            }
            else
            {
                Debug.LogError("❌ GPT 請求失敗：" + request.error);
                Debug.LogError("❌ 錯誤內容：" + request.downloadHandler.text);
            }
        }
    }

    IEnumerator CallTTS(string message)
    {
        string jsonBody = "{\"text\": \"" + message + "\"}";
        byte[] jsonBytes = Encoding.UTF8.GetBytes(jsonBody);

        UnityWebRequest request = new UnityWebRequest(ttsApiUrl, "POST");
        request.uploadHandler = new UploadHandlerRaw(jsonBytes);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            byte[] audioData = request.downloadHandler.data;
            WAV wav = new WAV(audioData);

            AudioClip clip = AudioClip.Create("TTS", wav.SampleCount, 1, wav.Frequency, false);
            clip.SetData(wav.LeftChannel, 0);

            audioSource.clip = clip;
            audioSource.Play();
        }
        else
        {
            Debug.LogError("❌ TTS 請求失敗：" + request.error);
        }
    }

    string ExtractGPTReply(string json)
    {
        var match = Regex.Match(json, "\"content\"\\s*:\\s*\"(.*?)\"", RegexOptions.Singleline);
        if (match.Success)
        {
            string result = match.Groups[1].Value;
            result = result.Replace("\\n", "\n").Replace("\\\"", "\"");
            return result;
        }
        return "[無法解析回應]";
    }

    string ExtractAction(string text)
    {
        var match = Regex.Match(text, @"\*(.*?)\*");
        return match.Success ? match.Groups[1].Value : "";
    }

    string RemoveActionTag(string text)
    {
        return Regex.Replace(text, @"\*.*?\*", "").Trim();
    }

    void PlayAnimation(string actionName)
    {
        if (string.IsNullOrEmpty(actionName))
        {
            Debug.LogWarning("❗ 未指定動作名稱");
            return;
        }

        string trigger = MapActionToTrigger(actionName);

        if (!string.IsNullOrEmpty(trigger) && characterController != null)
        {
            characterController.TriggerCurrentCharacterAnimation(trigger);
        }
        else
        {
            Debug.LogWarning($"❌ 找不到對應動畫觸發器或未設定角色控制器！");
        }
    }

    string MapActionToTrigger(string action)
    {
        switch (action)
        {
            case "跳舞": return "Action1";
            case "揮手": return "Action2";
            case "鼓掌": return "Action3";
            case "敬禮": return "Action4";
            case "打招呼": return "Action5";
            default: return null;
        }
    }
}
