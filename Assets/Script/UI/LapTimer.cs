using ArcadeBP;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.Video;

public class LapTimer : MonoBehaviour
{
   [Header("TEXT")]
    public TMP_Text totalTimeText;
    public TMP_Text lapTimeText;
    public TMP_Text lapCounterText;
    public TMP_Text countdownText; // Untuk menampilkan countdown
    public TMP_Text FinishText;

    [Header("INPUT Player")]
    public GameObject Player;
    public GameObject Proses, Finish;
    public int currentLap = 0;
    public bool raceStarted = false;
    public AudioSource Countdown;
    public float durasitahanrem = 5f;

    [Header("Lap Settings")]
    public int totalLaps = 2;

    private InputAction startRaceAction; // Input Action untuk memulai race
    private float totalTime = 0f;
    private float lapStartTime;
    private float[] lapTimes;
    
    private bool isCountingDown = false;
    public PlayerInput playerInput; // Reference ke PlayerInput

    [Header("Replay System")]
    public ReplayManager replayManager;
    public GameObject[] UInotReplay;

    [Header("Camera")]
    public GameObject cutsceneCamera;
    public GameObject gameplayCamera;
    public Animator cutsceneAnimator; // Animator untuk cutscene
    public string cutsceneTriggerName = "PlayCutscene"; // Trigger animasi

    [Header("Video")]
    public VideoPlayer introVideoPlayer;
    public GameObject VideoRAWImage;
    public float duration = 2f;
    [Header("UI Fade")]
    public CanvasGroup whiteFadeCanvasGroup;
    public float fadeDuration = 1f;


    private void Awake()
    {
        startRaceAction = playerInput.actions["StartRace"]; // Ambil action dari PlayerInput
        StartCoroutine(CountdownCoroutine());
        Player.GetComponent<Record>().enabled = false;
        Player.transform.Find("Sound").GetComponent<Record>().enabled = false;

    }

    private void OnEnable()
    {
        startRaceAction.performed += StartRaceInput; // Bind event saat tombol ditekan
        startRaceAction.Enable();
    }

    private void OnDisable()
    {
        startRaceAction.performed -= StartRaceInput;
        startRaceAction.Disable();
    }

    private void StartRaceInput(InputAction.CallbackContext context)
    {
        if (!isCountingDown && !raceStarted)
        {
            StartCoroutine(CountdownCoroutine());
            
        }
    }

    private void Start()
    {
        Proses.SetActive(true);
        Finish.SetActive(false);
        Player.GetComponent<ArcadeMotor>().enabled = false;
        Player.GetComponent<Rigidbody>().isKinematic = true;

        lapTimes = new float[totalLaps]; // ✅ Inisialisasi sesuai total lap
        lapCounterText.text = "Lap: 0 / " + totalLaps;
        lapTimeText.text = "0'00'00";
        totalTimeText.text = "0'00'00";
        countdownText.text = "";
    }

    private void Update()
    {
        if (raceStarted)
        {
            totalTime += Time.deltaTime;
            totalTimeText.text = FormatTime(totalTime);
            
        }
    }
    private IEnumerator FadeCanvasGroup(CanvasGroup canvasGroup, float start, float end, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            canvasGroup.alpha = Mathf.Lerp(start, end, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        canvasGroup.alpha = end;
    }
    private IEnumerator CountdownCoroutine()
    {
        isCountingDown = true;
        AudioSource[] audioSources = Player.GetComponents<AudioSource>();

        // Mematikan semua AudioSource
        foreach (AudioSource source in audioSources)
        {
            source.enabled = false;
        }

        // 1. Matikan semua kamera terlebih dahulu
        gameplayCamera.SetActive(false);
        cutsceneCamera.SetActive(false);
        VideoRAWImage.SetActive(true);
        // 2. Aktifkan VideoPlayer
        if (introVideoPlayer != null)
        {
            introVideoPlayer.gameObject.SetActive(true);

            introVideoPlayer.Prepare();

            // Tunggu sampai siap
            yield return new WaitUntil(() => introVideoPlayer.isPrepared);
            
            // Play jika sudah siap
            introVideoPlayer.Play();

            Debug.Log("Intro Video Started");
            // 3. Tunggu sampai waktu tersisa tinggal 2 detik

            double videoLength = introVideoPlayer.length;
            while (introVideoPlayer.time < videoLength -2f)
            {
                yield return null;
            }
            // 4. Aktifkan cutscene camera lebih awal (2 detik sebelum video berakhir)
            // Mulai fade putih masuk
            yield return StartCoroutine(FadeCanvasGroup(whiteFadeCanvasGroup, 0f, 1f, fadeDuration));
            cutsceneCamera.SetActive(true);
            gameplayCamera.SetActive(false);
            Debug.Log("Cutscene camera ON");

            // 5. Tunggu sampai video selesai
            yield return new WaitUntil(() => !introVideoPlayer.isPlaying);
            Debug.Log("Intro Video Finished");

            introVideoPlayer.gameObject.SetActive(false);
            VideoRAWImage.SetActive(false);
            
        }

        // Mainkan animasi cutscene kamera
        if (cutsceneAnimator != null)
        {
            cutsceneAnimator.SetTrigger(cutsceneTriggerName);
            // Fade putih keluar
            yield return StartCoroutine(FadeCanvasGroup(whiteFadeCanvasGroup, 1f, 0f, fadeDuration));
            
            // Tunggu sampai animator masuk ke state "Cutscene Camera"
            yield return new WaitUntil(() => cutsceneAnimator.GetCurrentAnimatorStateInfo(0).IsName("Cutscene Camera"));

            // Tunggu sampai animasi hampir selesai (hindari looping atau transisi balik)
            yield return new WaitUntil(() => cutsceneAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 0.99f);

            // Langsung matikan cutscene camera dan nyalakan gameplay camera
            // Atau menghidupkan semuanya
            foreach (AudioSource source in audioSources)
            {
                source.enabled = true;
            }
            cutsceneCamera.SetActive(false);
            gameplayCamera.SetActive(true);
        }

        // Mulai countdown
        Countdown.Play();
        countdownText.text = "3";
        yield return new WaitForSeconds(1f);

        countdownText.text = "2";
        yield return new WaitForSeconds(1f);

        countdownText.text = "1";
        yield return new WaitForSeconds(1f);

        countdownText.text = "GO!";
        StartRace();

        yield return new WaitForSeconds(1f);
        countdownText.text = "";
    }
    public void StartRace()
    {
        var motor = Player.GetComponent<ArcadeMotor>();
        motor.allowOnlyAccelerate = true; // ✅ hanya boleh gas

        Player.GetComponent<ArcadeMotor>().enabled = true;
        Player.GetComponent<Rigidbody>().isKinematic = false;
        raceStarted = true;
        totalTime = 0f;
        currentLap = 0;
        NextLap();

        StartCoroutine(UnlockFullControlAfterDelay(durasitahanrem));
        Player.GetComponent<Record>().enabled = true;
        Player.transform.Find("Sound").GetComponent<Record>().enabled = true;
    }
    private IEnumerator UnlockFullControlAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        var motor = Player.GetComponent<ArcadeMotor>();
        motor.allowOnlyAccelerate = false; // ✅ semua input aktif
    }

    public void NextLap()
    {
        if (currentLap < totalLaps)
        {
            if (currentLap > 0)
            {
                lapTimes[currentLap - 1] = Time.time - lapStartTime;
                lapTimeText.text = "Lap " + currentLap + " Time: " + FormatTime(lapTimes[currentLap - 1]);
            }

            lapStartTime = Time.time;
            currentLap++;
            lapCounterText.text = "Lap: " + currentLap + " / " + totalLaps;

        }
        else
        {
            StartCoroutine(FinishRaceWithFade());
            
        }
    }
    private IEnumerator FinishRaceWithFade()
    {
        // Fade putih masuk (transisi mulai)
        yield return StartCoroutine(FadeCanvasGroup(whiteFadeCanvasGroup, 0f, 1f, fadeDuration));
        FinishRace();
        yield return new WaitForSeconds(0.5f); // Delay saat layar putih penuh

        
        // Fade putih keluar (transisi selesai)
        yield return StartCoroutine(FadeCanvasGroup(whiteFadeCanvasGroup, 1f, 0f, fadeDuration));
    }

    private void FinishRace()
    {
        //StartCoroutine(FinishRaceWithFade());
        raceStarted = false;
        lapTimes[currentLap - 1] = Time.time - lapStartTime;
        lapTimeText.text = "Lap 2 Time: " + FormatTime(lapTimes[1]);
        Debug.Log("Race Finished!");
        FinishText.text = FormatTime(totalTime);

        Proses.SetActive(false);
        Finish.SetActive(true);
        Player.GetComponent<ArcadeMotor>().enabled = false;
        //Player.GetComponent<ArcadeMotor>().engineSound.mute = true;
        //Player.GetComponent<ArcadeMotor>().SkidSound.mute = true;
        DeactivateAllUI();
        replayManager.AutoPlayReplay();
    }

    private string FormatTime(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60);
        int seconds = Mathf.FloorToInt(time % 60);
        int milliseconds = Mathf.FloorToInt((time * 100) % 100); // Hanya 2 digit

        return string.Format("{0:00}'{1:00}\"{2:00}", minutes, seconds, milliseconds);
    }

    public void DeactivateAllUI()
    {
        foreach (GameObject obj in UInotReplay)
        {
            obj.SetActive(false);
        }
    }

}
