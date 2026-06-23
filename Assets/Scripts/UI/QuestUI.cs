using System;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class QuestUIImageEntry
{
    [Tooltip("Harus sama dengan Item Id pada quest, misalnya monitor atau keyboard.")]
    public string itemId;

    [Tooltip("Wadah Image yang menampilkan status item ini.")]
    public Image imageContainer;

    [Tooltip("Sprite saat item belum selesai.")]
    public Sprite notCompletedSprite;

    [Tooltip("Sprite saat item sudah selesai.")]
    public Sprite completedSprite;
}

public class QuestUI : MonoBehaviour
{
    [Header("Quest Items")]
    [SerializeField] private QuestUIImageEntry[] items;

    public void ResetItems()
    {
        if (items == null)
            return;

        foreach (QuestUIImageEntry item in items)
            SetImageState(item, false);
    }

    public void MarkCompleted(string itemId)
    {
        if (items == null || string.IsNullOrWhiteSpace(itemId))
            return;

        string normalizedId = NormalizeItemId(itemId);

        foreach (QuestUIImageEntry item in items)
        {
            if (item != null &&
                NormalizeItemId(item.itemId) == normalizedId)
            {
                SetImageState(item, true);
                return;
            }
        }

        Debug.LogWarning(
            $"[QuestUI] Item Id '{itemId}' tidak ditemukan pada Quest Items.",
            this
        );
    }

    private static string NormalizeItemId(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
            return string.Empty;

        string normalized = itemId.Trim().ToLowerInvariant();

        return normalized == "power_supply"
            ? "power_ups"
            : normalized;
    }

    private static void SetImageState(
        QuestUIImageEntry item,
        bool completed)
    {
        if (item == null || item.imageContainer == null)
            return;

        item.imageContainer.sprite = completed
            ? item.completedSprite
            : item.notCompletedSprite;
    }
}
