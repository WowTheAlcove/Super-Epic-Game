using System;
using Ink.Runtime;
using UnityEngine;

public class StoryWrapper
{
    public Story Story { get; private set; }

    public StoryWrapper(TextAsset storyJson)
    {
        Story = new Story(storyJson.text);
    }

    public void BindExternalFunction(string inkFunctionName, Action action)
    {
        Story.BindExternalFunction(inkFunctionName, action);
    }

    public void UnbindExternalFunction(string inkFunctionName)
    {
        Story.UnbindExternalFunction(inkFunctionName);
    }
}
