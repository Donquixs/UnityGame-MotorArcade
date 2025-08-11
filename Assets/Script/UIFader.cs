using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIFader : MonoBehaviour
{
    public CanvasGroup canvasGroup;
    public float fadeDuration = 1f;

    void Start()
    {
        // Awalnya alpha 0
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

    }

    IEnumerator FadeIn()
    {
        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            canvasGroup.alpha = Mathf.Clamp01(timer / fadeDuration);
            yield return null;
        }

        // Aktifkan interaksi UI kalau perlu
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;
    }

    public IEnumerator FadeOut()
    {
        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            canvasGroup.alpha = Mathf.Clamp01(1 - (timer / fadeDuration));
            yield return null;
        }

        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
    }
}
