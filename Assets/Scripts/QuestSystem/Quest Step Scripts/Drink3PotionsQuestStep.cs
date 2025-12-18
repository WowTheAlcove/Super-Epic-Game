using System;
using UnityEngine;
using Unity.Netcode;

public class Drink3PotionsQuestStep : QuestStep
{
    private int potionsDrank = 0;
    private int potionsDrinkToComplete = 3;

    private void OnEnable() {
        GameEventsManager.Instance.miscEvents.OnPotionDrank += MiscEvents_OnPotionDrank;
    }


    private void OnDisable() {
        GameEventsManager.Instance.miscEvents.OnPotionDrank -= MiscEvents_OnPotionDrank;
    }

    private void MiscEvents_OnPotionDrank(object sender, System.EventArgs e) {
        if (playerIndex.Value != NetworkManager.LocalClient.PlayerObject.GetComponent<PlayerController>().PlayerIndex) {
            return;
        }
        if (potionsDrank < potionsDrinkToComplete) {
            potionsDrank++;
        }

        if (potionsDrank >= potionsDrinkToComplete) {
            FinishQuestStep();
        }
    }

    [Serializable]
    private struct SavedData {
        public int potionsDrank;
    }

    protected override string SerializeDataToString() {
        SavedData data = new SavedData {
            potionsDrank = this.potionsDrank,
        };

        string jsonString = JsonUtility.ToJson(data);

        return jsonString;
    }

    protected override void DeserializeStringToData(string serializedState) {
        SavedData saveData = JsonUtility.FromJson<SavedData>(serializedState);
        potionsDrank = saveData.potionsDrank;
    }
}
