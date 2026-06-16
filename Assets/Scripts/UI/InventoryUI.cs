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