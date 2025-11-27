using System;
using Unity.Netcode;
using UnityEngine;

public class SharedDrink3PotionsQuestStep : QuestStep {

    public NetworkVariable<int> potionsDrank = new NetworkVariable<int>(0);
    private int potionsDrinkToComplete = 3;

    private void OnEnable() {
        GameEventsManager.Instance.miscEvents.OnPotionDrank += MiscEvents_OnPotionDrank; ;
        potionsDrank.OnValueChanged += (int previousValue, int newValue) => {
            if (newValue >= potionsDrinkToComplete) {
                FinishQuestStep();
            }
        };
    }

    private void OnDisable() {
        GameEventsManager.Instance.miscEvents.OnPotionDrank -= MiscEvents_OnPotionDrank;
    }

    private void MiscEvents_OnPotionDrank(object sender, System.EventArgs e) {
        if (potionsDrank.Value < potionsDrinkToComplete) {
            PotionsDrankIncrementServerRpc();
        }
    }

    [Rpc(SendTo.Server)]
    private void PotionsDrankIncrementServerRpc() {
        potionsDrank.Value++;
    }

    [Serializable]
    private struct SavedData {
        public int potionsDrank;
    }

    protected override void DeserializeStringToData(string serializedState) {
        SavedData data = JsonUtility.FromJson<SavedData>(serializedState);

        this.potionsDrank.Value = data.potionsDrank;
    }

    protected override string SerializeDataToString() {
        SavedData data = new SavedData {
            potionsDrank = this.potionsDrank.Value,
        };

        string jsonString = JsonUtility.ToJson(data);

        return jsonString;
    }
}
