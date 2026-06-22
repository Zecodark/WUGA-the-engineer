using UnityEngine;
using TMPro;
using System;
using UnityEngine.UI;
using System.Collections;

public class MonologueController : MonoBehaviour
{
    
    [Header("UI References")]
    [SerializeField] private GameObject monologuePanel;
    [SerializeField] private Image panelArtwork;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private GameObject continuePrompt;

    [Header("Dialogue")]
    [SerializeField] private MonologueLine[] lines;

    [Header("Typing")]
    [SerializeField, Min(0.001f)] private float secondsPerCharacter = 0.035f;
    [SerializeField] private AudioSource typingAudioSource;
    [SerializeField] private AudioClip typingSound;
    [SerializeField, Min(1)] private int soundEveryCharacters = 2;

    private int currentLineIndex;
    private bool isPlaying;
    private bool isTyping;
    private Coroutine typingCoroutine;
    private Action onFinished;

    public bool IsPlaying => isPlaying; 



    private void Awake()
    {
        if (monologuePanel != null)
            monologuePanel.SetActive(false);

        if (continuePrompt != null)
            continuePrompt.SetActive(false);
    }

    private void Update()
    {
        if (!isPlaying)
            return;

        if (!Input.GetKeyDown(KeyCode.E) && 
            !Input.GetKeyDown(KeyCode.Return))
        {
            return;
            
        }

        if (isTyping)
        {
            CompleteTypingImmediately();
            return;
        }

        ShowNextLine();
    }
    

public void Play(Action finishedCallback = null)
{
    if (lines == null || lines.Length == 0)
    {
        Debug.LogWarning("[MonologueController] Lines masih kosong.");
        finishedCallback?.Invoke();
        return;
    }

    onFinished = finishedCallback;
    currentLineIndex = 0;
    isPlaying = true;

    monologuePanel.SetActive(true);
    ShowCurrentLine();
}

private void ShowNextLine()
{
    currentLineIndex++;

    if(currentLineIndex >= lines.Length)
    {
        Finish();
        return;
    }
    ShowCurrentLine();
}


private void ShowCurrentLine()
{
    MonologueLine line = lines[currentLineIndex];

    if (line.panelSprite != null)
    {
        panelArtwork.sprite = line.panelSprite;
        panelArtwork.preserveAspect = true;
    }

    if (typingCoroutine != null)
        StopCoroutine(typingCoroutine);

    typingCoroutine = StartCoroutine(TypeText(line.text));

}

private IEnumerator TypeText(string text)
{
    isTyping = true;
    dialogueText.text = "";

    if (continuePrompt != null)
        continuePrompt.SetActive(false);

    for (int i = 0; i < text.Length; i++)
    {
        dialogueText.text += text[i];

        bool shouldPlaySound = 
        !char.IsWhiteSpace(text[i]) &&
        i % soundEveryCharacters == 0;
        
        if (shouldPlaySound &&
            typingAudioSource != null &&
            typingSound != null)
        {
            typingAudioSource.PlayOneShot(typingSound);
        }
        yield return new WaitForSeconds(secondsPerCharacter);
    }

typingCoroutine = null;
isTyping = false;

    if (continuePrompt != null)
        continuePrompt.SetActive(true);
}

private void CompleteTypingImmediately()
{
    if (typingCoroutine != null)  
    {
        StopCoroutine(typingCoroutine);
        typingCoroutine = null;
    }

    dialogueText.text = lines[currentLineIndex].text;
    isTyping = false;

    if (continuePrompt != null)
        continuePrompt.SetActive(true);

}


private void Finish()
{

    if (typingCoroutine != null)
        StopCoroutine(typingCoroutine);

    typingCoroutine = null;
    isTyping = false;
    isPlaying = false;

    dialogueText.text = "";
    monologuePanel.SetActive(false);

    Action callback = onFinished;
    onFinished = null;
    callback?.Invoke();
}

}
