using UnityEngine;

public class GameKalah : MonoBehaviour
{
    [Header("Popup")]
    [SerializeField] private GameObject popupPanel;
    [SerializeField] private bool hideOnStart = true;
    [SerializeField] private bool pauseGameWhenShown = true;

    [Header("Audio")]
    [SerializeField] private AudioClip taskCompleteSound;
    [SerializeField, Range(0f, 1f)] private float taskCompleteVolume = 1f;

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

        PlayTaskCompleteSound();

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

    private void PlayTaskCompleteSound()
    {
        if (taskCompleteSound == null || taskCompleteVolume <= 0f)
            return;

        Vector3 position = Camera.main != null
            ? Camera.main.transform.position
            : transform.position;

        AudioSource.PlayClipAtPoint(
            taskCompleteSound,
            position,
            taskCompleteVolume
        );
    }

    private void OnValidate()
    {
        taskCompleteVolume = Mathf.Clamp01(taskCompleteVolume);
    }
}
