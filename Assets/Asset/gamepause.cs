using UnityEngine;
using UnityEngine.SceneManagement;

public class gamepause : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private bool hideOnStart = true;

    [Header("Input")]
    [SerializeField] private KeyCode pauseKey = KeyCode.Escape;

    [Header("Scene")]
    [SerializeField] private string mainMenuSceneName = "MenuUtama";

    private bool isPaused;
    private float previousTimeScale = 1f;

    public bool IsPaused => isPaused;

    private void Awake()
    {
        ResolveReferences();

        if (hideOnStart)
            SetPanelVisible(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(pauseKey))
            TogglePause();
    }

    public void TogglePause()
    {
        if (isPaused)
            ResumeGame();
        else
            PauseGame();
    }

    public void PauseGame()
    {
        if (isPaused)
            return;

        isPaused = true;
        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        SetPanelVisible(true);
    }

    public void ResumeGame()
    {
        if (!isPaused)
            return;

        isPaused = false;
        Time.timeScale = previousTimeScale <= 0f ? 1f : previousTimeScale;
        SetPanelVisible(false);
    }

    public void UlangLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void PergiKeMenu()
    {
        Time.timeScale = 1f;

        if (!string.IsNullOrWhiteSpace(mainMenuSceneName) &&
            Application.CanStreamedLevelBeLoaded(mainMenuSceneName))
        {
            SceneManager.LoadScene(mainMenuSceneName);
            return;
        }

        Debug.LogError(
            $"Scene menu '{mainMenuSceneName}' belum masuk Build Settings.",
            this
        );
    }

    private void ResolveReferences()
    {
        if (pausePanel == null)
        {
            Transform panel = transform.Find("PausePanel");
            pausePanel = panel != null ? panel.gameObject : gameObject;
        }
    }

    private void SetPanelVisible(bool visible)
    {
        if (pausePanel != null)
            pausePanel.SetActive(visible);
        else
            gameObject.SetActive(visible);

        foreach (Transform child in transform)
            child.gameObject.SetActive(visible);
    }

    private void OnDisable()
    {
        if (isPaused)
        {
            isPaused = false;
            Time.timeScale = previousTimeScale <= 0f ? 1f : previousTimeScale;
        }
    }
}
