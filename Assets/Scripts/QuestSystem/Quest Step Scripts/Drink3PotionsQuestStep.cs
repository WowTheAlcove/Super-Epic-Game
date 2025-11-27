using System;
using UnityEngine;
using Unity.Netcode;

public class Drink3PotionsQuestStep : QuestStep
{
    private int potionsDrank = 0;
    private int potionsDrinkToComplete = 3;
    private int bingoBongoIncrementCount = 0;
    private int bingoBongoToComplete = 3;

    private void OnEnable() {
        GameEventsManager.Instance.miscEvents.OnPotionDrank += MiscEvents_OnPotionDrank;
        GameEventsManager.Instance.miscEvents.OnBingoBongoIncremented += MiscEvents_OnBingoBongoIncremented;
    }


    private void OnDisable() {
        GameEventsManager.Instance.miscEvents.OnPotionDrank -= MiscEvents_OnPotionDrank;
        GameEventsManager.Instance.miscEvents.OnBingoBongoIncremented -= MiscEvents_OnBingoBongoIncremented;
    }

    private void MiscEvents_OnPotionDrank(object sender, System.EventArgs e) {
        if (playerIndex.Value != NetworkManager.LocalClient.PlayerObject.GetComponent<PlayerController>().PlayerIndex) {
            Debug.Log("Player index " + playerIndex + " does not match local player index " + NetworkManager.LocalClient.PlayerObject.GetComponent<PlayerController>().PlayerIndex);
            return;
        }
        if (potionsDrank < potionsDrinkToComplete) {
            potionsDrank++;
        }

        if (potionsDrank >= potionsDrinkToComplete && bingoBongoIncrementCount >= bingoBongoToComplete) {
            FinishQuestStep();
        }
    }

    private void MiscEvents_OnBingoBongoIncremented(object sender, MiscEvents.BingoBongoIncrementedEventArgs e) {
        if (playerIndex.Value != NetworkManager.LocalClient.PlayerObject.GetComponent<PlayerController>().PlayerIndex) {
            return;
        }
        if (bingoBongoIncrementCount < bingoBongoToComplete) {
            bingoBongoIncrementCount++;
        }

        if (potionsDrank >= potionsDrinkToComplete && bingoBongoIncrementCount >= bingoBongoToComplete) {
            FinishQuestStep();
        }
    }

    [Serializable]
    private struct SavedData {
        public int potionsDrank;
        public int bingoBongoIncrementCount;
    }

    protected override string SerializeDataToString() {
        SavedData data = new SavedData {
            potionsDrank = this.potionsDrank,
            bingoBongoIncrementCount = this.bingoBongoIncrementCount
        };

        string jsonString = JsonUtility.ToJson(data);

        return jsonString;
    }

    protected override void DeserializeStringToData(string serializedState) {
        SavedData saveData = JsonUtility.FromJson<SavedData>(serializedState);
        potionsDrank = saveData.potionsDrank;
        bingoBongoIncrementCount = saveData.bingoBongoIncrementCount;
    }
}
