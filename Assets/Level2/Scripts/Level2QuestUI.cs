using System;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class Level2QuestUIEntry
{
    public string itemId;
    public Image imageContainer;
    public Sprite notCompletedSprite;
    public Sprite completedSprite;
}

public class Level2QuestUI : MonoBehaviour
{
    [SerializeField] private Level2QuestUIEntry[] items;

    private void Awake()
    {
        gameObject.SetActive(true);
        ResetItems();
    }

    public void ResetItems()
    {
        if (items == null)
            return;

        foreach (Level2QuestUIEntry item in items)
        {
            if (item?.imageContainer == null)
                continue;

            item.imageContainer.gameObject.SetActive(true);
            item.imageContainer.enabled = true;
            item.imageContainer.sprite = item.notCompletedSprite;
        }
    }

    public void MarkCompleted(string itemId)
    {
        if (items == null || string.IsNullOrWhiteSpace(itemId))
            return;

        foreach (Level2QuestUIEntry item in items)
        {
            if (item == null || item.imageContainer == null ||
                !string.Equals(item.itemId, itemId, StringComparison.OrdinalIgnoreCase))
                continue;

            item.imageContainer.gameObject.SetActive(true);
            item.imageContainer.enabled = true;
            item.imageContainer.sprite = item.completedSprite;
            return;
        }

        Debug.LogWarning($"[Level2QuestUI] Slot '{itemId}' tidak ditemukan.", this);
    }
}
