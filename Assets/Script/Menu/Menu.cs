using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class Menu : MonoBehaviour
{
    [Header("References")]
    public PlayerInput playerInput;
    public CanvasGroup canvasGroup;
    public VideoPlayer videoPlayer;
    public Animator animator;

    [Header("Settings")]
    public float fadeDuration = 1f;
    public string nextSceneName = "SceneGame";
    public float videoStartDelay = 3f;      // durasi tunggu sebelum trigger Start animasi
    public float videoPlayBeforeFade = 5f;  // durasi video main sebelum fade out setelah StartRace
    public float blinkingtime = 1.5f;
    private bool isTransitioning = false;

    private void OnEnable()
    {
        playerInput.actions["StartRace"].performed += OnStartRace;
    }

    private void OnDisable()
    {
        playerInput.actions["StartRace"].performed -= OnStartRace;
    }

    private void Start()
    {
        // Mulai transisi fade in
        StartCoroutine(FadeInAndPlayVideo());
        canvasGroup.gameObject.SetActive(false);
    }

    private IEnumerator FadeInAndPlayVideo()
    {
        float timer = 0f;
        canvasGroup.gameObject.SetActive(true);
        canvasGroup.alpha = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, timer / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = 1f;

        // Play video
        if (videoPlayer != null)
        {
            videoPlayer.Play();
        }

        // Tunggu beberapa detik sebelum trigger Start animasi
        yield return new WaitForSeconds(videoStartDelay);

        // Trigger Start di animator
        if (animator != null)
        {
            animator.SetTrigger("Start");
            // Tunggu animasi Start selesai
            yield return new WaitUntil(() => animator.GetCurrentAnimatorStateInfo(0).IsName("Start") &&
                                             animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1f);

            // Setelah Start selesai, trigger Idle
            //animator.SetTrigger("Idle");
        }
    }

    private void OnStartRace(InputAction.CallbackContext context)
    {
        if (!isTransitioning)
        {
            StartCoroutine(FadeOutAndLoadScene());
        }
    }

    private IEnumerator PlayVideoThenFadeOut()
    {
        isTransitioning = true;

        // Mainkan video selama beberapa detik sebelum fade
        if (videoPlayer != null)
        {
            videoPlayer.Play();
        }

        yield return new WaitForSeconds(videoPlayBeforeFade);

        StartCoroutine(FadeOutAndLoadScene());
    }

    private IEnumerator FadeOutAndLoadScene()
    {
        // Trigger animasi Blinking dulu
        if (animator != null)
        {
            animator.SetTrigger("Blinking");
            // Tunggu animasi Blinking selesai atau pakai delay tertentu
            // Misal Blinking berdurasi 2 detik
            yield return new WaitForSeconds(blinkingtime);
        }

        // Mulai fade out
        float timer = 0f;
        canvasGroup.gameObject.SetActive(true);
        canvasGroup.alpha = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, timer / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = 1f;

        // Ganti scene
        SceneManager.LoadScene(nextSceneName);
    }

}
