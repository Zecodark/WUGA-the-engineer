using HPhysic;
using System;
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

    [Header("Quest UI")]
    [SerializeField] private QuestUI questUI;
    [SerializeField] private bool resetQuestUIOnStart = true;
    [SerializeField] private CableQuestUIBinding[] questUIBindings;

    [Header("Timer & Penilaian")]
    [SerializeField] private Level3GameOver gameOver;

    [Header("Dialog")]
    [SerializeField] private dialogAwalScene canvasDialog;
    [SerializeField] private SimpleBitFollower bitCompanion;
    [SerializeField] private DialogAwalEntry[] completionDialog;

    [Header("Portal")]
    [SerializeField] private GameObject portalObject;
    [SerializeField] private bool hidePortalOnStart = true;

    [Header("Event")]
    [SerializeField] private UnityEngine.Events.UnityEvent onQuestStarted;
    [SerializeField] private UnityEngine.Events.UnityEvent onQuestCompleted;

    [Header("Debug")]
    [SerializeField] private bool logProgress = true;

    private bool questStarted;
    private bool isCompleted;
    private int lastCorrectCount = -1;
    private bool[] completedSocketStates;

    public bool IsStarted => questStarted;
    public bool IsCompleted => isCompleted;

    public void SetCompletionDialog(DialogAwalEntry[] dialogEntries)
    {
        completionDialog = dialogEntries;
    }

    private void Start()
    {
        if (questUI == null)
            questUI = FindFirstObjectByType<QuestUI>(FindObjectsInactive.Include);

        if (gameOver == null)
            gameOver = FindFirstObjectByType<Level3GameOver>(FindObjectsInactive.Include);

        if (canvasDialog == null)
            canvasDialog = FindFirstObjectByType<dialogAwalScene>(FindObjectsInactive.Include);

        if (bitCompanion == null)
            bitCompanion = FindFirstObjectByType<SimpleBitFollower>(FindObjectsInactive.Include);

        completedSocketStates = new bool[requiredSockets != null ? requiredSockets.Length : 0];

        if (portalObject != null && hidePortalOnStart)
            portalObject.SetActive(false);

        if (resetQuestUIOnStart && questUI != null)
            questUI.ResetItems();

        if (acceptQuestOnStart)
            StartCableQuest();
    }

    private void Update()
    {
        CheckProgress();
    }

    public void StartCableQuest()
    {
        if (questStarted)
            return;

        questStarted = true;
        isCompleted = false;
        lastCorrectCount = -1;

        completedSocketStates = new bool[requiredSockets != null ? requiredSockets.Length : 0];

        if (resetQuestUIOnStart && questUI != null)
            questUI.ResetItems();

        if (portalObject != null && hidePortalOnStart)
            portalObject.SetActive(false);

        if (gameOver != null)
            gameOver.StartLevelTimer(GetRequiredSocketCount());

        if (questToAccept != null && QuestManager.Instance != null)
            QuestManager.Instance.AcceptQuest(questToAccept);

        onQuestStarted?.Invoke();

        if (logProgress)
            Debug.Log("[CableQuest] Level 3 quest started.", this);
    }

    public void CheckProgress()
    {
        if (!questStarted || isCompleted || requiredSockets == null || requiredSockets.Length == 0)
            return;

        int correctCount = CountCorrectSockets();

        if (gameOver != null)
            gameOver.UpdateQuestProgress(correctCount, requiredSockets.Length);

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

        if (bitCompanion != null)
            bitCompanion.StopFollowing();

        if (canvasDialog != null &&
            completionDialog != null &&
            completionDialog.Length > 0)
        {
            canvasDialog.PlayDialog(
                completionDialog,
                DialogPlaybackMode.Cutscene,
                FinishQuestCompletion
            );
        }
        else
        {
            FinishQuestCompletion();
        }
    }

    private void FinishQuestCompletion()
    {
        if (portalObject != null)
            portalObject.SetActive(true);

        onQuestCompleted?.Invoke();
        Debug.Log("[CableQuest] Cable puzzle completed.", this);
    }

    private int CountCorrectSockets()
    {
        int correctCount = 0;

        for (int i = 0; i < requiredSockets.Length; i++)
        {
            Connector socket = requiredSockets[i];

            if (socket != null && socket.IsConnectedRight)
            {
                socket.LockCurrentConnection();
                SnapConnectedCableToRoute(socket);
                MarkQuestUICompleted(i, socket);
                correctCount++;
            }
        }

        return correctCount;
    }

    private void SnapConnectedCableToRoute(Connector socket)
    {
        if (socket == null || socket.ConnectedTo == null)
            return;

        PhysicCable cable = socket.ConnectedTo.GetComponentInParent<PhysicCable>();
        if (cable == null)
            cable = socket.GetComponentInParent<PhysicCable>();

        if (cable != null)
            cable.SnapConnectedCableToRoute();
    }

    private void MarkQuestUICompleted(int socketIndex, Connector socket)
    {
        if (questUI == null ||
            completedSocketStates == null ||
            socketIndex < 0 ||
            socketIndex >= completedSocketStates.Length ||
            completedSocketStates[socketIndex])
        {
            return;
        }

        string itemId = GetQuestItemId(socket);
        if (string.IsNullOrWhiteSpace(itemId))
            return;

        questUI.MarkCompleted(itemId);
        completedSocketStates[socketIndex] = true;
    }

    private string GetQuestItemId(Connector socket)
    {
        if (socket == null)
            return string.Empty;

        if (questUIBindings != null)
        {
            foreach (CableQuestUIBinding binding in questUIBindings)
            {
                if (binding != null &&
                    binding.socket == socket &&
                    !string.IsNullOrWhiteSpace(binding.itemId))
                {
                    return binding.itemId;
                }
            }
        }

        return socket.ConnectionColor switch
        {
            Connector.CableColor.Red => "Kabel merah",
            Connector.CableColor.Yellow => "Kabel Kuning",
            Connector.CableColor.Green => "Kabel Hijau",
            Connector.CableColor.Blue => "Kabel Biru",
            _ => $"Kabel {socket.ConnectionColor}"
        };
    }

    private int GetRequiredSocketCount()
    {
        return requiredSockets != null ? requiredSockets.Length : 0;
    }

    [Serializable]
    private class CableQuestUIBinding
    {
        public Connector socket;
        public string itemId;
    }
}
