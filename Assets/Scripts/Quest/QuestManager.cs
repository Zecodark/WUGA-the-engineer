using UnityEngine;
using System;
public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }
    private QuestData activeQuest;
    private System.Collections.Generic.List<QuestData> completedQuests = new();

    public event Action<QuestData> OnQuestAccepted;
    public event Action<QuestData> OnQuestCompleted;
    public event Action OnObjectiveUpdated;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void AcceptQuest(QuestData quest)
    {
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
            if (obj.type == type && obj.targetId == targetId)
            {
                obj.currentAmount = Mathf.Min(obj.currentAmount +
                amount, obj.requiredAmount);
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
        completedQuests.Add(activeQuest);
        OnQuestCompleted?.Invoke(activeQuest);

        if (activeQuest.nextQuest != null)
            AcceptQuest(activeQuest.nextQuest);
        else
            activeQuest = null;
    }

    public QuestData GetActiveQuest() => activeQuest;
    public bool IsQuestActive() => activeQuest != null;

}
