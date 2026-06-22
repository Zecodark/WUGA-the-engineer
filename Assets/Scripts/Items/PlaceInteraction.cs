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
    private bool isPlacing;

    void Start()
    {
        carrySystem = FindFirstObjectByType<CarrySystem>();
    }

    void Update()
    {
        if (carrySystem == null || !carrySystem.IsCarrying() || isPlacing)
        {
            if(promptUI != null) promptUI.Hide();
            return;
        }

        bool playerInRange = CheckPlayerProximity();

        if(promptUI != null)
        {
            if(playerInRange)
                promptUI.Show("Press E or G to Place");
            else
                promptUI.Hide();
        }

        if (playerInRange && (Input.GetKeyDown(KeyCode.G) || Input.GetKeyDown(KeyCode.E)))
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
        if (isPlacing || TableSocket == null)
            return;

        GameObject currentItem = carrySystem.GetCurrentItem();
        if (currentItem == null) return;

        GrabInteraction currentGrab = currentItem.GetComponent<GrabInteraction>();
        ItemData currentItemData =
            currentGrab != null ? currentGrab.GetItemData() : null;

        if (currentItemData == null)
        {
            Debug.LogWarning(
                "[PlaceInteraction] Item yang dibawa tidak memiliki ItemData.",
                currentItem
            );
            return;
        }

        if (currentGrab != null &&
            Level2ProgressController.Instance != null &&
            !Level2ProgressController.Instance.CanInteractWith(
                currentItemData
            ))
        {
            return;
        }

        int socketIndex =
            TableSocket.GetSocketIndexForItem(currentItemData);

        if (socketIndex == -1)
        {
            Debug.Log(
                $"Tidak ada socket meja untuk {currentItemData.itemName}.",
                this
            );
            return;
        }

        isPlacing = true;
        StartCoroutine(SmoothPlace(currentItem, socketIndex));
    }
    
    IEnumerator SmoothPlace(GameObject item, int socketIndex)
    {
        Transform socket = TableSocket.GetSocket(socketIndex);

        if (socket == null)
        {
            isPlacing = false;
            yield break;
        }

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

            grab.MarkPlaced();

            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.UpdateObjective(
                    ObjectiveType.Grab,
                    placedItem.itemId,
                    1
                );
            }

            OnItemPlaced?.Invoke(placedItem);
        }

        isPlacing = false;
        Debug.Log($"Placed {item.name} on white table.");

    }    

}
