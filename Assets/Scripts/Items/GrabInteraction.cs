using System;
using UnityEngine;

public class GrabInteraction : MonoBehaviour
{
    public static event Action<ItemData> OnItemGrabbed;

    [SerializeField] private ItemData itemData;
    [SerializeField] private float interactionDistance = 3.5f;
    [SerializeField] private GameObject interactionPrompt;

    [Header("Carry Offsets")]
    [SerializeField] private Vector3 carryPositionOffset = Vector3.zero;
    [SerializeField] private Vector3 carryRotationOffset = Vector3.zero;

    private bool isGrabbed;
    private bool isPlaced;

    public ItemData GetItemData() => itemData;

    private void Update()
    {
        if (isGrabbed || isPlaced) return;

        if (DialogueSystem.Instance != null && DialogueSystem.Instance.IsDialogueActiveOrJustEnded())
            return;

        CarrySystem carrySystem = FindFirstObjectByType<CarrySystem>();
        if (carrySystem == null || carrySystem.IsCarrying())
        {
            if (interactionPrompt != null) interactionPrompt.SetActive(false);
            return;
        }

        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player == null || player.IsInputLocked)
        {
            if (interactionPrompt != null) interactionPrompt.SetActive(false);
            return;
        }

        bool playerInRange = CheckPlayerProximity();

        if (interactionPrompt != null)
            interactionPrompt.SetActive(playerInRange && CanBeGrabbed());

        if (playerInRange && CanBeGrabbed() && Input.GetKeyDown(KeyCode.G))
        {
            Grab(carrySystem);
        }
    }

    bool CheckPlayerProximity()
    {
        Collider[] colliders = Physics.OverlapSphere(
            transform.position,
            interactionDistance,
            LayerMask.GetMask("Player")
        );
        return colliders.Length > 0;
    }

    private void Grab(CarrySystem carrySystem)
    {
        if (!carrySystem.CarryItem(gameObject))
            return;

        // Terapkan offset khusus item ini agar posisinya pas di tangan (tidak terbalik / mengambang)
        transform.localPosition = carryPositionOffset;
        transform.localEulerAngles = carryRotationOffset;

        isGrabbed = true;

        if (interactionPrompt != null)
            interactionPrompt.SetActive(false);

        if (itemData != null && DialogueSystem.Instance != null)
        {
            DialogueData data = ScriptableObject.CreateInstance<DialogueData>();
            data.speakerName = "Sistem";
            
            string description = string.IsNullOrWhiteSpace(itemData.description) 
                ? "Bisa ditempatkan di meja." 
                : itemData.description;
                
            data.lines = new string[] { 
                $"Kamu mengambil {itemData.itemName}.", 
                description 
            };
            
            PlayerController player = FindFirstObjectByType<PlayerController>();
            Vector3 pos = player != null ? player.transform.position : transform.position;
            DialogueSystem.Instance.StartDialogue(data, pos);
        }

        OnItemGrabbed?.Invoke(itemData);
    }

    private bool CanBeGrabbed()
    {
        if (isGrabbed || isPlaced || itemData == null)
            return false;

        Level2ProgressController quest = Level2ProgressController.Instance;
        return quest == null || quest.CanInteractWith(itemData);
    }

    public void MarkPlaced()
    {
        isPlaced = true;
        isGrabbed = false;

        if (interactionPrompt != null)
            interactionPrompt.SetActive(false);

        enabled = false;
    }
}
