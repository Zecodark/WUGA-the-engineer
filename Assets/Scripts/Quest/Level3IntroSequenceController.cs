using UnityEngine;

public class Level3IntroSequenceController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private dialogAwalScene openingDialog;
    [SerializeField] private CableQuestController cableQuestController;
    [SerializeField] private SimpleBitFollower companionController;

    [Header("Start")]
    [SerializeField] private bool startOnSceneLoad = true;

    private bool sequenceStarted;

    private void Awake()
    {
        ResolveReferences();
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

        if (openingDialog != null)
            openingDialog.BeginDialog(HandleOpeningDialogFinished);
        else
            HandleOpeningDialogFinished();
    }

    private void HandleOpeningDialogFinished()
    {
        if (companionController != null)
            companionController.StartFollowing();

        if (cableQuestController != null)
            cableQuestController.StartCableQuest();
    }

    private void ResolveReferences()
    {
        if (openingDialog == null)
            openingDialog = FindFirstObjectByType<dialogAwalScene>(FindObjectsInactive.Include);

        if (cableQuestController == null)
            cableQuestController = FindFirstObjectByType<CableQuestController>(FindObjectsInactive.Include);

        if (companionController == null)
            companionController = FindFirstObjectByType<SimpleBitFollower>(FindObjectsInactive.Include);
    }
}
