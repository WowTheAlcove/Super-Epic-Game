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

    #region ---- SAVE/LOAD DATA ----
    
    //called by DPM, syncs all shared variables, then gives each client's DM 
    public void LoadData(GameData gameData)
    {
        if (!IsServer) return; //Only server handles load distribution
        
        //Sync shared variables before loading
        SyncInkSharedVars(gameData);
        
        //For each of the collections if ink variables, send it to each client in an InkNSerializable array
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
            
            ClientsInkVariableSaveDataCollection collection = gameData.allClientsInkVariableSaveDataCollections[clientIdStr];
            
            //Convert InkVariableSaveData to InkNSerializableVariable
            //Then send it to the associated client through RPC
            InkNSerializableVariable[] serializableVariables = new InkNSerializableVariable[collection.inkVariables.Count];
            int i = 0;
            
            foreach (var varKvp in collection.inkVariables)
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
        float timeout = 10f;
        float elapsed = 0f;

        while (elapsed < timeout &&
               pendingClientInkVariables.Count < NetworkManager.Singleton.ConnectedClientsIds.Count)
        {
            yield return new WaitForSeconds(0.1f);
            elapsed += 0.1f;
        }

        if (pendingClientInkVariables.Count < NetworkManager.Singleton.ConnectedClientsIds.Count)
        {
            Debug.LogWarning(
                $"Timeout waiting for ink variables. Received {pendingClientInkVariables.Count}/{NetworkManager.Singleton.ConnectedClientsIds.Count}");
        }
        
        SendPendingVariablesToGameData(gameData);
    }
    
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
            
            gameData.allClientsInkVariableSaveDataCollections.Add(clientIdStr, collection);
        }
        
        isSaving = false;
        
        //Notify DPM that we're done
        onFinishedSaving?.Invoke();
        Debug.Log("DM announced its done saving");
    }
    
    //In given gameData, set all client's shared ink variables equal to the server's version
    private void SyncInkSharedVars(GameData gameData)
    {
        if (!gameData.allClientsInkVariableSaveDataCollections.ContainsKey("0"))
        {
            Debug.LogWarning("No server ink variables found (client 0), cannot sync shared vars");
            return;
        }
        
        List<KeyValuePair<string, InkVariableSaveData>> sharedVars = new List<KeyValuePair<string, InkVariableSaveData>>();
        ClientsInkVariableSaveDataCollection serverCollection = gameData.allClientsInkVariableSaveDataCollections["0"];
        
        //Collect all shared variables from server
        foreach (var kvp in serverCollection.inkVariables)
        {
            if (kvp.Key.StartsWith("SharedVar"))
            {
                sharedVars.Add(kvp);
            }
        }
        
        //Apply shared variables to all clients
        foreach (var clientKvp in gameData.allClientsInkVariableSaveDataCollections)
        {
            if (clientKvp.Key == "0") continue; //Skip server
            
            foreach (var sharedVar in sharedVars)
            {
                if (clientKvp.Value.inkVariables.ContainsKey(sharedVar.Key))
                {
                    clientKvp.Value.inkVariables[sharedVar.Key] = sharedVar.Value;
                }
            }
        }
    }
    
    //called by whoever wants to know when the dialogue manager is done saving
    public void RegisterSaveCompleteCallback(Action callback)
    {
        onFinishedSaving = callback;
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
        InkNSerializableVariable[] entries = myInkVariablesWrapper.ToNSerializableArray();
        SendInkVariablesToServerServerRpc(entries);
    }
    
    //Server receives serializable ink variable arrays
    //Adds it to pendingClientInkVariables to be used for saving
    [Rpc(SendTo.Server)]
    private void SendInkVariablesToServerServerRpc(InkNSerializableVariable[] entries, RpcParams rpcParams = default)
    {
        ulong senderClientId = rpcParams.Receive.SenderClientId;
        pendingClientInkVariables[senderClientId] = entries;
        Debug.Log($"Dialgoue manager received {entries.Length} ink variables from client {senderClientId} to put into pendingClientInkVariables");
    }
    
    
    //A specified client gives its Ink Variables Wrapper a dictionary arrays
    //The variables are converted from an array of serializable Ink variables
    [Rpc(SendTo.SpecifiedInParams)]
    private void LoadInkVariablesClientRpc(InkNSerializableVariable[] entries, RpcParams rpcParams)
    {
        myInkVariablesWrapper.FromNSerializableArray(entries);
        Debug.Log($"Loaded {entries.Length} ink variables on client {NetworkManager.Singleton.LocalClientId}");
    }

    [Rpc(SendTo.SpecifiedInParams)]
    private void ResetInkVariablesClientRpc(RpcParams rpcParams)
    {
        myStory.ResetState();
        myInkVariablesWrapper = new InkVariablesWrapper(myStory);
    }
    
    #endregion
}