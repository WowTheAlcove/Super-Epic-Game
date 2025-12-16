using TMPro;
using UnityEngine;

public class BingoBongoDisplayer : MonoBehaviour
{
    private BingoBongoStorer connectedBingoBongoStorer;
    private PlayerInputActions myPlayerInputActions;
    private TextMeshProUGUI bingoBongoTMP;
    [SerializeField] private GameObject kickablePot; //for testing
    
    private void Awake() {
        bingoBongoTMP = this.GetComponent<TextMeshProUGUI>();
    }

    private void Start() {
        UIController.Instance.OnAllInventoryPanelsRefreshRequested += UIController_OnAllInventoryPanelsRefreshRequested;
    }

    private void OnDestroy() {
        UIController.Instance.OnAllInventoryPanelsRefreshRequested -= UIController_OnAllInventoryPanelsRefreshRequested;
    }

    private void UIController_OnAllInventoryPanelsRefreshRequested(object sender, System.EventArgs e) {
        RefreshVisual();
    }

    public void SetBingoBongoStorerConnection(BingoBongoStorer bingoBongoStorerToDisplay) {
        connectedBingoBongoStorer = bingoBongoStorerToDisplay;
    }

    public void RefreshVisual() {
        if(connectedBingoBongoStorer == null) {
            return;
        }

        int currentBingoBongoCount = connectedBingoBongoStorer.GetBingoBongoCount();
        bingoBongoTMP.text = "Bingo Bongo Count: " + currentBingoBongoCount.ToString();
    }
}
