using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.IO;

[RequireComponent(typeof(AudioSource))]



public class WhisperRingRecorder : MonoBehaviour
{
    public int sampleRate = 44100;
    public float silenceThreshold = 0.01f;
    public float silenceDuration = 1.0f;

    private AudioClip clip;
    public GPTActionResponder gptResponder; // 拖到 Inspector 連接

    private int micPosition = 0;
    private float silenceTimer = 0f;
    private bool isRecording = false;
    private string filePath;

    void Start()
    {
        filePath = Path.Combine(Application.persistentDataPath, "auto_record.wav");
        StartListening(); // 一開始就開始錄音
    }

    public void StartListening()
    {
        if (isRecording) return;

        Debug.Log("🎙 開始錄音...");
        clip = Microphone.Start(null, true, 60, sampleRate); // 最長錄60秒
        isRecording = true;
        silenceTimer = 0f;
        micPosition = 0;
        StartCoroutine(CheckSilence());
    }

    IEnumerator CheckSilence()
    {
        while (isRecording)
        {
            yield return new WaitForSeconds(0.1f);

            int pos = Microphone.GetPosition(null);
            if (pos <= 0 || pos == micPosition) continue;

            float[] samples = new float[pos - micPosition];
            clip.GetData(samples, micPosition);

            float maxVolume = 0f;
            foreach (float sample in samples)
                maxVolume = Mathf.Max(maxVolume, Mathf.Abs(sample));

            if (maxVolume < silenceThreshold)
            {
                silenceTimer += 0.1f;
                if (silenceTimer >= silenceDuration)
                {
                    StopRecording();
                    break;
                }
            }
            else
            {
                silenceTimer = 0f; // 有聲音 → 重置計時
            }

            micPosition = pos;
        }
    }

    void StopRecording()
    {
        if (!isRecording) return;

        isRecording = false;
        Microphone.End(null);
        Debug.Log("🛑 偵測靜音，停止錄音...");

        byte[] wavData = WavUtility.FromAudioClip(clip);
        File.WriteAllBytes(filePath, wavData);
        Debug.Log("💾 已儲存音訊：" + filePath);

        StartCoroutine(SendToWhisper(filePath));
    }

    IEnumerator SendToWhisper(string path)
    {
        Debug.Log("📤 傳送音檔到 Whisper 伺服器...");
        byte[] audioBytes = File.ReadAllBytes(path);

        WWWForm form = new WWWForm();
        form.AddBinaryData("audio", audioBytes, "recorded.wav", "audio/wav");

        using (UnityWebRequest request = UnityWebRequest.Post("http://localhost:5000/transcribe", form))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("❌ 傳送失敗：" + request.error);
            }
            else
            {
                string whisperJson = request.downloadHandler.text;
                Debug.Log("✅ Whisper 回應：" + whisperJson);

                string whisperText = "";
                try
                {
                    var match = System.Text.RegularExpressions.Regex.Match(whisperJson, "\"text\"\\s*:\\s*\"(.*?)\"");
                    if (match.Success)
                    {
                        whisperText = match.Groups[1].Value;
                    }
                    else
                    {
                        Debug.LogWarning("⚠️ Whisper 回應中找不到 text 欄位");
                    }
                }
                catch
                {
                    whisperText = "[解析 Whisper 回應失敗]";
                }

                if (gptResponder != null && !string.IsNullOrEmpty(whisperText))
                {
                    StartCoroutine(gptResponder.CallGPT(whisperText));
                }
            }
        }

        // 🌀 完成後再次開始錄音
        StartListening();
    }
}
