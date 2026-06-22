using UnityEngine;
using System.Collections;
using System;
public class PlaceInteraction : MonoBehaviour
{
    public static event Action<ItemData> OnItemPlaced;

    [SerializeField] private TableSocket TableSocket;
    [SerializeField] private float placeRange = 3f;
    [SerializeField] private InteractionPromptUI promptUI;

    private CarrySystem carrySystem;

    void Start()
    {
        carrySystem = FindFirstObjectByType<CarrySystem>();
    }

    void Update()
    {
        if (carrySystem == null || !carrySystem.IsCarrying())
        {
            if(promptUI != null) promptUI.Hide();
            return;
        }

        bool playerInRange = CheckPlayerProximity();

        if(promptUI != null)
        {
            if(playerInRange)
            promptUI.Show("Press G to Place");
            else
            promptUI.Hide();
        }

        if (playerInRange && Input.GetKeyDown(KeyCode.G))
        PlaceItem();
    }

    bool CheckPlayerProximity()
    {
        Collider[] colliders = Physics.OverlapSphere(
            transform.position,
            placeRange,
            LayerMask.GetMask("Player")
        );
        return colliders.Length > 0;
    }

    void PlaceItem()
    {
        GameObject currentItem = carrySystem.GetCurrentItem();
        if (currentItem == null) return;

        GrabInteraction currentGrab = currentItem.GetComponent<GrabInteraction>();
        if (currentGrab != null &&
            Level2ProgressController.Instance != null &&
            !Level2ProgressController.Instance.CanInteractWith(
                currentGrab.GetItemData()
            ))
        {
            return;
        }

        int socketIndex = TableSocket.GetAvailableSocketIndex();
        if (socketIndex == -1)
        {
            Debug.Log("No Available sockets!");
            return;
        }

        // Mulai coroutine untuk smooth place
        StartCoroutine(SmoothPlace(currentItem, socketIndex));
    }
    
    IEnumerator SmoothPlace(GameObject item, int socketIndex)
    {
        Transform socket = TableSocket.GetSocket(socketIndex);
        float duration = 0.3f;
        float elapsed = 0f;

        Vector3 startPos = item.transform.position;
        Quaternion startRot = item.transform.rotation;

        // Detach dari carry position

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            item.transform.position = Vector3.Lerp(startPos, socket.position, t);
            item.transform.rotation = Quaternion.Lerp(startRot, socket.rotation, t);
            yield return null;
        }

        // Setelah smooth, snap ke socket
        carrySystem.DropItem();
        TableSocket.PlaceItem(item, socketIndex);

        GrabInteraction grab = item.GetComponent<GrabInteraction>();
        if (grab != null)
        {
            ItemData placedItem = grab.GetItemData();

            QuestManager.Instance.UpdateObjective(
                ObjectiveType.Grab,
                placedItem.itemId,
                1
            );
            OnItemPlaced?.Invoke(placedItem);
        }

        Debug.Log("Placed item on table");

    }    

}
