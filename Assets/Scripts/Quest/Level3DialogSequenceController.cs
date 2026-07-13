using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

public class Level3DialogSequenceController : MonoBehaviour
{
    private static readonly FieldInfo DialogEntriesField =
        typeof(dialogAwalScene).GetField(
            "dialogEntries",
            BindingFlags.Instance | BindingFlags.NonPublic
        );

    [Header("References")]
    [SerializeField] private dialogAwalScene canvasDialog;
    [SerializeField] private CableQuestController cableQuestController;
    [SerializeField] private SimpleBitFollower bitCompanion;
    [SerializeField] private Transform player;
    [SerializeField] private Level3LanSnake[] lanSnakes;

    [Header("Dialog Ranges")]
    [SerializeField, Min(1)] private int openingStart = 1;
    [SerializeField, Min(1)] private int openingEnd = 4;
    [SerializeField, Min(1)] private int approachStart = 5;
    [SerializeField, Min(1)] private int approachEnd = 8;
    [SerializeField, Min(1)] private int confrontationStart = 9;
    [SerializeField, Min(1)] private int confrontationEnd = 10;
    [SerializeField, Min(1)] private int completionStart = 11;
    [SerializeField, Min(1)] private int completionEnd = 14;

    [Header("Triggers")]
    [SerializeField] private bool startOnSceneLoad = true;
    [SerializeField] private bool startQuestAfterOpening = true;
    [SerializeField, Min(0f)] private float requiredMovementBeforeApproach = 1.5f;
    [SerializeField, Min(0.1f)] private float approachDistance = 9f;
    [SerializeField, Min(0.1f)] private float confrontationDistance = 5f;
    [SerializeField] private bool pauseBitDuringCutsceneDialog = true;

    private Vector3 playerPositionWhenOpeningFinished;
    private bool openingFinished;
    private bool approachPlayed;
    private bool confrontationPlayed;
    private bool sequenceStarted;
    private Coroutine proximityRoutine;

    private void Awake()
    {
        ResolveReferences();
        PrepareCompletionDialog();
    }

    private void Start()
    {
        if (startOnSceneLoad)
            StartSequence();
    }

    private void Update()
    {
        TryPlayProximityDialogs();
    }

    private IEnumerator WatchProximityDialogs()
    {
        WaitForSecondsRealtime delay = new(0.1f);

        while (!approachPlayed || !confrontationPlayed)
        {
            TryPlayProximityDialogs();
            yield return delay;
        }

        proximityRoutine = null;
    }

    private void TryPlayProximityDialogs()
    {
        if (!openingFinished ||
            canvasDialog == null ||
            canvasDialog.IsActive)
        {
            return;
        }

        float nearestSnakeDistance = GetNearestSnakeDistance();

        if (!approachPlayed && ShouldPlayApproachDialog(nearestSnakeDistance))
        {
            PlayApproachDialog();
            return;
        }

        if (approachPlayed &&
            !confrontationPlayed &&
            nearestSnakeDistance <= confrontationDistance)
        {
            PlayConfrontationDialog();
        }
    }

    public void StartSequence()
    {
        if (sequenceStarted)
            return;

        sequenceStarted = true;
        ResolveReferences();
        PrepareCompletionDialog();

        PlayCutsceneRange(
            openingStart,
            openingEnd,
            HandleOpeningFinished
        );
    }

    private void HandleOpeningFinished()
    {
        openingFinished = true;

        if (player != null)
            playerPositionWhenOpeningFinished = player.position;

        if (proximityRoutine == null)
            proximityRoutine = StartCoroutine(WatchProximityDialogs());

        if (startQuestAfterOpening && cableQuestController != null)
            cableQuestController.StartCableQuest();

        if (bitCompanion != null)
            bitCompanion.StartFollowing();
    }

    private void PlayApproachDialog()
    {
        approachPlayed = true;

        PlayCutsceneRange(
            approachStart,
            approachEnd,
            () =>
            {
                if (bitCompanion != null)
                    bitCompanion.StartFollowing();
            }
        );
    }

    private void PlayConfrontationDialog()
    {
        confrontationPlayed = true;
        PlayRange(
            confrontationStart,
            confrontationEnd,
            DialogPlaybackMode.Passive
        );
    }

    private void PlayCutsceneRange(
        int startNumber,
        int endNumber,
        Action finishedCallback)
    {
        if (pauseBitDuringCutsceneDialog && bitCompanion != null)
            bitCompanion.StopFollowing();

        PlayRange(
            startNumber,
            endNumber,
            DialogPlaybackMode.Cutscene,
            finishedCallback
        );
    }

    private void PlayRange(
        int startNumber,
        int endNumber,
        DialogPlaybackMode mode,
        Action finishedCallback = null)
    {
        DialogAwalEntry[] entries = CreateDialogRange(
            startNumber,
            endNumber
        );

        if (entries.Length == 0)
        {
            finishedCallback?.Invoke();
            return;
        }

        canvasDialog.PlayDialog(entries, mode, finishedCallback);
    }

    private void PrepareCompletionDialog()
    {
        if (cableQuestController == null)
            return;

        cableQuestController.SetCompletionDialog(
            CreateDialogRange(completionStart, completionEnd)
        );
    }

    private DialogAwalEntry[] CreateDialogRange(
        int startNumber,
        int endNumber)
    {
        DialogAwalEntry[] sourceEntries = GetDialogEntries();

        if (sourceEntries == null || sourceEntries.Length == 0)
            return Array.Empty<DialogAwalEntry>();

        int startIndex = Mathf.Max(0, startNumber - 1);
        int endIndex = Mathf.Min(sourceEntries.Length - 1, endNumber - 1);

        if (startIndex > endIndex)
            return Array.Empty<DialogAwalEntry>();

        int count = endIndex - startIndex + 1;
        DialogAwalEntry[] range = new DialogAwalEntry[count];
        Array.Copy(sourceEntries, startIndex, range, 0, count);
        return range;
    }

    private DialogAwalEntry[] GetDialogEntries()
    {
        if (canvasDialog == null || DialogEntriesField == null)
            return null;

        return DialogEntriesField.GetValue(canvasDialog)
            as DialogAwalEntry[];
    }

    private float GetNearestSnakeDistance()
    {
        if (player == null)
            return float.PositiveInfinity;

        if (lanSnakes == null || lanSnakes.Length == 0)
            ResolveSnakes();

        float nearestSqrDistance = float.PositiveInfinity;

        foreach (Level3LanSnake snake in lanSnakes)
        {
            if (snake == null || snake.IsDead || !snake.gameObject.activeInHierarchy)
                continue;

            float sqrDistance =
                (snake.transform.position - player.position).sqrMagnitude;

            if (sqrDistance < nearestSqrDistance)
                nearestSqrDistance = sqrDistance;
        }

        return Mathf.Sqrt(nearestSqrDistance);
    }

    private bool HasPlayerMovedAfterOpening()
    {
        if (player == null)
            return false;

        Vector3 movement = player.position - playerPositionWhenOpeningFinished;
        movement.y = 0f;
        return movement.sqrMagnitude >=
               requiredMovementBeforeApproach * requiredMovementBeforeApproach;
    }

    private bool ShouldPlayApproachDialog(float nearestSnakeDistance)
    {
        if (nearestSnakeDistance > approachDistance)
            return false;

        return HasPlayerMovedAfterOpening() ||
               nearestSnakeDistance <= confrontationDistance;
    }

    private void ResolveReferences()
    {
        if (canvasDialog == null)
        {
            canvasDialog =
                FindFirstObjectByType<dialogAwalScene>(
                    FindObjectsInactive.Include
                );
        }

        if (cableQuestController == null)
        {
            cableQuestController =
                FindFirstObjectByType<CableQuestController>(
                    FindObjectsInactive.Include
                );
        }

        if (bitCompanion == null)
        {
            bitCompanion =
                FindFirstObjectByType<SimpleBitFollower>(
                    FindObjectsInactive.Include
                );
        }

        if (player == null)
        {
            PlayerController playerController =
                FindFirstObjectByType<PlayerController>(
                    FindObjectsInactive.Include
                );

            if (playerController != null)
                player = playerController.transform;
        }

        ResolveSnakes();
    }

    private void ResolveSnakes()
    {
        lanSnakes = FindObjectsByType<Level3LanSnake>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );
    }
}
