using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuestChecklistSlotUI : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI labelText;
    [SerializeField] private Image checkmarkImage;
    [SerializeField] private Color pendingColor = Color.white;
    [SerializeField] private Color completedColor = Color.green;

    private Sprite silhouetteIcon;
    private Sprite completedIcon;

    public void Setup(
        string label,
        Sprite pendingSprite,
        Sprite finishedSprite)
    {
        silhouetteIcon = pendingSprite;
        completedIcon = finishedSprite;

        if (labelText != null)
            labelText.text = label;

        SetCompleted(false);
    }

    public void SetCompleted(bool completed)
    {
        if (iconImage != null)
            iconImage.sprite = completed ? completedIcon : silhouetteIcon;

        if (labelText != null)
            labelText.color = completed ? completedColor : pendingColor;

        if (checkmarkImage != null)
            checkmarkImage.gameObject.SetActive(completed);
    }
}
