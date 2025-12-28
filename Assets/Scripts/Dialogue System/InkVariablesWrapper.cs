using System.Collections.Generic;
using UnityEngine;
using Ink.Runtime;

public class InkVariablesWrapper
{
    private Dictionary<string, object> myVariables;
    private Story currentStoryObject;
    private bool isListeningToInk;

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
        isListeningToInk = true;
    }

    public void StopListening(Story story)
    {
        currentStoryObject = null;
        story.variablesState.variableChangedEvent -= UpdateVariableStateFromInk;
        isListeningToInk = false;
    }

    //updates a given variable in the dictionary with a value
    public void UpdateVariableState(string name, object value, Story story)
    {
        if (!myVariables.ContainsKey(name) && !myVariables.ContainsKey("SharedVar" + name)) //only update variables that were initialized
        {
            Debug.LogWarning("Variable " + name + " not found in InkVariableWrapper's dictionary");
            return;
        }
        // Debug.Log("Updating Variable: " + name + " in InkVariableWrapper's dictionary to:  " + value);

        //casting the ink.runtime.object to CLR object
        object clrValue = value;
        if (value is Ink.Runtime.Value inkValue)
        {
            clrValue = inkValue.valueObject;
        }

        if (myVariables.ContainsKey(name))
        {
            myVariables[name] = value;
        } else if (myVariables.ContainsKey("SharedVar" + name))
        {
            myVariables["SharedVar" + name] = value;
        }

        if (isListeningToInk)
        {
            story.variablesState.variableChangedEvent -= UpdateVariableStateFromInk;
            SyncVariablesToStory(story); //keeps Ink variables immediately up to date
            story.variablesState.variableChangedEvent += UpdateVariableStateFromInk;
        }
        else
        {
            SyncVariablesToStory(story); //keeps Ink variables immediately up to date
        }
    }
    
    //for while the dialogue is playing, lets ink sync to dictionary and pass most recently opened story object
    public void UpdateVariableStateFromInk(string name, object value)
    {
        if (!myVariables.ContainsKey(name)) //only update variables that were initialized
        {
            Debug.LogWarning("Variable " + name + " not found in InkVariableWrapper's dictionary");
            return;
        }
        
        //casting the ink.runtime.object to CLR object
        object clrValue = value;
        if (value is Ink.Runtime.Value inkValue)
        {
            clrValue = inkValue.valueObject;
        }
        
        // Debug.Log("Updating Variable: " + name + " in InkVariableWrapper's dictionary to:  " + value);

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
    public InkNSerializableVariable[] InkVarsToNSerializableArray()
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
    public void SetVarsFromNSerializableArray(InkNSerializableVariable[] entries)
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
}