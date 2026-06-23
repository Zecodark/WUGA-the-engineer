using UnityEngine;

public class Level1IntroSequenceController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private dialogAwalScene openingDialog;
    [SerializeField] private Level1ProgressController progressController;
    [SerializeField] private SimpleBitFollower companionController;

    [Header("Start")]
    [SerializeField] private bool startOnSceneLoad = true;

    private bool sequenceStarted;

    private void Awake()
    {
        DisableOldCutsceneCameras();
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
        if (progressController != null)
            progressController.StartLevelProgress();
        else if (companionController != null)
            companionController.StartFollowing();
    }

    private void ResolveReferences()
    {
        if (companionController == null)
            companionController =
                FindFirstObjectByType<SimpleBitFollower>();

        if (progressController == null)
            progressController =
                FindFirstObjectByType<Level1ProgressController>();
    }

    private static void DisableOldCutsceneCameras()
    {
        GameObject oldCameraRoot =
            GameObject.Find("Bit Cutscene Camera");

        if (oldCameraRoot != null)
            oldCameraRoot.SetActive(false);
    }
}
