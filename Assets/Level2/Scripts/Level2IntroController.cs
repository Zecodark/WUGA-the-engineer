using UnityEngine;

public class Level2IntroController : MonoBehaviour
{
    [SerializeField] private dialogAwalScene openingDialog;
    [SerializeField] private KilasInfo kilasInfo;
    [SerializeField] private Level2QuestController progressController;
    [SerializeField] private SimpleBitFollower companionController;
    [SerializeField] private bool startOnSceneLoad = true;
    private bool started;

    private void Awake()
    {
        openingDialog ??= FindFirstObjectByType<dialogAwalScene>();
        kilasInfo ??= FindFirstObjectByType<KilasInfo>(FindObjectsInactive.Include);
        progressController ??= FindFirstObjectByType<Level2QuestController>();
        companionController ??= FindFirstObjectByType<SimpleBitFollower>();
    }

    private void Start()
    {
        if (startOnSceneLoad)
            StartIntroSequence();
    }

    public void StartIntroSequence()
    {
        if (started)
            return;
        started = true;
        companionController?.StopFollowing();

        if (openingDialog != null)
            openingDialog.BeginDialog(ShowKilasInfo);
        else
            ShowKilasInfo();
    }

    private void ShowKilasInfo()
    {
        if (kilasInfo != null)
        {
            kilasInfo.Show(StartQuest);
            return;
        }

        StartQuest();
    }

    private void StartQuest()
    {
        if (progressController != null)
            progressController.StartLevelQuest();
        else
            companionController?.StartFollowing();
    }
}
