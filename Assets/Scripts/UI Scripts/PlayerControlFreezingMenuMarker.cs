using UnityEngine;

public class PlayerControlFreezingMenuMarker : MonoBehaviour
{
    private void OnDisable() {
        if (UIController.Instance != null) {
            UIController.Instance.ControlFreezingMenuDisabled();
        }
    }

    private void OnEnable() {
        if (UIController.Instance != null) {
            UIController.Instance.ControlFreezingMenuEnabled();
        }
    }
}
