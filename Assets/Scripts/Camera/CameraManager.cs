using UnityEngine;
using System.Collections;

public class CameraManager : MonoBehaviour
{   
    public static CameraManager Instance { get; private set; }

    [SerializeField] private CanvasGroup fadePanel;
    [SerializeField] private float fadeDuration = 0.5f;


    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    
    public void FadeIn()
    {
        StartCoroutine(Fade(1f, 0f));
    }

    public void FadeOut()
    {
        StartCoroutine(Fade(0f, 1f));
    }

    IEnumerator Fade(float from, float to)
    {
        float elapsed = 0f;
        fadePanel.alpha = from;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            fadePanel.alpha = Mathf.Lerp(from, to, elapsed / fadeDuration);
            yield return null;
        }

        fadePanel.alpha = to;

    }



}
