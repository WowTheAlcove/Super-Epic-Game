using UnityEngine;

public abstract class QuestRewardLogic : MonoBehaviour
{
    protected int playerIndex;

    public void SetPlayerIndex(int index) {
        playerIndex = index;
    }

    public abstract void PerformRewardLogic();
}
