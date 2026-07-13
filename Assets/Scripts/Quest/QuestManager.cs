using UnityEngine;
using System;
public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }
    private QuestData activeQuest;
    private System.Collections.Generic.List<QuestData> completedQuests = new();

    [Header("Quest Audio")]
    [SerializeField] private AudioSource questAudioSource;
    [SerializeField] private AudioClip taskCompleteSound;
    [SerializeField, Range(0f, 1f)] private float taskCompleteVolume = 0.9f;

    public event Action<QuestData> OnQuestAccepted;
    public event Action<QuestData> OnQuestCompleted;
    public event Action OnObjectiveUpdated;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // Jangan hapus seluruh GameObject: scene controller lain dapat
            // ditempatkan pada object yang sama dengan QuestManager.
            Destroy(this);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (questAudioSource != null)
        {
            questAudioSource.playOnAwake = false;
            questAudioSource.loop = false;
            questAudioSource.volume = taskCompleteVolume;
        }
    }

    public void AcceptQuest(QuestData quest)
    {
        Debug.Log("[QuestManager] AcceptQuest called: " + quest.questName);
        activeQuest = quest;
        foreach (var obj in activeQuest.objectives)
        obj.currentAmount = 0;

        OnQuestAccepted?.Invoke(activeQuest);
    }

    public void UpdateObjective(ObjectiveType type, string targetId, int amount)
    {
        if (activeQuest == null) return;
        foreach (var obj in activeQuest.objectives)
        {
            if (obj.type == type &&
                NormalizeTargetId(obj.targetId) ==
                NormalizeTargetId(targetId))
            {
                obj.currentAmount = Mathf.Min(obj.currentAmount +
                amount, obj.requiredAmount);
                Debug.Log("[QuestManager] UpdateObjective: " + obj.description + " = " + obj.currentAmount + "/" + obj.requiredAmount);
                OnObjectiveUpdated?.Invoke();

                if (IsQuestComplete())
                CompleteQuest();
            }
        }
    }

    public bool IsQuestComplete()
    {
        if (activeQuest == null) return false;
        foreach (var obj in activeQuest.objectives)
        if (obj.currentAmount < obj.requiredAmount) return false;
        return true;
    }

    void CompleteQuest()
    {
        Debug.Log("[QuestManager] CompleteQuest called: " + activeQuest.questName);
        completedQuests.Add(activeQuest);
        PlayTaskCompleteSound();
        OnQuestCompleted?.Invoke(activeQuest);

        if (activeQuest.nextQuest != null)
            AcceptQuest(activeQuest.nextQuest);
        else
            activeQuest = null;
    }

    public QuestData GetActiveQuest() => activeQuest;
    public bool IsQuestActive() => activeQuest != null;

    private void PlayTaskCompleteSound()
    {
        if (questAudioSource != null && taskCompleteSound != null)
            questAudioSource.PlayOneShot(taskCompleteSound);
    }

    private static string NormalizeTargetId(string targetId)
    {
        if (string.IsNullOrWhiteSpace(targetId))
            return string.Empty;

        string normalized = targetId.Trim().ToLowerInvariant();

        return normalized == "power_supply"
            ? "power_ups"
            : normalized;
    }

}
