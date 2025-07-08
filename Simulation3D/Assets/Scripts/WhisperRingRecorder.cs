using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.IO;

[RequireComponent(typeof(AudioSource))]
public class WhisperRingRecorder : MonoBehaviour
{
    public int sampleRate = 44100;
    public float silenceThreshold = 0.03f;
    public float silenceDuration = 1.0f;
    public int preBufferSeconds = 1;

    private AudioClip micClip;
    private string filePath;
    private bool isRecordingSegment = false;
    private int segmentStartPosition = 0;
    private float silenceTimer = 0f;

    void Start()
    {
        filePath = Path.Combine(Application.persistentDataPath, "auto_record.wav");
        micClip = Microphone.Start(null, true, 60, sampleRate);
        Debug.Log("🎧 背景錄音已啟動");
        StartCoroutine(MonitorMicrophone());
    }

    IEnumerator MonitorMicrophone()
    {
        while (true)
        {
            int currentPos = Microphone.GetPosition(null);
            if (currentPos < 0) { yield return null; continue; }

            int checkLength = 1024;
            int start = currentPos - checkLength;
            if (start < 0) start += micClip.samples;

            float[] samples = new float[checkLength];
            micClip.GetData(samples, start);

            float maxVolume = 0f;
            foreach (var s in samples)
                maxVolume = Mathf.Max(maxVolume, Mathf.Abs(s));

            if (!isRecordingSegment)
            {
                if (maxVolume > silenceThreshold)
                {
                    int bufferSamples = preBufferSeconds * sampleRate;
                    segmentStartPosition = currentPos - bufferSamples;
                    if (segmentStartPosition < 0) segmentStartPosition += micClip.samples;

                    isRecordingSegment = true;
                    silenceTimer = 0f;
                    Debug.Log("🎙 偵測到聲音，開始錄音段");
                }
            }
            else
            {
                if (maxVolume < silenceThreshold)
                {
                    silenceTimer += 0.1f;
                    if (silenceTimer >= silenceDuration)
                    {
                        int segmentEndPosition = Microphone.GetPosition(null);
                        SaveAndSendSegment(segmentStartPosition, segmentEndPosition);
                        isRecordingSegment = false;
                        silenceTimer = 0f;
                        Debug.Log("🛑 錄音段結束");
                    }
                }
                else
                {
                    silenceTimer = 0f;
                }
            }

            yield return new WaitForSeconds(0.1f);
        }
    }

    void SaveAndSendSegment(int startPos, int endPos)
    {
        int totalSamples = endPos - startPos;
        if (totalSamples < 0) totalSamples += micClip.samples;

        float[] data = new float[totalSamples * micClip.channels];
        micClip.GetData(data, startPos);

        AudioClip clip = AudioClip.Create("segment", totalSamples, micClip.channels, micClip.frequency, false);
        clip.SetData(data, 0);

        byte[] wav = WavUtility.FromAudioClip(clip);
        File.WriteAllBytes(filePath, wav);
        Debug.Log("💾 錄音儲存完成：" + filePath);

        StartCoroutine(SendToWhisper(filePath));
    }

    IEnumerator SendToWhisper(string path)
    {
        Debug.Log("📤 傳送至 Whisper...");
        byte[] audioBytes = File.ReadAllBytes(path);

        WWWForm form = new WWWForm();
        form.AddBinaryData("audio", audioBytes, "recorded.wav", "audio/wav");

        using (UnityWebRequest request = UnityWebRequest.Post("http://localhost:5000/transcribe", form))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
                Debug.LogError("❌ Whisper 回傳錯誤：" + request.error);
            else
                Debug.Log("✅ Whisper 回應：" + request.downloadHandler.text);
        }
    }
}

