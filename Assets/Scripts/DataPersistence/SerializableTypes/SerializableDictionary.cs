using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[System.Serializable]

public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
{
    [SerializeField] private List<TKey> keys = new List<TKey>();
    [SerializeField] private List<TValue> values = new List<TValue>();

    public void OnBeforeSerialize() {
        keys.Clear();
        values.Clear();
        foreach(KeyValuePair<TKey, TValue> kvp in this) {
            keys.Add(kvp.Key);
            values.Add(kvp.Value);
        }
    }

    public void OnAfterDeserialize() {
        this.Clear();

        if (keys.Count != values.Count) {
            Debug.LogError("While trying to deserialize a SerializableDictionary, the amt of keys: " + keys.Count + ", did not match the amt of values: " + values.Count + ". So something went horribly wrong!!");
        }

        for (int i = 0; i < keys.Count; i++) {
            this.Add(keys[i], values[i]);
        }
    }
}
