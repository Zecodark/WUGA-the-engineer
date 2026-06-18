using UnityEngine;
using System.Collections.Generic;


[CreateAssetMenu(fileName = "NewQuest", menuName = "WUGA/Quest")]
public class QuestData : ScriptableObject
{
    public string questId;
    public string questName;
    public List<QuestObjective> objectives;
    public QuestData nextQuest;

}

[System.Serializable]
public class QuestObjective
{
   public string description;
   public ObjectiveType type;
   public string targetId;
   public int requiredAmount;
   [HideInInspector] public int currentAmount;
}

public enum ObjectiveType
{
    Grab,
    Defeat,
    Reach,
    Interact
}
