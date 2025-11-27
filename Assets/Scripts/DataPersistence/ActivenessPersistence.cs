using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActivenessPersistence : MonoBehaviour, IDataPersistence {
    private string id;
    private bool defaultActiveState;
    private void Awake() {
        defaultActiveState = this.gameObject.activeSelf;

        if (TryGetComponent<SaveID>(out SaveID saveID)) {
            id = saveID.Id;
        } else {
            Debug.LogError($"SaveID component not found on GameObject: {gameObject}");
        }
    }

    public void LoadData(GameData gameData) {
        if (gameData.allSavedActivenesses.TryGetValue(id, out bool isActive)) {//if the id is found in the saved activenesses
            this.gameObject.SetActive(isActive);
        } else {
            // If the ID is not found, use the default active state
            this.gameObject.SetActive(defaultActiveState);
        }
    }

    public void SaveData(ref GameData gameData) {
        gameData.allSavedActivenesses[id] = this.gameObject.activeSelf;
    }
}
