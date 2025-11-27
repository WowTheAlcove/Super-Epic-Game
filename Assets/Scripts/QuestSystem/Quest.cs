using UnityEngine;

public class Quest
{
    //static info
    public QuestInfoSO info;
    public int playerIndex;

    //state info
    public QuestState state;
    private int currentQuestStepIndex;

    public Quest(QuestInfoSO questInfo, int playerIndex) {
        this.info = questInfo;
        this.state = QuestState.REQUIREMENTS_NOT_MET;
        this.currentQuestStepIndex = 0;
        
        this.playerIndex = playerIndex;
    }
    public void MoveToNextQuestStep() {
        currentQuestStepIndex++;
    }

    public bool CurrentQuestStepExists() {
        return (currentQuestStepIndex < info.questStepPrefabs.Length);
    }

    public GameObject GetCurrentQuestStepPrefab() {
        if (CurrentQuestStepExists()) {
            return info.questStepPrefabs[currentQuestStepIndex];
        } else {
            Debug.LogError("Tried to get a quest step at an index that doesn't exist");
            return null;
        }
    }

    public void InstantiateCurrentQuestStepPrefab(Transform parentTransform) {
        GameObject questStepPrefab = GetCurrentQuestStepPrefab();

        if (questStepPrefab != null) {
            QuestStep questStep = Object.Instantiate<GameObject>(questStepPrefab, parentTransform).GetComponent<QuestStep>();

            questStep.InitializeQuestStep(this.info.id, this.playerIndex);
            questStep.NetworkObject.Spawn();
        }
    }

    public void InstantiateQuestRewardPrefab(Transform parentTransform) {
        if (info.questRewardPrefab != null) {
            QuestRewardLogic instantiatedQuestRewardLogic = Object.Instantiate<GameObject>(info.questRewardPrefab, parentTransform).GetComponent<QuestRewardLogic>();
            instantiatedQuestRewardLogic.SetPlayerIndex(playerIndex);
            instantiatedQuestRewardLogic.PerformRewardLogic();
        } else {
            Debug.LogError("Tried to instantiate a quest reward prefab that doesn't exist on quest: " + this.info.id);
        }
    }

    public void SetCurrentQuestStepIndex(int index) {
        currentQuestStepIndex = index;
    }

    public int GetCurrentQuestStepIndex() {
        return currentQuestStepIndex;
    }
}


public enum QuestState {
    REQUIREMENTS_NOT_MET,
    CAN_START,
    IN_PROGRESS,
    CAN_FINISH,
    FINISHED
}