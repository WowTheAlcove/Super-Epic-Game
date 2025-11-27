using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;


[RequireComponent(typeof(SaveID))]
public class DynamicPrefabStorer : MonoBehaviour
{
    [SerializeField] private string prefabID;
    private SaveID saveIDClassToOverwrite;
    void Awake()
    {
        GenerateGuid();
    }

    private void Start() {
        NetworkManager.Singleton.OnClientConnectedCallback += NetworkManager_OnClientConnectedCallback;
    }

    private void OnDestroy() {
        if (NetworkManager.Singleton != null) {
            NetworkManager.Singleton.OnClientConnectedCallback -= NetworkManager_OnClientConnectedCallback;
        }
    }

    //For late joiners: when a client connects, check if the NetworkObject shouldn't exist anymore
    private void NetworkManager_OnClientConnectedCallback(ulong obj) {
        NetworkObject networkObjectReference = GetComponent<NetworkObject>();
        if (networkObjectReference != null) {
            if (!networkObjectReference.IsSpawned) {
                Destroy(gameObject);
            }
        } else {
            Debug.LogError("DynamicPrefabStorer: NetworkObject component is missing on " + gameObject.name);
        }
    }

    //generates a random GUID for this instance, overwriting the SaveID component's ID.
    private void GenerateGuid() {
        this.saveIDClassToOverwrite = GetComponent<SaveID>();
        saveIDClassToOverwrite.OverwriteID(System.Guid.NewGuid().ToString());
    }

    //gets the prefab ID, which is used to identify the prefab in the saved data
    public string GetPrefabID() {
        return prefabID;
    }

    //gives the saved GUID to the SaveID component so that the DPM can give the saved data to its owner
    public void SetUniqueID(string savedUniqueID) {
        this.saveIDClassToOverwrite = GetComponent<SaveID>();
        saveIDClassToOverwrite.OverwriteID(savedUniqueID);

    }

    //gets the unique ID of the prefab, which is used to identify it in the save data
    public string GetUniqueID() {
        this.saveIDClassToOverwrite = GetComponent<SaveID>();
        return saveIDClassToOverwrite.Id;
    }
}
