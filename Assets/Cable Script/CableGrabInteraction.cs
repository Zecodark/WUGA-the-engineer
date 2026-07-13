using UnityEngine;
using HPhysic; // Untuk Connector

/// <summary>
/// Pasang script ini di ujung kabel (End) yang ingin bisa di-grab player.
/// Tombol G untuk grab & drop, sama seperti GrabInteraction biasa.
/// </summary>
public class CableGrabInteraction : MonoBehaviour
{
    [SerializeField] private float interactionDistance = 3.5f;
    [SerializeField] private float socketDetectionRadius = 2.5f;
    [SerializeField] private GameObject interactionPrompt;

    private Connector _connector;
    private PhysicCable ownCable;
    private bool isGrabbed;

    private void Awake()
    {
        _connector = GetComponent<Connector>();
        ownCable = GetComponentInParent<PhysicCable>();
    }

    private void Update()
    {
        if (IsLockedCorrectly())
        {
            if (interactionPrompt != null)
                interactionPrompt.SetActive(false);

            enabled = false;
            return;
        }

        // Skip kalau dialog aktif
        if (DialogueSystem.Instance != null && DialogueSystem.Instance.IsDialogueActiveOrJustEnded())
            return;

        CarrySystem carrySystem = FindFirstObjectByType<CarrySystem>();
        if (carrySystem == null)
            return;

        // === SAAT SEDANG PEGANG KABEL INI ===
        if (isGrabbed && carrySystem.IsCarryingCable())
        {
            // Tekan G lagi untuk lepas / colok ke socket
            if (Input.GetKeyDown(KeyCode.G))
            {
                DropCable(carrySystem);
            }
            return;
        }

        // === SAAT BELUM PEGANG ===
        if (isGrabbed || carrySystem.IsCarrying())
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
            interactionPrompt.SetActive(playerInRange);

        if (playerInRange && Input.GetKeyDown(KeyCode.G))
        {
            GrabCable(carrySystem);
        }
    }

    private bool CheckPlayerProximity()
    {
        Collider[] colliders = Physics.OverlapSphere(
            transform.position,
            interactionDistance,
            LayerMask.GetMask("Player")
        );
        return colliders.Length > 0;
    }

    private void GrabCable(CarrySystem carrySystem)
    {
        if (IsLockedCorrectly())
            return;

        if (ownCable != null)
            ownCable.ReleaseRetraction(transform);

        // Jika kabel sedang tercolok, cabut dulu
        if (_connector != null && _connector.IsConnected)
            _connector.Disconnect();

        if (!carrySystem.CarryCable(gameObject))
            return;

        isGrabbed = true;

        if (interactionPrompt != null)
            interactionPrompt.SetActive(false);

        Debug.Log($"[CableGrab] Mengambil kabel {gameObject.name}");
    }

    private void DropCable(CarrySystem carrySystem)
    {
        Connector nearestSocket = null;

        if (_connector != null)
            nearestSocket = FindNearestCompatibleSocket();

        // Lepas dari tangan
        carrySystem.DropCable();
        isGrabbed = false;

        // Cari socket (Female Connector) terdekat
        if (_connector != null)
        {
            if (nearestSocket != null)
            {
                // Auto connect!
                nearestSocket.Connect(_connector);
                Debug.Log($"[CableGrab] Kabel {gameObject.name} tercolok ke {nearestSocket.gameObject.name}!");
            }
            else
            {
                Debug.Log($"[CableGrab] Kabel {gameObject.name} dilepas (tidak ada socket di dekat).");
            }
        }
    }

    /// <summary>
    /// Cari Connector Female terdekat yang bisa dicolok
    /// </summary>
    private Connector FindNearestCompatibleSocket()
    {
        Connector bestSocket = null;
        float bestDistance = float.MaxValue;
        Vector3 plugPosition = _connector != null
            ? _connector.ConnectionPosition
            : transform.position;

        Physics.SyncTransforms();

        foreach (Connector socket in FindObjectsByType<Connector>(FindObjectsSortMode.None))
        {
            if (socket == null || socket == _connector || IsOwnCableConnector(socket))
                continue;

            // Harus bisa connect (beda tipe, warna cocok, belum terpakai)
            if (!_connector.CanConnect(socket)) continue;

            float dist = GetSocketDistance(socket, plugPosition);
            if (dist > socketDetectionRadius)
                continue;

            if (dist < bestDistance)
            {
                bestDistance = dist;
                bestSocket = socket;
            }
        }

        return bestSocket;
    }

    private float GetSocketDistance(Connector socket, Vector3 plugPosition)
    {
        float bestDistance = Vector3.Distance(plugPosition, socket.ConnectionPosition);
        bestDistance = Mathf.Min(
            bestDistance,
            Vector3.Distance(transform.position, socket.ConnectionPosition)
        );
        bestDistance = Mathf.Min(
            bestDistance,
            Vector3.Distance(plugPosition, socket.transform.position)
        );

        foreach (Collider socketCollider in socket.GetComponentsInChildren<Collider>())
        {
            Vector3 closestPoint = socketCollider.ClosestPoint(plugPosition);
            bestDistance = Mathf.Min(
                bestDistance,
                Vector3.Distance(plugPosition, closestPoint)
            );
        }

        return bestDistance;
    }

    private bool IsOwnCableConnector(Connector socket)
    {
        if (ownCable != null && socket.GetComponentInParent<PhysicCable>() == ownCable)
            return true;

        return socket.GetComponent<CableGrabInteraction>() != null;
    }

    private bool IsLockedCorrectly()
    {
        return _connector != null &&
            _connector.IsConnectedRight &&
            _connector.IsConnectionLocked;
    }
}
