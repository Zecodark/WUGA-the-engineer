using UnityEngine;

public class RobotNPC : MonoBehaviour
{
    [SerializeField] private float hoverSpeed = 2f;
    [SerializeField] private float interactRange = 3f;
    [SerializeField] private QuestData[] availableQuests;
    [SerializeField] private InteractionPromptUI promptUI;
    private int currentQuestIndex = 0;
    private Vector3 startPosition;

    void Start()
    {
        startPosition = transform.position;
    }

    void Update()
    {

        // Hover
        float newY = startPosition.y + Mathf.Sin(Time.time * hoverSpeed) * 0.1f;
        transform.position = new Vector3(startPosition.x, newY, startPosition.z);

        //Proximity
        bool playerInRange = CheckPlayerProximity();
        
        // Show UI Prompt
        if (promptUI != null)
        {
            if(playerInRange)
                promptUI.Show("Press E to talk");
            else
                promptUI.Hide();
        }
        //Interact
        if (playerInRange && Input.GetKeyDown(KeyCode.E))
            OnInteract();
    }

    bool CheckPlayerProximity()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position,
        interactRange, LayerMask.GetMask("Player"));
        return hits.Length > 0;
    }

    void OnInteract()
    {
        if (currentQuestIndex >= availableQuests.Length)
        {
            Debug.Log("NPC: Tidak ada Quest lagi.");
            return;
        }

        if (QuestManager.Instance.IsQuestActive())
        {
            Debug.Log("NPC: Selesaikan quest dulu!");
            return;
        }

        QuestData quest = availableQuests[currentQuestIndex];
        QuestManager.Instance.AcceptQuest(quest);
        currentQuestIndex++;
        Debug.Log("NPC: Quest accepted - " + quest.questName);
    }
}
