using System.Reflection;
using System;
using UnityEngine;

[DefaultExecutionOrder(-1000)]
public class Level4DialogSceneController : MonoBehaviour
{
    private static readonly FieldInfo DialogEntriesField =
        typeof(dialogAwalScene).GetField(
            "dialogEntries",
            BindingFlags.NonPublic | BindingFlags.Instance
        );

    private static readonly FieldInfo QuestItemManagerItemsField =
        typeof(QuestItemManager).GetField(
            "allItems",
            BindingFlags.NonPublic | BindingFlags.Instance
        );

    private static readonly FieldInfo PortalObjectField =
        typeof(PortalLevel1).GetField(
            "portalObject",
            BindingFlags.NonPublic | BindingFlags.Instance
        );

    [Header("Dialog")]
    [SerializeField] private dialogAwalScene openingDialog;
    [SerializeField] private bool startOnSceneLoad = true;

    [Header("Dialog Ranges")]
    [SerializeField, Min(1)] private int openingStart = 1;
    [SerializeField, Min(1)] private int openingEnd = 6;
    [SerializeField, Min(1)] private int bossAngryStart = 7;
    [SerializeField, Min(1)] private int bossAngryEnd = 7;
    [SerializeField, Min(1)] private int finalStart = 8;
    [SerializeField, Min(1)] private int finalEnd = 13;

    [Header("Dialog Triggers")]
    [SerializeField, Min(1)] private int bossAngryRequiredQuestCount = 4;
    [SerializeField] private bool finalRequiresAllQuestItems = true;
    [SerializeField, Range(0.01f, 1f)] private float finalBossHealthPercent = 0.25f;

    [Header("After Dialog")]
    [SerializeField] private KilasInfo kilasInfo;
    [SerializeField] private Level4QuestController questController;
    [SerializeField, HideInInspector] private Level1ProgressController progressController;
    [SerializeField] private SimpleBitFollower companionController;
    [SerializeField] private bool startCompanionIfNoProgress = true;
    [SerializeField] private BossHealt bossHealth;
    [SerializeField] private BossAttackControl[] bossAttackControls;
    [SerializeField] private PortalLevel1 levelPortal;
    [SerializeField] private GameOver gameOver;
    [SerializeField] private bool unlockPortalAfterFinalDialog = true;
    [SerializeField] private bool showResultPanelAfterFinalDialog = false;

    [Header("Level 4 Runtime Cleanup")]
    [SerializeField] private bool disableLegacyIntroControllers = false;
    [SerializeField] private bool disableQuestItemManagers = true;
    [SerializeField] private bool disableUnassignedPortalsOnStart = true;

    private bool sequenceStarted;
    private bool openingFinished;
    private bool bossAngryDialogPlayed;
    private bool finalDialogPlayed;

    private void Awake()
    {
        ResolveReferences();
        DisableOldCutsceneCameras();

        if (disableLegacyIntroControllers)
            DisableLegacyIntroControllers();

        if (disableQuestItemManagers)
            DisableQuestItemManagers();

        if (disableUnassignedPortalsOnStart)
            DisablePortalsDuringStartup();
    }

    private void Start()
    {
        if (startOnSceneLoad)
            StartIntroSequence();
    }

    private void Update()
    {
        TryPlayBossAngryDialog();
        TryPlayFinalDialog();
    }

    public void StartIntroSequence()
    {
        if (sequenceStarted)
            return;

        sequenceStarted = true;
        ResolveReferences();

        if (companionController != null)
            companionController.StopFollowing();

        StopBossAttacks();

        PlayCutsceneRange(
            openingStart,
            openingEnd,
            HandleOpeningDialogFinished
        );
    }

    private void HandleOpeningDialogFinished()
    {
        openingFinished = true;
        ResolveReferences();

        if (kilasInfo != null)
        {
            kilasInfo.Show(HandleKilasInfoFinished);
            return;
        }

        HandleKilasInfoFinished();
    }

    private void HandleKilasInfoFinished()
    {
        ResolveReferences();
        StartBossAttacks();

        if (questController != null)
        {
            questController.StartLevelQuest();
            return;
        }

        if (startCompanionIfNoProgress && companionController != null)
            companionController.StartFollowing();
    }

    private void TryPlayBossAngryDialog()
    {
        if (!openingFinished ||
            bossAngryDialogPlayed ||
            openingDialog == null ||
            openingDialog.IsActive ||
            questController == null ||
            questController.CompletedCount < bossAngryRequiredQuestCount)
        {
            return;
        }

        bossAngryDialogPlayed = true;
        StopBossAttacks();

        PlayCutsceneRange(
            bossAngryStart,
            bossAngryEnd,
            StartBossAttacks
        );
    }

    private void TryPlayFinalDialog()
    {
        if (!openingFinished ||
            finalDialogPlayed ||
            openingDialog == null ||
            openingDialog.IsActive ||
            questController == null ||
            bossHealth == null)
        {
            ResolveReferences();
        }

        if (!openingFinished ||
            finalDialogPlayed ||
            openingDialog == null ||
            openingDialog.IsActive ||
            questController == null ||
            bossHealth == null)
        {
            return;
        }

        if (!bossAngryDialogPlayed)
            return;

        if (finalRequiresAllQuestItems && !questController.AreAllItemsCompleted)
            return;

        if (!IsBossHealthLow())
            return;

        finalDialogPlayed = true;
        StopBossAttacks();

        PlayCutsceneRange(
            finalStart,
            finalEnd,
            HandleFinalDialogFinished
        );
    }

    private bool IsBossHealthLow()
    {
        if (bossHealth == null || bossHealth.MaxHealth <= 0)
            return false;

        int healthThreshold =
            Mathf.CeilToInt(bossHealth.MaxHealth * finalBossHealthPercent);

        return bossHealth.CurrentHealth <= healthThreshold;
    }

    private void HandleFinalDialogFinished()
    {
        if (companionController != null)
            companionController.StopFollowing();

        StopBossAttacks();

        if (unlockPortalAfterFinalDialog && levelPortal != null)
            levelPortal.UnlockPortal();

        if (showResultPanelAfterFinalDialog && gameOver != null)
        {
            int completed = questController != null ? questController.CompletedCount : 0;
            int total = questController != null ? questController.TotalCount : 0;
            gameOver.CompleteLevel(completed, total);
        }
    }

    private void PlayCutsceneRange(
        int startNumber,
        int endNumber,
        Action finishedCallback)
    {
        if (companionController != null)
            companionController.StopFollowing();

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
        if (openingDialog == null)
        {
            finishedCallback?.Invoke();
            return;
        }

        DialogAwalEntry[] entries = CreateDialogRange(startNumber, endNumber);

        if (entries.Length == 0)
        {
            finishedCallback?.Invoke();
            return;
        }

        openingDialog.PlayDialog(entries, mode, finishedCallback);
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
        if (openingDialog == null || DialogEntriesField == null)
            return null;

        return DialogEntriesField.GetValue(openingDialog)
            as DialogAwalEntry[];
    }

    private void ResolveReferences()
    {
        if (openingDialog == null)
            openingDialog = FindFirstObjectByType<dialogAwalScene>();

        if (progressController == null)
            progressController = FindFirstObjectByType<Level1ProgressController>();

        if (questController == null)
            questController = FindFirstObjectByType<Level4QuestController>();

        if (kilasInfo == null)
            kilasInfo = FindFirstObjectByType<KilasInfo>(FindObjectsInactive.Include);

        if (companionController == null)
            companionController = FindFirstObjectByType<SimpleBitFollower>();

        if (bossHealth == null)
            bossHealth = FindFirstObjectByType<BossHealt>(FindObjectsInactive.Include);

        if (levelPortal == null)
            levelPortal = FindFirstObjectByType<PortalLevel1>(
                FindObjectsInactive.Include
            );

        if (gameOver == null)
            gameOver = FindFirstObjectByType<GameOver>(FindObjectsInactive.Include);

        if (bossAttackControls == null || bossAttackControls.Length == 0)
        {
            bossAttackControls = FindObjectsByType<BossAttackControl>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );
        }
    }

    private void StartBossAttacks()
    {
        if (bossAttackControls == null)
            return;

        foreach (BossAttackControl bossAttackControl in bossAttackControls)
        {
            if (bossAttackControl == null ||
                bossAttackControl.gameObject.scene != gameObject.scene)
                continue;

            bossAttackControl.BeginAttacking();
        }
    }

    private void StopBossAttacks()
    {
        if (bossAttackControls == null)
            return;

        foreach (BossAttackControl bossAttackControl in bossAttackControls)
        {
            if (bossAttackControl == null ||
                bossAttackControl.gameObject.scene != gameObject.scene)
                continue;

            bossAttackControl.StopAttacking();
        }
    }

    private void DisableLegacyIntroControllers()
    {
        Level1IntroSequenceController[] controllers =
            FindObjectsByType<Level1IntroSequenceController>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        foreach (Level1IntroSequenceController controller in controllers)
        {
            if (controller != null && controller.gameObject.scene == gameObject.scene)
                controller.enabled = false;
        }
    }

    private void DisableQuestItemManagers()
    {
        QuestItemManager[] managers =
            FindObjectsByType<QuestItemManager>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        foreach (QuestItemManager manager in managers)
        {
            if (manager != null &&
                manager.gameObject.scene == gameObject.scene &&
                HasMissingQuestItems(manager))
            {
                manager.enabled = false;
            }
        }
    }

    private void DisablePortalsDuringStartup()
    {
        PortalLevel1[] portals =
            FindObjectsByType<PortalLevel1>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        foreach (PortalLevel1 portal in portals)
        {
            if (portal != null &&
                portal.gameObject.scene == gameObject.scene &&
                GetAssignedPortalObject(portal) == null)
            {
                portal.enabled = false;
            }
        }
    }

    private static bool HasMissingQuestItems(QuestItemManager manager)
    {
        if (QuestItemManagerItemsField == null)
            return true;

        GameObject[] items =
            QuestItemManagerItemsField.GetValue(manager) as GameObject[];

        if (items == null)
            return true;

        foreach (GameObject item in items)
        {
            if (item == null)
                return true;
        }

        return false;
    }

    private static GameObject GetAssignedPortalObject(PortalLevel1 portal)
    {
        return PortalObjectField?.GetValue(portal) as GameObject;
    }

    private static void DisableOldCutsceneCameras()
    {
        GameObject oldCameraRoot = GameObject.Find("Bit Cutscene Camera");

        if (oldCameraRoot != null)
            oldCameraRoot.SetActive(false);
    }
}
