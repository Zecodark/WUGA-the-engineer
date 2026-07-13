using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class InventoryUI : MonoBehaviour
{
    [SerializeField] private Transform slotParent;
    [SerializeField] private GameObject slotPrefab;
    private List<GameObject> slots = new List<GameObject>();

    void Start()
    {
        if (InventoryManager.Instance == null)
        {
            Debug.LogWarning("[InventoryUI] InventoryManager tidak ditemukan di scene.", this);
            return;
        }

        // Subscribe ke event > Update UI saat inventory berubah
        InventoryManager.Instance.OnInventoryChanged += UpdateUI;
        UpdateUI();
    }

    private void OnDestroy()
    {
        if (InventoryManager.Instance != null)
        InventoryManager.Instance.OnInventoryChanged -= UpdateUI;
    }


    void UpdateUI()
    {
        if (InventoryManager.Instance == null || slotParent == null || slotPrefab == null)
            return;

        // Hapus slot lama
        foreach (GameObject slot in slots)
        {
            Destroy(slot);
        }
        slots.Clear();

        List<ItemData> items = InventoryManager.Instance.GetAllItems();
        foreach (ItemData item in items)
        {
            GameObject slot = Instantiate(slotPrefab, slotParent);
            slot.GetComponent<Image>().sprite = item.icon;
            slots.Add(slot);
        }
    }


}
