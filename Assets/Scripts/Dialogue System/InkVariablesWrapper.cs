using System.Collections.Generic;
using UnityEngine;
using Ink.Runtime;

public class InkVariablesWrapper
{
    private Dictionary<string, object> myVariables;
    private Story currentStoryObject;

    //constructor: creates the dictionary. Key: Ink variable name. Value: Default ink variable value
    public InkVariablesWrapper(Story story)
    {
        myVariables = new Dictionary<string, object>();
        foreach (string name in story.variablesState) //loops through all the ink variables not declared in a knot
        {
            object value = story.variablesState[name];
            myVariables.Add(name, value);
        }
    }

    //Syncs dictionary to Ink variables, then listens during dialogue to sync Ink to dictionary
    public void SyncVariablesAndStartListening(Story story)
    {
        SyncVariablesToStory(story);
        currentStoryObject = story;
        story.variablesState.variableChangedEvent += UpdateVariableStateFromInk;
    }

    public void StopListening(Story story)
    {
        currentStoryObject = null;
        story.variablesState.variableChangedEvent -= UpdateVariableStateFromInk;
    }

    //updates a given variable in the dictionary with a value
    public void UpdateVariableState(string name, object value, Story story)
    {
        if (!myVariables.ContainsKey(name)) //only update variables that were initialized
        {
            Debug.LogWarning("Variable " + name + " not found in InkVariableWrapper's dictionary");
            return;
        }
        Debug.Log("Updating Variable: " + name + " in InkVariableWrapper's dictionary to:  " + value);

        object clrValue = value;
        if (value is Ink.Runtime.Value inkValue) //casting the ink.runtime.object to CLR object
        {
            clrValue = inkValue.valueObject;
        }
        
        myVariables[name] = value;
        
        SyncVariablesToStory(story); //keeps Ink variables immediately up to date
    }
    
    //for while the dialogue is playing, lets ink sync to dictionary and pass most recently opened story object
    public void UpdateVariableStateFromInk(string name, object value)
    {
        if (!myVariables.ContainsKey(name)) //only update variables that were initialized
        {
            Debug.LogWarning("Variable " + name + " not found in InkVariableWrapper's dictionary");
            return;
        }
        
        object clrValue = value;
        if (value is Ink.Runtime.Value inkValue) //casting the ink.runtime.object to CLR object
        {
            clrValue = inkValue.valueObject;
        }
        
        Debug.Log("Updating Variable: " + name + " in InkVariableWrapper's dictionary to:  " + value);

        myVariables[name] = clrValue;
    }

    //sets all Ink variables values to dictionary values
    private void SyncVariablesToStory(Story story)
    {
        foreach (KeyValuePair<string, object> variable in myVariables)
        {
            story.variablesState[variable.Key] = variable.Value;
        }
    }
    
    //returns ink variable dictionary in form of InkNSerializableVariable array
    public InkNSerializableVariable[] ToNSerializableArray()
    {
        var serializableVariables = new InkNSerializableVariable[myVariables.Count];
        int i = 0;

        foreach (var kvp in myVariables)
        {
            serializableVariables[i] = InkNSerializableVariable.FromValue(kvp.Key, kvp.Value);
            i++;
        }

        return serializableVariables;
    }

    //Sets dictionary of ink variables from a given InkNSerializableVariable array
    public void FromNSerializableArray(InkNSerializableVariable[] entries)
    {
        myVariables.Clear();

        foreach (var entry in entries)
        {
            myVariables[entry.name] = entry.GetValue();
        }
        
        if (currentStoryObject != null)
        {
            SyncVariablesToStory(currentStoryObject);
        }
    }

    // public void SaveData(ref GameData gameData)
    // {
    //     foreach (KeyValuePair<string, Ink.Runtime.Object> variable in myVariables)
    //     {
    //         InkVariableSaveData inkVariableSaveData = new InkVariableSaveData()
    //         {
    //             inkVariableSerialized = JsonUtility.ToJson(variable.Value)
    //         };
    //         gameData.allInkVariableData.Add(variable.Key, inkVariableSaveData);
    //     }
    // }
    //
    // public void LoadData(ref GameData gameData)
    // {
    //     myVariables.Clear();
    //     foreach (KeyValuePair<string, InkVariableSaveData> variableData in gameData.allInkVariableData)
    //     {
    //         Ink.Runtime.Object deserializedInkVariableValue = JsonUtility.FromJson<Ink.Runtime.Object>(variableData.Value.inkVariableSerialized);
    //         myVariables.Add(variableData.Key, deserializedInkVariableValue);
    //     }
    // }
}