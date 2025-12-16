using UnityEngine;

public class PlayerControlFreezingMenuMarker : MonoBehaviour
{
    private void OnDisable() {
        if (UIController.Instance != null) {
            UIController.Instance.InputFreezingMenuDisabled();
        }
    }

    private void OnEnable() {
        if (UIController.Instance != null) {
            UIController.Instance.InputFreezingMenuEnabled();
        }
    }
}
