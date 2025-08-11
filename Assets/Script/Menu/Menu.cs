using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class Menu : MonoBehaviour
{
    public PlayerInput playerInput; // referensi dari Inspector
    public CanvasGroup canvasGroup;
    public float fadeDuration = 1f;
    public string nextSceneName = "SceneGame"; // ganti sesuai kebutuhan

    private bool isTransitioning = false;

    private void OnEnable()
    {
        playerInput.actions["StartRace"].performed += OnStartRace;
    }

    private void OnDisable()
    {
        playerInput.actions["StartRace"].performed -= OnStartRace;
    }

    private void OnStartRace(InputAction.CallbackContext context)
    {
        if (!isTransitioning)
        {
            StartCoroutine(FadeOutAndLoadScene());
        }
    }

    private IEnumerator FadeOutAndLoadScene()
    {
        isTransitioning = true;

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

        // Ganti ke nama scene tujuan
        SceneManager.LoadScene(nextSceneName);
    }
}
