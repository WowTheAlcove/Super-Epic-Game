using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CinemachineScript : MonoBehaviour, IDataPersistence
{
    GameObject boundingShape;
    public void LoadData(GameData gameData) {
        GameObject objWithBoundingShape  = GameObject.Find(gameData.currentBoundingShape);

        if (objWithBoundingShape.TryGetComponent<PolygonCollider2D>(out PolygonCollider2D boundingShape)){
            this.GetComponent<CinemachineConfiner>().m_BoundingShape2D = boundingShape;
        } else {
            Debug.LogError("There was an error trying to find the game object (w/ polygon collider 2d) with the name: " + gameData.currentBoundingShape);
        }
    }

    public void SaveData(ref GameData gameData) {

        if (gameData == null) {
            Debug.LogError("GameData is null in CinemachineScript.SaveData");
            return;
        }
        gameData.currentBoundingShape = GetComponent<CinemachineConfiner>().m_BoundingShape2D.name;

    }
}
