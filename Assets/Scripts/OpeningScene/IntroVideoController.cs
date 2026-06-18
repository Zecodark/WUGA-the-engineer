using System.Collections;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;

public class IntroVideoController : MonoBehaviour
{
    // Controller untuk alur empat video pada scene IntroLevel1.
    [Header("Video Player")]
    public VideoPlayer videoPlayer;

    [Header("Daftar Video Intro")]
    public VideoClip[] videoClips;

    [Header("Render Texture per Video")]
    public RenderTexture[] renderTextures;

    [Header("Panel Dialog per Video")]
    public GameObject[] dialogPanels;

    [Header("Transisi Fade")]
    public CanvasGroup fadePanel;
    [Min(0.05f)] public float fadeDuration = 0.4f;
    [Min(1f)] public float prepareTimeout = 10f;

    [Header("Scene Setelah Intro")]
    public string nextSceneName = "Level1";

    [Header("Input")]
    public bool allowMouseClick = true;
    public bool allowSpaceKey = true;

    private int currentVideoIndex = 0;
    private int lastAdvanceFrame = -1;
    private bool isChangingVideo = false;
    private bool videoPrepared = false;
    private bool videoError = false;

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

        videoPlayer.errorReceived += OnVideoError;

        if (fadePanel != null)
        {
            fadePanel.alpha = 1f;
            fadePanel.blocksRaycasts = true;
        }

        StartCoroutine(TransitionToVideo(0, false));
    }

    private void Update()
    {
        if (isChangingVideo)
            return;

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
        // Mencegah klik tombol UI dan klik mouse pada frame yang sama
        // melompati dua video sekaligus.
        if (isChangingVideo || lastAdvanceFrame == Time.frameCount)
            return;

        lastAdvanceFrame = Time.frameCount;
        int nextVideoIndex = currentVideoIndex + 1;

        if (nextVideoIndex < videoClips.Length)
        {
            StartCoroutine(TransitionToVideo(nextVideoIndex, true));
        }
        else
        {
            StartCoroutine(FadeAndLoadNextScene());
        }
    }

    private IEnumerator TransitionToVideo(int targetIndex, bool fadeToBlack)
    {
        isChangingVideo = true;

        if (fadeToBlack)
            yield return FadeTo(1f);

        currentVideoIndex = targetIndex;
        ShowCurrentDialog();

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
                    this);
            }

            yield return FadeTo(0f);
            isChangingVideo = false;
            yield break;
        }

        yield return FadeTo(0f);
        isChangingVideo = false;
    }

    private void ShowCurrentDialog()
    {
        if (dialogPanels == null)
            return;

        for (int i = 0; i < dialogPanels.Length; i++)
        {
            if (dialogPanels[i] != null)
                dialogPanels[i].SetActive(i == currentVideoIndex);
        }
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
        Debug.LogError(
            $"Gagal memutar video index {currentVideoIndex}: {message}",
            this);
    }

    private IEnumerator FadeAndLoadNextScene()
    {
        isChangingVideo = true;
        yield return FadeTo(1f);

        if (string.IsNullOrWhiteSpace(nextSceneName))
        {
            Debug.LogError("Nama scene tujuan belum diisi.", this);
            yield return FadeTo(0f);
            isChangingVideo = false;
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
                Mathf.Clamp01(elapsed / fadeDuration));
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
