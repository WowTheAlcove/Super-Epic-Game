using System;
using TMPro;
using UnityEngine.UI;
using UnityEngine;

public class DialogueChoiceButton : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Button choiceButton;
    [SerializeField] private TextMeshProUGUI choiceText;

    private int choiceIndex = -1;

    private void Awake()
    {
        choiceButton.onClick.AddListener(ChoiceButtonClicked);
    }

    public void SetChoiceIndex(int index)
    {
        this.choiceIndex = index;
    }
    
    public void SetChoiceText(string text) {
        this.choiceText.text = text;
    }

    public void SelectButton()
    {
        choiceButton.Select();
    }

    private void ChoiceButtonClicked()
    {
        GameEventsManager.Instance.dialogueEvents.InvokeOnChoiceIndexChosen(this, choiceIndex);
    }
}
