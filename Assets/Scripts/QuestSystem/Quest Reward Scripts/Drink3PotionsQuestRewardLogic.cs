using UnityEngine;
using Unity.Netcode;

public class Drink3PotionsQuestRewardLogic : QuestRewardLogic
{

    public override void PerformRewardLogic() {
        PlayerController[] playerControllers = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);

        foreach(PlayerController pc in playerControllers) {
            if (pc.PlayerIndex == playerIndex) {
                pc.GetComponentInChildren<BingoBongoStorer>().IncrementBingoBongoCount();
                Destroy(this.gameObject);
                return;
            }
        }
    }

}
