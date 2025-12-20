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
    private InkVariablesWrapper myInkVariablesWrapper;
    
    #region ---- INITIALIZATION ----
    private void Awake()
    {
        myStory = new Story(inkJson.text);
        myInputActions = new PlayerInputActions();
        myInkVariablesWrapper = new InkVariablesWrapper(myStory);
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
        GameEventsManager.Instance.dialogueEvents.OnBindInkExternalFunction += DialogueEvents_OnBindInkExternalFunction;
        GameEventsManager.Instance.dialogueEvents.OnUnbindInkExternalFunction += DialogueEvents_OnUnbindInkExternalFunction;
        GameEventsManager.Instance.dialogueEvents.OnUpdateInkVariable += DialogueEvents_OnUpdateInkVariable;
        GameEventsManager.Instance.questEvents.OnQuestStateChange += QuestEvents_OnQuestStateChange;
        
        BindQuestEventFunctions();
    }

    private void OnDisable()
    {
        myInputActions.Dialogue.AdvanceDialogue.performed -= SkipDialogue_performed;
        GameEventsManager.Instance.dialogueEvents.OnEnterDialogue -= DialogueEvents_OnEnterDialogue;    
        GameEventsManager.Instance.dialogueEvents.onChoiceIndexChosen -= DialogueEvents_OnChoiceIndexChosen;
        GameEventsManager.Instance.dialogueEvents.OnBindInkExternalFunction -= DialogueEvents_OnBindInkExternalFunction;
        GameEventsManager.Instance.dialogueEvents.OnUnbindInkExternalFunction -= DialogueEvents_OnUnbindInkExternalFunction;
        GameEventsManager.Instance.dialogueEvents.OnUpdateInkVariable -= DialogueEvents_OnUpdateInkVariable;
        GameEventsManager.Instance.questEvents.OnQuestStateChange -= QuestEvents_OnQuestStateChange;
        
        
        UnbindQuestEventFunctions();
        
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= NetworkManager_OnClientConnectedCallback;
        }
    }

    private void NetworkManager_OnClientConnectedCallback(ulong obj)
    {
        localPlayer = FindLocalPlayer();
    }
    #endregion
    
    #region ---- DIALOGUE FLOW ----
    private void DialogueEvents_OnEnterDialogue(object sender, DialogueEvents.DialogueStartedEventArgs e)
    {
        if(dialoguePlaying) {
            return;
        }

        dialoguePlaying = true;
        myInputActions.Dialogue.Enable();
        localPlayer.DisableInput();
        myInkVariablesWrapper.SyncVariablesAndStartListening(myStory);
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
            Debug.Log("test");
            string dialogueLine = myStory.Continue();
            Debug.Log("line" + dialogueLine);
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
        myInkVariablesWrapper.StopListening(myStory);
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

    //invoked by dialogue panel's choice buttons
    private void DialogueEvents_OnChoiceIndexChosen(object sender, DialogueEvents.ChoiceChosenEventArgs e)
    {
        myStory.ChooseChoiceIndex(e.ChosenChoiceIndex);
        ContinueOrExitStory();
    }
    #endregion
    
    private void DialogueEvents_OnUpdateInkVariable(object sender, DialogueEvents.UpdateInkVariableEventArgs e)
    {
        myInkVariablesWrapper.UpdateVariableState(e.variableName, e.newValue);
    }
    private void QuestEvents_OnQuestStateChange(object sender, QuestEvents.QuestStateChangeEventArgs e)
    {
        myInkVariablesWrapper.UpdateVariableState(
            e.QuestID + "State",
            new StringValue(e.NewQuestState.ToString())
            );
    }
    
    #region ---- HELPER FUNCTIONS ----
    private bool IsLineBlank(string dialogueLine)
    {
        return dialogueLine.Trim().Equals("") || dialogueLine.Trim().Equals("\n");
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
    #endregion
    
    private void DialogueEvents_OnBindInkExternalFunction(object sender, DialogueEvents.BindInkExternalFunctionEventArgs e)
    {
        myStory.BindExternalFunction(e.functionName, e.action);
    }
    private void DialogueEvents_OnUnbindInkExternalFunction(object sender, DialogueEvents.UnbindInkExternalFunctionEventArgs e)
    {
        myStory.UnbindExternalFunction(e.functionName);
    }

    //binds the quest ink external functions invoke the appropriate quest events
            //passes the local client id as player index, all dialogue managers are local
    private void BindQuestEventFunctions()
    {
        myStory.BindExternalFunction("StartQuest", (string questId) => 
        {
            Debug.Log("binding StartQuest");
            GameEventsManager.Instance.questEvents.InvokeStartQuest(this, questId, (int)NetworkManager.Singleton.LocalClientId);            
        });
        myStory.BindExternalFunction("AdvanceQuest", (string questId) =>
        {
            GameEventsManager.Instance.questEvents.InvokeAdvanceQuest(this, questId, (int)NetworkManager.Singleton.LocalClientId);
        });
        myStory.BindExternalFunction("FinishQuest", (string questId) =>
        {
            GameEventsManager.Instance.questEvents.InvokeFinishQuest(this, questId, (int)NetworkManager.Singleton.LocalClientId);
        });
    }

    private void UnbindQuestEventFunctions()
    {
        myStory.UnbindExternalFunction("StartQuest");
        myStory.UnbindExternalFunction("AdvanceQuest");
        myStory.UnbindExternalFunction("FinishQuest");
    }
}
