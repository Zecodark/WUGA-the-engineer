using UnityEngine;
using UnityEngine.InputSystem;

public class Level2QuestItemInteraction : MonoBehaviour
{
    private Level2QuestController controller;
    private CarrySystem carrySystem;
    private Transform placementSocket;
    private string itemId;
    private bool grabbed;
    private bool placed;

    public void Configure(
        Level2QuestController owner,
        string configuredItemId,
        Transform socket)
    {
        controller = owner;
        itemId = configuredItemId;
        placementSocket = socket;
        EnsureCollider();
    }

    private void Update()
    {
        if (placed || controller == null || !controller.IsQuestActive)
            return;

        carrySystem ??= FindFirstObjectByType<CarrySystem>();
        if (carrySystem == null || controller.IsDialogBlockingInteraction())
            return;

        if (!grabbed)
        {
            if (!carrySystem.IsCarrying() && IsPlayerNearItem() &&
                IsGrabPressed())
            {
                Grab();
            }
            return;
        }

        if (carrySystem.GetCurrentItem() == gameObject && placementSocket != null &&
            IsPlayerNear(placementSocket.position) && IsGrabPressed())
        {
            Place();
        }
    }

    private void Grab()
    {
        if (!carrySystem.CarryItem(gameObject))
        {
            Debug.LogWarning($"[Level2Quest] G ditekan pada {itemId}, tetapi CarrySystem menolak item.", this);
            return;
        }

        grabbed = true;
        Debug.Log($"[Level2Quest] Item berhasil di-grab: {itemId}", this);
        controller.NotifyItemGrabbed(itemId);
    }

    private void Place()
    {
        carrySystem.DropItem();
        transform.SetParent(placementSocket, false);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        Rigidbody body = GetComponent<Rigidbody>();
        if (body != null)
        {
            body.isKinematic = true;
            body.useGravity = false;
        }

        placed = true;
        controller.NotifyItemPlaced(itemId);
    }

    private bool IsPlayerNear(Vector3 target)
    {
        PlayerController player = FindFirstObjectByType<PlayerController>();
        return player != null &&
               Vector3.Distance(player.transform.position, target) <=
               controller.InteractionDistance;
    }

    private bool IsPlayerNearItem()
    {
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player == null)
            return false;

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return IsPlayerNear(transform.position);

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        Vector3 closestPoint = bounds.ClosestPoint(player.transform.position);
        return Vector3.Distance(player.transform.position, closestPoint) <=
               controller.InteractionDistance;
    }

    private static bool IsGrabPressed()
    {
        bool legacyPressed = Input.GetKeyDown(KeyCode.G);
        bool inputSystemPressed =
            Keyboard.current != null &&
            Keyboard.current.gKey.wasPressedThisFrame;
        return legacyPressed || inputSystemPressed;
    }

    private void EnsureCollider()
    {
        if (GetComponentInChildren<Collider>(true) != null)
            return;

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return;

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        BoxCollider box = gameObject.AddComponent<BoxCollider>();
        box.center = transform.InverseTransformPoint(bounds.center);
        Vector3 localMax = transform.InverseTransformVector(bounds.size);
        box.size = new Vector3(
            Mathf.Abs(localMax.x),
            Mathf.Abs(localMax.y),
            Mathf.Abs(localMax.z));
    }
}
