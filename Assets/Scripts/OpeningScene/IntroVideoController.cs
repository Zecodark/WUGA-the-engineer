using System.Collections;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class IntroVideoController : MonoBehaviour
{
    [Header("Video Player")]
    public VideoPlayer videoPlayer;

    [Header("Daftar Video Intro")]
    public VideoClip[] videoClips;

    [Header("Render Texture per Video")]
    public RenderTexture[] renderTextures;

    [Header("Panel Scene")]
    public GameObject[] scenePanels;

    [Header("Panel Dialog Image per Scene")]
    public Image[] panelDialogImages;

    [Header("Dialog Text per Scene")]
    public Text[] dialogTexts;

    [Header("Transisi Fade")]
    public CanvasGroup fadePanel;
    [Min(0.05f)] public float fadeDuration = 0.4f;
    [Min(1f)] public float prepareTimeout = 10f;

    [Header("Animasi Dialog")]
    [Min(0.05f)] public float dialogShowDuration = 0.25f;
    [Min(0.005f)] public float typeSpeed = 0.025f;

    [Header("Scene Setelah Intro")]
    public string nextSceneName = "Level1";

    [Header("Input")]
    public bool allowMouseClick = true;
    public bool allowSpaceKey = true;

    private int currentVideoIndex = 0;
    private int lastAdvanceFrame = -1;

    private bool isTransitioning = false;
    private bool isTyping = false;
    private bool skipTyping = false;

    private bool videoPrepared = false;
    private bool videoError = false;

    private Coroutine typingCoroutine;
    private string[] originalDialogTexts;

    private void Start()
    {
        if (videoPlayer == null)
        {
            Debug.LogError("VideoPlayer belum dipasang di IntroVideoController.", this);
            enabled = false;
            return;
        }

        if (videoClips == null || videoClips.Length == 0)
        {
            Debug.LogError("Daftar video intro masih kosong.", this);
            enabled = false;
            return;
        }

        SaveOriginalDialogTexts();

        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = true;
        videoPlayer.errorReceived += OnVideoError;

        HideAllScenePanels();

        if (fadePanel != null)
        {
            fadePanel.alpha = 1f;
            fadePanel.blocksRaycasts = true;
        }

        StartCoroutine(TransitionToVideo(0, false));
    }

    private void Update()
    {
        if (allowMouseClick && Input.GetMouseButtonDown(0))
        {
            NextVideo();
        }

        if (allowSpaceKey && Input.GetKeyDown(KeyCode.Space))
        {
            NextVideo();
        }
    }

    public void NextVideo()
    {
        // Mencegah klik UI button dan klik mouse melompat 2 scene dalam frame yang sama
        if (lastAdvanceFrame == Time.frameCount)
            return;

        lastAdvanceFrame = Time.frameCount;

        if (isTransitioning)
            return;

        // Kalau teks masih mengetik, klik pertama hanya menyelesaikan teks
        if (isTyping)
        {
            skipTyping = true;
            return;
        }

        int nextIndex = currentVideoIndex + 1;

        if (nextIndex < videoClips.Length)
        {
            StartCoroutine(TransitionToVideo(nextIndex, true));
        }
        else
        {
            StartCoroutine(FadeAndLoadNextScene());
        }
    }

    private IEnumerator TransitionToVideo(int targetIndex, bool fadeToBlack)
    {
        isTransitioning = true;
        skipTyping = false;

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        isTyping = false;

        if (fadeToBlack)
        {
            yield return FadeTo(1f);
        }

        currentVideoIndex = targetIndex;

        ActivateScenePanel(currentVideoIndex);
        PrepareDialogVisual(currentVideoIndex);

        videoPlayer.prepareCompleted -= OnVideoPrepared;
        videoPlayer.Stop();

        videoPlayer.clip = videoClips[currentVideoIndex];

        if (renderTextures != null &&
            currentVideoIndex < renderTextures.Length &&
            renderTextures[currentVideoIndex] != null)
        {
            videoPlayer.targetTexture = renderTextures[currentVideoIndex];
        }

        videoPlayer.isLooping = true;

        videoPrepared = false;
        videoError = false;

        videoPlayer.prepareCompleted += OnVideoPrepared;
        videoPlayer.Prepare();

        float elapsed = 0f;

        while (!videoPrepared && !videoError && elapsed < prepareTimeout)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (!videoPrepared)
        {
            if (!videoError)
            {
                Debug.LogError(
                    $"Video index {currentVideoIndex} gagal disiapkan dalam {prepareTimeout} detik.",
                    this
                );
            }

            yield return FadeTo(0f);
            isTransitioning = false;
            yield break;
        }

        yield return FadeTo(0f);

        yield return ShowDialogFadeAnimation(currentVideoIndex);

        isTransitioning = false;

        typingCoroutine = StartCoroutine(TypeCurrentDialog());
    }

    private void SaveOriginalDialogTexts()
    {
        if (dialogTexts == null)
            return;

        originalDialogTexts = new string[dialogTexts.Length];

        for (int i = 0; i < dialogTexts.Length; i++)
        {
            if (dialogTexts[i] != null)
            {
                originalDialogTexts[i] = dialogTexts[i].text;
            }
            else
            {
                originalDialogTexts[i] = "";
            }
        }
    }

    private void ActivateScenePanel(int activeIndex)
    {
        if (scenePanels == null)
            return;

        for (int i = 0; i < scenePanels.Length; i++)
        {
            if (scenePanels[i] != null)
            {
                scenePanels[i].SetActive(i == activeIndex);
            }
        }
    }

    private void HideAllScenePanels()
    {
        if (scenePanels == null)
            return;

        for (int i = 0; i < scenePanels.Length; i++)
        {
            if (scenePanels[i] != null)
            {
                scenePanels[i].SetActive(false);
            }
        }
    }

    private void PrepareDialogVisual(int index)
    {
        if (panelDialogImages != null &&
            index < panelDialogImages.Length &&
            panelDialogImages[index] != null)
        {
            Image panelImage = panelDialogImages[index];

            panelImage.enabled = true;

            Color imageColor = panelImage.color;
            imageColor.a = 0f;
            panelImage.color = imageColor;

            // Penting:
            // Jangan ubah localScale di sini.
            // Ukuran PanelDialog tetap mengikuti setting manual di Unity.
        }

        if (dialogTexts != null &&
            index < dialogTexts.Length &&
            dialogTexts[index] != null)
        {
            Text text = dialogTexts[index];

            text.text = "";

            Color textColor = text.color;
            textColor.a = 0f;
            text.color = textColor;
        }
    }

    private IEnumerator ShowDialogFadeAnimation(int index)
    {
        Image panelImage = null;
        Text text = null;

        if (panelDialogImages != null &&
            index < panelDialogImages.Length)
        {
            panelImage = panelDialogImages[index];
        }

        if (dialogTexts != null &&
            index < dialogTexts.Length)
        {
            text = dialogTexts[index];
        }

        float elapsed = 0f;

        while (elapsed < dialogShowDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / dialogShowDuration);

            if (panelImage != null)
            {
                Color imageColor = panelImage.color;
                imageColor.a = Mathf.Lerp(0f, 1f, t);
                panelImage.color = imageColor;
            }

            if (text != null)
            {
                Color textColor = text.color;
                textColor.a = Mathf.Lerp(0f, 1f, t);
                text.color = textColor;
            }

            yield return null;
        }

        if (panelImage != null)
        {
            Color imageColor = panelImage.color;
            imageColor.a = 1f;
            panelImage.color = imageColor;
        }

        if (text != null)
        {
            Color textColor = text.color;
            textColor.a = 1f;
            text.color = textColor;
        }
    }

    private IEnumerator TypeCurrentDialog()
    {
        if (dialogTexts == null ||
            originalDialogTexts == null ||
            currentVideoIndex >= dialogTexts.Length ||
            currentVideoIndex >= originalDialogTexts.Length ||
            dialogTexts[currentVideoIndex] == null)
        {
            yield break;
        }

        isTyping = true;
        skipTyping = false;

        Text currentText = dialogTexts[currentVideoIndex];
        string fullText = originalDialogTexts[currentVideoIndex];

        currentText.text = "";

        for (int i = 0; i < fullText.Length; i++)
        {
            if (skipTyping)
                break;

            currentText.text += fullText[i];
            yield return new WaitForSecondsRealtime(typeSpeed);
        }

        currentText.text = fullText;

        isTyping = false;
        skipTyping = false;
        typingCoroutine = null;
    }

    private void OnVideoPrepared(VideoPlayer source)
    {
        source.prepareCompleted -= OnVideoPrepared;
        source.Play();
        videoPrepared = true;
    }

    private void OnVideoError(VideoPlayer source, string message)
    {
        videoError = true;
        Debug.LogError($"Gagal memutar video index {currentVideoIndex}: {message}", this);
    }

    private IEnumerator FadeAndLoadNextScene()
    {
        isTransitioning = true;

        yield return FadeTo(1f);

        if (string.IsNullOrWhiteSpace(nextSceneName))
        {
            Debug.LogError("Nama scene tujuan belum diisi.", this);
            yield return FadeTo(0f);
            isTransitioning = false;
            yield break;
        }

        SceneManager.LoadScene(nextSceneName);
    }

    private IEnumerator FadeTo(float targetAlpha)
    {
        if (fadePanel == null)
            yield break;

        fadePanel.blocksRaycasts = true;

        float startAlpha = fadePanel.alpha;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            fadePanel.alpha = Mathf.Lerp(
                startAlpha,
                targetAlpha,
                Mathf.Clamp01(elapsed / fadeDuration)
            );

            yield return null;
        }

        fadePanel.alpha = targetAlpha;
        fadePanel.blocksRaycasts = targetAlpha > 0f;
    }

    private void OnDestroy()
    {
        if (videoPlayer != null)
        {
            videoPlayer.prepareCompleted -= OnVideoPrepared;
            videoPlayer.errorReceived -= OnVideoError;
        }
    }
}