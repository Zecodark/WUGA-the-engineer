using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Komponen perpindahan scene yang dapat dipakai oleh semua UI Button.
/// </summary>
public class SceneManagerButton : MonoBehaviour
{
    [Header("Tujuan Default")]
    [Tooltip("Nama scene yang dimuat oleh LoadConfiguredScene().")]
    [SerializeField] private string targetSceneName;

    [Header("Loading")]
    [Tooltip("Gunakan loading asynchronous agar game tidak membeku saat scene besar dimuat.")]
    [SerializeField] private bool loadAsynchronously = true;

    [Min(0f)]
    [Tooltip("Jeda sebelum pindah scene. Gunakan 0 jika tidak membutuhkan animasi.")]
    [SerializeField] private float loadDelay;

    [Tooltip("Opsional. Button dinonaktifkan setelah diklik agar tidak memuat scene dua kali.")]
    [SerializeField] private Button sourceButton;

    [Header("Fade Opsional")]
    [SerializeField] private CanvasGroup fadePanel;

    [Min(0.05f)]
    [SerializeField] private float fadeDuration = 0.35f;

    private bool isLoading;

    public bool IsLoading => isLoading;

    private void Reset()
    {
        sourceButton = GetComponent<Button>();
    }

    private void Awake()
    {
        if (sourceButton == null)
            sourceButton = GetComponent<Button>();
    }

    /// <summary>
    /// Pasang metode ini pada Button OnClick untuk memuat Target Scene Name.
    /// </summary>
    public void LoadConfiguredScene()
    {
        LoadScene(targetSceneName);
    }

    /// <summary>
    /// Bisa dipilih pada Button OnClick dan menerima nama scene sebagai parameter.
    /// </summary>
    public void LoadScene(string sceneName)
    {
        if (isLoading)
            return;

        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError("Nama scene tujuan belum diisi.", this);
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError(
                $"Scene '{sceneName}' tidak ditemukan. Tambahkan scene tersebut ke File > Build Profiles > Scene List.",
                this
            );
            return;
        }

        StartCoroutine(LoadSceneRoutine(sceneName));
    }

    /// <summary>
    /// Memuat scene berdasarkan Build Index.
    /// </summary>
    public void LoadSceneByIndex(int buildIndex)
    {
        if (isLoading)
            return;

        if (buildIndex < 0 || buildIndex >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogError($"Build Index {buildIndex} tidak valid.", this);
            return;
        }

        StartCoroutine(LoadSceneRoutine(buildIndex));
    }

    public void ReloadCurrentScene()
    {
        LoadSceneByIndex(SceneManager.GetActiveScene().buildIndex);
    }

    public void LoadNextScene()
    {
        int currentIndex = SceneManager.GetActiveScene().buildIndex;
        int nextIndex = currentIndex + 1;

        if (nextIndex >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogWarning("Scene aktif adalah scene terakhir di Build Settings.", this);
            return;
        }

        LoadSceneByIndex(nextIndex);
    }

    public void LoadPreviousScene()
    {
        int previousIndex = SceneManager.GetActiveScene().buildIndex - 1;

        if (previousIndex < 0)
        {
            Debug.LogWarning("Scene aktif adalah scene pertama di Build Settings.", this);
            return;
        }

        LoadSceneByIndex(previousIndex);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private IEnumerator LoadSceneRoutine(string sceneName)
    {
        BeginLoading();
        yield return WaitBeforeLoading();

        if (loadAsynchronously)
        {
            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);

            if (operation != null)
                yield return operation;
        }
        else
        {
            SceneManager.LoadScene(sceneName);
        }
    }

    private IEnumerator LoadSceneRoutine(int buildIndex)
    {
        BeginLoading();
        yield return WaitBeforeLoading();

        if (loadAsynchronously)
        {
            AsyncOperation operation = SceneManager.LoadSceneAsync(buildIndex);

            if (operation != null)
                yield return operation;
        }
        else
        {
            SceneManager.LoadScene(buildIndex);
        }
    }

    private void BeginLoading()
    {
        isLoading = true;

        if (sourceButton != null)
            sourceButton.interactable = false;
    }

    private IEnumerator WaitBeforeLoading()
    {
        if (fadePanel != null)
            yield return FadeToBlack();

        if (loadDelay > 0f)
            yield return new WaitForSecondsRealtime(loadDelay);
    }

    private IEnumerator FadeToBlack()
    {
        fadePanel.gameObject.SetActive(true);
        fadePanel.blocksRaycasts = true;

        float startAlpha = fadePanel.alpha;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            fadePanel.alpha = Mathf.Lerp(
                startAlpha,
                1f,
                Mathf.Clamp01(elapsed / fadeDuration)
            );
            yield return null;
        }

        fadePanel.alpha = 1f;
    }
}
