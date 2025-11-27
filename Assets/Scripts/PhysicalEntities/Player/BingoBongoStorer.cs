using Unity.Netcode;
using UnityEngine;

public class BingoBongoStorer : NetworkBehaviour
{
    private NetworkVariable<int> bingoBongoCount = new NetworkVariable<int>(0);

    public override void OnNetworkSpawn() {
        //subscribe to the bingo bongo count change event
        bingoBongoCount.OnValueChanged += (oldValue, newValue) => {
            if (IsOwner) {
                UIController.Instance.UpdateBingoBongoCounterUI(this);
                GameEventsManager.Instance.miscEvents.InvokeOnBingoBongoChanged(this, newValue);
            }
        };
    }

    public override void OnNetworkDespawn() {
        //unsubscribe from the bingo bongo count change event
        bingoBongoCount.OnValueChanged -= (oldValue, newValue) => {
            if (IsOwner) {
                UIController.Instance.UpdateBingoBongoCounterUI(this);
                GameEventsManager.Instance.miscEvents.InvokeOnBingoBongoChanged(this, newValue);
            }
        };
    }

    [Rpc(SendTo.Server)]
    public void IncrementBingoBongoCountNVServerRpc() {
        bingoBongoCount.Value++;
    }

    public void IncrementBingoBongoCount() {
        InvokeBingoBongoIncrementEvent(bingoBongoCount.Value + 1);
        IncrementBingoBongoCountNVServerRpc();
    }

    private void InvokeBingoBongoIncrementEvent(int newBingoBongoCount) {
        GameEventsManager.Instance.miscEvents.InvokeOnBingoBongoIncremented(this, newBingoBongoCount);
    }

    public int GetBingoBongoCount() {
        return bingoBongoCount.Value;
    }

    public void LoadPlayerData(int bingoBongoCount) {
        this.bingoBongoCount.Value = bingoBongoCount;
    }

    public void ResetBingoBongoCount() {
        bingoBongoCount.Value = 0;
    }
}
