using System;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[Serializable]
public class DialogAwalEntry
{
    [Tooltip("Nama/kategori agar mudah dikenali di Inspector.")]
    public string categoryName = "Dialog";

    [Tooltip("Sprite desain PanelDialog untuk dialog ini.")]
    public Sprite panelSprite;

    [TextArea(2, 6)]
    [Tooltip("Isi teks dialog.")]
    public string content;
}

public enum DialogPlaybackMode
{
    Cutscene,
    Passive
}

/// <summary>
/// Dialog pembuka dengan satu PanelDialog dan satu TextDialog.
/// Setiap entry dapat mengganti sprite panel dan isi teks secara dinamis.
/// </summary>
public class dialogAwalScene : MonoBehaviour
{
    [Header("UI Bersama")]
    [SerializeField] private Image dialogPanel;
    [SerializeField] private Text dialogText;

    [Header("Daftar Dialog")]
    [Tooltip("Satu elemen = kategori, sprite panel, dan isi teks.")]
    [SerializeField] private DialogAwalEntry[] dialogEntries;

    [Header("Pemicu")]
    [SerializeField] private bool playOnStart = true;

    [Header("Efek Teks")]
    [SerializeField] private bool useTypingEffect = true;

    [Min(0.001f)]
    [SerializeField] private float characterDelay = 0.025f;

    [Min(0f)]
    [SerializeField] private float initialInputDelay = 0.35f;

    [Header("Kunci Gameplay")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private bool lockCameraInput = true;

    [Header("Event Opsional")]
    [SerializeField] private UnityEvent onDialogStarted;
    [SerializeField] private UnityEvent onDialogFinished;

    private CinemachineInputAxisController cameraInput;
    private Coroutine typingCoroutine;
    private int currentDialogIndex;
    private bool isTyping;
    private bool isActive;
    private bool previousCameraInputState;
    private float inputReadyTime;
    private DialogAwalEntry[] activeEntries;
    private DialogPlaybackMode playbackMode;
    private Action runtimeFinishedCallback;

    public bool IsActive => isActive;
    public int CurrentDialogIndex => currentDialogIndex;

    private DialogAwalEntry CurrentEntry =>
        activeEntries != null &&
        currentDialogIndex >= 0 &&
        currentDialogIndex < activeEntries.Length
            ? activeEntries[currentDialogIndex]
            : null;

    private void Reset()
    {
        ResolveReferences();
    }

    private void Awake()
    {
        ResolveReferences();
        SetDialogVisible(false);
    }

    private void Start()
    {
        if (playOnStart)
            BeginDialog();
    }

    private void Update()
    {
        if (!isActive)
            return;

        if (playbackMode == DialogPlaybackMode.Cutscene)
            KeepGameplayLocked();

        if (Time.unscaledTime < inputReadyTime)
            return;

        if (Input.GetKeyDown(KeyCode.E) ||
            Input.GetKeyDown(KeyCode.Return))
        {
            AdvanceDialog();
        }
    }

    public void BeginDialog()
    {
        PlayDialog(dialogEntries, DialogPlaybackMode.Cutscene);
    }

    public void BeginDialog(Action finishedCallback)
    {
        PlayDialog(
            dialogEntries,
            DialogPlaybackMode.Cutscene,
            finishedCallback
        );
    }

    public void PlayDialog(
        DialogAwalEntry[] entries,
        DialogPlaybackMode mode = DialogPlaybackMode.Passive,
        Action finishedCallback = null)
    {
        if (isActive)
            FinishDialog();

        ResolveReferences();

        if (dialogPanel == null || dialogText == null)
        {
            Debug.LogError(
                "PanelDialog Image atau TextDialog belum dipasang.",
                this
            );
            return;
        }

        if (entries == null || entries.Length == 0)
        {
            Debug.LogWarning(
                "Daftar dialog masih kosong. Gameplay langsung dimulai.",
                this
            );
            finishedCallback?.Invoke();
            return;
        }

        activeEntries = entries;
        playbackMode = mode;
        runtimeFinishedCallback = finishedCallback;
        currentDialogIndex = 0;
        isActive = true;
        inputReadyTime = Time.unscaledTime + initialInputDelay;

        SetDialogVisible(true);
        if (playbackMode == DialogPlaybackMode.Cutscene)
            LockGameplay();

        onDialogStarted?.Invoke();
        ShowCurrentDialog();
    }

    public void AdvanceDialog()
    {
        if (!isActive)
            return;

        if (isTyping)
        {
            CompleteTypingImmediately();
            return;
        }

        currentDialogIndex++;

        if (currentDialogIndex >= activeEntries.Length)
        {
            FinishDialog();
            return;
        }

        ShowCurrentDialog();
    }

    public void FinishDialog()
    {
        StopTyping();
        isActive = false;
        SetDialogVisible(false);

        if (playbackMode == DialogPlaybackMode.Cutscene)
            UnlockGameplay();

        onDialogFinished?.Invoke();

        Action callback = runtimeFinishedCallback;
        runtimeFinishedCallback = null;
        activeEntries = null;
        callback?.Invoke();
    }

    private void ShowCurrentDialog()
    {
        DialogAwalEntry entry = CurrentEntry;

        if (entry == null)
        {
            AdvanceDialog();
            return;
        }

        // Sprite boleh kosong. Jika kosong, sprite sebelumnya tetap digunakan.
        if (entry.panelSprite != null)
            dialogPanel.sprite = entry.panelSprite;

        string line = entry.content ?? string.Empty;

        StopTyping();

        if (useTypingEffect && characterDelay > 0f)
        {
            isTyping = true;
            typingCoroutine = StartCoroutine(TypeLine(line));
        }
        else
        {
            dialogText.text = line;
        }
    }

    private IEnumerator TypeLine(string line)
    {
        dialogText.text = string.Empty;

        for (int i = 0; i < line.Length; i++)
        {
            dialogText.text = line.Substring(0, i + 1);
            yield return new WaitForSecondsRealtime(characterDelay);
        }

        dialogText.text = line;
        isTyping = false;
        typingCoroutine = null;
    }

    private void CompleteTypingImmediately()
    {
        StopTyping();
        dialogText.text = CurrentEntry?.content ?? string.Empty;
    }

    private void StopTyping()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        isTyping = false;
    }

    private void SetDialogVisible(bool visible)
    {
        if (dialogPanel != null)
            dialogPanel.gameObject.SetActive(visible);

        if (dialogText != null)
        {
            dialogText.gameObject.SetActive(visible);

            if (!visible)
                dialogText.text = string.Empty;
        }
    }

    private void LockGameplay()
    {
        if (playerController != null)
            playerController.LockInput();

        if (lockCameraInput && cameraInput != null)
        {
            previousCameraInputState = cameraInput.enabled;
            cameraInput.enabled = false;
        }
    }

    private void KeepGameplayLocked()
    {
        if (playerController != null && !playerController.IsInputLocked)
            playerController.LockInput();

        if (lockCameraInput && cameraInput != null && cameraInput.enabled)
            cameraInput.enabled = false;
    }

    private void UnlockGameplay()
    {
        if (playerController != null)
            playerController.UnlockInput();

        if (lockCameraInput && cameraInput != null)
            cameraInput.enabled = previousCameraInputState;
    }

    private void ResolveReferences()
    {
        if (playerController == null)
            playerController = FindFirstObjectByType<PlayerController>();

        if (cameraInput == null)
            cameraInput = FindFirstObjectByType<CinemachineInputAxisController>();

        if (dialogPanel == null)
        {
            foreach (Image image in GetComponentsInChildren<Image>(true))
            {
                if (image.name == "PanelDialog")
                {
                    dialogPanel = image;
                    break;
                }
            }
        }

        if (dialogText == null)
        {
            foreach (Text text in GetComponentsInChildren<Text>(true))
            {
                if (text.name == "TextDialog")
                {
                    dialogText = text;
                    break;
                }
            }
        }
    }

    private void OnDisable()
    {
        if (isActive && playbackMode == DialogPlaybackMode.Cutscene)
            UnlockGameplay();
    }
}
