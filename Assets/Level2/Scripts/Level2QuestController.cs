using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class Level2QuestController : MonoBehaviour
{
    private static readonly string[] ItemIds =
    {
        "Prosesor", "Storage", "RAM", "Motherboard", "VGA Card", "Power Supply"
    };

    private static readonly string[] WorldNames =
    {
        "prosesor", "storage", "ram", "motherboard", "vga", "powerSupply"
    };

    private static readonly string[] SocketNames =
    {
        "prosesor", "storage", "ram", "motherboard", "vga", "powersupply"
    };

    [Header("Hierarchy Level 2")]
    [SerializeField] private Transform itemsRoot;
    [SerializeField] private Transform socketRoot;
    [SerializeField] private Transform[] configuredWorldItems = new Transform[6];
    [SerializeField] private Transform[] configuredSockets = new Transform[6];

    [Header("References")]
    [SerializeField] private QuestUI questUI;
    [SerializeField] private Level2QuestUI level2QuestUI;
    [SerializeField] private GameOver gameOver;
    [SerializeField] private SimpleBitFollower bitCompanion;
    [SerializeField] private PortalFinishTrigger finishTrigger;
    [SerializeField] private Transform portalRoot;

    [Header("Interaction")]
    [SerializeField, Min(1f)] private float interactionDistance = 8f;
    [SerializeField, Min(1f)] private float placementDistance = 5f;

    private readonly Transform[] worldItems = new Transform[6];
    private readonly Transform[] sockets = new Transform[6];
    private readonly Quaternion[] originalLocalRotations = new Quaternion[6];
    private readonly bool[] placed = new bool[6];
    private CarrySystem carrySystem;
    private PlayerController player;
    private Camera gameplayCamera;
    private int carriedIndex = -1;
    private int completedCount;
    private bool questActive;
    private bool initialized;
    private bool questCompleted;

    public bool IsQuestActive => questActive;
    public float InteractionDistance => interactionDistance;

    private void Awake()
    {
        interactionDistance = Mathf.Max(interactionDistance, 8f);
        ResolveReferences();
        ResolveHierarchy();
        ConfigureInitialState();
    }

    private void Start()
    {
        if (FindFirstObjectByType<Level2IntroController>() == null)
            StartLevelQuest();
    }

    private void Update()
    {
        if (!questActive || !WasGrabPressed())
            return;

        if (carriedIndex >= 0)
            TryPlaceCarriedItem();
        else
            TryGrabNearestItem();
    }

    private void LateUpdate()
    {
        if (questCompleted)
            ActivatePortal();
    }

    public void StartLevelQuest()
    {
        if (!initialized)
        {
            ResolveReferences();
            ResolveHierarchy();
            ConfigureInitialState();
        }

        questActive = true;
        questCompleted = false;
        completedCount = 0;
        carriedIndex = -1;
        Array.Clear(placed, 0, placed.Length);

        for (int i = 0; i < worldItems.Length; i++)
            if (worldItems[i] != null)
                worldItems[i].gameObject.SetActive(true);

        ResetQuestUI();
        gameOver?.StartLevelTimer(ItemIds.Length);
        bitCompanion?.StartFollowing();
        Level2AudioController.SetBitTarget(bitCompanion?.transform);
        Debug.Log("[Level2Quest] Quest aktif. Dekati komponen tersembunyi dan tekan G.", this);
    }

    public bool IsDialogBlockingInteraction() => false;
    public void NotifyItemGrabbed(string itemId) { }
    public void NotifyItemPlaced(string itemId) { }

    private void TryGrabNearestItem()
    {
        if (player == null || carrySystem == null || carrySystem.IsCarrying())
            return;

        int nearestIndex = FindAimedItemIndex();
        if (nearestIndex < 0)
            nearestIndex = FindNearestItemIndex();

        if (nearestIndex < 0)
        {
            Debug.Log("[Level2Quest] G ditekan, tetapi tidak ada komponen yang diarahkan atau dalam jangkauan.", this);
            return;
        }

        Transform item = worldItems[nearestIndex];
        originalLocalRotations[nearestIndex] = item.localRotation;
        EnsureItemPhysics(item);
        EnsureGrabTarget(item);
        if (!carrySystem.CarryItem(item.gameObject))
        {
            Debug.LogError($"[Level2Quest] CarrySystem gagal membawa {ItemIds[nearestIndex]}.", item);
            return;
        }

        carriedIndex = nearestIndex;
        item.localPosition = GetCarryOffset(nearestIndex);
        item.localRotation = originalLocalRotations[nearestIndex];
        Debug.Log($"[Level2Quest] GRAB BERHASIL: {ItemIds[nearestIndex]}", item);
    }

    private int FindAimedItemIndex()
    {
        gameplayCamera ??= Camera.main;
        if (gameplayCamera == null)
            return -1;

        Ray ray = gameplayCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit[] hits = Physics.RaycastAll(ray, 30f, ~0, QueryTriggerInteraction.Collide);
        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            int hitIndex = FindItemIndexFromTransform(hit.transform);
            if (hitIndex >= 0 && !placed[hitIndex])
                return hitIndex;
        }

        int bestIndex = -1;
        float bestScreenScore = float.MaxValue;
        for (int i = 0; i < worldItems.Length; i++)
        {
            if (placed[i] || worldItems[i] == null || !worldItems[i].gameObject.activeInHierarchy)
                continue;

            Vector3 viewport = gameplayCamera.WorldToViewportPoint(GetItemVisualCenter(worldItems[i]));
            if (viewport.z <= 0f)
                continue;

            float screenScore = Vector2.Distance(
                new Vector2(viewport.x, viewport.y),
                new Vector2(0.5f, 0.5f));
            float playerDistance = GetHorizontalInteractionDistance(
                worldItems[i], player.transform.position);

            if (screenScore <= 0.3f && playerDistance <= 15f && screenScore < bestScreenScore)
            {
                bestIndex = i;
                bestScreenScore = screenScore;
            }
        }

        return bestIndex;
    }

    private int FindNearestItemIndex()
    {
        int nearestIndex = -1;
        float nearestDistance = float.MaxValue;

        for (int i = 0; i < worldItems.Length; i++)
        {
            if (placed[i] || worldItems[i] == null || !worldItems[i].gameObject.activeInHierarchy)
                continue;

            float distance = GetHorizontalInteractionDistance(
                worldItems[i],
                player.transform.position);
            if (distance <= interactionDistance && distance < nearestDistance)
            {
                nearestIndex = i;
                nearestDistance = distance;
            }
        }
        return nearestIndex;
    }

    private int FindItemIndexFromTransform(Transform hitTransform)
    {
        if (hitTransform == null)
            return -1;

        for (int i = 0; i < worldItems.Length; i++)
        {
            Transform item = worldItems[i];
            if (item != null &&
                (hitTransform == item || hitTransform.IsChildOf(item)))
                return i;
        }

        return -1;
    }

    private void TryPlaceCarriedItem()
    {
        if (carrySystem == null || carriedIndex < 0 || carriedIndex >= sockets.Length)
            return;

        Transform socket = sockets[carriedIndex];
        if (socket == null || player == null)
            return;

        int index = carriedIndex;
        Transform item = worldItems[index];
        carrySystem.DropItem();
        Vector3 preservedWorldScale = item.lossyScale;
        item.SetParent(socket, true);
        item.position = socket.position;
        item.rotation = socket.rotation;
        SetWorldScale(item, preservedWorldScale);

        Rigidbody body = item.GetComponent<Rigidbody>();
        if (body != null)
        {
            body.isKinematic = true;
            body.useGravity = false;
        }

        placed[index] = true;
        carriedIndex = -1;
        completedCount++;
        MarkQuestUICompleted(ItemIds[index]);
        gameOver?.UpdateQuestProgress(completedCount, ItemIds.Length);
        Debug.Log($"[Level2Quest] PASANG BERHASIL: {ItemIds[index]} ({completedCount}/6)", item);

        if (completedCount >= ItemIds.Length)
            CompleteQuest();
    }

    private void CompleteQuest()
    {
        questActive = false;
        questCompleted = true;
        bitCompanion?.StopFollowing();

        Level2AudioController.PlayTaskCompleteSound();
        ActivatePortal();

        Debug.Log("[Level2Quest] Semua komponen selesai. Portal dibuka.", this);
    }

    private void ResolveReferences()
    {
        player ??= FindFirstObjectByType<PlayerController>();
        carrySystem ??= FindFirstObjectByType<CarrySystem>();
        gameplayCamera ??= Camera.main;
        questUI ??= FindFirstObjectByType<QuestUI>();
        level2QuestUI ??= FindFirstObjectByType<Level2QuestUI>();
        gameOver ??= FindFirstObjectByType<GameOver>();
        bitCompanion ??= FindFirstObjectByType<SimpleBitFollower>();
        finishTrigger ??= FindFirstObjectByType<PortalFinishTrigger>(FindObjectsInactive.Include);
        portalRoot ??= FindSceneTransformByName("PortalEffect");
        if (portalRoot == null && finishTrigger != null)
            portalRoot = finishTrigger.transform;
    }

    private void ResolveHierarchy()
    {
        itemsRoot ??= FindBestRoot("Items", WorldNames, false);
        socketRoot ??= FindBestRoot("meja-putih", SocketNames, true);

        for (int i = 0; i < ItemIds.Length; i++)
        {
            worldItems[i] = GetConfigured(configuredWorldItems, i) ??
                            FindChild(itemsRoot, WorldNames[i], false);
            sockets[i] = GetConfigured(configuredSockets, i) ??
                         FindChild(socketRoot, SocketNames[i], true);

            if (worldItems[i] == null || sockets[i] == null)
                Debug.LogError($"[Level2Quest] Referensi gagal: {ItemIds[i]}", this);
            else
            {
                originalLocalRotations[i] = worldItems[i].localRotation;
                EnsureItemPhysics(worldItems[i]);
                EnsureGrabTarget(worldItems[i]);
                Debug.Log($"[Level2Quest] Siap: {ItemIds[i]} -> {worldItems[i].name} / {sockets[i].name}", this);
            }
        }

        initialized = true;
    }

    private void ConfigureInitialState()
    {
        if (finishTrigger != null)
        {
            finishTrigger.SetPortalUnlocked(false);
        }

        if (portalRoot != null)
            portalRoot.gameObject.SetActive(false);

        EnsureQuestPanelActive();
        ResetQuestUI();

    }

    private void ActivatePortal()
    {
        if (portalRoot == null)
            portalRoot = FindSceneTransformByName("PortalEffect");
        if (portalRoot == null && finishTrigger != null)
            portalRoot = finishTrigger.transform;

        if (portalRoot == null)
        {
            Debug.LogError("[Level2Quest] Root PortalEffect tidak ditemukan.", this);
            return;
        }

        Transform ancestor = portalRoot;
        while (ancestor != null)
        {
            ancestor.gameObject.SetActive(true);
            ancestor = ancestor.parent;
        }

        SetPortalChildrenActive(portalRoot);

        if (finishTrigger == null)
            finishTrigger = portalRoot.GetComponentInChildren<PortalFinishTrigger>(true);

        if (finishTrigger != null)
        {
            finishTrigger.gameObject.SetActive(true);
            finishTrigger.enabled = true;
            Collider triggerCollider = finishTrigger.GetComponent<Collider>();
            if (triggerCollider != null)
                triggerCollider.enabled = true;
            finishTrigger.SetPortalUnlocked(true);
        }

        Level2AudioController.PlayPortalEffect(portalRoot);
    }

    private static void SetPortalChildrenActive(Transform root)
    {
        root.gameObject.SetActive(true);

        Renderer renderer = root.GetComponent<Renderer>();
        if (renderer != null)
            renderer.enabled = true;

        foreach (Transform child in root)
            SetPortalChildrenActive(child);
    }

    private static Transform FindSceneTransformByName(string objectName)
    {
        Transform[] all = FindObjectsByType<Transform>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        foreach (Transform candidate in all)
            if (string.Equals(candidate.name, objectName, StringComparison.OrdinalIgnoreCase))
                return candidate;
        return null;
    }

    private void EnsureQuestPanelActive()
    {
        GameObject panel = level2QuestUI != null
            ? level2QuestUI.gameObject
            : questUI != null
                ? questUI.gameObject
                : GameObject.Find("CanvasQuest");

        if (panel == null)
        {
            Debug.LogError("[Level2Quest] CanvasQuest tidak ditemukan.", this);
            return;
        }

        panel.SetActive(true);
        foreach (string itemId in ItemIds)
        {
            Transform slot = FindChild(panel.transform, itemId, true);
            if (slot != null)
                slot.gameObject.SetActive(true);
            else
                Debug.LogWarning($"[Level2Quest] Slot UI '{itemId}' tidak ditemukan.", panel);
        }
    }

    private void ResetQuestUI()
    {
        EnsureQuestPanelActive();
        if (level2QuestUI != null)
            level2QuestUI.ResetItems();
        else
            questUI?.ResetItems();
    }

    private void MarkQuestUICompleted(string itemId)
    {
        EnsureQuestPanelActive();
        if (level2QuestUI != null)
            level2QuestUI.MarkCompleted(itemId);
        else
            questUI?.MarkCompleted(itemId);
    }

    private static bool WasGrabPressed()
    {
        return Input.GetKeyDown(KeyCode.G) ||
               (Keyboard.current != null && Keyboard.current.gKey.wasPressedThisFrame);
    }

    private static float DistanceToItem(Transform item, Vector3 playerPosition)
    {
        Renderer[] renderers = item.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return Vector3.Distance(item.position, playerPosition);

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);
        return Vector3.Distance(bounds.ClosestPoint(playerPosition), playerPosition);
    }

    private static Vector3 GetItemVisualCenter(Transform item)
    {
        Renderer[] renderers = item.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return item.position;

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);
        return bounds.center;
    }

    private static float GetHorizontalInteractionDistance(
        Transform item,
        Vector3 playerPosition)
    {
        Vector3 visualCenter = GetItemVisualCenter(item);
        Vector2 playerXZ = new(playerPosition.x, playerPosition.z);
        Vector2 rootXZ = new(item.position.x, item.position.z);
        Vector2 visualXZ = new(visualCenter.x, visualCenter.z);

        return Mathf.Min(
            Vector2.Distance(playerXZ, rootXZ),
            Vector2.Distance(playerXZ, visualXZ));
    }

    private static Vector3 GetCarryOffset(int index)
    {
        return index switch
        {
            0 => new Vector3(0f, 0f, 0.35f),
            1 => new Vector3(0f, -0.05f, 0.3f),
            2 => new Vector3(0f, 0f, 0.35f),
            3 => new Vector3(0f, -0.08f, 0.3f),
            4 => new Vector3(0f, -0.05f, 0.35f),
            5 => new Vector3(0f, -0.08f, 0.3f),
            _ => new Vector3(0f, 0f, 0.3f)
        };
    }

    private static void EnsureItemPhysics(Transform item)
    {
        if (item.GetComponentInChildren<Collider>(true) == null)
        {
            BoxCollider collider = item.gameObject.AddComponent<BoxCollider>();
            Renderer[] renderers = item.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length > 0)
            {
                Bounds bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                    bounds.Encapsulate(renderers[i].bounds);
                collider.center = item.InverseTransformPoint(bounds.center);
                Vector3 size = item.InverseTransformVector(bounds.size);
                collider.size = new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z));
            }
        }

        if (item.GetComponent<Rigidbody>() == null)
        {
            Rigidbody body = item.gameObject.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
        }
    }

    private static void EnsureGrabTarget(Transform item)
    {
        Transform existing = item.Find("Level2GrabTarget");
        if (existing != null)
            return;

        Renderer[] renderers = item.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return;

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        GameObject targetObject = new("Level2GrabTarget");
        Transform target = targetObject.transform;
        target.SetParent(item, true);
        target.position = bounds.center;
        target.rotation = Quaternion.identity;

        SphereCollider collider = targetObject.AddComponent<SphereCollider>();
        float desiredWorldRadius = Mathf.Clamp(
            bounds.extents.magnitude * 0.4f,
            0.75f,
            2.5f);
        Vector3 lossyScale = target.lossyScale;
        float largestScale = Mathf.Max(
            Mathf.Abs(lossyScale.x),
            Mathf.Abs(lossyScale.y),
            Mathf.Abs(lossyScale.z));
        collider.radius = desiredWorldRadius / Mathf.Max(largestScale, 0.0001f);
        collider.isTrigger = true;
    }

    private static void SetWorldScale(Transform target, Vector3 desiredWorldScale)
    {
        Transform parent = target.parent;
        if (parent == null)
        {
            target.localScale = desiredWorldScale;
            return;
        }

        Vector3 parentScale = parent.lossyScale;
        target.localScale = new Vector3(
            SafeDivide(desiredWorldScale.x, parentScale.x),
            SafeDivide(desiredWorldScale.y, parentScale.y),
            SafeDivide(desiredWorldScale.z, parentScale.z));
    }

    private static float SafeDivide(float value, float divisor)
    {
        return Mathf.Abs(divisor) > 0.0001f ? value / divisor : value;
    }

    private static Transform GetConfigured(Transform[] array, int index)
    {
        return array != null && index >= 0 && index < array.Length ? array[index] : null;
    }

    private static Transform FindBestRoot(string rootName, string[] expected, bool recursive)
    {
        Transform best = null;
        int bestScore = -1;
        Transform[] all = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Transform candidate in all)
        {
            if (!string.Equals(candidate.name, rootName, StringComparison.OrdinalIgnoreCase))
                continue;
            int score = 0;
            foreach (string name in expected)
                if (FindChild(candidate, name, recursive) != null)
                    score++;
            if (score > bestScore)
            {
                best = candidate;
                bestScore = score;
            }
        }
        return best;
    }

    private static Transform FindChild(Transform root, string name, bool recursive)
    {
        if (root == null)
            return null;
        foreach (Transform child in root)
        {
            if (string.Equals(child.name, name, StringComparison.OrdinalIgnoreCase))
                return child;
            if (recursive)
            {
                Transform found = FindChild(child, name, true);
                if (found != null)
                    return found;
            }
        }
        return null;
    }
}
