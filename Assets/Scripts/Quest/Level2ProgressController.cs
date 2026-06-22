using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class Level2QuestStep
{
    public string itemId;
    public string displayName;
    [Tooltip("GameObject item yang ditempatkan di map.")]
    public GameObject worldObject;
    public Sprite silhouetteIcon;
    public Sprite completedIcon;

    [Header("Dialog setelah item diambil")]
    public DialogAwalEntry[] afterGrabDialog;

    [Header("Dialog setelah item dipasang")]
    public DialogAwalEntry[] afterPlaceDialog;
}

public class Level2ProgressController : MonoBehaviour
{
    public static Level2ProgressController Instance { get; private set; }

    [Header("References")]
    [SerializeField] private QuestData levelQuest;
    [SerializeField] private dialogAwalScene canvasDialog;
    [SerializeField] private SimpleBitFollower bitCompanion;
    [SerializeField] private Level2QuestChecklistUI checklistUI;

    [Header("Quest Items - Urutan Bebas")]
    [SerializeField] private Level2QuestStep[] steps;
    [SerializeField, Min(0f)] private float grabDialogDelay = 1.5f;
    [SerializeField] private bool hideWorldItemsBeforeQuest = true;

    [Header("Final Dialog")]
    [SerializeField] private DialogAwalEntry[] finalDialog;
    [SerializeField] private UnityEvent onLevelSequenceFinished;

    private readonly HashSet<string> grabbedItemIds = new();
    private readonly HashSet<string> placedItemIds = new();
    private readonly Dictionary<string, Coroutine> grabDialogRoutines = new();
    private bool sequenceStarted;
    private bool finalSequenceStarted;

    public bool IsStarted => sequenceStarted;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (bitCompanion == null)
            bitCompanion = FindFirstObjectByType<SimpleBitFollower>();

        ResolveWorldItems();

        if (hideWorldItemsBeforeQuest)
            SetWorldItemsVisible(false);
    }

    private void OnEnable()
    {
        GrabInteraction.OnItemGrabbed += HandleItemGrabbed;
        PlaceInteraction.OnItemPlaced += HandleItemPlaced;
    }

    private void OnDisable()
    {
        GrabInteraction.OnItemGrabbed -= HandleItemGrabbed;
        PlaceInteraction.OnItemPlaced -= HandleItemPlaced;
    }

    private void Start()
    {
        Debug.Log("[Level2ProgressController] Start() called - auto-starting level progress");
        StartLevelProgress();
    }

    public void StartLevelProgress()
    {
        if (sequenceStarted)
            return;

        sequenceStarted = true;
        finalSequenceStarted = false;
        grabbedItemIds.Clear();
        placedItemIds.Clear();
        SetWorldItemsVisible(true);

        if (levelQuest != null && QuestManager.Instance != null)
            QuestManager.Instance.AcceptQuest(levelQuest);

        if (checklistUI != null)
            checklistUI.Build(steps);

        if (bitCompanion != null)
            bitCompanion.StartFollowing();
        else
            Debug.LogError(
                "[Level2ProgressController] SimpleBitFollower tidak ditemukan.",
                this
            );

        Debug.Log(
            "[Level2ProgressController] Quest dimulai. " +
            "Semua komponen boleh diambil dalam urutan bebas.",
            this
        );
    }

    public bool CanInteractWith(ItemData item)
    {
        if (!sequenceStarted || item == null)
        {
            Debug.Log($"[Level2ProgressController] CanInteractWith: sequenceStarted={sequenceStarted}, item={item?.name}");
            return false;
        }

        int stepIndex = FindStepIndex(item.itemId);
        bool isPlaced = placedItemIds.Contains(item.itemId);
        bool result = stepIndex >= 0 && !isPlaced;
        
        Debug.Log($"[Level2ProgressController] CanInteractWith({item.name}): stepIndex={stepIndex}, isPlaced={isPlaced}, result={result}");
        return result;
    }

    private void HandleItemGrabbed(ItemData item)
    {
        if (!sequenceStarted || item == null)
            return;

        int stepIndex = FindStepIndex(item.itemId);

        if (stepIndex < 0 || !grabbedItemIds.Add(item.itemId))
            return;

        if (grabDialogRoutines.TryGetValue(
            item.itemId,
            out Coroutine previousRoutine))
        {
            StopCoroutine(previousRoutine);
        }

        grabDialogRoutines[item.itemId] = StartCoroutine(
            ShowGrabDialogAfterDelay(item.itemId, stepIndex)
        );
    }

    private IEnumerator ShowGrabDialogAfterDelay(
        string itemId,
        int stepIndex)
    {
        yield return new WaitForSeconds(grabDialogDelay);
        grabDialogRoutines.Remove(itemId);

        if (canvasDialog != null &&
            stepIndex >= 0 &&
            stepIndex < steps.Length)
        {
            canvasDialog.PlayDialog(
                steps[stepIndex].afterGrabDialog,
                DialogPlaybackMode.Passive
            );
        }
    }

    private void HandleItemPlaced(ItemData item)
    {
        if (!sequenceStarted || item == null)
            return;

        int stepIndex = FindStepIndex(item.itemId);

        if (stepIndex < 0 || !placedItemIds.Add(item.itemId))
            return;

        if (grabDialogRoutines.TryGetValue(
            item.itemId,
            out Coroutine pendingRoutine))
        {
            StopCoroutine(pendingRoutine);
            grabDialogRoutines.Remove(item.itemId);
        }

        if (checklistUI != null)
            checklistUI.MarkCompleted(stepIndex);

        bool allCompleted =
            placedItemIds.Count >= GetRequiredItemCount();

        if (canvasDialog != null)
        {
            canvasDialog.PlayDialog(
                steps[stepIndex].afterPlaceDialog,
                DialogPlaybackMode.Passive,
                allCompleted ? FinishLevelSequence : null
            );
        }
        else if (allCompleted)
        {
            FinishLevelSequence();
        }
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
                () => onLevelSequenceFinished?.Invoke()
            );
        }
        else
        {
            onLevelSequenceFinished?.Invoke();
        }
    }

    private int FindStepIndex(string itemId)
    {
        if (steps == null || string.IsNullOrWhiteSpace(itemId))
            return -1;

        for (int i = 0; i < steps.Length; i++)
        {
            if (steps[i] != null && steps[i].itemId == itemId)
                return i;
        }

        return -1;
    }

    private int GetRequiredItemCount()
    {
        if (steps == null)
            return 0;

        int count = 0;

        foreach (Level2QuestStep step in steps)
        {
            if (step != null && !string.IsNullOrWhiteSpace(step.itemId))
                count++;
        }

        return count;
    }

    private void SetWorldItemsVisible(bool visible)
    {
        if (steps == null)
            return;

        foreach (Level2QuestStep step in steps)
        {
            if (step != null && step.worldObject != null)
                step.worldObject.SetActive(visible);
        }
    }

    private void ResolveWorldItems()
    {
        if (steps == null)
            return;

        GrabInteraction[] grabItems =
            FindObjectsByType<GrabInteraction>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        foreach (Level2QuestStep step in steps)
        {
            if (step == null || step.worldObject != null)
                continue;

            foreach (GrabInteraction grabItem in grabItems)
            {
                ItemData data = grabItem.GetItemData();

                if (data != null && data.itemId == step.itemId)
                {
                    step.worldObject = grabItem.gameObject;
                    break;
                }
            }
        }
    }
}
