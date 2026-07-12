using UnityEngine;

public class QuestItemManager : MonoBehaviour
{
    [SerializeField] private GameObject[] allItems;

    void Start()
    {
        foreach (var item in allItems)
        item.SetActive(false);

        QuestManager.Instance.OnQuestAccepted += OnQuestAccepted;
        QuestManager.Instance.OnQuestCompleted += OnQuestCompleted;
    }

    void OnQuestAccepted(QuestData quest)
    {
        foreach(var item in allItems)
        item.SetActive(true);
    }

    void OnQuestCompleted(QuestData quest)
    {
        // Do nothing. Items should remain visible when the quest completes.
    }

    public void SetPreviewVisible(bool visible)
    {
        foreach (GameObject item in allItems)
        {
            if (item != null)
                item.SetActive(visible);
        }
    }

}
