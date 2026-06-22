using UnityEngine;

public class AutoQuestStarter : MonoBehaviour
{
    [SerializeField] private QuestData questToStart;
    [SerializeField] private bool startOnSceneLoad = true;
    [SerializeField] private float startDelay = 0.5f;

    private bool hasStarted;
    private void Start()
    {
        if (startOnSceneLoad)
            Invoke(nameof(StartQuest), startDelay);
    }

    public void StartQuest()
    {
        if (hasStarted || questToStart == null)
        return;

        if (QuestManager.Instance == null)
        {
            Debug.LogError(
                "[AutoQuestStarter] QuestManager tidak ditemukan."
            );
            return;
        }

        // Hindari mengganti quest yang sedang aktif
        if (QuestManager.Instance.IsQuestActive())
        {
            Debug.LogWarning(
                "[AutoQuestStarter] Masih ada quest aktif."
            );
            return;
        }

        hasStarted = true;
        QuestManager.Instance.AcceptQuest(questToStart);
    }

}
