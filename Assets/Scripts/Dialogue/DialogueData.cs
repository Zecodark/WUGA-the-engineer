using UnityEngine;


[CreateAssetMenu(fileName = "NewDialogue", menuName = "WUGA/Dialogue")]

public class DialogueData : ScriptableObject
{
    public string speakerName;
    [TextArea(3, 10)]
    public string[] lines;
    public DialogueChoice[] choices;
    public QuestData questToGive;
}

[System.Serializable]
public class DialogueChoice
{
    
    public string choiceText;
    public int nextLineIndex;
    public QuestData questToGive;

}