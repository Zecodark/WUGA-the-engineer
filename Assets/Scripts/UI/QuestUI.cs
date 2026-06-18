using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QuestUI : MonoBehaviour
{
    [SerializeField] private GameObject questPanel;
    [SerializeField] private TextMeshProUGUI questNameText;
    [SerializeField] private TextMeshProUGUI objectiveText;

    void Start()
    {
        QuestManager.Instance.OnQuestAccepted += ShowQuest;
        QuestManager.Instance.OnObjectiveUpdated += UpdateObjectives;
        QuestManager.Instance.OnQuestCompleted += ShowCompleted;
        questNameText.text = "No Active Quest";
        objectiveText.text = "";
    }

    void ShowQuest(QuestData quest)
    {
        questPanel.SetActive(true);
        questNameText.text = quest.questName;
        UpdateObjectives();
    }

    void UpdateObjectives()
    {
        QuestData quest = QuestManager.Instance.GetActiveQuest();
        if (quest == null) return;

        string text = "";
        foreach (var obj in quest.objectives)
            text += obj.description + ": " + obj.currentAmount + "/" +
            obj.requiredAmount + "\n";

            objectiveText.text = text;
    }

    void ShowCompleted(QuestData quest)
    {
        questNameText.text = "QUEST COMPLETED!";
        objectiveText.text = "";
    }

}