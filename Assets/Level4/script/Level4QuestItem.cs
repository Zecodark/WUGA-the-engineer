using UnityEngine;

[DisallowMultipleComponent]
public class Level4QuestItem : MonoBehaviour
{
    [Header("Quest")]
    [SerializeField] private string itemId;
    [SerializeField] private string displayName;

    [Header("Interaction")]
    [SerializeField] private KeyCode interactKey = KeyCode.G;
    [SerializeField, Min(0.1f)] private float interactionDistance = 3.5f;
    [SerializeField] private GameObject interactionPrompt;

    [Header("Carry Offsets")]
    [SerializeField] private Vector3 carryPositionOffset = Vector3.zero;
    [SerializeField] private Vector3 carryRotationOffset = Vector3.zero;

    private Level4QuestController controller;
    private CarrySystem carrySystem;
    private bool hasBeenPickedUp;
    private bool isPlaced;

    public string ItemId => itemId;
    public string DisplayName =>
        string.IsNullOrWhiteSpace(displayName) ? itemId : displayName;
    public bool IsPlaced => isPlaced;
    public bool HasBeenPickedUp => hasBeenPickedUp;
    public bool IsCarried =>
        carrySystem != null && carrySystem.GetCurrentItem() == gameObject;

    private void Awake()
    {
        if (string.IsNullOrWhiteSpace(itemId))
            itemId = name;

        if (string.IsNullOrWhiteSpace(displayName))
            displayName = name;

        controller = Level4QuestController.Instance;
        EnsurePhysicsSetup();
    }

    private void Update()
    {
        if (isPlaced)
        {
            SetPromptVisible(false);
            return;
        }

        ResolveCarrySystem();

        if (carrySystem == null ||
            carrySystem.IsCarrying() ||
            IsWeaponModeActive() ||
            IsDialogueBlockingInput() ||
            IsPlayerInputLocked())
        {
            SetPromptVisible(false);
            return;
        }

        Level4QuestController questController = GetController();
        bool canInteract =
            questController != null &&
            IsPlayerInRange() &&
            questController.CanInteractWith(this);

        SetPromptVisible(canInteract);

        if (canInteract && Input.GetKeyDown(interactKey))
            TryPickUp();
    }

    public void SetController(Level4QuestController questController)
    {
        controller = questController;
    }

    public void MarkPlaced(Transform socket)
    {
        isPlaced = true;
        SetPromptVisible(false);
        ReleaseFromCarrySystemIfHeld();
        LockToSocket(socket);
        enabled = false;
    }

    public bool MatchesId(string candidateId)
    {
        return Level4QuestController.NormalizeId(itemId) ==
               Level4QuestController.NormalizeId(candidateId);
    }

    private void TryPickUp()
    {
        ResolveCarrySystem();

        Level4QuestController questController = GetController();

        if (carrySystem == null ||
            questController == null ||
            IsWeaponModeActive() ||
            !questController.CanInteractWith(this) ||
            !carrySystem.CarryItem(gameObject))
        {
            return;
        }

        transform.localPosition = carryPositionOffset;
        transform.localEulerAngles = carryRotationOffset;
        hasBeenPickedUp = true;
        SetPromptVisible(false);
    }

    private void LockToSocket(Transform socket)
    {
        if (socket != null)
        {
            transform.SetParent(socket, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }

        Rigidbody[] bodies = GetComponentsInChildren<Rigidbody>(true);
        foreach (Rigidbody body in bodies)
        {
            if (body == null)
                continue;

            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.useGravity = false;
            body.isKinematic = true;
        }

        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        foreach (Collider itemCollider in colliders)
        {
            if (itemCollider != null)
                itemCollider.enabled = true;
        }
    }

    private void ReleaseFromCarrySystemIfHeld()
    {
        ResolveCarrySystem();

        if (carrySystem == null || !carrySystem.IsCarryingItem())
            return;

        GameObject currentItem = carrySystem.GetCurrentItem();

        if (currentItem == null)
            return;

        bool isThisItemHeld =
            currentItem == gameObject ||
            transform.IsChildOf(currentItem.transform) ||
            currentItem.transform.IsChildOf(transform);

        if (isThisItemHeld)
            carrySystem.DropItem();
    }

    private void EnsurePhysicsSetup()
    {
        Collider itemCollider = GetComponent<Collider>();

        if (itemCollider == null)
        {
            BoxCollider box = gameObject.AddComponent<BoxCollider>();
            Renderer renderer = GetComponentInChildren<Renderer>();

            if (renderer != null)
            {
                Bounds bounds = renderer.bounds;
                box.center = transform.InverseTransformPoint(bounds.center);
                box.size = WorldSizeToLocalColliderSize(
                    bounds.size,
                    0.25f,
                    1.5f
                );
            }
        }

        Rigidbody body = GetComponent<Rigidbody>();

        if (body == null)
            body = gameObject.AddComponent<Rigidbody>();

        body.useGravity = false;
        body.isKinematic = true;
    }

    private bool IsPlayerInRange()
    {
        int playerLayer = LayerMask.GetMask("Player");

        if (playerLayer != 0)
        {
            Collider[] hits = Physics.OverlapSphere(
                transform.position,
                interactionDistance,
                playerLayer
            );

            if (hits.Length > 0)
                return true;
        }

        PlayerController player = FindFirstObjectByType<PlayerController>();

        return player != null &&
               Vector3.Distance(
                   transform.position,
                   player.transform.position
               ) <= interactionDistance;
    }

    private bool IsPlayerInputLocked()
    {
        PlayerController player = FindFirstObjectByType<PlayerController>();
        return player != null && player.IsInputLocked;
    }

    private static bool IsWeaponModeActive()
    {
        DebugBlasterWeapon weapon =
            FindFirstObjectByType<DebugBlasterWeapon>();

        return weapon != null && weapon.isActiveAndEnabled;
    }

    private static bool IsDialogueBlockingInput()
    {
        return DialogueSystem.Instance != null &&
               DialogueSystem.Instance.IsDialogueActiveOrJustEnded();
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

    private void SetPromptVisible(bool visible)
    {
        if (interactionPrompt != null &&
            interactionPrompt.activeSelf != visible)
        {
            interactionPrompt.SetActive(visible);
        }
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
