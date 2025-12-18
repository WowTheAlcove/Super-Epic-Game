using System;
using UnityEngine.InputSystem;
using UnityEngine;
using Ink.Runtime;
using Unity.Netcode;
using UnityEngine.U2D.Animation;

public class DialogueManager : MonoBehaviour
{
    [Header("Ink Story")]
    [SerializeField] private TextAsset inkJson;

    private Story myStory;
    private bool dialoguePlaying = false;
    private PlayerInputActions myInputActions;
    private PlayerController localPlayer;

    private void Awake()
    {
        myStory = new Story(inkJson.text);
        myInputActions = new PlayerInputActions();
    }
    private void Start()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += NetworkManager_OnClientConnectedCallback;
    }
    private void OnEnable()
    {
        myInputActions.Dialogue.AdvanceDialogue.performed += SkipDialogue_performed;
        GameEventsManager.Instance.dialogueEvents.OnEnterDialogue += DialogueEvents_OnEnterDialogue;
        GameEventsManager.Instance.dialogueEvents.onChoiceIndexChosen += DialogueEvents_OnChoiceIndexChosen;
        GameEventsManager.Instance.dialogueEvents.OnBindInkExternalFunction += DialogueEventsOnOnBindInkExternalFunction;
        GameEventsManager.Instance.dialogueEvents.OnUnbindInkExternalFunction += DialogueEventsOnOnUnbindInkExternalFunction;
    }
    private void OnDisable()
    {
        myInputActions.Dialogue.AdvanceDialogue.performed -= SkipDialogue_performed;
        GameEventsManager.Instance.dialogueEvents.OnEnterDialogue -= DialogueEvents_OnEnterDialogue;    
        GameEventsManager.Instance.dialogueEvents.onChoiceIndexChosen -= DialogueEvents_OnChoiceIndexChosen;
        GameEventsManager.Instance.dialogueEvents.OnBindInkExternalFunction -= DialogueEventsOnOnBindInkExternalFunction;
        GameEventsManager.Instance.dialogueEvents.OnUnbindInkExternalFunction -= DialogueEventsOnOnUnbindInkExternalFunction;
        
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= NetworkManager_OnClientConnectedCallback;
        }
    }

    private void DialogueEvents_OnEnterDialogue(object sender, DialogueEvents.DialogueStartedEventArgs e)
    {
        if(dialoguePlaying) {
            return;
        }

        dialoguePlaying = true;
        myInputActions.Dialogue.Enable();
        localPlayer.DisableInput();
        GameEventsManager.Instance.dialogueEvents.InvokeOnDialogueStarted(this);

        if (!e.KnotName.Equals("")) {
            myStory.ChoosePathString(e.KnotName);
        } else {
            Debug.LogError("Tried to enter dialogue at knot name that doesn't exist: " + e.KnotName);
        }

        ContinueOrExitStory();
    }

    private void ContinueOrExitStory()
    {
        if (myStory.canContinue)
        {
            string dialogueLine = myStory.Continue();
            
            //handling weird cases where Ink gives empty dialogue lines
            while (IsLineBlank(dialogueLine) && myStory.canContinue)
            {
                dialogueLine = myStory.Continue();
            }
            
            //if we ended up running out of dialogue lines from skipping from blanks
            if (IsLineBlank(dialogueLine) && !myStory.canContinue)
            {
                ExitDialogue();
            }
            else
            {
                GameEventsManager.Instance.dialogueEvents.InvokeOnDialogueDisplayed(
                    this,
                    dialogueLine,
                    myStory.currentChoices
                    );
            }
            
        }
        else if(myStory.currentChoices.Count == 0) //if last line of dialogue, and there are no choices
        {
            ExitDialogue();
        }
    }

    private void ExitDialogue()
    {
        dialoguePlaying = false;
        myInputActions.Dialogue.Disable();
        localPlayer.EnableInput();
        myStory.ResetState();
        GameEventsManager.Instance.dialogueEvents.InvokeOnDialogueEnded(this);
    }
    private void SkipDialogue_performed(InputAction.CallbackContext obj)
    {
        if(!dialoguePlaying || myStory.currentChoices.Count > 0)
        {
            return;
        }
        ContinueOrExitStory();
    }
    private void NetworkManager_OnClientConnectedCallback(ulong obj)
    {
        localPlayer = FindLocalPlayer();
    }

    private PlayerController FindLocalPlayer()
    {
        foreach (PlayerController player in FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
        {
            if (player.IsLocalPlayer)
            {
                return player;
            }
        }
        Debug.LogError("Could not find local player in DialogueManager");
        return null;
    }

    //invoked by dialogue panel's choice buttons
    private void DialogueEvents_OnChoiceIndexChosen(object sender, DialogueEvents.ChoiceChosenEventArgs e)
    {
        myStory.ChooseChoiceIndex(e.ChosenChoiceIndex);
        ContinueOrExitStory();
    }

    public void BindExternalFunction(string inkFunctionName, Action action)
    {
        myStory.BindExternalFunction(inkFunctionName, action);
    }

    public void UnbindExternalFunction(string inkFunctionName)
    {
        myStory.UnbindExternalFunction(inkFunctionName);
    }
    
    private bool IsLineBlank(string dialogueLine)
    {
        return dialogueLine.Trim().Equals("") || dialogueLine.Trim().Equals("\n");
    }
    private void DialogueEventsOnOnBindInkExternalFunction(object sender, DialogueEvents.BindInkExternalFunctionEventArgs e)
    {
        Debug.Log("binding" + e.functionName);
        myStory.BindExternalFunction(e.functionName, e.action);
    }
    private void DialogueEventsOnOnUnbindInkExternalFunction(object sender, DialogueEvents.UnbindInkExternalFunctionEventArgs e)
    {
        myStory.UnbindExternalFunction(e.functionName);
    }

}
