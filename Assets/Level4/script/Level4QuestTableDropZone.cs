using UnityEngine;

[DisallowMultipleComponent]
public class Level4QuestTableDropZone : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private KeyCode placeKey = KeyCode.G;

    [Header("Placement Area")]
    [SerializeField, Min(0f)] private float touchPadding = 1.25f;
    [SerializeField, Min(1f)] private float triggerHeight = 6f;
    [SerializeField, Min(0.5f)] private float fallbackPlaceRange = 8f;
    [SerializeField] private Level4QuestSocket[] sockets;

    private readonly System.Collections.Generic.HashSet<Collider> playerContacts =
        new System.Collections.Generic.HashSet<Collider>();

    private CarrySystem carrySystem;
    private BoxCollider tableTrigger;

    private void Awake()
    {
        RefreshSockets();
        EnsureTableTrigger();
    }

    private void Update()
    {
        if (!Input.GetKeyDown(placeKey))
            return;

        ResolveCarrySystem();

        if (carrySystem == null || !carrySystem.IsCarryingItem())
            return;

        if (!EnsureQuestRunning())
            return;

        if (!IsPlayerAtTable())
            return;

        Level4QuestItem item =
            Level4QuestSocket.GetQuestItemFromCarriedObject(
                carrySystem.GetCurrentItem()
            );

        if (item == null || item.IsPlaced)
            return;

        Level4QuestSocket socket = FindSocketForItem(item);

        if (socket == null)
        {
            Debug.Log(
                $"[Level4QuestTableDropZone] {item.DisplayName} bukan untuk {name}.",
                this
            );
            return;
        }

        if (socket.TryPlaceCarriedItemFromTable(item, carrySystem))
        {
            Debug.Log(
                $"[Level4QuestTableDropZone] {item.DisplayName} dipasang ke {socket.name}.",
                socket
            );
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        TrackPlayerContact(other, true);
    }

    private void OnTriggerStay(Collider other)
    {
        TrackPlayerContact(other, true);
    }

    private void OnTriggerExit(Collider other)
    {
        TrackPlayerContact(other, false);
    }

    private Level4QuestSocket FindSocketForItem(Level4QuestItem item)
    {
        if (item == null)
            return null;

        RefreshSockets();

        Level4QuestSocket nearestSocket = null;
        float nearestDistance = float.MaxValue;
        Vector3 referencePosition = GetPlacementReferencePosition(item);

        foreach (Level4QuestSocket socket in sockets)
        {
            if (socket == null ||
                socket.IsOccupied ||
                !socket.Accepts(item))
            {
                continue;
            }

            float distance = HorizontalDistance(
                socket.transform.position,
                referencePosition
            );

            if (distance < nearestDistance)
            {
                nearestSocket = socket;
                nearestDistance = distance;
            }
        }

        return nearestDistance <= fallbackPlaceRange
            ? nearestSocket
            : null;
    }

    private bool IsPlayerAtTable()
    {
        ClearMissingContacts();

        if (playerContacts.Count > 0)
            return true;

        if (IsPlayerColliderInsideTableTrigger())
            return true;

        Transform player = FindPlayerTransform();

        if (player == null)
            return false;

        if (!TryGetAreaBounds(out Bounds bounds))
        {
            return HorizontalDistance(
                player.position,
                transform.position
            ) <= touchPadding;
        }

        return HorizontalDistanceToBounds(player.position, bounds) <=
               touchPadding;
    }

    private bool IsPlayerColliderInsideTableTrigger()
    {
        Bounds areaBounds;

        if (tableTrigger != null && tableTrigger.enabled)
        {
            areaBounds = tableTrigger.bounds;
        }
        else if (!TryGetAreaBounds(out areaBounds))
        {
            return false;
        }

        Collider[] hits = Physics.OverlapBox(
            areaBounds.center,
            areaBounds.extents + Vector3.one * 0.05f,
            Quaternion.identity,
            ~0,
            QueryTriggerInteraction.Ignore
        );

        foreach (Collider hit in hits)
        {
            if (hit != null && IsPlayerCollider(hit))
                return true;
        }

        return false;
    }

    private Vector3 GetPlacementReferencePosition(Level4QuestItem item)
    {
        if (item != null)
            return item.transform.position;

        Transform player = FindPlayerTransform();

        if (player != null)
            return player.position;

        return transform.position;
    }

    private bool TryGetAreaBounds(out Bounds bounds)
    {
        bool hasBounds = false;
        bounds = new Bounds(transform.position, Vector3.zero);

        Collider[] tableColliders =
            GetComponentsInChildren<Collider>(true);

        foreach (Collider tableCollider in tableColliders)
        {
            if (tableCollider == null || !tableCollider.enabled)
                continue;

            if (!hasBounds)
            {
                bounds = tableCollider.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(tableCollider.bounds);
            }
        }

        if (hasBounds)
            return true;

        Renderer[] renderers =
            GetComponentsInChildren<Renderer>(true);

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
                continue;

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return hasBounds;
    }

    private void RefreshSockets()
    {
        if (sockets == null || sockets.Length == 0)
            sockets = GetComponentsInChildren<Level4QuestSocket>(true);
    }

    private void EnsureTableTrigger()
    {
        tableTrigger = GetExistingTableTrigger();

        if (tableTrigger == null)
            tableTrigger = gameObject.AddComponent<BoxCollider>();

        tableTrigger.isTrigger = true;

        if (!TryGetSolidTableBounds(out Bounds bounds))
            return;

        Vector3 triggerSize = bounds.size + new Vector3(
            touchPadding * 2f,
            0f,
            touchPadding * 2f
        );
        triggerSize.y = Mathf.Max(triggerHeight, bounds.size.y);

        tableTrigger.center = transform.InverseTransformPoint(bounds.center);
        tableTrigger.size = WorldSizeToLocalColliderSize(triggerSize);
    }

    private BoxCollider GetExistingTableTrigger()
    {
        foreach (BoxCollider box in GetComponents<BoxCollider>())
        {
            if (box != null && box.isTrigger)
                return box;
        }

        return null;
    }

    private bool TryGetSolidTableBounds(out Bounds bounds)
    {
        bool hasBounds = false;
        bounds = new Bounds(transform.position, Vector3.zero);

        foreach (Collider tableCollider in GetComponentsInChildren<Collider>(true))
        {
            if (tableCollider == null ||
                !tableCollider.enabled ||
                tableCollider == tableTrigger ||
                tableCollider.isTrigger)
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = tableCollider.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(tableCollider.bounds);
            }
        }

        if (hasBounds)
            return true;

        foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
        {
            if (renderer == null)
                continue;

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return hasBounds;
    }

    private void TrackPlayerContact(Collider other, bool isTouching)
    {
        if (other == null || !IsPlayerCollider(other))
            return;

        if (isTouching)
            playerContacts.Add(other);
        else
            playerContacts.Remove(other);
    }

    private static bool IsPlayerCollider(Collider other)
    {
        if (other.GetComponentInParent<PlayerController>() != null)
            return true;

        try
        {
            return other.CompareTag("Player") ||
                   other.GetComponentInParent<Transform>()
                        .CompareTag("Player");
        }
        catch (UnityException)
        {
            return false;
        }
    }

    private void ClearMissingContacts()
    {
        playerContacts.RemoveWhere(contact => contact == null);
    }

    private void ResolveCarrySystem()
    {
        if (carrySystem == null)
            carrySystem = FindFirstObjectByType<CarrySystem>();
    }

    private static bool EnsureQuestRunning()
    {
        if (Level4QuestController.Instance == null)
            return false;

        if (!Level4QuestController.Instance.IsStarted)
            Level4QuestController.Instance.StartLevelQuest();

        return Level4QuestController.Instance.IsStarted;
    }

    private static bool IsDialogueBlockingInput()
    {
        return DialogueSystem.Instance != null &&
               DialogueSystem.Instance.IsDialogueActiveOrJustEnded();
    }

    private static Transform FindPlayerTransform()
    {
        PlayerController player = FindFirstObjectByType<PlayerController>();

        if (player != null)
            return player.transform;

        try
        {
            GameObject taggedPlayer =
                GameObject.FindGameObjectWithTag("Player");
            return taggedPlayer != null ? taggedPlayer.transform : null;
        }
        catch (UnityException)
        {
            return null;
        }
    }

    private static float HorizontalDistance(
        Vector3 a,
        Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    private static float HorizontalDistanceToBounds(
        Vector3 position,
        Bounds bounds)
    {
        float dx = Mathf.Max(
            bounds.min.x - position.x,
            0f,
            position.x - bounds.max.x
        );

        float dz = Mathf.Max(
            bounds.min.z - position.z,
            0f,
            position.z - bounds.max.z
        );

        return Mathf.Sqrt(dx * dx + dz * dz);
    }

    private Vector3 WorldSizeToLocalColliderSize(Vector3 worldSize)
    {
        Vector3 scale = transform.lossyScale;

        return new Vector3(
            DivideByScale(worldSize.x, scale.x),
            DivideByScale(worldSize.y, scale.y),
            DivideByScale(worldSize.z, scale.z)
        );
    }

    private static float DivideByScale(float value, float scale)
    {
        float absoluteScale = Mathf.Abs(scale);
        return absoluteScale > 0.0001f ? value / absoluteScale : value;
    }
}
