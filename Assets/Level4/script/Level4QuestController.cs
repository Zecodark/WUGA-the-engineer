using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class Level4QuestStep
{
    public string itemId;
    public string displayName;
    public Level4QuestItem item;
    public Level4QuestSocket socket;

    [Header("Dialog setelah item dipasang")]
    public DialogAwalEntry[] afterPlaceDialog;
}

public class Level4QuestController : MonoBehaviour
{
    public static Level4QuestController Instance { get; private set; }

    [Header("References")]
    [SerializeField] private QuestData levelQuest;
    [SerializeField] private dialogAwalScene canvasDialog;
    [SerializeField] private SimpleBitFollower bitCompanion;
    [SerializeField] private QuestUI questUI;
    [SerializeField] private GameOver gameOver;
    [SerializeField] private PortalLevel1 levelPortal;

    [Header("Quest Items")]
    [SerializeField] private Level4QuestStep[] steps;
    [SerializeField] private bool startOnSceneLoad;
    [SerializeField] private bool acceptQuestManagerQuest = true;
    [SerializeField] private KeyCode manualPlaceKey = KeyCode.G;
    [SerializeField, Min(0.5f)] private float manualPlaceRange = 12f;

    [Header("Final")]
    [SerializeField] private DialogAwalEntry[] finalDialog;
    [SerializeField] private bool showResultPanelWhenComplete = true;
    [SerializeField] private UnityEvent onLevelFinished;

    private readonly HashSet<string> completedItemIds =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    private bool sequenceStarted;
    private bool finalSequenceStarted;
    private CarrySystem carrySystem;

    public bool IsStarted => sequenceStarted;
    public int CompletedCount => completedItemIds.Count;
    public int TotalCount => GetRequiredItemCount();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        ResolveReferences();
        ConfigureSceneActors();
    }

    private void Start()
    {
        if (startOnSceneLoad)
            StartLevelQuest();
    }

    private void Update()
    {
    }

    public void StartLevelQuest()
    {
        if (sequenceStarted)
            return;

        ResolveReferences();
        ConfigureSceneActors();

        sequenceStarted = true;
        finalSequenceStarted = false;
        completedItemIds.Clear();

        if (gameOver != null)
            gameOver.StartLevelTimer(TotalCount);

        if (questUI != null)
            questUI.ResetItems();

        if (acceptQuestManagerQuest &&
            levelQuest != null &&
            QuestManager.Instance != null)
        {
            QuestManager.Instance.AcceptQuest(levelQuest);
        }

        if (bitCompanion != null)
            bitCompanion.StartFollowing();

        Debug.Log(
            $"[Level4QuestController] Quest Level4 dimulai: 0/{TotalCount}.",
            this
        );
    }

    public bool CanInteractWith(Level4QuestItem item)
    {
        if (!sequenceStarted || item == null || item.IsPlaced)
            return false;

        int stepIndex = FindStepIndex(item.ItemId);

        return stepIndex >= 0 &&
               !completedItemIds.Contains(NormalizeId(item.ItemId));
    }

    public bool IsCompleted(string itemId)
    {
        return completedItemIds.Contains(NormalizeId(itemId));
    }

    public bool TryCompleteItem(
        Level4QuestItem item,
        Level4QuestSocket socket)
    {
        if (!sequenceStarted || item == null)
            return false;

        int stepIndex = FindStepIndex(item.ItemId);

        if (stepIndex < 0)
        {
            Debug.LogWarning(
                $"[Level4QuestController] Item '{item.ItemId}' belum masuk daftar quest.",
                item
            );
            return false;
        }

        string normalizedId = NormalizeId(steps[stepIndex].itemId);

        if (!completedItemIds.Add(normalizedId))
            return false;

        string questItemId = steps[stepIndex].itemId;

        if (questUI != null)
            questUI.MarkCompleted(questItemId);

        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.UpdateObjective(
                ObjectiveType.Grab,
                questItemId,
                1
            );
        }

        if (gameOver != null)
            gameOver.UpdateQuestProgress(CompletedCount, TotalCount);

        bool allCompleted = CompletedCount >= TotalCount;
        Action afterDialog = allCompleted ? FinishLevelSequence : null;
        DialogAwalEntry[] placeDialog = steps[stepIndex].afterPlaceDialog;

        Debug.Log(
            $"[Level4QuestController] {questItemId} selesai: {CompletedCount}/{TotalCount}.",
            this
        );

        if (canvasDialog != null &&
            placeDialog != null &&
            placeDialog.Length > 0)
        {
            canvasDialog.PlayDialog(
                placeDialog,
                DialogPlaybackMode.Passive,
                afterDialog
            );
        }
        else
        {
            afterDialog?.Invoke();
        }

        return true;
    }

    private void FinishLevelSequence()
    {
        if (finalSequenceStarted)
            return;

        finalSequenceStarted = true;

        if (bitCompanion != null)
            bitCompanion.StopFollowing();

        if (canvasDialog != null &&
            finalDialog != null &&
            finalDialog.Length > 0)
        {
            canvasDialog.PlayDialog(
                finalDialog,
                DialogPlaybackMode.Cutscene,
                ShowLevelResult
            );
        }
        else
        {
            ShowLevelResult();
        }
    }

    private void ShowLevelResult()
    {
        if (levelPortal != null)
            levelPortal.UnlockPortal();
        else if (showResultPanelWhenComplete && gameOver != null)
            gameOver.CompleteLevel(CompletedCount, TotalCount);

        onLevelFinished?.Invoke();
    }

    private void ResolveReferences()
    {
        if (canvasDialog == null)
            canvasDialog = FindFirstObjectByType<dialogAwalScene>();

        if (bitCompanion == null)
            bitCompanion = FindFirstObjectByType<SimpleBitFollower>();

        if (questUI == null)
            questUI = FindFirstObjectByType<QuestUI>();

        if (gameOver == null)
            gameOver = FindFirstObjectByType<GameOver>();

        if (levelPortal == null)
            levelPortal = FindFirstObjectByType<PortalLevel1>(
                FindObjectsInactive.Include
            );
    }

    private void TryPlaceQuestItemWithKey()
    {
        if (!Input.GetKeyDown(manualPlaceKey) || finalSequenceStarted)
            return;

        ResolveCarrySystem();

        Level4QuestItem item = GetCarriedQuestItem();

        if (item == null)
            item = FindNearestLooseQuestItemForPlacement();

        if (item == null || item.IsPlaced)
            return;

        if (!sequenceStarted)
            StartLevelQuest();

        if (!CanInteractWith(item))
            return;

        Level4QuestSocket socket = FindManualPlacementSocket(item);

        if (socket == null)
        {
            Debug.Log(
                $"[Level4QuestController] {item.DisplayName} belum dekat socket/meja yang sesuai.",
                item
            );
            return;
        }

        bool itemIsCarried = GetCarriedQuestItem() == item;

        if (socket.TryPlaceCarriedItemFromTable(
            item,
            itemIsCarried ? carrySystem : null))
        {
            Debug.Log(
                $"[Level4QuestController] {item.DisplayName} dipasang ke {socket.name}.",
                socket
            );
        }
    }

    private void ResolveCarrySystem()
    {
        if (carrySystem == null)
            carrySystem = FindFirstObjectByType<CarrySystem>();
    }

    private Level4QuestItem GetCarriedQuestItem()
    {
        if (carrySystem == null || !carrySystem.IsCarryingItem())
            return null;

        return Level4QuestSocket.GetQuestItemFromCarriedObject(
            carrySystem.GetCurrentItem()
        );
    }

    private Level4QuestItem FindNearestLooseQuestItemForPlacement()
    {
        Transform player = FindPlayerTransform();

        if (player == null)
            return null;

        Level4QuestItem nearestItem = null;
        float nearestDistance = float.MaxValue;

        foreach (Level4QuestStep step in steps)
        {
            if (step == null ||
                step.item == null ||
                step.item.IsPlaced)
            {
                continue;
            }

            if (sequenceStarted && !CanInteractWith(step.item))
                continue;

            float distance = HorizontalDistance(
                player.position,
                step.item.transform.position
            );

            if (distance > manualPlaceRange || distance >= nearestDistance)
                continue;

            if (FindManualPlacementSocket(step.item) == null)
                continue;

            nearestItem = step.item;
            nearestDistance = distance;
        }

        return nearestItem;
    }

    private Level4QuestSocket FindManualPlacementSocket(Level4QuestItem item)
    {
        if (item == null || steps == null)
            return null;

        int stepIndex = FindStepIndex(item.ItemId);

        if (stepIndex >= 0)
        {
            Level4QuestSocket socket = steps[stepIndex].socket;

            if (CanUseSocketForManualPlacement(item, socket))
                return socket;
        }

        foreach (Level4QuestStep step in steps)
        {
            if (step == null ||
                step.socket == null ||
                step.socket == (stepIndex >= 0 ? steps[stepIndex].socket : null))
            {
                continue;
            }

            if (CanUseSocketForManualPlacement(item, step.socket))
                return step.socket;
        }

        return null;
    }

    private bool CanUseSocketForManualPlacement(
        Level4QuestItem item,
        Level4QuestSocket socket)
    {
        return socket != null &&
               !socket.IsOccupied &&
               socket.Accepts(item) &&
               IsNearSocketOrTable(item, socket);
    }

    private bool IsNearSocketOrTable(
        Level4QuestItem item,
        Level4QuestSocket socket)
    {
        Transform player = FindPlayerTransform();

        if (IsNearSocketOrTablePosition(item.transform.position, socket))
            return true;

        return player != null &&
               IsNearSocketOrTablePosition(player.position, socket);
    }

    private bool IsNearSocketOrTablePosition(
        Vector3 position,
        Level4QuestSocket socket)
    {
        if (HorizontalDistance(position, socket.transform.position) <=
            manualPlaceRange)
        {
            return true;
        }

        Transform root =
            socket.transform.parent != null
                ? socket.transform.parent
                : socket.transform;

        return IsNearBounds(root, position, manualPlaceRange);
    }

    private static bool IsNearBounds(
        Transform root,
        Vector3 position,
        float range)
    {
        if (root == null)
            return false;

        bool hasBounds = false;
        Bounds bounds = new Bounds(root.position, Vector3.one);

        foreach (Collider collider in root.GetComponentsInChildren<Collider>(true))
        {
            if (collider == null || !collider.enabled)
                continue;

            if (!hasBounds)
            {
                bounds = collider.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(collider.bounds);
            }
        }

        if (!hasBounds)
        {
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null)
                    continue;

                if (!hasBounds)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }
        }

        return hasBounds &&
               HorizontalDistanceToBounds(position, bounds) <= range;
    }

    private static Transform FindPlayerTransform()
    {
        PlayerController player = FindFirstObjectByType<PlayerController>();

        if (player != null)
            return player.transform;

        try
        {
            GameObject taggedPlayer =
                GameObject.FindGameObjectWithTag("Player");
            return taggedPlayer != null ? taggedPlayer.transform : null;
        }
        catch (UnityException)
        {
            return null;
        }
    }

    private static float HorizontalDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    private static float HorizontalDistanceToBounds(
        Vector3 position,
        Bounds bounds)
    {
        float dx = Mathf.Max(
            bounds.min.x - position.x,
            0f,
            position.x - bounds.max.x
        );

        float dz = Mathf.Max(
            bounds.min.z - position.z,
            0f,
            position.z - bounds.max.z
        );

        return Mathf.Sqrt(dx * dx + dz * dz);
    }

    private void ConfigureSceneActors()
    {
        if (steps == null)
            return;

        foreach (Level4QuestStep step in steps)
        {
            if (step == null)
                continue;

            if (step.item != null)
                step.item.SetController(this);

            if (step.socket != null)
                step.socket.SetController(this);
        }
    }

    private int FindStepIndex(string itemId)
    {
        if (steps == null)
            return -1;

        string normalizedId = NormalizeId(itemId);

        for (int i = 0; i < steps.Length; i++)
        {
            if (steps[i] == null)
                continue;

            if (NormalizeId(steps[i].itemId) == normalizedId)
                return i;
        }

        return -1;
    }

    private int GetRequiredItemCount()
    {
        if (steps == null)
            return 0;

        int count = 0;

        foreach (Level4QuestStep step in steps)
        {
            if (step != null && !string.IsNullOrWhiteSpace(step.itemId))
                count++;
        }

        return count;
    }

    public static string NormalizeId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return value.Trim()
            .ToLowerInvariant()
            .Replace("_", string.Empty)
            .Replace("-", string.Empty)
            .Replace(" ", string.Empty);
    }
}
