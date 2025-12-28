using UnityEngine;

public class Break5PotsQuestRewardLogic : QuestRewardLogic
{
    public override void PerformRewardLogic()
    {
        PlayerController[] playerControllers = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);

        foreach(PlayerController pc in playerControllers)
        {
            pc.GetInventoryISS().AddItemToFirstSlotInInventoryServerRpc(2);
        }
        Destroy(this.gameObject);
        return;

    }
}