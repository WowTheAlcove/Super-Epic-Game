using UnityEngine;
using Unity.Netcode;

public class Drink3PotionsQuestRewardLogic : QuestRewardLogic
{

    public override void PerformRewardLogic() {
        PlayerController[] playerControllers = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);

        foreach(PlayerController pc in playerControllers) {
            if (PlayerIndexManager.Instance.GetPlayerIndexForClientId(pc.OwnerClientId) == this.playerIndex) {
                pc.GetComponentInChildren<BingoBongoStorer>().IncrementBingoBongoCount();
                Destroy(this.gameObject);
                return;
            }
        }
    }

}