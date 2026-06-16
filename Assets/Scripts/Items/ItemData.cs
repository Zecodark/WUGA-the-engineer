using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "WUGA/Item")]

public class ItemData : ScriptableObject
{
    public string itemId;
    public string itemName;
    public Sprite icon;
    public ItemSize size;
    public string description;
}

public enum ItemSize
{
    Small,
    Medium,
    Large
}