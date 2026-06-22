using System;
using System.Collections.Generic;
using UnityEngine;

public class GrabInteraction : MonoBehaviour
{
    public static event Action<ItemData> OnItemGrabbed;

    [SerializeField] private ItemData itemData;
    [SerializeField, Min(0.5f)] private float interactionDistance = 3.5f;
    [SerializeField] private GameObject interactionPrompt;

    private static int inputHandledFrame = -1;

    private bool isGrabbed;
    private PlayerController playerController;
    private CarrySystem carrySystem;

    private void Start()
    {
        ResolveReferences();
    }

    private void Update()
    {
        ResolveReferences();

        bool canGrabThis =
            !isGrabbed &&
            IsQuestAvailable() &&
            IsInsidePlayerInteractionSphere();

        if (interactionPrompt != null)
            interactionPrompt.SetActive(canGrabThis);

        if (!Input.GetKeyDown(KeyCode.G) ||
            inputHandledFrame == Time.frameCount)
        {
            return;
        }

        inputHandledFrame = Time.frameCount;

        if (playerController == null ||
            playerController.IsInputLocked)
        {
            Debug.Log("[GrabInteraction] G ditolak: player masih terkunci.");
            return;
        }

        if (carrySystem != null && carrySystem.IsCarrying())
        {
            Debug.Log("[GrabInteraction] Player sedang membawa item lain.");
            return;
        }

        GrabInteraction nearest = FindNearestItemAroundPlayer();

        if (nearest == null)
        {
            Debug.Log(
                "[GrabInteraction] Tidak ada collider komponen dalam radius " +
                $"{interactionDistance:0.0} meter dari player."
            );
            LogItemDistances();
            return;
        }

        nearest.StartGrab();
    }

    private GrabInteraction FindNearestItemAroundPlayer()
    {
        if (playerController == null)
            return null;

        Vector3 center = GetPlayerInteractionCenter();
        Collider[] nearbyColliders = Physics.OverlapSphere(
            center,
            interactionDistance,
            ~0,
            QueryTriggerInteraction.Collide
        );

        HashSet<GrabInteraction> candidates = new();

        foreach (Collider nearbyCollider in nearbyColliders)
        {
            GrabInteraction item =
                nearbyCollider.GetComponentInParent<GrabInteraction>();

            if (item != null &&
                !item.isGrabbed &&
                item.IsQuestAvailable())
            {
                candidates.Add(item);
            }
        }

        GrabInteraction nearest = null;
        float nearestDistance = float.PositiveInfinity;

        foreach (GrabInteraction candidate in candidates)
        {
            float distance = candidate.DistanceFrom(center);

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = candidate;
            }
        }

        return nearest;
    }

    private bool IsInsidePlayerInteractionSphere()
    {
        if (playerController == null)
            return false;

        return DistanceFrom(GetPlayerInteractionCenter()) <=
               interactionDistance;
    }

    private float DistanceFrom(Vector3 point)
    {
        Collider[] itemColliders = GetComponentsInChildren<Collider>(true);
        float nearestSqrDistance = float.PositiveInfinity;

        foreach (Collider itemCollider in itemColliders)
        {
            if (itemCollider == null || !itemCollider.enabled)
                continue;

            Vector3 closestPoint =
                itemCollider.bounds.ClosestPoint(point);
            float sqrDistance = (closestPoint - point).sqrMagnitude;
            nearestSqrDistance = Mathf.Min(
                nearestSqrDistance,
                sqrDistance
            );
        }

        if (!float.IsPositiveInfinity(nearestSqrDistance))
            return Mathf.Sqrt(nearestSqrDistance);

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);

        foreach (Renderer itemRenderer in renderers)
        {
            Vector3 closestPoint =
                itemRenderer.bounds.ClosestPoint(point);
            float sqrDistance = (closestPoint - point).sqrMagnitude;
            nearestSqrDistance = Mathf.Min(
                nearestSqrDistance,
                sqrDistance
            );
        }

        if (!float.IsPositiveInfinity(nearestSqrDistance))
            return Mathf.Sqrt(nearestSqrDistance);

        return Vector3.Distance(transform.position, point);
    }

    private Vector3 GetPlayerInteractionCenter()
    {
        CharacterController characterController =
            playerController.GetComponent<CharacterController>();

        if (characterController != null)
            return characterController.bounds.center;

        return playerController.transform.position + Vector3.up;
    }

    private bool IsQuestAvailable()
    {
        Level2ProgressController progress =
            Level2ProgressController.Instance;

        if (progress == null)
        {
            Debug.Log("[GrabInteraction] Level2ProgressController.Instance is NULL - allowing grab");
            return true;
        }

        bool canInteract = progress.CanInteractWith(itemData);
        Debug.Log($"[GrabInteraction] CanInteractWith({itemData?.name}): {canInteract}");
        return canInteract;
    }

    private void StartGrab()
    {
        ResolveReferences();

        if (carrySystem == null)
        {
            Debug.LogError(
                $"[GrabInteraction] CarrySystem tidak ditemukan untuk {name}.",
                this
            );
            return;
        }

        isGrabbed = true;

        if (interactionPrompt != null)
            interactionPrompt.SetActive(false);

        carrySystem.CarryItem(gameObject);
        OnItemGrabbed?.Invoke(itemData);
        Debug.Log(
            $"[GrabInteraction] GRABBED: {name} ({itemData?.itemId})",
            this
        );
    }

    private void ResolveReferences()
    {
        if (playerController == null)
            playerController = FindFirstObjectByType<PlayerController>();

        if (carrySystem == null)
            carrySystem = FindFirstObjectByType<CarrySystem>();
    }

    private void LogItemDistances()
    {
        Vector3 center = GetPlayerInteractionCenter();
        GrabInteraction[] items =
            FindObjectsByType<GrabInteraction>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None
            );

        foreach (GrabInteraction item in items)
        {
            Debug.Log(
                $"[GrabInteraction] Jarak {item.name}: " +
                $"{item.DistanceFrom(center):0.00} m | " +
                $"ID: {item.itemData?.itemId ?? "NULL"}"
            );
        }
    }

    public ItemData GetItemData()
    {
        return itemData;
    }
}
