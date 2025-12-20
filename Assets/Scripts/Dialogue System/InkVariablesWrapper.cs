using System.Collections.Generic;
using UnityEngine;
using Ink.Runtime;

public class InkVariablesWrapper
{
    private Dictionary<string, Ink.Runtime.Object> variables;

    //constructor: creates the dictionary. Key: Ink variable name. Value: Default ink variable value
    public InkVariablesWrapper(Story story)
    {
        variables = new Dictionary<string, Ink.Runtime.Object>();
        foreach (string name in story.variablesState) //loops through all the ink variables not declared in a knot
        {
            Ink.Runtime.Object value = story.variablesState.GetVariableWithName(name);
            variables.Add(name, value);
            Debug.Log("Initialized global dialogue variable: " + name + ": " + value);
        }
    }

    //Syncs dictionary to Ink variables, then listens during dialogue to sync Ink to dictionary
    public void SyncVariablesAndStartListening(Story story)
    {
        SyncVariablesToStory(story);
        story.variablesState.variableChangedEvent += UpdateVariableState;
    }

    public void StopListening(Story story)
    {
        story.variablesState.variableChangedEvent -= UpdateVariableState;
    }

    //updates a given variable in the dictionary with a value
    public void UpdateVariableState(string name, Ink.Runtime.Object value)
    {
        if (!variables.ContainsKey(name)) //only update variables that were initialized
        {
            Debug.LogWarning("Variable " + name + " not found in InkVariableWrapper's dictionary");
            return;
        }

        variables[name] = value;
        Debug.Log("Updated dialogue variable: " + name +  ": " + value);

    }

    //sets all Ink variables values to dictionary values
    private void SyncVariablesToStory(Story story)
    {
        foreach (KeyValuePair<string, Ink.Runtime.Object> variable in variables)
        {
            story.variablesState.SetGlobal(variable.Key, variable.Value);
        }
    }
}
