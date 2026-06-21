using UnityEngine;

public class TableSocket : MonoBehaviour
{
    [SerializeField] private Transform[] sockets;
    [SerializeField] private string requiredItemId;

    private bool[] occupiedSockets;

    void Start()
    {
        occupiedSockets = new bool[sockets.Length];
    }

    public Transform GetAvailableSocket()
    {
        for (int i = 0; i < sockets.Length; i++)
        {
            if(!occupiedSockets[i])
            return sockets[i];
        }
        return null;
    }
    
    public int GetAvailableSocketIndex()
    {
        for (int i = 0; i < sockets.Length; i++)
        {
            if (!occupiedSockets[i])
            return i;
        }
        return -1;
    }

    public void PlaceItem(GameObject item, int socketIndex)
    {
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


}
