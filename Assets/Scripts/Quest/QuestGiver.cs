using UnityEngine;

public class QuestGiver : MonoBehaviour
{
   [SerializeField] private GameObject questMarker;

    void Start()
    {
        QuestManager.Instance.OnQuestAccepted += _ => UpdateMarker();
        QuestManager.Instance.OnQuestCompleted += _ => UpdateMarker();
        UpdateMarker();
    }

    void UpdateMarker()
    {
        if (questMarker != null)
            questMarker.SetActive(!QuestManager.Instance.IsQuestActive());
    }
}
