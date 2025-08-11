using UnityEngine;
using System.Collections;

public class SceneFadeIn : MonoBehaviour
{
    public CanvasGroup fadeGroup;
    public float fadeDuration = 1f;

    private void Start()
    {
        if (fadeGroup != null)
        {
            fadeGroup.alpha = 1f;
            fadeGroup.gameObject.SetActive(true);
            StartCoroutine(FadeIn());
        }
    }

    private IEnumerator FadeIn()
    {
        yield return new WaitForSeconds(1f);
        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            fadeGroup.alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration);
            yield return null;
        }

        fadeGroup.alpha = 0f;
        fadeGroup.gameObject.SetActive(false); // Optional: matikan setelah fade
    }
}
