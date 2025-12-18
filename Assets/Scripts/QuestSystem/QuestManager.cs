using System.Collections.Generic;
using System.Collections.Specialized;
using Unity.Netcode;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.AddressableAssets;
using System.Linq;

public class QuestManager : NetworkBehaviour, IDataPersistence
{

    private List<Dictionary<string, Quest>> playerQuestMaps;

    private List<int> currentBingoBongoCounts;

    private void Awake() {
        currentBingoBongoCounts = new List<int>() { 0, 0, 0, 0 };

        playerQuestMaps = new List<Dictionary<string, Quest>>();
        for(int i = 0; i < 4; i++) {
            playerQuestMaps.Add(CreateQuestMap(i));
        }
    }

    private void Start() {
        GameEventsManager.Instance.questEvents.OnStartQuest += QuestEvents_OnStartQuest;
        GameEventsManager.Instance.questEvents.OnAdvanceQuest += QuestEvents_OnAdvanceQuest;
        GameEventsManager.Instance.questEvents.OnFinishQuest += QuestEvents_OnFinishQuest;

        GameEventsManager.Instance.miscEvents.OnBingoBongoChanged += MiscEvents_OnBingoBongoChanged;

        if (NetworkManager.Singleton != null) {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        } else {
            Debug.LogError("QuestManager OnEnable: NetworkManager.Singleton was null");
        }
    }

    private void OnDisable() {
        GameEventsManager.Instance.questEvents.OnStartQuest -= QuestEvents_OnStartQuest;
        GameEventsManager.Instance.questEvents.OnAdvanceQuest -= QuestEvents_OnAdvanceQuest;
        GameEventsManager.Instance.questEvents.OnFinishQuest -= QuestEvents_OnFinishQuest;

        GameEventsManager.Instance.miscEvents.OnBingoBongoChanged -= MiscEvents_OnBingoBongoChanged;

        if (NetworkManager.Singleton != null) {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }
    }

    private void OnClientConnected(ulong clientId) {
        BroadcastAllQuestStatesToRespectiveClients();
    }

    //initializing dictionary of quest IDs to Quests
    private Dictionary<string, Quest> CreateQuestMap(int playerIndex) {
        List<QuestInfoSO> allQuestInfoSOs = new List<QuestInfoSO>(Addressables.LoadAssetsAsync<QuestInfoSO>("QuestInfoSOLabel").WaitForCompletion());

        Dictionary<string, Quest> idToQuestMap = new Dictionary<string, Quest>();


        foreach (QuestInfoSO questInfoSO in allQuestInfoSOs) {

            if (idToQuestMap.ContainsKey(questInfoSO.id)) {
                Debug.LogError("When creating quest map, found multiple QuestInfoSOs with the same ID");
            }

            idToQuestMap.Add(questInfoSO.id, new Quest(questInfoSO, playerIndex));
        }

        return idToQuestMap;
    }

    private Quest GetQuestByID(string id, int playerIndex) {
        if (playerQuestMaps[playerIndex].ContainsKey(id)) { //0 because all players have the same quests
            return playerQuestMaps[playerIndex][id];
        } else {
            Debug.LogError("QuestManager's QuestMap didn't contain id: " + id);
            return null;
        }

    }
    
    private QuestInfoSO GetQuestInfoSOByID(string id) {
        if (playerQuestMaps[0].ContainsKey(id)) { //0 because all players have the same quests
            return playerQuestMaps[0][id].info;
        } else {
            Debug.LogError("QuestManager's QuestMap didn't contain id: " + id);
            return null;
        }
    }

    //changes a quest's state and broadcasts the change with the event manager
    private void ChangeQuestStateForGivenPlayerIndex(int playerIndex, string questToChangeId, QuestState newState) {
        Quest quest = GetQuestByID(questToChangeId, playerIndex);
        quest.state = newState;
        InvokeQuestStateChangeEventOnGivenClientRpc(playerIndex, quest.info.id, newState);
    }

    //If the quest is shared, then change the quest state on all clients
    //If not, then only on the given player index
    private void ChangeQuestStateOnAppropriateClients(int playerIndex, string questToChangeId, QuestState newState) {
        Quest quest = GetQuestByID(questToChangeId, playerIndex);
        if(quest.info.IsShared) {
            for(int i = 0; i < playerQuestMaps.Count; i++) {
                ChangeQuestStateForGivenPlayerIndex(i, questToChangeId, newState);
            }
        } else {
            ChangeQuestStateForGivenPlayerIndex(playerIndex, questToChangeId, newState);
        }
    }

    [Rpc(SendTo.Server)]
    private void CheckAllRequirementsMetRpc() {
        if (!IsServer) return;

        for (int i = 0; i < 4; i++) {
            foreach (Quest quest in playerQuestMaps[i].Values) {
                if (quest.state == QuestState.REQUIREMENTS_NOT_MET) {
                    //if the requirements were marked as not met

                    if (CheckRequirementsMet(quest, i)) {
                        //Debug.Log("Quest requirements met for quest: " + quest.info.id);
                        ChangeQuestStateForGivenPlayerIndex(i, quest.info.id, QuestState.CAN_START);
                    }
                } else if (quest.state == QuestState.CAN_START){
                    //if the requirements were marked as met

                    if (!CheckRequirementsMet(quest, i)) {
                        //Debug.Log("Quest requirements no longer met for quest: " + quest.info.id);
                        ChangeQuestStateForGivenPlayerIndex(i, quest.info.id, QuestState.REQUIREMENTS_NOT_MET);
                    }
                }
            }
        }
    }

    private bool CheckRequirementsMet(Quest quest, int playerIndex) {
        if (currentBingoBongoCounts[playerIndex] < quest.info.bingoBongoRequirement) {
            return false;
        }

        foreach(QuestInfoSO prerequisiteQuestInfoSO in quest.info.prerequisiteQuestInfoSos) {
            if(GetQuestByID(prerequisiteQuestInfoSO.id, playerIndex).state != QuestState.FINISHED) {
                return false;
            }
        }

        //no requirements failed, so all must be met
        return true;
    }


    private void QuestEvents_OnStartQuest(object sender, QuestEvents.QuestEventArgs e) {
        QuestEvents_OnStartQuestServerRpc(e.QuestID, e.PlayerIndex);
    }
    private void QuestEvents_OnAdvanceQuest(object sender, QuestEvents.QuestEventArgs e) {
        QuestEvents_OnAdvanceQuestServerRpc(e.QuestID, e.PlayerIndex);
    }
    private void QuestEvents_OnFinishQuest(object sender, QuestEvents.QuestEventArgs e) {
        QuestEvents_OnFinishQuestServerRpc(e.QuestID, e.PlayerIndex);
    }

    [Rpc(SendTo.Server)]
    private void QuestEvents_OnStartQuestServerRpc(string questId, int playerIndex) {
        Quest quest = GetQuestByID(questId, playerIndex);

        if(quest.state != QuestState.CAN_START) {
            Debug.LogWarning("Tried to start a quest that wasn't in the CAN_START state");
            return;
        }

        ChangeQuestStateOnAppropriateClients(playerIndex, questId, QuestState.IN_PROGRESS);

        quest.InstantiateCurrentQuestStepPrefab(this.transform);
    }

    [Rpc(SendTo.Server)]
    private void QuestEvents_OnAdvanceQuestServerRpc(string questId, int playerIndex) {
        Quest quest = GetQuestByID(questId, playerIndex);
        quest.MoveToNextQuestStep();

        if (quest.CurrentQuestStepExists()) { // if there is another step, instantiate it
            quest.InstantiateCurrentQuestStepPrefab(this.transform);
        } else {// if there are no more steps, mark the quest as ready to finish
            ChangeQuestStateOnAppropriateClients(playerIndex, questId, QuestState.CAN_FINISH);
        }
    }

    [Rpc(SendTo.Server)]
    private void QuestEvents_OnFinishQuestServerRpc(string questId, int playerIndex) {
        Quest quest = GetQuestByID(questId, playerIndex);

        if(quest.state != QuestState.CAN_FINISH) {
            Debug.LogWarning("Tried to finish a quest that wasn't in the CAN_FINISH state");
            return;
        }


        ChangeQuestStateOnAppropriateClients(playerIndex, questId, QuestState.FINISHED);

        quest.InstantiateQuestRewardPrefab(this.transform); 

        CheckAllRequirementsMetRpc();
    }


    //tracking bingo bongo for quest requirements
    private void MiscEvents_OnBingoBongoChanged(object sender, MiscEvents.BingoBongoChangedEventArgs e) {
        UpdateBingoBongoCountForGivenClientIdRpc((int)NetworkManager.Singleton.LocalClientId, e.newBingoBongoCount);
        CheckAllRequirementsMetRpc();
    }

    [Rpc(SendTo.Server)]
    private void UpdateBingoBongoCountForGivenClientIdRpc(int playerIndex, int newBingoBongoCount){
        currentBingoBongoCounts[playerIndex] = newBingoBongoCount;
    }
    public void LoadData(GameData data) {
        for (int playerIndex = 0; playerIndex < playerQuestMaps.Count; playerIndex++) {
            Dictionary<string, Quest> questMap = playerQuestMaps[playerIndex];

            // Iterate over every saved quest in GameData that matches this player
            foreach (var kvp in data.allQuestData) {
                // Keys are in the format "questID:playerIndex:X"
                string[] keyParts = kvp.Key.Split(':');
                if (keyParts.Length == 3 && keyParts[1] == "playerIndex") {
                    if (int.TryParse(keyParts[2], out int savedPlayerIndex) && savedPlayerIndex == playerIndex) {
                        string questID = keyParts[0];

                        if (questMap.ContainsKey(questID)) {
                            Quest quest = questMap[questID];
                            quest.state = kvp.Value.state;
                            quest.SetCurrentQuestStepIndex(kvp.Value.currentStepIndex);
                        } else {
                            Debug.LogWarning($"QuestManager: Quest ID {questID} was in save data but not in quest map for player {playerIndex}!");
                        }
                    }
                }
            }
        }
        BroadcastAllQuestStatesToRespectiveClients();
    }
    public void SaveData(ref GameData data) {
        // Loop through all players' quest maps
        for (int playerIndex = 0; playerIndex < playerQuestMaps.Count; playerIndex++) {
            Dictionary<string, Quest> questMap = playerQuestMaps[playerIndex];

            foreach (var kvp in questMap) {
                string questID = kvp.Key;
                Quest quest = kvp.Value;

                // Save using the format "questID:playerIndex:X"
                string keyWithPlayerIndex = $"{questID}:playerIndex:{playerIndex}";

                data.allQuestData[keyWithPlayerIndex] = new QuestSaveData {
                    state = quest.state,
                    currentStepIndex = quest.GetCurrentQuestStepIndex()
                };
            }
        }
    }

    //invokes the quest state change event for all quests, on the client that owns that quest
    private void BroadcastAllQuestStatesToRespectiveClients() {
        if(!IsServer) return;

        for (int i = 0; i < playerQuestMaps.Count; i++) {
            foreach (Quest quest in playerQuestMaps[i].Values) {
                InvokeQuestStateChangeEventOnGivenClientRpc(i, quest.info.id, quest.state);
            }
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void InvokeQuestStateChangeEventOnGivenClientRpc(int clientIdToInvokeEvent, string questId, QuestState newQuestState) {
        if ((int)NetworkManager.Singleton.LocalClientId == clientIdToInvokeEvent) {
            //Debug.Log("Invoking quest state change event for quest: " + questId + " to state: " + newQuestState + " on client: " + clientIdToInvokeEvent);
            GameEventsManager.Instance.questEvents.InvokeQuestStateChange(this, questId, newQuestState);
        }
    }
}
