using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Level3GameOver : MonoBehaviour
{
    [Header("Panel Hasil")]
    [SerializeField] private GameObject resultPanel;

    [Header("Teks")]
    [SerializeField] private Text resultTitleText;

    [Tooltip("Teks timer yang terlihat selama permainan.")]
    [SerializeField] private Text liveTimerText;

    [Tooltip("Teks waktu pada panel hasil.")]
    [SerializeField] private Text resultTimeText;

    [Tooltip("Teks jumlah quest, contoh 4/4.")]
    [SerializeField] private Text questProgressText;

    [Header("Bintang")]
    [SerializeField] private Image[] starImages = new Image[3];
    [SerializeField] private Sprite emptyStarSprite;
    [SerializeField] private Sprite activeStarSprite;

    [Header("Batas Waktu Penilaian Level 3")]
    [Tooltip("Waktu kurang dari atau sama dengan nilai ini mendapat 3 bintang.")]
    [SerializeField, Min(0f)] private float threeStarMaxTime = 120f;

    [Tooltip("Waktu kurang dari atau sama dengan nilai ini mendapat 2 bintang. Lebih lambat mendapat 1.")]
    [SerializeField, Min(0f)] private float twoStarMaxTime = 165f;

    [Header("Pengaturan Level 3")]
    [SerializeField] private bool hideResultPanelOnStart = true;
    [SerializeField] private bool hideLiveTimerWhenFinished = true;
    [SerializeField] private bool pauseGameWhenFinished = false;
    [SerializeField] private bool lockPlayerWhenFinished = true;
    [SerializeField] private PlayerController playerController;

    [Header("Button Scene")]
    [SerializeField] private Button nextLevelButton;
    [SerializeField] private Button retryLevelButton;
    [SerializeField] private Button mainMenuButton;

    [Tooltip("Isi nama scene level berikutnya di Inspector.")]
    [SerializeField] private string nextLevelSceneName;

    [Tooltip("Isi nama scene menu utama di Inspector.")]
    [SerializeField] private string mainMenuSceneName;

    [Header("Judul")]
    [SerializeField] private string successTitle = "Level 3 Selesai";
    [SerializeField] private string gameOverTitle = "Game Over";

    private float elapsedTime;
    private int completedQuestCount;
    private int totalQuestCount;
    private bool timerRunning;
    private bool resultShown;

    public float ElapsedTime => elapsedTime;
    public int EarnedStars => CalculateStars(elapsedTime);

    private void Awake()
    {
        Time.timeScale = 1f;

        if (playerController == null)
            playerController = FindFirstObjectByType<PlayerController>(FindObjectsInactive.Include);

        ConfigureButtons();

        if (hideResultPanelOnStart && resultPanel != null)
            resultPanel.SetActive(false);

        UpdateTimeTexts();
        UpdateQuestText();
        SetStars(0);

        if (resultTitleText != null)
            resultTitleText.text = successTitle;

        if (nextLevelButton != null)
            nextLevelButton.interactable = true;
    }

    private void Update()
    {
        if (!timerRunning)
            return;

        elapsedTime += Time.unscaledDeltaTime;
        UpdateTimeTexts();
    }

    public void StartLevelTimer(int totalQuests)
    {
        elapsedTime = 0f;
        completedQuestCount = 0;
        totalQuestCount = Mathf.Max(0, totalQuests);
        timerRunning = true;
        resultShown = false;

        Time.timeScale = 1f;

        if (resultPanel != null)
            resultPanel.SetActive(false);

        if (liveTimerText != null)
            liveTimerText.gameObject.SetActive(true);

        if (playerController != null)
            playerController.UnlockInput();

        UpdateTimeTexts();
        UpdateQuestText();
        SetStars(0);
    }

    public void UpdateQuestProgress(int completed, int total)
    {
        completedQuestCount = Mathf.Max(0, completed);
        totalQuestCount = Mathf.Max(0, total);
        UpdateQuestText();
    }

    public void CompleteLevel(int completed, int total)
    {
        if (resultShown)
            return;

        resultShown = true;
        timerRunning = false;

        if (resultTitleText != null)
            resultTitleText.text = successTitle;

        if (nextLevelButton != null)
            nextLevelButton.interactable = true;

        UpdateQuestProgress(completed, total);
        UpdateTimeTexts();
        SetStars(CalculateStars(elapsedTime));

        if (resultPanel != null)
            resultPanel.SetActive(true);

        if (hideLiveTimerWhenFinished && liveTimerText != null)
            liveTimerText.gameObject.SetActive(false);

        if (lockPlayerWhenFinished && playerController != null)
            playerController.LockInput();

        if (pauseGameWhenFinished)
            Time.timeScale = 0f;
    }

    public void CompleteLevel()
    {
        CompleteLevel(completedQuestCount, totalQuestCount);
    }

    public void ShowGameOver()
    {
        if (resultShown)
            return;

        resultShown = true;
        timerRunning = false;

        if (resultTitleText != null)
            resultTitleText.text = gameOverTitle;

        if (nextLevelButton != null)
            nextLevelButton.interactable = false;

        UpdateTimeTexts();
        SetStars(0);

        if (resultPanel != null)
            resultPanel.SetActive(true);

        if (hideLiveTimerWhenFinished && liveTimerText != null)
            liveTimerText.gameObject.SetActive(false);

        if (lockPlayerWhenFinished && playerController != null)
            playerController.LockInput();

        if (pauseGameWhenFinished)
            Time.timeScale = 0f;
    }

    public void ResetLevelResult()
    {
        Time.timeScale = 1f;
        StartLevelTimer(totalQuestCount);
    }

    public void LoadNextLevel()
    {
        LoadSceneByName(nextLevelSceneName);
    }

    public void RetryLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void LoadMainMenu()
    {
        LoadSceneByName(mainMenuSceneName);
    }

    private int CalculateStars(float seconds)
    {
        if (seconds <= threeStarMaxTime)
            return 3;

        if (seconds <= twoStarMaxTime)
            return 2;

        return 1;
    }

    private void SetStars(int activeCount)
    {
        if (starImages == null)
            return;

        for (int i = 0; i < starImages.Length; i++)
        {
            if (starImages[i] == null)
                continue;

            starImages[i].sprite =
                i < activeCount
                    ? activeStarSprite
                    : emptyStarSprite;
        }
    }

    private void UpdateTimeTexts()
    {
        string formattedTime = FormatTime(elapsedTime);

        if (liveTimerText != null)
            liveTimerText.text = formattedTime;

        if (resultTimeText != null)
            resultTimeText.text = formattedTime;
    }

    private void UpdateQuestText()
    {
        if (questProgressText != null)
            questProgressText.text = $"{completedQuestCount}/{totalQuestCount}";
    }

    private void ConfigureButtons()
    {
        if (nextLevelButton != null)
        {
            nextLevelButton.onClick.RemoveListener(LoadNextLevel);
            nextLevelButton.onClick.AddListener(LoadNextLevel);
        }

        if (retryLevelButton != null)
        {
            retryLevelButton.onClick.RemoveListener(RetryLevel);
            retryLevelButton.onClick.AddListener(RetryLevel);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveListener(LoadMainMenu);
            mainMenuButton.onClick.AddListener(LoadMainMenu);
        }
    }

    private void LoadSceneByName(string sceneName)
    {
        Time.timeScale = 1f;

        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("Nama scene tujuan belum diisi.", this);
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogWarning(
                $"Scene '{sceneName}' belum tersedia di Build Settings.",
                this
            );
            return;
        }

        SceneManager.LoadScene(sceneName);
    }

    private static string FormatTime(float seconds)
    {
        int minutes = Mathf.FloorToInt(seconds / 60f);
        float remainingSeconds = seconds % 60f;
        return $"{minutes:00}:{remainingSeconds:00.00}";
    }

    private void OnValidate()
    {
        threeStarMaxTime = Mathf.Max(0f, threeStarMaxTime);
        twoStarMaxTime = Mathf.Max(threeStarMaxTime, twoStarMaxTime);
    }
}
