using HPhysic;
using UnityEngine;

public class CableQuestController : MonoBehaviour
{
    [Header("Quest")]
    [SerializeField] private QuestData questToAccept;
    [SerializeField] private bool acceptQuestOnStart;
    [SerializeField] private ObjectiveType objectiveType = ObjectiveType.Interact;
    [SerializeField] private string objectiveTargetId = "cable_puzzle";

    [Header("Sockets")]
    [SerializeField] private Connector[] requiredSockets;

    [Header("Debug")]
    [SerializeField] private bool logProgress = true;

    private bool isCompleted;
    private int lastCorrectCount = -1;

    private void Start()
    {
        if (acceptQuestOnStart && questToAccept != null && QuestManager.Instance != null)
            QuestManager.Instance.AcceptQuest(questToAccept);
    }

    private void Update()
    {
        CheckProgress();
    }

    public void CheckProgress()
    {
        if (isCompleted || requiredSockets == null || requiredSockets.Length == 0)
            return;

        int correctCount = CountCorrectSockets();

        if (logProgress && correctCount != lastCorrectCount)
        {
            Debug.Log(
                $"[CableQuest] Correct cables: {correctCount}/{requiredSockets.Length}",
                this
            );
            lastCorrectCount = correctCount;
        }

        if (correctCount < requiredSockets.Length)
            return;

        isCompleted = true;

        if (QuestManager.Instance != null)
            QuestManager.Instance.UpdateObjective(objectiveType, objectiveTargetId, 1);

        Debug.Log("[CableQuest] Cable puzzle completed.", this);
    }

    private int CountCorrectSockets()
    {
        int correctCount = 0;

        foreach (Connector socket in requiredSockets)
        {
            if (socket != null && socket.IsConnectedRight)
                correctCount++;
        }

        return correctCount;
    }
}
