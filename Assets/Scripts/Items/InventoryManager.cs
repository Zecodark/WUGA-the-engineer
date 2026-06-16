using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }
    private List<ItemData> grabbedItems = new List<ItemData>();

    public event Action OnInventoryChanged;

    void Awake()
    {
        if (Instance  != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void AddItem(ItemData item)
    {
        grabbedItems.Add(item);
        Debug.Log("Grabbed: " + item.itemName);

        OnInventoryChanged?.Invoke();
    }

    public void RemoveItem(string itemId)
    {
        ItemData item = grabbedItems.Find(i => i.itemId == itemId);
        if (item != null)
        {
            grabbedItems.Remove(item);
            OnInventoryChanged?.Invoke();
        }
    }

    public void ClearAll()
    {
        grabbedItems.Clear();
        OnInventoryChanged?.Invoke();
    }

    public bool HasItem(string itemId)
    {
        return grabbedItems.Exists(i => i.itemId == itemId);
    }

    public int GetItemCount(string itemId)
    {
        return grabbedItems.Count(i => i.itemId == itemId);
    }

    public List<ItemData> GetAllItems()
    {
        return grabbedItems;
    }


}