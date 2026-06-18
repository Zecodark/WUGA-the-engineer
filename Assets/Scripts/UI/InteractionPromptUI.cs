using UnityEngine;
using TMPro;

public class InteractionPromptUI : MonoBehaviour
{
    [SerializeField] private GameObject promptPanel;
    [SerializeField] private TextMeshProUGUI promptText;

    void Start()
    {
        promptPanel.SetActive(false);
    }

    public void Show(string message)
    {   
        promptPanel.SetActive(true);
        promptText.text = message;
    }

    public void Hide()
    {
        promptPanel.SetActive(false);
    }
}