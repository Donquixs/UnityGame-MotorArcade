using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using System.Collections;

public class SceneReloader : MonoBehaviour
{
    public PlayerInput playerInput;
    private InputAction reloadAction;

    [Header("Scene Settings")]
    public string menuSceneName = "Menu";

    [Header("Fade Settings")]
    public CanvasGroup fadeGroup;
    public float fadeDuration = 1f;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        reloadAction = playerInput.actions["Reload"];
    }

    private void OnEnable()
    {
        reloadAction.performed += TryReloadScene;
        reloadAction.Enable();
    }

    private void OnDisable()
    {
        reloadAction.performed -= TryReloadScene;
        reloadAction.Disable();
    }

    private void TryReloadScene(InputAction.CallbackContext context)
    {
        LapTimer lapTimer = FindObjectOfType<LapTimer>();
        if (lapTimer != null && lapTimer.raceStarted)
        {
            StartCoroutine(FadeAndReload());
        }
        else
        {
            Debug.Log("Race belum dimulai atau LapTimer tidak ditemukan.");
        }
    }

    private IEnumerator FadeAndReload()
    {
        if (fadeGroup != null)
        {
            fadeGroup.gameObject.SetActive(true);
            float timer = 0f;
            while (timer < fadeDuration)
            {
                timer += Time.deltaTime;
                fadeGroup.alpha = Mathf.Lerp(0f, 1f, timer / fadeDuration);
                yield return null;
            }
            fadeGroup.alpha = 1f;
        }

        SceneManager.LoadScene(menuSceneName);
    }
}
