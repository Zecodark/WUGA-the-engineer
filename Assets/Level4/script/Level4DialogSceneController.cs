using System.Reflection;
using UnityEngine;

[DefaultExecutionOrder(-1000)]
public class Level4DialogSceneController : MonoBehaviour
{
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

    [Header("After Dialog")]
    [SerializeField] private KilasInfo kilasInfo;
    [SerializeField] private Level4QuestController questController;
    [SerializeField, HideInInspector] private Level1ProgressController progressController;
    [SerializeField] private SimpleBitFollower companionController;
    [SerializeField] private bool startCompanionIfNoProgress = true;
    [SerializeField] private BossAttackControl[] bossAttackControls;

    [Header("Level 4 Runtime Cleanup")]
    [SerializeField] private bool disableLegacyIntroControllers = false;
    [SerializeField] private bool disableQuestItemManagers = true;
    [SerializeField] private bool disableUnassignedPortalsOnStart = true;

    private bool sequenceStarted;

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

    public void StartIntroSequence()
    {
        if (sequenceStarted)
            return;

        sequenceStarted = true;
        ResolveReferences();

        if (companionController != null)
            companionController.StopFollowing();

        StopBossAttacks();

        if (openingDialog != null)
            openingDialog.BeginDialog(HandleOpeningDialogFinished);
        else
            HandleOpeningDialogFinished();
    }

    private void HandleOpeningDialogFinished()
    {
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
