using UnityEngine;

public class GameKalah : MonoBehaviour
{
    [Header("Popup")]
    [SerializeField] private GameObject popupPanel;
    [SerializeField] private bool hideOnStart = true;
    [SerializeField] private bool pauseGameWhenShown = true;

    private bool isShown;

    public bool IsShown => isShown;

    private void Awake()
    {
        if (popupPanel == null)
            popupPanel = gameObject;

        if (hideOnStart)
            HideGameKalah(false);
    }

    public void ShowGameKalah()
    {
        if (isShown)
            return;

        isShown = true;

        if (popupPanel != null)
            popupPanel.SetActive(true);

        if (pauseGameWhenShown)
            Time.timeScale = 0f;
    }

    public void HideGameKalah(bool resumeGame = true)
    {
        isShown = false;

        if (popupPanel != null)
            popupPanel.SetActive(false);

        if (resumeGame)
            Time.timeScale = 1f;
    }
}
