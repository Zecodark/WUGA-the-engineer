using UnityEngine;
using System.Collections;
using UnityEngine.Video;
public class OpeningCutsceneController : MonoBehaviour
{
   [Header("References")]
   [SerializeField] private VideoPlayer videoPlayer;
   [SerializeField] private GameObject cutsceneCanvas;
   [SerializeField] private PlayerController playerController;
   [SerializeField] private AutoQuestStarter AutoQuestStarter;
   
   [Header("Settings")] 
   [SerializeField] private float finishDelay = 0.25f;

   private bool hasFinished;


    private void Start()
    {
        StartOpening();
    }

    public void StartOpening()
    {
        if (videoPlayer == null || cutsceneCanvas == null)
        {
            Debug.LogError("[OpeningCutsceneController] reference belum lengkap.");
            return;
        }

        hasFinished = false;

        if (playerController != null)
            playerController.LockInput();

        cutsceneCanvas.SetActive(true);

        videoPlayer.loopPointReached -= HandleVideoFinished;
        videoPlayer.loopPointReached += HandleVideoFinished;
        videoPlayer.Play();

    }


    private void HandleVideoFinished(VideoPlayer source)
    {
        if (!hasFinished)
            StartCoroutine(FinishRoutine());
    }

    private IEnumerator FinishRoutine()
    {
        hasFinished = true;

        yield return new WaitForSeconds(finishDelay);

        videoPlayer.Stop();
        cutsceneCanvas.SetActive(false);


        if (AutoQuestStarter != null)
            AutoQuestStarter.StartQuest();
            
        if (playerController != null)
            playerController.UnlockInput();

    }
        private void OnDestroy()
        {
            if (videoPlayer != null)
                videoPlayer.loopPointReached -= HandleVideoFinished;
        }


}
