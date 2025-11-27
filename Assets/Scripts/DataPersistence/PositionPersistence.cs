using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;
using static UnityEngine.RuleTile.TilingRuleOutput;

public class PositionPersistence : MonoBehaviour, IDataPersistence {

    private string id;

    protected void Awake() {
    }

    private void Start() {
        if (TryGetComponent<SaveID>(out SaveID saveID)) { // Get the ID from the SaveID component from somewhere on the same GameObject
            id = saveID.Id;
        }else {
            //if there's no SaveID component
            Debug.LogError($"PositionPersistence script cannot find SaveID component on GameObject: {gameObject}");
        }
    }

    public void LoadData(GameData gameData) {
        if (TryGetComponent<SaveID>(out SaveID saveID)) { // Get the ID from the SaveID component from somewhere on the same GameObject
            id = saveID.Id;
        } else {
            //if there's no SaveID component
            Debug.LogError($"PositionPersistence script cannot find SaveID component on GameObject: {gameObject}");
        }

        if (gameData.allSavedPositions.TryGetValue(id, out Vector3 savedPosition)) {
            transform.position = savedPosition;
        } else {
            //if there's no saved position for this ID, shouldn't happen I don't think
        }
        
    }
    public void SaveData(ref GameData data) {
        if (TryGetComponent<SaveID>(out SaveID saveID)) { // Get the ID from the SaveID component from somewhere on the same GameObject
            id = saveID.Id;
        } else {
            //if there's no SaveID component
            Debug.LogError($"PositionPersistence script cannot find SaveID component on GameObject: {gameObject}");
        }
        data.allSavedPositions[id] = transform.position;
    }

}
