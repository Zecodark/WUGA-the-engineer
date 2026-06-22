using System.Collections.Generic;
using UnityEngine;

public class Level2QuestChecklistUI : MonoBehaviour
{
    [SerializeField] private Transform slotParent;
    [SerializeField] private QuestChecklistSlotUI slotPrefab;

    private readonly List<QuestChecklistSlotUI> slots = new();

    public void Build(Level2QuestStep[] steps)
    {
        Clear();

        if (steps == null || slotParent == null || slotPrefab == null)
            return;

        foreach (Level2QuestStep step in steps)
        {
            QuestChecklistSlotUI slot = Instantiate(slotPrefab, slotParent);
            slot.Setup(step.displayName, step.silhouetteIcon, step.completedIcon);
            slots.Add(slot);
        }
    }

    public void MarkCompleted(int index)
    {
        if (index < 0 || index >= slots.Count)
            return;

        slots[index].SetCompleted(true);
    }

    private void Clear()
    {
        foreach (Transform child in slotParent)
            Destroy(child.gameObject);

        slots.Clear();
    }
}
