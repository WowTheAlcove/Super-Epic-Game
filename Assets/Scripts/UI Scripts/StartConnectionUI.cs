using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class StartConnectionUI : MonoBehaviour
{
    [SerializeField] private Button startHostButton;
    [SerializeField] private Button startClientButton;

    private void Awake() {
        startHostButton.onClick.AddListener(OnStartHostButtonClicked);
        startClientButton.onClick.AddListener(OnStartClientButtonClicked);
    }

    private void OnStartHostButtonClicked() {
        NetworkManager.Singleton.StartHost();
        Hide();
    }

    private void OnStartClientButtonClicked() {
        NetworkManager.Singleton.StartClient();
        Hide();
    }

    private void Hide() {
        gameObject.SetActive(false);
        }
    }
