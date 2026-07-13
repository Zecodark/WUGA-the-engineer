using UnityEngine;

[DisallowMultipleComponent]
public class Level4QuestSocket : MonoBehaviour
{
    [Header("Quest")]
    [SerializeField] private string requiredItemId;
    [SerializeField] private string[] acceptedAliases;

    [Header("Placement")]
    [SerializeField] private Transform snapPoint;
    [SerializeField] private KeyCode placeKey = KeyCode.G;
    [SerializeField, Min(0.1f)] private float placeRange = 3f;
    [SerializeField] private bool placeWhenItemEntersTrigger = true;
    [SerializeField] private bool requirePlaceKeyForTriggerPlacement = true;
    [SerializeField] private bool requireSocketTouchForManualPlace = true;
    [SerializeField] private bool allowManualPlaceKey = true;
    [SerializeField, Min(0f)] private float socketTouchPadding = 0.35f;

    private Level4QuestController controller;
    private CarrySystem carrySystem;
    private Level4QuestItem pendingTriggerItem;
    private bool occupied;

    public string RequiredItemId => requiredItemId;
    public bool IsOccupied => occupied;

    private void Awake()
    {
        if (string.IsNullOrWhiteSpace(requiredItemId))
            requiredItemId = name;

        if (snapPoint == null)
            snapPoint = transform;

        controller = Level4QuestController.Instance;
        EnsureTriggerCollider();
    }

    private void Update()
    {
        TryPlacePendingTriggerItem();

        Level4QuestController questController = GetController();

        if (occupied || questController == null)
            return;

        if (!questController.IsStarted)
            EnsureQuestStarted();

        ResolveCarrySystem();

        if (carrySystem == null || !carrySystem.IsCarryingItem())
            return;

        Level4QuestItem item =
            GetQuestItemFromCarriedObject(carrySystem.GetCurrentItem());

        if (item == null || !Accepts(item))
        {
            return;
        }

        if (CanPlaceCarriedItem(item))
            PlaceItem(item, true);
    }

    private void OnTriggerEnter(Collider other)
    {
        TrackTriggerCandidate(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TrackTriggerCandidate(other);
    }

    private void OnTriggerExit(Collider other)
    {
        Level4QuestItem item =
            other.GetComponentInParent<Level4QuestItem>();

        if (item != null && item == pendingTriggerItem)
            pendingTriggerItem = null;
    }

    private void TrackTriggerCandidate(Collider other)
    {
        if (!placeWhenItemEntersTrigger ||
            occupied ||
            GetController() == null)
        {
            return;
        }

        Level4QuestItem item =
            other.GetComponentInParent<Level4QuestItem>();

        if (item == null ||
            item.IsPlaced ||
            !Accepts(item))
        {
            return;
        }

        pendingTriggerItem = item;
    }

    public void SetController(Level4QuestController questController)
    {
        controller = questController;
    }

    public bool Accepts(Level4QuestItem item)
    {
        if (item == null)
            return false;

        if (IdMatches(item.ItemId) || IdMatches(item.name))
            return true;

        return false;
    }

    public bool TryPlaceCarriedItem(
        Level4QuestItem item,
        CarrySystem sourceCarrySystem)
    {
        Level4QuestController questController = GetController();

        if (occupied ||
            questController == null ||
            !questController.IsStarted ||
            item == null ||
            item.IsPlaced ||
            !Accepts(item) ||
            !questController.CanInteractWith(item))
        {
            return false;
        }

        if (!IsPlayerNearPlacementArea())
            return false;

        if (requireSocketTouchForManualPlace && !IsItemTouchingSocket(item))
            return false;

        if (sourceCarrySystem != null)
            carrySystem = sourceCarrySystem;

        PlaceItem(item, true);
        return item.IsPlaced;
    }

    public bool TryPlaceCarriedItemFromTable(
        Level4QuestItem item,
        CarrySystem sourceCarrySystem)
    {
        Level4QuestController questController = GetController();

        if (occupied ||
            questController == null ||
            !questController.IsStarted ||
            item == null ||
            item.IsPlaced ||
            !Accepts(item) ||
            !questController.CanInteractWith(item))
        {
            return false;
        }

        if (sourceCarrySystem != null)
            carrySystem = sourceCarrySystem;

        PlaceItem(item, true);
        return item.IsPlaced;
    }

    private bool CanPlaceCarriedItem(Level4QuestItem item)
    {
        if (!allowManualPlaceKey || !Input.GetKeyDown(placeKey))
            return false;

        if (!IsPlayerNearPlacementArea() && !IsItemTouchingSocket(item))
            return false;

        return true;
    }

    private bool CanAutoPlaceCarriedItem(Level4QuestItem item)
    {
        return placeWhenItemEntersTrigger &&
               item != null &&
               Accepts(item) &&
               (IsItemTouchingSocket(item) || IsPlayerNearPlacementArea());
    }

    private void TryPlacePendingTriggerItem()
    {
        if (!requirePlaceKeyForTriggerPlacement ||
            pendingTriggerItem == null ||
            pendingTriggerItem.IsPlaced ||
            !Input.GetKeyDown(placeKey))
        {
            return;
        }

        if (!Accepts(pendingTriggerItem))
        {
            pendingTriggerItem = null;
            return;
        }

        if (IsAnotherQuestItemBeingCarried(pendingTriggerItem))
            return;

        EnsureQuestStarted();
        PlaceItem(pendingTriggerItem, false);
        pendingTriggerItem = null;
    }

    private bool IsAnotherQuestItemBeingCarried(Level4QuestItem item)
    {
        ResolveCarrySystem();

        if (carrySystem == null || !carrySystem.IsCarryingItem())
            return false;

        Level4QuestItem carriedItem =
            GetQuestItemFromCarriedObject(carrySystem.GetCurrentItem());

        return carriedItem != null && carriedItem != item;
    }

    private void EnsureQuestStarted()
    {
        Level4QuestController questController = GetController();

        if (questController != null && !questController.IsStarted)
            questController.StartLevelQuest();
    }

    private void PlaceItem(Level4QuestItem item, bool fromCarry)
    {
        if (item == null ||
            occupied ||
            GetController() == null ||
            !GetController().CanInteractWith(item))
        {
            return;
        }

        ReleaseFromCarrySystemIfNeeded(item);

        occupied = true;
        item.MarkPlaced(snapPoint);

        if (!GetController().TryCompleteItem(item, this))
            occupied = false;
    }

    private void ReleaseFromCarrySystemIfNeeded(Level4QuestItem item)
    {
        ResolveCarrySystem();

        if (carrySystem == null ||
            !carrySystem.IsCarryingItem() ||
            !IsCurrentCarriedItem(item))
        {
            return;
        }

        carrySystem.DropItem();
    }

    private bool IsItemTouchingSocket(Level4QuestItem item)
    {
        if (item == null)
            return false;

        Bounds socketBounds = GetPlacementBounds();
        socketBounds.Expand(socketTouchPadding);

        if (TryGetItemBounds(item, out Bounds itemBounds))
        {
            return socketBounds.Intersects(itemBounds) ||
                   socketBounds.Contains(itemBounds.center);
        }

        Vector3 snapPosition =
            snapPoint != null ? snapPoint.position : transform.position;

        return Vector3.Distance(item.transform.position, snapPosition) <=
               Mathf.Max(0.25f, socketTouchPadding);
    }

    private Bounds GetPlacementBounds()
    {
        Collider[] colliders = GetComponents<Collider>();
        bool hasBounds = false;
        Bounds bounds = new Bounds(
            snapPoint != null ? snapPoint.position : transform.position,
            Vector3.one
        );

        foreach (Collider socketCollider in colliders)
        {
            if (socketCollider == null || !socketCollider.enabled)
                continue;

            if (!hasBounds)
            {
                bounds = socketCollider.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(socketCollider.bounds);
            }
        }

        return bounds;
    }

    private static bool TryGetItemBounds(
        Level4QuestItem item,
        out Bounds bounds)
    {
        bounds = new Bounds(item.transform.position, Vector3.one * 0.35f);
        bool hasBounds = false;

        Renderer[] renderers =
            item.GetComponentsInChildren<Renderer>(true);

        foreach (Renderer itemRenderer in renderers)
        {
            if (itemRenderer == null)
                continue;

            if (!hasBounds)
            {
                bounds = itemRenderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(itemRenderer.bounds);
            }
        }

        if (hasBounds)
            return true;

        Collider[] colliders =
            item.GetComponentsInChildren<Collider>(true);

        foreach (Collider itemCollider in colliders)
        {
            if (itemCollider == null || !itemCollider.enabled)
                continue;

            if (!hasBounds)
            {
                bounds = itemCollider.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(itemCollider.bounds);
            }
        }

        return hasBounds;
    }

    private bool IdMatches(string candidateId)
    {
        string required = Level4QuestController.NormalizeId(requiredItemId);
        string candidate = Level4QuestController.NormalizeId(candidateId);

        if (required == candidate)
            return true;

        if (acceptedAliases == null)
            return false;

        foreach (string alias in acceptedAliases)
        {
            if (Level4QuestController.NormalizeId(alias) == candidate)
                return true;
        }

        return false;
    }

    private bool IsPlayerNearPlacementArea()
    {
        Transform player = FindPlayerTransform();

        if (player == null)
            return false;

        Vector3 playerPosition = player.position;

        Transform tableRoot =
            transform.parent != null ? transform.parent : transform;

        if (IsNearBounds(tableRoot, playerPosition))
            return true;

        Vector3 snapPosition =
            snapPoint != null ? snapPoint.position : transform.position;

        return IsWithinHorizontalRange(tableRoot.position, playerPosition) ||
               IsWithinHorizontalRange(transform.position, playerPosition) ||
               IsWithinHorizontalRange(snapPosition, playerPosition);
    }

    private bool IsNearBounds(Transform root, Vector3 position)
    {
        if (root == null)
            return false;

        bool hasBounds = false;
        Bounds bounds = new Bounds(root.position, Vector3.one);

        foreach (Collider tableCollider in root.GetComponentsInChildren<Collider>(true))
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

        if (!hasBounds)
        {
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
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
        }

        if (!hasBounds)
            return false;

        return HorizontalDistanceToBounds(position, bounds) <= placeRange;
    }

    private bool IsWithinHorizontalRange(Vector3 origin, Vector3 position)
    {
        origin.y = 0f;
        position.y = 0f;
        return Vector3.Distance(origin, position) <= placeRange;
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

    private bool IsCurrentCarriedItem(Level4QuestItem item)
    {
        if (item == null || carrySystem == null)
            return false;

        return GetQuestItemFromCarriedObject(carrySystem.GetCurrentItem()) ==
               item;
    }

    public static Level4QuestItem GetQuestItemFromCarriedObject(
        GameObject carriedObject)
    {
        if (carriedObject == null)
            return null;

        Level4QuestItem item = carriedObject.GetComponent<Level4QuestItem>();

        if (item != null)
            return item;

        item = carriedObject.GetComponentInParent<Level4QuestItem>();

        if (item != null)
            return item;

        return carriedObject.GetComponentInChildren<Level4QuestItem>(true);
    }

    private Level4QuestController GetController()
    {
        if (controller == null)
            controller = Level4QuestController.Instance;

        return controller;
    }

    private void ResolveCarrySystem()
    {
        if (carrySystem == null)
            carrySystem = FindFirstObjectByType<CarrySystem>();
    }

    private void EnsureTriggerCollider()
    {
        Collider trigger = GetComponent<Collider>();

        if (trigger == null)
        {
            BoxCollider box = gameObject.AddComponent<BoxCollider>();
            box.size = WorldSizeToLocalColliderSize(Vector3.one, 0.8f, 2f);
            trigger = box;
        }

        trigger.isTrigger = true;
    }

    private Vector3 WorldSizeToLocalColliderSize(
        Vector3 worldSize,
        float minWorldSize,
        float maxWorldSize)
    {
        Vector3 clampedWorldSize = new Vector3(
            Mathf.Clamp(Mathf.Abs(worldSize.x), minWorldSize, maxWorldSize),
            Mathf.Clamp(Mathf.Abs(worldSize.y), minWorldSize, maxWorldSize),
            Mathf.Clamp(Mathf.Abs(worldSize.z), minWorldSize, maxWorldSize)
        );

        Vector3 scale = transform.lossyScale;

        return new Vector3(
            DivideByScale(clampedWorldSize.x, scale.x),
            DivideByScale(clampedWorldSize.y, scale.y),
            DivideByScale(clampedWorldSize.z, scale.z)
        );
    }

    private static float DivideByScale(float value, float scale)
    {
        float absoluteScale = Mathf.Abs(scale);
        return absoluteScale > 0.0001f ? value / absoluteScale : value;
    }
}
