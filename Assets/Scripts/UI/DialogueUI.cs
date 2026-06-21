using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class DialogueUI : MonoBehaviour
{
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TextMeshProUGUI speakerNameText;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private Transform choiceParent;
    [SerializeField] private GameObject choiceButtonPrefab;
    private Vector3 npcPosition;
    private Coroutine showChoicesCoroutine;

    void Awake()
    {
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
    }

    public void ShowDialogue(string speakerName, string line)
    {
        dialoguePanel.SetActive(true);
        speakerNameText.text = speakerName;
        dialogueText.text = line;

        foreach (Transform child in choiceParent)
        {
            Destroy(child.gameObject);
        }
    }

    public void UpdateDialogue(string line)
    {
        dialogueText.text = line;
    }

    public void ShowChoices(DialogueChoice[] choices)
    {
        CancelPendingChoices();

        foreach (Transform child in choiceParent)
        {
            Destroy(child.gameObject);
        }

        for (int i = 0; i < choices.Length; i++)
        {
            GameObject choiceObj = Instantiate(choiceButtonPrefab, choiceParent, false);
            
            // Set text
            TextMeshProUGUI label = choiceObj.GetComponentInChildren<TextMeshProUGUI>();
            Button button = choiceObj.GetComponent<Button>();

            // Fix positioning - reset RectTransform
            RectTransform rect = choiceObj.GetComponent<RectTransform>();
            if (label == null || button == null)
            {
                Debug.LogError("Choice Prefab membutuhkan button dan text meshpro");
                Destroy(choiceObj);
                continue;
            }

            if (rect != null)
            {
                rect.localScale = Vector3.one;
                rect.localRotation = Quaternion.identity;
                rect.sizeDelta = new Vector2(rect.sizeDelta.x, 56f);
            }

            label.text = choices[i].choiceText;

            // Add click listener
            int choiceIndex = i;
            button.onClick.AddListener(() =>
            {
                DialogueSystem.Instance.SelectChoice(choiceIndex);
            });
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate( choiceParent as RectTransform );
    }

    public void SetNPCPosition(Vector3 position)
    {
        npcPosition = position;
        // Canvas positioning is handled by the World Space Canvas on the NPC
        // Don't move the canvas here - it should stay as a child of the NPC
    }



    public void HideDialogue()
    {
        CancelPendingChoices();
        dialoguePanel.SetActive(false);
    }

    public void HideChoices()
    {
        CancelPendingChoices();

        foreach (Transform child in choiceParent)
        {
            Destroy(child.gameObject);
        }
    }

    public void ShowChoicesAfterDelay(DialogueChoice[] choices, float delay)
    {
        CancelPendingChoices();
        showChoicesCoroutine = StartCoroutine(ShowChoicesCoroutine(choices, delay));
    }

    IEnumerator ShowChoicesCoroutine(DialogueChoice[] choices, float delay)
    {
        yield return new WaitForSeconds(delay);
        showChoicesCoroutine = null;

        if (DialogueSystem.Instance == null ||
            !DialogueSystem.Instance.IsDialogueActive() ||
            DialogueSystem.Instance.IsExternalSequenceActive())
        {
            yield break;
        }

        ShowChoices(choices);
    }

    private void CancelPendingChoices()
    {
        if (showChoicesCoroutine == null)
            return;

        StopCoroutine(showChoicesCoroutine);
        showChoicesCoroutine = null;
    }
}
