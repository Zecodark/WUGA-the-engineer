using UnityEngine;
using UnityEngine.UI;

public class GameOver : MonoBehaviour
{
    [Header("Panel Hasil")]
    [SerializeField] private GameObject resultPanel;

    [Header("Teks")]
    [Tooltip("Teks timer yang terlihat selama permainan.")]
    [SerializeField] private Text liveTimerText;

    [Tooltip("Teks waktu pada panel hasil.")]
    [SerializeField] private Text resultTimeText;

    [Tooltip("Teks jumlah quest, contoh 5/5.")]
    [SerializeField] private Text questProgressText;

    [Header("Bintang")]
    [SerializeField] private Image[] starImages = new Image[3];
    [SerializeField] private Sprite emptyStarSprite;
    [SerializeField] private Sprite activeStarSprite;

    [Header("Batas Waktu Penilaian")]
    [Tooltip("Waktu kurang dari atau sama dengan nilai ini mendapat 3 bintang.")]
    [SerializeField, Min(0f)] private float threeStarMaxTime = 90f;

    [Tooltip("Waktu kurang dari atau sama dengan nilai ini mendapat 2 bintang. Lebih lambat mendapat 1.")]
    [SerializeField, Min(0f)] private float twoStarMaxTime = 150f;

    [Header("Pengaturan")]
    [SerializeField] private bool hideResultPanelOnStart = true;
    [SerializeField] private bool hideLiveTimerWhenFinished = true;
    [SerializeField] private bool pauseGameWhenFinished = false;

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

        if (hideResultPanelOnStart && resultPanel != null)
            resultPanel.SetActive(false);

        UpdateTimeTexts();
        UpdateQuestText();
        SetStars(0);
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

        UpdateQuestProgress(completed, total);
        UpdateTimeTexts();
        SetStars(CalculateStars(elapsedTime));

        if (resultPanel != null)
            resultPanel.SetActive(true);

        if (hideLiveTimerWhenFinished && liveTimerText != null)
            liveTimerText.gameObject.SetActive(false);

        if (pauseGameWhenFinished)
            Time.timeScale = 0f;
    }

    public void CompleteLevel()
    {
        CompleteLevel(completedQuestCount, totalQuestCount);
    }

    public void ResetLevelResult()
    {
        Time.timeScale = 1f;
        StartLevelTimer(totalQuestCount);
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
        {
            questProgressText.text =
                $"{completedQuestCount}/{totalQuestCount}";
        }
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
        twoStarMaxTime = Mathf.Max(
            threeStarMaxTime,
            twoStarMaxTime
        );
    }
}
