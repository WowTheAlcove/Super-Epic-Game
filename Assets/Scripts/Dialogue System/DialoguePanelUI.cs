using System;
using Ink.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class DialoguePanelUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject dialoguePanelContentParent;
    [SerializeField] private TextMeshProUGUI dialogueTMP;
    [SerializeField] private GameObject choiceButtonsContainer;
    [SerializeField] private GameObject choiceButtonPrefab;

    private void Awake()
    {
        dialoguePanelContentParent.SetActive(false);
        ResetDialogueTMP();
    }

    private void OnEnable()
    {
        GameEventsManager.Instance.dialogueEvents.OnDialogueStarted += DialogueEventsOnOnDialogueStarted;
        GameEventsManager.Instance.dialogueEvents.OnDialogueEnded += DialogueEventsOnOnDialogueEnded;
        GameEventsManager.Instance.dialogueEvents.OnDialogueDisplayed += DialogueEventsOnOnDialogueDisplayed;
    }

    private void OnDisable()
    {
        GameEventsManager.Instance.dialogueEvents.OnDialogueStarted -= DialogueEventsOnOnDialogueStarted;
        GameEventsManager.Instance.dialogueEvents.OnDialogueEnded -= DialogueEventsOnOnDialogueEnded;
        GameEventsManager.Instance.dialogueEvents.OnDialogueDisplayed -= DialogueEventsOnOnDialogueDisplayed;
    }

    private void DialogueEventsOnOnDialogueStarted(object sender, EventArgs e)
    {
        dialoguePanelContentParent.SetActive(true);
    }
    
    private void DialogueEventsOnOnDialogueEnded(object sender, EventArgs e)
    {
        dialoguePanelContentParent.SetActive(false);
        ResetDialogueTMP();
        ClearChoicebuttons();
    }

    //the event calling this gets invoked by Dialogue Manager's Continue()
    private void DialogueEventsOnOnDialogueDisplayed(object sender, DialogueEvents.DialogueDisplayedEventArgs e)
    {
        dialogueTMP.text = e.DialogueLine;
        ClearChoicebuttons();

        if (e.CurrentChoices.Count > 0) //if there are choices after current line of dialogue
        {
            int choiceAmt = e.CurrentChoices.Count - 1;
            for (int i = 0; i < e.CurrentChoices.Count; i++) //for all the choices
            {
                Choice inkChoice = e.CurrentChoices[i];
                DialogueChoiceButton choiceButton = Instantiate( //instantiate choiceButton in container, get the script
                    choiceButtonPrefab,
                    choiceButtonsContainer.transform,
                    false //need this so it doesn't change my button scale
                ).GetComponent<DialogueChoiceButton>();
                choiceButton.SetChoiceIndex(i); //initialize the choice button
                choiceButton.SetChoiceText(e.CurrentChoices[i].text);
            }
        }
    }

    private void ResetDialogueTMP()
    {
        dialogueTMP.text = "";
    }

    private void ClearChoicebuttons()
    {
        foreach (Transform child in choiceButtonsContainer.transform)
        {
            Destroy(child.gameObject);
        }
    }
}

