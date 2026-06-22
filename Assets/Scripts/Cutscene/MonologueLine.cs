using UnityEngine;
using System;

[Serializable]
public class MonologueLine
{
    [TextArea(2, 5)]
    public string text;

    public Sprite panelSprite;

}
