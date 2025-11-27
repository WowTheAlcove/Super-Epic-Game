using UnityEngine.UI;
using UnityEngine;

public class SettingsPageController : MonoBehaviour
{
    [SerializeField] public Button saveGameButton;
    [SerializeField] private Button loadGameButton;
    [SerializeField] private Button resetGameButton;

    private DataPersistenceManager dataPersistenceManager;

    //connects the menu settings page buttons to the DataPersistenceManager functions
    private void Start() {
        dataPersistenceManager = DataPersistenceManager.Instance;
        if (dataPersistenceManager == null) {
            Debug.LogError("DataPersistenceManager instance not found. Make sure it is initialized before using SettingsPageController.");
            return;
        }

        saveGameButton.onClick.AddListener(() => dataPersistenceManager.SaveGame(SaveFileType.Manual));
        loadGameButton.onClick.AddListener(() => dataPersistenceManager.LoadGame(SaveFileType.Manual));
        resetGameButton.onClick.AddListener(() => dataPersistenceManager.LoadGame(SaveFileType.Auto));
    }
}
