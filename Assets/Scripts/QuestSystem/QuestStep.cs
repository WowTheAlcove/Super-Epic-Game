using Unity.Netcode;
using UnityEngine;

public abstract class QuestStep : NetworkBehaviour, IDataPersistence
{
    private bool isFinished = false;


    private string questId;

    private int initializedPlayerIndex;
    protected NetworkVariable<int> playerIndex = new NetworkVariable<int>();

    public void InitializeQuestStep(string id, int playerIndex) {
        this.questId = id;
        this.initializedPlayerIndex = playerIndex;
    }

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();
        if (IsServer) {
            playerIndex.Value = initializedPlayerIndex;
        }
    }

    protected void FinishQuestStep() {
        FinishQuestStepServerRpc();
    }

    [Rpc(SendTo.Server)]
    protected void FinishQuestStepServerRpc() {
        if (!isFinished) {
            isFinished = true;

            GameEventsManager.Instance.questEvents.InvokeAdvanceQuest(this, questId, playerIndex.Value);

            DataPersistenceManager.Instance.RemoveSaveId(GetComponent<SaveID>().Id);
            
            this.NetworkObject.Despawn(true);
        }
    }
    public void LoadData(GameData gameData) {
        if (gameData.allQuestStepData.TryGetValue(GetComponent<SaveID>().Id, out QuestStepData questStepData)) {
            this.questId = questStepData.questId;
            this.playerIndex.Value = questStepData.playerIndex;
            DeserializeStringToData(questStepData.questStepState);
        }

    }

    public void SaveData(ref GameData gameData) {
        string serializedState = SerializeDataToString();

        if (!string.IsNullOrEmpty(serializedState)) {
            // We'll store quest step data in a new dictionary in GameData

            QuestStepData questStepDataToSave = new QuestStepData {
                questId = this.questId,
                playerIndex = this.playerIndex.Value,
                questStepState = serializedState
            };

            if (!gameData.allQuestStepData.ContainsKey(GetComponent<SaveID>().Id)) {
                gameData.allQuestStepData.Add(GetComponent<SaveID>().Id, questStepDataToSave);
            } else {
                gameData.allQuestStepData[GetComponent<SaveID>().Id] = questStepDataToSave;
            }
        }
    }

    protected abstract string SerializeDataToString();

    protected abstract void DeserializeStringToData(string serializedState);
}