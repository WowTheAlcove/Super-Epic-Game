//NEW - entire file
using System;
using Unity.Netcode;
using UnityEngine;

[Serializable]
public struct InkNSerializableVariable : INetworkSerializable
{
    public string name;
    public byte type; // 0=int, 1=float, 2=bool, 3=string
    public int intValue;
    public float floatValue;
    public bool boolValue;
    public string stringValue;

    //stuff I don't fully understand that makes sure its serializable
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref name);
        serializer.SerializeValue(ref type);
        serializer.SerializeValue(ref intValue);
        serializer.SerializeValue(ref floatValue);
        serializer.SerializeValue(ref boolValue);
        if (stringValue == null) //since can't serialize null stuff
        {
            stringValue = string.Empty;
        }
        serializer.SerializeValue(ref stringValue);
    }

    //checks what the type of the variable is, then returnsn the appropriate field
    public object GetValue()
    {
        return type switch
        {
            0 => intValue,
            1 => floatValue,
            2 => boolValue,
            3 => stringValue,
            _ => null
        };
    }

    //returns a new InkNSerializableVariable from a variable name and value
    public static InkNSerializableVariable FromValue(string varName, object value)
    {
        var newNSVariable = new InkNSerializableVariable { name = varName };

        switch (value)
        {
            case int i:
                newNSVariable.type = 0;
                newNSVariable.intValue = i;
                break;
            case float f:
                newNSVariable.type = 1;
                newNSVariable.floatValue = f;
                break;
            case bool b:
                newNSVariable.type = 2;
                newNSVariable.boolValue = b;
                break;
            case string s:
                newNSVariable.type = 3;
                newNSVariable.stringValue = s;
                break;
            default:
                Debug.LogWarning($"Unsupported ink type for '{varName}': {value?.GetType()}");
                break;
        }

        return newNSVariable;
    }
}