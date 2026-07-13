using UnityEngine;

/// <summary>
/// Mengatur seluruh sound effect khusus Level 2 tanpa mengubah hierarchy UI.
/// </summary>
public sealed class Level2AudioController : MonoBehaviour
{
    public static Level2AudioController Instance { get; private set; }

    [Header("Audio Clips Level 2")]
    [SerializeField] private AudioClip bitEffect;
    [SerializeField] private AudioClip portalEffect;
    [SerializeField] private AudioClip taskComplete;
    [SerializeField] private AudioClip jumpSound;
    [SerializeField] private AudioClip runSound;
    [SerializeField] private AudioClip dialogTypingSound;

    [Header("Target Suara Spasial")]
    [SerializeField] private Transform bitTarget;
    [SerializeField] private Transform portalTarget;

    [Header("Volume")]
    [SerializeField, Range(0f, 1f)] private float bitVolume = 0.35f;
    [SerializeField, Range(0f, 1f)] private float portalVolume = 0.55f;
    [SerializeField, Range(0f, 1f)] private float taskCompleteVolume = 0.9f;
    [SerializeField, Range(0f, 1f)] private float jumpVolume = 0.75f;
    [SerializeField, Range(0f, 1f)] private float runVolume = 0.45f;
    [SerializeField, Range(0f, 1f)] private float dialogVolume = 0.4f;

    private AudioSource oneShotSource;
    private AudioSource bitSource;
    private AudioSource portalSource;
    private AudioSource runSource;
    private AudioSource dialogSource;
    private float nextTargetSearchTime;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[Level2Audio] Controller duplikat dinonaktifkan.", this);
            enabled = false;
            return;
        }

        Instance = this;

        oneShotSource = CreateSource("OneShotAudio", false, 0f, 1f);
        bitSource = CreateSource("BitAudio", true, 0.8f, bitVolume);
        portalSource = CreateSource("PortalAudio", true, 0.85f, portalVolume);
        runSource = CreateSource("WugaRunAudio", true, 0f, runVolume);
        dialogSource = CreateSource("DialogTypingAudio", true, 0f, dialogVolume);

        bitSource.clip = bitEffect;
        portalSource.clip = portalEffect;
        runSource.clip = runSound;
        dialogSource.clip = dialogTypingSound;
    }

    private void Start()
    {
        ResolveSpatialTargets();
        RefreshSpatialAudio();
        WarnForMissingClips();
    }

    private void Update()
    {
        if (Time.unscaledTime >= nextTargetSearchTime &&
            (bitTarget == null || portalTarget == null))
        {
            nextTargetSearchTime = Time.unscaledTime + 1f;
            ResolveSpatialTargets();
        }

        RefreshSpatialAudio();
    }

    private void OnDisable()
    {
        StopSource(bitSource);
        StopSource(portalSource);
        StopSource(runSource);
        StopSource(dialogSource);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public static void PlayJumpSound()
    {
        Level2AudioController controller = Instance;
        if (controller == null || controller.jumpSound == null)
            return;

        controller.oneShotSource.PlayOneShot(
            controller.jumpSound,
            controller.jumpVolume
        );
    }

    public static void SetRunSoundActive(bool active)
    {
        Level2AudioController controller = Instance;
        if (controller == null)
            return;

        controller.SetLoopState(
            controller.runSource,
            controller.runSound,
            active
        );
    }

    public static void SetDialogSoundActive(bool active)
    {
        Level2AudioController controller = Instance;
        if (controller == null)
            return;

        controller.SetLoopState(
            controller.dialogSource,
            controller.dialogTypingSound,
            active
        );
    }

    public static void PlayTaskCompleteSound()
    {
        Level2AudioController controller = Instance;
        if (controller == null || controller.taskComplete == null)
            return;

        controller.oneShotSource.PlayOneShot(
            controller.taskComplete,
            controller.taskCompleteVolume
        );
    }

    public static void PlayPortalEffect(Transform target)
    {
        Level2AudioController controller = Instance;
        if (controller == null)
            return;

        if (target != null)
            controller.portalTarget = target;

        controller.RefreshPortalAudio();
    }

    public static void SetBitTarget(Transform target)
    {
        Level2AudioController controller = Instance;
        if (controller == null)
            return;

        if (target != null)
            controller.bitTarget = target;

        controller.RefreshBitAudio();
    }

    private AudioSource CreateSource(
        string objectName,
        bool loop,
        float spatialBlend,
        float volume)
    {
        GameObject sourceObject = new(objectName);
        sourceObject.transform.SetParent(transform, false);

        AudioSource source = sourceObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = loop;
        source.spatialBlend = spatialBlend;
        source.volume = volume;
        source.dopplerLevel = 0f;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.minDistance = 2f;
        source.maxDistance = 28f;
        return source;
    }

    private void ResolveSpatialTargets()
    {
        if (bitTarget == null)
        {
            SimpleBitFollower bit =
                FindFirstObjectByType<SimpleBitFollower>(FindObjectsInactive.Include);

            if (bit != null)
                bitTarget = bit.transform;
        }

        if (portalTarget == null)
        {
            PortalFinishTrigger portal =
                FindFirstObjectByType<PortalFinishTrigger>(FindObjectsInactive.Include);

            if (portal != null)
                portalTarget = portal.transform;
        }
    }

    private void RefreshSpatialAudio()
    {
        RefreshBitAudio();
        RefreshPortalAudio();
    }

    private void RefreshBitAudio()
    {
        bool shouldPlay =
            bitTarget != null && bitTarget.gameObject.activeInHierarchy;

        if (bitTarget != null && bitSource != null)
            bitSource.transform.position = bitTarget.position;

        SetLoopState(bitSource, bitEffect, shouldPlay);
    }

    private void RefreshPortalAudio()
    {
        bool shouldPlay =
            portalTarget != null && portalTarget.gameObject.activeInHierarchy;

        if (portalTarget != null && portalSource != null)
            portalSource.transform.position = portalTarget.position;

        SetLoopState(portalSource, portalEffect, shouldPlay);
    }

    private void SetLoopState(
        AudioSource source,
        AudioClip clip,
        bool shouldPlay)
    {
        if (source == null)
            return;

        if (!shouldPlay || clip == null)
        {
            StopSource(source);
            return;
        }

        if (source.clip != clip)
            source.clip = clip;

        if (!source.isPlaying)
            source.Play();
    }

    private static void StopSource(AudioSource source)
    {
        if (source != null && source.isPlaying)
            source.Stop();
    }

    private void WarnForMissingClips()
    {
        if (bitEffect == null ||
            portalEffect == null ||
            taskComplete == null ||
            jumpSound == null ||
            runSound == null ||
            dialogTypingSound == null)
        {
            Debug.LogError(
                "[Level2Audio] Ada AudioClip Level 2 yang belum dipasang.",
                this
            );
        }
    }
}
