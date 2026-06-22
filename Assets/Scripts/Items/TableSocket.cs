using UnityEngine;

public class TableSocket : MonoBehaviour
{
    [SerializeField] private Transform[] sockets;
    [SerializeField] private string requiredItemId;

    private bool[] occupiedSockets;

    private void Awake()
    {
        EnsureSocketState();
    }

    public Transform GetAvailableSocket()
    {
        EnsureSocketState();

        for (int i = 0; i < sockets.Length; i++)
        {
            if(!occupiedSockets[i])
            return sockets[i];
        }
        return null;
    }
    
    public int GetAvailableSocketIndex()
    {
        EnsureSocketState();

        for (int i = 0; i < sockets.Length; i++)
        {
            if (!occupiedSockets[i])
            return i;
        }
        return -1;
    }

    public int GetSocketIndexForItem(ItemData item)
    {
        EnsureSocketState();

        if (item == null || string.IsNullOrWhiteSpace(item.itemId))
            return -1;

        string itemId = Normalize(item.itemId);
        bool hasNamedItemSocket = false;

        for (int i = 0; i < sockets.Length; i++)
        {
            if (sockets[i] == null)
                continue;

            string socketName = Normalize(sockets[i].name);

            if (!IsRecognizedSocket(socketName))
                continue;

            hasNamedItemSocket = true;

            if (!occupiedSockets[i] &&
                SocketAcceptsItem(socketName, itemId))
            {
                return i;
            }
        }

        return hasNamedItemSocket ? -1 : GetAvailableSocketIndex();
    }

    public void PlaceItem(GameObject item, int socketIndex)
    {
        EnsureSocketState();

        if (socketIndex < 0 || socketIndex >= sockets.Length) return;
        occupiedSockets[socketIndex] = true;

        item.transform.SetParent(sockets[socketIndex]);
        item.transform.localPosition = Vector3.zero;
        item.transform.localRotation = Quaternion.identity;

        Collider col = item.GetComponent<Collider>();
        if (col != null) col.enabled = true;

        Debug.Log("Placed item at socket " + socketIndex);
    }

    public bool IsSocketAvailable(int index)
    {
        EnsureSocketState();

        if (index < 0 || index >= sockets.Length) return false;
        return !occupiedSockets[index];
    }

    public string GetRequiredItemId()
    {
        return requiredItemId;
    }

    public Transform GetSocket(int index)
    {
        if (index < 0 || index >= sockets.Length) return null;
        return sockets[index];
    }

    private static bool IsRecognizedSocket(string socketName)
    {
        return socketName.Contains("cpu") ||
               socketName.Contains("pc") ||
               socketName.Contains("monitor") ||
               socketName.Contains("mouse") ||
               socketName.Contains("keyboard") ||
               socketName.Contains("power") ||
               socketName.Contains("ups") ||
               socketName.Contains("lan");
    }

    private static bool SocketAcceptsItem(
        string socketName,
        string itemId)
    {
        if (itemId == "cpu")
            return socketName.Contains("cpu") ||
                   socketName.Contains("pc");

        if (itemId == "monitor")
            return socketName.Contains("monitor");

        if (itemId == "mouse")
            return socketName.Contains("mouse");

        if (itemId == "keyboard")
            return socketName.Contains("keyboard");

        if (itemId == "power_supply" || itemId == "power_ups")
        {
            return socketName.Contains("power") ||
                   socketName.Contains("ups") ||
                   socketName.Contains("lan");
        }

        return false;
    }

    private static string Normalize(string value)
    {
        return value.Trim().ToLowerInvariant()
            .Replace("-", "_")
            .Replace(" ", "_");
    }

    private void EnsureSocketState()
    {
        int socketCount = sockets != null ? sockets.Length : 0;

        if (occupiedSockets == null ||
            occupiedSockets.Length != socketCount)
        {
            occupiedSockets = new bool[socketCount];
        }
    }

}
