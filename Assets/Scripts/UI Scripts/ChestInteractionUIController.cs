using UnityEngine;
using UnityEngine.UI;

public class ChestInteractionUIController : MonoBehaviour, IShouldHideOnMenuToggle
{
    [SerializeField] private InventoryDisplayer userInventoryInvDisplayer;
    [SerializeField] private InventoryDisplayer chestInvDisplayer;
    [SerializeField] private Button closeButton;
    //[SerializeField] private InventoryDisplayer userHotbarInvDisplayer;

    private void Awake() {
        Hide();

        closeButton.onClick.AddListener(() => {
            Hide();
        });
    }

    private void Start() {
        UIController.Instance.OnAllInventoryPanelsHideRequested += UIController_OnAllInventoryPanelsHideRequested;
    }

    private void UIController_OnAllInventoryPanelsHideRequested(object sender, System.EventArgs e) {
        Hide();
    }
    public void Show() {
        this.gameObject.SetActive(true);

    }

    public void Hide() {
        this.gameObject.SetActive(false);
    }

    public void SetUserInventorySS(InventoryStateStorer issToSet) {
        userInventoryInvDisplayer.SetCurrentInventoryStateStorer(issToSet);
    }
    public void SetChestSS(InventoryStateStorer issToSet) {
        chestInvDisplayer.SetCurrentInventoryStateStorer(issToSet);
    }

    //public void SetUserHotbarSS(InventoryStateStorer issToSet) {
    //    userHotbarInvDisplayer.SetCurrentInventoryStateStorer(issToSet);
    //}

    public bool IsActive() {
        return this.gameObject.activeSelf;
    }

}
