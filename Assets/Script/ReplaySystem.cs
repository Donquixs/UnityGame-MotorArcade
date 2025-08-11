using UnityEngine;
using UnityEngine.Video;
using System.Diagnostics;
using System.IO;
using System.Collections;

public class ReplaySystem : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    public Camera recordingCamera;
    public RenderTexture renderTexture;
    private string videoFilePath;
    private string ffmpegPath = "C:/ffmpeg/bin/ffmpeg.exe"; // Sesuaikan dengan lokasi FFmpeg

    void Start()
    {
        videoFilePath = Application.persistentDataPath + "/Replay.mp4";
    }

    // 🎬 Mulai Rekaman
    public void StartRecord()
    {
        recordingCamera.targetTexture = renderTexture;
        UnityEngine.Debug.Log("Recording Started...");
    }

    // ⏹ Hentikan Rekaman dan Simpan Video
    public void StopRecord()
    {
        recordingCamera.targetTexture = null;
        SaveVideo();
        UnityEngine.Debug.Log("Recording Stopped.");
    }

    // 📽️ Konversi RenderTexture ke Video dengan FFmpeg
    void SaveVideo()
    {
        if (!File.Exists(ffmpegPath))
        {
            UnityEngine.Debug.LogError("FFmpeg tidak ditemukan! Pastikan path benar.");
            return;
        }

        string framesPath = Application.persistentDataPath + "/frames.rgb";
        string command = $"-y -f rawvideo -pix_fmt rgba -s {renderTexture.width}x{renderTexture.height} " +
                         $"-i {framesPath} -c:v libx264 -preset fast {videoFilePath}";

        ProcessStartInfo processInfo = new ProcessStartInfo
        {
            FileName = ffmpegPath,
            Arguments = command,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        Process process = new Process { StartInfo = processInfo };
        process.Start();
        process.WaitForExit();

        UnityEngine.Debug.Log("Video Saved: " + videoFilePath);
    }

    // ▶ Mulai Replay
    public void StartReplay()
    {
        string filePath = Application.persistentDataPath + "/Replay.mp4";

        if (System.IO.File.Exists(filePath))
        {
            UnityEngine.Debug.Log("Replay file found. Playing now...");
            videoPlayer.url = filePath;
            videoPlayer.Play();
        }
        else
        {
            UnityEngine.Debug.LogError("Replay file NOT found! Cek apakah FFmpeg menyimpan file dengan benar.");
        }
    }

    IEnumerator DelayBeforeReplay()
    {
        string filePath = Application.persistentDataPath + "/Replay.mp4";

        // Debugging: Pastikan path benar
        UnityEngine.Debug.Log("Checking file path: " + filePath);

        float timer = 0f;
        while (!System.IO.File.Exists(filePath) && timer < 5f) // Tunggu maksimal 5 detik
        {
            UnityEngine.Debug.Log("Waiting for video file...");
            yield return new WaitForSeconds(0.5f);
            timer += 0.5f;
        }

        if (System.IO.File.Exists(filePath))
        {
            videoPlayer.url = filePath;
            videoPlayer.Play();
            UnityEngine.Debug.Log("Playing Replay...");
        }
        else
        {
            UnityEngine.Debug.LogError("Replay file NOT found! Cek apakah FFmpeg menyimpan file dengan benar.");
        }
    }

    public void OnRaceFinished()
    {
        StopRecord();
        StartCoroutine(DelayBeforeReplay());
    }
}