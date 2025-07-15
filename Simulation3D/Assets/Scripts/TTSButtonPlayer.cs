using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using System.Linq;

public class TTSButtonPlayer : MonoBehaviour
{
    public AudioSource audioSource;

    private string apiUrl = "http://localhost:5001/speak";

    void Start()
    {
        StartCoroutine(RequestTTS());
    }

    IEnumerator RequestTTS()
    {
        string jsonBody = "{\"text\": \"Hello from Unity, this is automatic playback.\"}";
        byte[] jsonBytes = Encoding.UTF8.GetBytes(jsonBody);

        UnityWebRequest request = new UnityWebRequest(apiUrl, "POST");
        request.uploadHandler = new UploadHandlerRaw(jsonBytes);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            byte[] audioData = request.downloadHandler.data;
            WAV wav = new WAV(audioData);

            if (audioSource.clip != null)
            {
                Destroy(audioSource.clip);
                audioSource.clip = null;
            }

            AudioClip clip = AudioClip.Create("TTS", wav.SampleCount, 1, wav.Frequency, false);
            clip.SetData(wav.LeftChannel, 0);
            audioSource.clip = clip;
            audioSource.Play();
        }
        else
        {
            Debug.LogError("TTS request failed: " + request.error);
        }
    }
}