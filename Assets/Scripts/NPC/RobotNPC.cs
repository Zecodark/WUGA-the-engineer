using UnityEngine;
using System.Collections;

public class RobotNPC : MonoBehaviour
{
    [SerializeField] private DialogueData firstMeetingDialogue;
    [SerializeField] private DialogueData questInProgressDialogue;
    [SerializeField] private DialogueData questCompletedDialogue;
    [SerializeField] private float hoverSpeed = 2f;
    [SerializeField] private float interactRange = 3f;
    [SerializeField] private InteractionPromptUI promptUI;
    [SerializeField] private Transform player;
    [SerializeField] private float rotationSpeed = 5f;
    
    [Header("Follow Settings")]
    [SerializeField] private float followDistance = 3f;
    [SerializeField] private float followSpeed = 5f;
    [SerializeField] private float hoverHeight = 1.5f;

    private DialogueState currentState = DialogueState.FirstMeeting;
    private Vector3 startPosition;
    private bool isStartingDialogue;
    private bool isFollowing = false;

    void Start()
    {
        startPosition = transform.position;

        // Subcribe ke quest Event
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestAccepted += OnQuestAccepted;
            QuestManager.Instance.OnQuestCompleted += OnQuestCompleted;
        }
    }

    void Update()
    {
        // Face Player
        if (player != null)
            FacePlayer();

        // Follow Player atau Hover di posisi
        if (isFollowing && player != null)
            FollowPlayer();
        else
            Hover();

        //Proximity
        bool playerInRange = CheckPlayerProximity();

        bool cutsceneActive =
            BitCutSceneDirector.Instance != null &&
            BitCutSceneDirector.Instance.IsCutsceneActive;
        
        // Show UI Prompt
        if (promptUI != null)
        {
            if(playerInRange && !cutsceneActive && !isStartingDialogue)
                promptUI.Show("Press E to talk");
            else
                promptUI.Hide();
        }

        if (!Input.GetKeyDown(KeyCode.E))
            return;

        if(BitCutSceneDirector.Instance != null &&
        BitCutSceneDirector.Instance.IsSequenceRunning)
        {
            return;
        }

        if (cutsceneActive)
        {
            if (DialogueSystem.Instance != null &&
            DialogueSystem.Instance.IsDialogueActive())
            {
                DialogueSystem.Instance.NextLine();
            }
            return; 
        }

        //Interact
        if (playerInRange && !isStartingDialogue)
            OnInteract();
    }

    void FacePlayer()
    {
        Vector3 direction = player.position - transform.position;
        direction.y = 0;
        if (direction.magnitude < 0.1f) return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation,
        targetRotation, rotationSpeed * Time.deltaTime);
    }

    void FollowPlayer()
    {
        // Hitung posisi target (di belakang player)
        Vector3 targetPosition = player.position - player.forward * followDistance;
        targetPosition.y = player.position.y + hoverHeight;

        // Smooth follow
        transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            followSpeed * Time.deltaTime
        );
    }
    
    void Hover()
    {
        // Hover di posisi saat ini
        float newY = startPosition.y + Mathf.Sin(Time.time * hoverSpeed) * 0.1f;
        transform.position = new Vector3(startPosition.x, newY, startPosition.z);
    }

    bool CheckPlayerProximity()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position,
        interactRange, LayerMask.GetMask("Player"));
        
        return hits.Length > 0;
    }

    void OnInteract()
    {
        
        if (DialogueSystem.Instance != null && DialogueSystem.Instance.IsDialogueActive())
        {
            DialogueSystem.Instance.NextLine();
            return;
        }

        DialogueData dialogueToUse = GetCurrentDialogue();
        Debug.Log("[RobotNPC] OnInteract | currentState: " + currentState + " | dialogueToUse: " + (dialogueToUse != null ? dialogueToUse.name : "null"));

        if (dialogueToUse == null)
        {
            Debug.LogWarning("[RobotNPC] Dialogue belum dipasang.");
            return;
            
        }

        StartCoroutine(StartDialogueWithTransition(dialogueToUse));
    
    }

    IEnumerator StartDialogueWithTransition(DialogueData dialogue)
    {
        isStartingDialogue = true;

        if (promptUI != null)
            promptUI.Hide();

        // Lock player dan aktifkan kamera percakapan.
        if (BitCutSceneDirector.Instance != null)
            BitCutSceneDirector.Instance.BeginCutscene();

        // Layar menjadi hitam.
        if (CameraManager.Instance != null)
            CameraManager.Instance.FadeOut();

        yield return new WaitForSeconds(0.5f);

        // Buka layar dari hitam.
        if (CameraManager.Instance != null)
            CameraManager.Instance.FadeIn();

        yield return new WaitForSeconds(0.3f);

        if (DialogueSystem.Instance != null)
            DialogueSystem.Instance.StartDialogue(dialogue, transform.position);

        isStartingDialogue = false;
    }
    

    DialogueData GetCurrentDialogue()
    {
        switch (currentState)
        {
            case DialogueState.FirstMeeting:
                return firstMeetingDialogue;
            case DialogueState.QuestInProgress:
                return questInProgressDialogue;
            case DialogueState.QuestCompleted:
                return questCompletedDialogue;
            default:
                return firstMeetingDialogue;
        }
    }


    void OnQuestAccepted(QuestData quest)
    {
        SetState(DialogueState.QuestInProgress);
        isFollowing = true;  // Mulai follow player
        Debug.Log("[RobotNPC] State changed to: QuestInProgress, Following player");
    }

    void OnQuestCompleted(QuestData quest)
    {
        SetState(DialogueState.QuestCompleted);
        isFollowing = false;  // Stop follow
        Debug.Log("[RobotNPC] State changed to: QuestCompleted, Stop following");
    }


    public void SetState(DialogueState newState)
    {
        currentState = newState;
    }

    public void StartFollowing()
    {
        isFollowing = true;
    }

    public void StopFollowing()
    {
        isFollowing = false;
    }

    void OnDestroy()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestAccepted -= OnQuestAccepted;
            QuestManager.Instance.OnQuestCompleted -= OnQuestCompleted;
        }
    }

}
