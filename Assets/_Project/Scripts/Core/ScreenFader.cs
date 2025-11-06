using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CanvasGroup))]
public class ScreenFader : MonoBehaviour
{
    private CanvasGroup canvasGroup;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    public void FadeToBlack(float duration)
    {
        StopAllCoroutines();
        canvasGroup.blocksRaycasts = true;
        StartCoroutine(FadeRoutine(1f, duration));
    }

    public void FadeToBlackInstant()
    {
        StopAllCoroutines();
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
    }

    public void FadeFromBlack(float duration)
    {
        StopAllCoroutines();
        canvasGroup.blocksRaycasts = false;
        StartCoroutine(FadeRoutine(0f, duration));
    }


    private IEnumerator FadeRoutine(float targetAlpha, float duration)
    {
        if (duration <= 0)
        {
            canvasGroup.alpha = targetAlpha;
            canvasGroup.blocksRaycasts = (targetAlpha > 0);
            yield break;
        }

        float startAlpha = canvasGroup.alpha;
        float time = 0;
        canvasGroup.blocksRaycasts = (targetAlpha > 0);

        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, time / duration);
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
    }
}