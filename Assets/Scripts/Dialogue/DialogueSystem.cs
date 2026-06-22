using System;
using UnityEngine;

public class DialogueSystem : MonoBehaviour
{
    public static DialogueSystem Instance { get; private set; }

    [SerializeField] private DialogueUI dialogueUI;

    private DialogueData currentDialogue;
    private int currentLineIndex;
    private bool isDialogueActive;
    private bool externalSequenceActive;
    private int startedFrame;

    public event Action OnDialogueStarted;
    public event Action OnDialogueEnded;
    public event Action<DialogueData, int> OnLineShown;
    public event Action<DialogueData, int> OnChoiceSelected;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Update()
    {
        if (isDialogueActive && !externalSequenceActive)
        {
            if (Time.frameCount > startedFrame && (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Return)))
            {
                NextLine();
            }
        }
    }

    public void StartDialogue(DialogueData dialogue, Vector3 npcPosition)
    {
        if (isDialogueActive || dialogue == null)
            return;

        if (dialogue.lines == null || dialogue.lines.Length == 0)
        {
            Debug.LogError("[DialogueSystem] Dialogue tidak memiliki line.");
            return;
        }

        currentDialogue = dialogue;
        currentLineIndex = 0;
        isDialogueActive = true;
        externalSequenceActive = false;
        startedFrame = Time.frameCount;

        dialogueUI.SetNPCPosition(npcPosition);
        dialogueUI.ShowDialogue(
            currentDialogue.speakerName,
            currentDialogue.lines[currentLineIndex]
        );

        OnDialogueStarted?.Invoke();
        OnLineShown?.Invoke(currentDialogue, currentLineIndex);
    }

    public void NextLine()
    {
        if (!isDialogueActive || externalSequenceActive)
            return;

        int nextIndex = currentLineIndex + 1;

        if (nextIndex >= currentDialogue.lines.Length)
        {
            if (currentDialogue.choices != null &&
                currentDialogue.choices.Length > 0)
            {
                dialogueUI.ShowChoices(currentDialogue.choices);
            }
            else
            {
                FinishDialogue(true);
            }

            return;
        }

        ShowLine(nextIndex);
    }

    public void SelectChoice(int choiceIndex)
    {
        if (!isDialogueActive || externalSequenceActive)
            return;

        if (currentDialogue.choices == null ||
            choiceIndex < 0 ||
            choiceIndex >= currentDialogue.choices.Length)
        {
            Debug.LogError($"[DialogueSystem] Invalid choice index: {choiceIndex}");
            return;
        }

        DialogueChoice choice = currentDialogue.choices[choiceIndex];

        if (choice.questToGive != null && QuestManager.Instance != null)
            QuestManager.Instance.AcceptQuest(choice.questToGive);

        if (choice.nextLineIndex < 0)
        {
            OnChoiceSelected?.Invoke(currentDialogue, choiceIndex);

            if (!externalSequenceActive)
                FinishDialogue(false);

            return;
        }

        if (choice.nextLineIndex >= currentDialogue.lines.Length)
        {
            Debug.LogError(
                $"[DialogueSystem] Invalid nextLineIndex: {choice.nextLineIndex}"
            );
            return;
        }

        dialogueUI.HideChoices();
        ShowLine(choice.nextLineIndex);

        // The response is visible before a choice-specific cutscene takes over.
        OnChoiceSelected?.Invoke(currentDialogue, choiceIndex);

        if (externalSequenceActive)
            return;

        dialogueUI.ShowChoicesAfterDelay(currentDialogue.choices, 2f);
    }

    private void ShowLine(int lineIndex)
    {
        currentLineIndex = lineIndex;
        dialogueUI.UpdateDialogue(currentDialogue.lines[currentLineIndex]);
        OnLineShown?.Invoke(currentDialogue, currentLineIndex);
    }

    public void BeginExternalSequence()
    {
        if (!isDialogueActive)
            return;

        externalSequenceActive = true;
        dialogueUI.HideChoices();
    }

    public void CompleteSequenceAndShowChoices()
    {
        if (!isDialogueActive)
            return;

        externalSequenceActive = false;

        if (currentDialogue.choices != null)
            dialogueUI.ShowChoices(currentDialogue.choices);
    }

    public void FinishDialogue(bool giveDialogueQuest)
    {
        if (!isDialogueActive)
            return;

        DialogueData finishedDialogue = currentDialogue;

        isDialogueActive = false;
        externalSequenceActive = false;
        currentDialogue = null;

        dialogueUI.HideChoices();
        dialogueUI.HideDialogue();

        if (giveDialogueQuest &&
            finishedDialogue.questToGive != null &&
            QuestManager.Instance != null)
        {
            QuestManager.Instance.AcceptQuest(finishedDialogue.questToGive);
        }

        OnDialogueEnded?.Invoke();
    }

    public bool IsDialogueActive()
    {
        return isDialogueActive;
    }

    public bool IsExternalSequenceActive()
    {
        return externalSequenceActive;
    }

    public DialogueData GetCurrentDialogue()
    {
        return currentDialogue;
    }

    public void TriggerQuestMarker(int itemIndex)
    {
        QuestMarker marker = FindFirstObjectByType<QuestMarker>();

        if (marker != null)
            marker.ShowMarker(itemIndex);
    }
}
