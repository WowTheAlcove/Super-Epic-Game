using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine;
using Ink.Runtime;
using Unity.Netcode;
using UnityEngine.U2D.Animation;

public class DialogueManager : NetworkBehaviour, IDataPersistence
{
    [Header("Ink Story")]
    [SerializeField] private TextAsset inkJson;

    private Story myStory;
    private bool dialoguePlaying = false;
    private PlayerInputActions myInputActions; //for listening to advance dialogue input
    private PlayerController localPlayer; //for freezing input
    private InkVariablesWrapper myInkVariablesWrapper; //for managing variables
    
    #region ---- SAVE/LOAD STATE ----
    private Dictionary<ulong, InkNSerializableVariable[]> pendingClientInkVariables = new Dictionary<ulong, InkNSerializableVariable[]>();
    private bool isSaving = false;
    private Action onFinishedSaving; //Callback to notify DPM when done saving
    #endregion
    
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
            string dialogueLine = myStory.Continue();
            // Debug.Log("line: " + dialogueLine);
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
        myInkVariablesWrapper.UpdateVariableState(e.variableName, e.newValue, myStory);
    }
    private void QuestEvents_OnQuestStateChange(object sender, QuestEvents.QuestStateChangeEventArgs e)
    {
        myInkVariablesWrapper.UpdateVariableState(
            e.QuestID + "State",
            e.NewQuestState.ToString(),
            myStory
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
    //If the quest is being started or ended, it also updates the Ink variable immediately so that dialogue won't have to wait for RPC
    private void BindQuestEventFunctions()
    {
        myStory.BindExternalFunction("StartQuest", (string questId) => 
        {
            // Debug.Log("binding StartQuest");
            GameEventsManager.Instance.questEvents.InvokeStartQuest(this, questId, (int)NetworkManager.Singleton.LocalClientId); 
            myInkVariablesWrapper.UpdateVariableState(questId + "State", "IN_PROGRESS", myStory);
        });
        myStory.BindExternalFunction("AdvanceQuest", (string questId) =>
        {
            GameEventsManager.Instance.questEvents.InvokeAdvanceQuest(this, questId, (int)NetworkManager.Singleton.LocalClientId);
        });
        myStory.BindExternalFunction("FinishQuest", (string questId) =>
        {
            GameEventsManager.Instance.questEvents.InvokeFinishQuest(this, questId, (int)NetworkManager.Singleton.LocalClientId);
            myInkVariablesWrapper.UpdateVariableState(questId + "State", "FINISHED", myStory);
        });
    }

    private void UnbindQuestEventFunctions()
    {
        myStory.UnbindExternalFunction("StartQuest");
        myStory.UnbindExternalFunction("AdvanceQuest");
        myStory.UnbindExternalFunction("FinishQuest");
    }

    #region ---- SAVE/LOAD DATA ----
    
    //called by DPM, syncs all shared variables, then gives each client's DM 
    public void LoadData(GameData gameData)
    {
        if (!IsServer) return; //Only server handles load distribution
        
        //For each of the connected clients, look up their client id in gamedata for ink variables
        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            string clientIdStr = clientId.ToString();
            
            //If we do not have saved data for this player, reset their ink variables
            if (!gameData.allClientsInkVariableSaveDataCollections.ContainsKey(clientIdStr))
            {
                RpcParams resetRpcParams = new RpcParams
                {
                    Send = new RpcSendParams
                    {
                        Target = RpcTarget.Single(clientId, RpcTargetUse.Temp)
                    }
                };
                ResetInkVariablesClientRpc(resetRpcParams);
                continue;
            }
            
            LoadDataForClient(clientId, gameData);
        }
    }

    //Finds a clientId's saved Ink Variables in GameData. then sends it to the associated client through RPC
    //Pre: clientId is a key in GameData's ink vars
    public void LoadDataForClient(ulong clientId, GameData gameData)
    {
        if (!gameData.allClientsInkVariableSaveDataCollections.ContainsKey(clientId.ToString()))
        {
            // The client didn't have any saved data
            return;
        }
        
        ClientsInkVariableSaveDataCollection clientsSavedInkVars = gameData.allClientsInkVariableSaveDataCollections[clientId.ToString()];
        
        //Convert InkVariableSaveData to InkNSerializableVariable
        InkNSerializableVariable[] serializableVariables = new InkNSerializableVariable[clientsSavedInkVars.inkVariables.Count];
        int i = 0;
        
        foreach (var varKvp in clientsSavedInkVars.inkVariables)
        {
            InkNSerializableVariable entry = new InkNSerializableVariable()
            {
                name = varKvp.Key,
                stringValue = string.Empty //Initialize to prevent null
            };
            
            switch (varKvp.Value.type)
            {
                case "int":
                    entry.type = 0;
                    entry.intValue = varKvp.Value.intValue;
                    break;
                case "float":
                    entry.type = 1;
                    entry.floatValue = varKvp.Value.floatValue;
                    break;
                case "bool":
                    entry.type = 2;
                    entry.boolValue = varKvp.Value.boolValue;
                    break;
                case "string":
                    entry.type = 3;
                    entry.stringValue = varKvp.Value.stringValue ?? string.Empty; //Handle null strings
                    break;
            }
            
            serializableVariables[i] = entry;
            i++;
        }
        
        //Send to the appropriate client
        RpcParams rpcParams = new RpcParams
        {
            Send = new RpcSendParams
            {
                Target = RpcTarget.Single(clientId, RpcTargetUse.Temp)
            }
        };
        LoadInkVariablesClientRpc(serializableVariables, rpcParams);
    }
    
    //called by the DPM. Begins the process of collecting variables from all clients
    public void SaveData(ref GameData gameData)
    {
        if (!IsServer) return; //Only server collects save data
        
        //Start the save process
        StartCoroutine(CollectInkVariablesFromClients(gameData));
    }
    
    //Asks all clients to put InkNSerializable arrays into pendingClientInkVariables
    //Then waits until that's done
    //Then call SendPendingVariablesToGameData
    private IEnumerator CollectInkVariablesFromClients(GameData gameData)
    {
        isSaving = true;
        pendingClientInkVariables.Clear();

        //Request ink variables from all clients
        RequestInkVariablesForSaveClientRpc();

        //Wait for all clients to respond
        float timeout = 5f;
        float elapsed = 0f;

        while (elapsed < timeout &&
               pendingClientInkVariables.Count < NetworkManager.Singleton.ConnectedClientsIds.Count)
        {
            yield return new WaitForSecondsRealtime(0.1f);
            elapsed += 0.1f;
        }

        if (pendingClientInkVariables.Count < NetworkManager.Singleton.ConnectedClientsIds.Count)
        {
            Debug.LogWarning(
                $"Timeout waiting for ink variables. Received {pendingClientInkVariables.Count}/{NetworkManager.Singleton.ConnectedClientsIds.Count}");
        }
        
        SendPendingVariablesToGameData(gameData);
    }
    
    //Pre: All clients' ink variables should be stored in pendingClientInkVariables (unless timeout)
    //Convert each array in pendingClientInkVariables into InkVariableSaveDataCollection, and give to GameData
    private void SendPendingVariablesToGameData(GameData gameData)
    {
        //Convert received ink variables to GameData format
        foreach (var kvp in pendingClientInkVariables)
        {
            string clientIdStr = kvp.Key.ToString();
            InkNSerializableVariable[] entries = kvp.Value;
            
            ClientsInkVariableSaveDataCollection collection = new ClientsInkVariableSaveDataCollection();
            
            foreach (InkNSerializableVariable entry in entries)
            {
                InkVariableSaveData saveData = new InkVariableSaveData();
                
                switch (entry.type)
                {
                    case 0:
                        saveData.type = "int";
                        saveData.intValue = entry.intValue;
                        break;
                    case 1:
                        saveData.type = "float";
                        saveData.floatValue = entry.floatValue;
                        break;
                    case 2:
                        saveData.type = "bool";
                        saveData.boolValue = entry.boolValue;
                        break;
                    case 3:
                        saveData.type = "string";
                        saveData.stringValue = entry.stringValue;
                        break;
                }
                
                collection.inkVariables.Add(entry.name, saveData);
            }
            
            gameData.allClientsInkVariableSaveDataCollections[clientIdStr] = collection;
        }
        
        isSaving = false;
        
        //Notify DPM that we're done
        onFinishedSaving?.Invoke();
        // Debug.Log("DM announced its done saving");
    }
    
    //method to check if dialogue manager is saving
    public bool IsSaving()
    {
        return isSaving;
    }
    #endregion
    
    #region ---- RPC METHODS ----
    
    //For each client
    //Converts Ink Variables Wrapper's dictionary into a serializable array, then gives to server
    [Rpc(SendTo.ClientsAndHost)]
    private void RequestInkVariablesForSaveClientRpc()
    {
        InkNSerializableVariable[] entries = myInkVariablesWrapper.InkVarsToNSerializableArray();
        SendInkVariablesToServerServerRpc(entries);
    }
    
    //Server receives serializable ink variable arrays
    //Adds it to pendingClientInkVariables to be used for saving
    [Rpc(SendTo.Server)]
    private void SendInkVariablesToServerServerRpc(InkNSerializableVariable[] entries, RpcParams rpcParams = default)
    {
        ulong senderClientId = rpcParams.Receive.SenderClientId;
        pendingClientInkVariables[senderClientId] = entries;
        // Debug.Log($"Dialgoue manager received {entries.Length} ink variables from client {senderClientId} to put into pendingClientInkVariables");
    }
    
    
    //A specified client gives its Ink Variables Wrapper a dictionary arrays
    //The variables are converted from an array of serializable Ink variables
    [Rpc(SendTo.SpecifiedInParams)]
    private void LoadInkVariablesClientRpc(InkNSerializableVariable[] entries, RpcParams rpcParams)
    {
        ExitDialogue();
        myInkVariablesWrapper.SetVarsFromNSerializableArray(entries);
        RequestSharedVariablesFromServerServerRpc();
        // Debug.Log($"Loaded {entries.Length} ink variables on client {NetworkManager.Singleton.LocalClientId}");
    }

    [Rpc(SendTo.SpecifiedInParams)]
    private void ResetInkVariablesClientRpc(RpcParams rpcParams)
    {
        ExitDialogue();
        myStory.ResetState();
        myInkVariablesWrapper = new InkVariablesWrapper(myStory);
        RequestSharedVariablesFromServerServerRpc();
    }
    
    //Pre: A client wants to sync its shared variables with the server's instance
    //Server sends a list of all Ink Variables with "SharedVar" prefix to a client
    [Rpc(SendTo.Server)]
    private void RequestSharedVariablesFromServerServerRpc(RpcParams rpcParams = default)
    {
        ulong requestingClientId = rpcParams.Receive.SenderClientId;
        
        //Get all shared variables from server's story
        List<InkNSerializableVariable> sharedVars = new List<InkNSerializableVariable>();
        
        foreach (string varName in myStory.variablesState)
        {
            if (varName.StartsWith("SharedVar"))
            {
                object value = myStory.variablesState[varName];
                InkNSerializableVariable entry = new InkNSerializableVariable()
                {
                    name = varName,
                    stringValue = string.Empty
                };
                
                if (value is int)
                {
                    entry.type = 0;
                    entry.intValue = (int)value;
                }
                else if (value is float)
                {
                    entry.type = 1;
                    entry.floatValue = (float)value;
                }
                else if (value is bool)
                {
                    entry.type = 2;
                    entry.boolValue = (bool)value;
                }
                else if (value is string)
                {
                    entry.type = 3;
                    entry.stringValue = (string)value ?? string.Empty;
                }
                
                sharedVars.Add(entry);
            }
        }
        
        //Send shared variables to requesting client
        RpcParams sendParams = new RpcParams
        {
            Send = new RpcSendParams
            {
                Target = RpcTarget.Single(requestingClientId, RpcTargetUse.Temp)
            }
        };
        
        SyncSharedVariablesClientRpc(sharedVars.ToArray(), sendParams);
    }
    
    //Client receives and applies shared variables from server
    [Rpc(SendTo.SpecifiedInParams)]
    private void SyncSharedVariablesClientRpc(InkNSerializableVariable[] sharedVars, RpcParams rpcParams)
    {
        //Apply each shared variable to the client's ink variables wrapper
        foreach (InkNSerializableVariable entry in sharedVars)
        {
            object value = null;
            
            switch (entry.type)
            {
                case 0:
                    value = entry.intValue;
                    break;
                case 1:
                    value = entry.floatValue;
                    break;
                case 2:
                    value = entry.boolValue;
                    break;
                case 3:
                    value = entry.stringValue;
                    break;
            }
            
            if (value != null)
            {
                myInkVariablesWrapper.UpdateVariableState(entry.name, value, myStory);
            }
        }
        
        // Debug.Log($"Synced {sharedVars.Length} shared variables from server on client {NetworkManager.Singleton.LocalClientId}");
    }
    
    #endregion
}