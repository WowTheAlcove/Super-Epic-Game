using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SaveID : MonoBehaviour
{
    [Header("Random GUID, may be overwritten if GO needs to be spawned by DPS")]
    [SerializeField] private string id;
    public string Id => id; // Public getter for id

    [ContextMenu("Generate guid for id")]
    private void GenerateGuid() {
        id = System.Guid.NewGuid().ToString();
    }

    public void OverwriteID(string newID) {
        id = newID;
    }
}
