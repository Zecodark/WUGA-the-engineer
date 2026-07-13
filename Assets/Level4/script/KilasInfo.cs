using System;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

public class KilasInfo : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private RectTransform animatedPanel;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Animation")]
    [SerializeField, Min(0f)] private float showDuration = 0.25f;
    [SerializeField, Min(0f)] private float minimumShowTime = 0.35f;
    [SerializeField] private Vector3 hiddenScale = new Vector3(0.85f, 0.85f, 1f);
    [SerializeField] private Vector3 visibleScale = Vector3.one;

    [Header("Gameplay Lock")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private bool lockCameraInput = true;

    private CinemachineInputAxisController cameraInput;
    private Action finishedCallback;
    private Coroutine showRoutine;
    private bool isShowing;
    private bool previousCameraInputState;
    private float previousTimeScale = 1f;
    private float inputReadyTime;

    public bool IsShowing => isShowing;

    private void Awake()
    {
        ResolveReferences();

        if (!isShowing)
            HideImmediate();
    }

    private void Update()
    {
        if (!isShowing || Time.unscaledTime < inputReadyTime)
            return;

        if (Input.anyKeyDown || Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
            Continue();
    }

    public void Show(Action onFinished = null)
    {
        if (isShowing)
            return;

        ResolveReferences();

        finishedCallback = onFinished;
        isShowing = true;
        inputReadyTime = Time.unscaledTime + minimumShowTime;

        LockGameplay();

        if (panelRoot != null)
            panelRoot.SetActive(true);

        if (showRoutine != null)
            StopCoroutine(showRoutine);

        showRoutine = StartCoroutine(ShowAnimation());
    }

    public void Continue()
    {
        if (!isShowing)
            return;

        if (showRoutine != null)
        {
            StopCoroutine(showRoutine);
            showRoutine = null;
        }

        isShowing = false;
        HideImmediate();
        UnlockGameplay();

        Action callback = finishedCallback;
        finishedCallback = null;
        callback?.Invoke();
    }

    private IEnumerator ShowAnimation()
    {
        if (canvasGroup != null)
            canvasGroup.alpha = 0f;

        if (animatedPanel != null)
            animatedPanel.localScale = hiddenScale;

        float elapsed = 0f;

        while (elapsed < showDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = showDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / showDuration);
            t = 1f - Mathf.Pow(1f - t, 3f);

            if (canvasGroup != null)
                canvasGroup.alpha = t;

            if (animatedPanel != null)
                animatedPanel.localScale = Vector3.LerpUnclamped(hiddenScale, visibleScale, t);

            yield return null;
        }

        if (canvasGroup != null)
            canvasGroup.alpha = 1f;

        if (animatedPanel != null)
            animatedPanel.localScale = visibleScale;

        showRoutine = null;
    }

    private void HideImmediate()
    {
        if (canvasGroup != null)
            canvasGroup.alpha = 0f;

        if (animatedPanel != null)
            animatedPanel.localScale = hiddenScale;

        if (panelRoot != null)
            panelRoot.SetActive(false);
        else
            gameObject.SetActive(false);
    }

    private void LockGameplay()
    {
        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        if (playerController != null)
            playerController.LockInput();

        if (lockCameraInput && cameraInput != null)
        {
            previousCameraInputState = cameraInput.enabled;
            cameraInput.enabled = false;
        }
    }

    private void UnlockGameplay()
    {
        Time.timeScale = previousTimeScale <= 0f ? 1f : previousTimeScale;

        if (playerController != null)
            playerController.UnlockInput();

        if (lockCameraInput && cameraInput != null)
            cameraInput.enabled = previousCameraInputState;
    }

    private void ResolveReferences()
    {
        if (panelRoot == null)
            panelRoot = gameObject;

        if (animatedPanel == null)
        {
            Transform infoPanel = transform.Find("InfoMusuh");
            animatedPanel = infoPanel != null
                ? infoPanel as RectTransform
                : transform as RectTransform;
        }

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();

            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        if (playerController == null)
            playerController = FindFirstObjectByType<PlayerController>();

        if (cameraInput == null)
            cameraInput = FindFirstObjectByType<CinemachineInputAxisController>();
    }

    private void OnDisable()
    {
        if (isShowing)
        {
            isShowing = false;
            UnlockGameplay();
        }
    }
}
