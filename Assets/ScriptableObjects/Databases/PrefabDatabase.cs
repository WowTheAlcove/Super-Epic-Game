using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

[CreateAssetMenu(menuName = "Scriptable Objects/PrefabDatabase")]
public class PrefabDatabase : ScriptableObject {
    [System.Serializable]
    public struct PrefabEntry {
        public string prefabID;
        public GameObject prefab;
    }

    public List<PrefabEntry> entries;

    private Dictionary<string, GameObject> prefabDict;

    //public void Init() {
    //    prefabDict = new Dictionary<string, GameObject>();
    //    foreach (var entry in entries) {
    //        prefabDict[entry.prefabID] = entry.prefab;
    //    }
    //}

    public void Init() {
        prefabDict = new Dictionary<string, GameObject>();

        // Make sure Addressables is initialized
        Addressables.InitializeAsync().WaitForCompletion();

        // Load all prefabs that have the label "GamePrefabs"
        var prefabs = Addressables.LoadAssetsAsync<GameObject>("GamePrefabLabel").WaitForCompletion();

        foreach (var prefab in prefabs) {
            // set an id that is just the prefab name + "Prefab"
            if(prefabDict.ContainsKey(prefab.name + "Prefab")) {
                Debug.LogError($"Duplicate prefab name detected: {prefab.name} when instantiating prefab dictionary.");
                continue;
            }

            prefabDict[prefab.name + "Prefab"] = prefab;
        }
    }

    public GameObject GetPrefabFromID(string id) {
        if (prefabDict == null) Init();
        return prefabDict.TryGetValue(id, out var prefab) ? prefab : null;
    }
}
