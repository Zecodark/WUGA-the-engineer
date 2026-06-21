using System;
using System.Collections;
using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.SceneManagement;


public enum CutsceneAction
{
    StartItemTour,
    StartItemTourThenAcceptQuest,
    StartCompletedSequence
}

[Serializable]
public class CutsceneShot
{
    public string shotName;
    public CinemachineCamera camera;
    [Min(0.1f)] public float duration = 2f;
}

[Serializable]
public class DialogueCutsceneCue
{
    public DialogueData dialogue;
    [Tooltip("Index dimulai dari 0, Line kelima = index 4.")]
    public int lineIndex;

    public CutsceneAction action;
}

[Serializable]
public class DialogueChoiceCutsceneCue
{
    public DialogueData dialogue;
    [Min(0)] public int choiceIndex;
    public CutsceneAction action;
}

public class BitCutSceneDirector : MonoBehaviour
{

    public static BitCutSceneDirector Instance { get; private set; }

    [Header("Player")]
    [SerializeField] private PlayerController playerController;
    [Tooltip("Drag child yang berisi mesh/model karakter, bukan root Player.")]
    [SerializeField] private GameObject playerVisualRoot;
    [SerializeField] private bool hidePlayerDuringCutscene = true;

    [Header("Camera Priority")]
    [SerializeField] private int inactivePriority = 0;
    [SerializeField] private int activePriority = 100;

    [Header("Main Camera")]
    [SerializeField] private CinemachineCamera gameplayCamera;
    [SerializeField] private CinemachineCamera conversationCamera;
    [SerializeField] private CinemachineCamera completedCamera;

    [Header("Item Tour")]
    [SerializeField] private CutsceneShot[] itemTourShots;

    [Header("Dialogue Line Cue")]
    [SerializeField] private DialogueCutsceneCue[] lineCues;

    [Header("Dialogue Choice Cues")]
    [SerializeField] private DialogueChoiceCutsceneCue[] choiceCues;

    [Header("Quest Items")]
    [SerializeField] private QuestItemManager questItemManager;

    [Header("Completed Transition")]
    [SerializeField, Min(0.1f)] private float completedShotDuration = 2f;
    [SerializeField, Min(0f)] private float fadeWaitDuration = 0.6f;
    [SerializeField] private string destinationSceneName;

    private bool sequenceRunning;
    private bool cutsceneActive;
    private bool endingForScene;
    private bool previousCursorVisible;
    private CursorLockMode previousCursorLockMode;

    public bool IsCutsceneActive => cutsceneActive;
    public bool IsSequenceRunning => sequenceRunning;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        ActiveOnly(gameplayCamera);
    }

    public void ShowConversationCamera()
    {
        if (sequenceRunning)
        return;
        ActiveOnly(conversationCamera);
    }

    public void ReturnToGameplayCamera()
    {
        EndCutscene();
    }

    public void PlayItemTour(Action onFinished = null)
    {
        if (sequenceRunning)
        return;
        StartCoroutine(ItemTourRoutine(onFinished));
    }

    public void PlayCompletedShot(Action onFinished = null)
    {
        if (sequenceRunning)
        return;
        StartCoroutine(CompletedShotRoutine(onFinished));
    }

    private IEnumerator ItemTourRoutine(Action onFinished)
    {
        sequenceRunning = true;
        foreach (CutsceneShot shot in itemTourShots)
        {
            if (shot == null || shot.camera == null)
            continue;

            ActiveOnly(shot.camera);
            yield return new WaitForSeconds(shot.duration);
        }

        ActiveOnly(conversationCamera);
        sequenceRunning = false;
        onFinished?.Invoke();

    }

    private IEnumerator CompletedShotRoutine(Action onFinished)
    {
        sequenceRunning = true;

        if (completedCamera != null)
        {
            ActiveOnly(completedCamera);
            yield return new WaitForSeconds(completedShotDuration);
        }

        sequenceRunning = false;
        onFinished?.Invoke();
    }

    private void ActiveOnly (CinemachineCamera targetCamera)
    {
        SetPriority(gameplayCamera, inactivePriority);
        SetPriority(conversationCamera, inactivePriority);
        SetPriority(completedCamera, inactivePriority);

        foreach (CutsceneShot shot in itemTourShots)
        {
            if (shot != null)
                SetPriority(shot.camera, inactivePriority);
        }

        SetPriority(targetCamera, activePriority);

    }

    private void SetPriority(CinemachineCamera camera, int value)
    {
        if (camera == null)
        return;

        PrioritySettings priority = camera.Priority;
        priority.Enabled = true;
        priority.Value = value;
        camera.Priority = priority;
    }

    public void BeginCutscene()
    {
        if (cutsceneActive)
        return;

        cutsceneActive = true;

        previousCursorVisible = Cursor.visible;
        previousCursorLockMode = Cursor.lockState;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        if (playerController != null)
            playerController.LockInput();

        SetPlayerVisible(false);
        ShowConversationCamera();
    }

    public void EndCutscene()
    {
        StopAllCoroutines();

        sequenceRunning = false;
        cutsceneActive = false;

        ActiveOnly(gameplayCamera);

        if (playerController != null)
            playerController.UnlockInput();

        SetPlayerVisible(true);
        Cursor.visible = previousCursorVisible;
        Cursor.lockState = previousCursorLockMode;
    }

    private void SetPlayerVisible(bool visible)
    {
        if (!hidePlayerDuringCutscene || playerVisualRoot == null)
            return;

        playerVisualRoot.SetActive(visible);
    }

    private void OnEnable()
    {
        StartCoroutine(SubscribeToDialogueSystem());
    }

    private IEnumerator SubscribeToDialogueSystem()
    {
        yield return null;

        if (DialogueSystem.Instance != null)
        {
            DialogueSystem.Instance.OnLineShown += HandleLineShown;
            DialogueSystem.Instance.OnChoiceSelected += HandleChoiceSelected;
            DialogueSystem.Instance.OnDialogueEnded += HandleDialogueEnded;
        }
    }

    private void OnDisable()
    {
        if (DialogueSystem.Instance != null)
        {
            DialogueSystem.Instance.OnLineShown -= HandleLineShown;
            DialogueSystem.Instance.OnChoiceSelected -= HandleChoiceSelected;
            DialogueSystem.Instance.OnDialogueEnded -= HandleDialogueEnded;
        }
    }

    private void HandleLineShown (DialogueData dialogue, int lineIndex)
    {
        if (sequenceRunning)
            return;

        foreach (DialogueCutsceneCue cue in lineCues)
        {
            if (cue == null)
                continue;

            if (cue.dialogue != dialogue || cue.lineIndex != lineIndex)
                continue;

            ExecuteCue(cue.action);
            return;
        }
    }

    private void HandleChoiceSelected(DialogueData dialogue, int choiceIndex)
    {
        if (sequenceRunning)
            return;

        foreach (DialogueChoiceCutsceneCue cue in choiceCues)
        {
            if (cue == null)
                continue;

            if (cue.dialogue != dialogue || cue.choiceIndex != choiceIndex)
                continue;

            ExecuteCue(cue.action);
            return;
        }
    }

    private void HandleDialogueEnded()
    {
        if (!cutsceneActive || sequenceRunning || endingForScene)
            return;

        EndCutscene();
    }

    private void ExecuteCue(CutsceneAction action)
    {
        if (DialogueSystem.Instance == null)
            return;

        switch (action)
        {
            case CutsceneAction.StartItemTour:
                DialogueSystem.Instance.BeginExternalSequence();

                PlayItemTour(()=> 
                {
                    DialogueSystem.Instance.CompleteSequenceAndShowChoices();
                });
                break;

            case CutsceneAction.StartItemTourThenAcceptQuest:
                DialogueSystem.Instance.BeginExternalSequence();

                if (questItemManager != null)
                    questItemManager.SetPreviewVisible(true);

                PlayItemTour(() =>
                {
                    DialogueSystem.Instance.FinishDialogue(true);
                    EndCutscene();
                });
                break;

            case CutsceneAction.StartCompletedSequence:
                DialogueSystem.Instance.BeginExternalSequence();
                endingForScene = true;

                PlayCompletedShot(()=>
                {
                    StartCoroutine(LoadDestinationRoutine());
                });
                break;
        }
    }

    private IEnumerator LoadDestinationRoutine()
    {
        if (DialogueSystem.Instance != null)
            DialogueSystem.Instance.FinishDialogue(false);

        if (CameraManager.Instance != null)
            CameraManager.Instance.FadeOut();

        yield return new WaitForSeconds(fadeWaitDuration);

        if (string.IsNullOrWhiteSpace(destinationSceneName))
        {
            Debug.LogWarning(
                "[BitCutSceneDirector] Destination Scene Name masih kosong."
            );

            endingForScene = false;
            EndCutscene();
            yield break;
        }

        SceneManager.LoadScene(destinationSceneName);
    }

}
