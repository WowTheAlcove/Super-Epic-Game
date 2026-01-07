using Unity.Netcode;
using UnityEngine;

public class SharedDrink3PotionsQuestRewardLogic : QuestRewardLogic {
    public override void PerformRewardLogic() {
        PlayerController[] players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);

        foreach (PlayerController pc in players) {
            pc.GetComponentInChildren<BingoBongoStorer>().IncrementBingoBongoCount();
        }
        Destroy(this);
    }
}