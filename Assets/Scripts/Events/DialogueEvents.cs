using UnityEngine;
using System;

public class DialogueEvents
{
    public class DialogueEventArgs : EventArgs
    {
        public string KnotName;
    }
    
    public event EventHandler OnEnterDialogue;
    public void InvokeOnEnterDialogue(object sender, string knotName)
    {
        OnEnterDialogue?.Invoke(sender, EventArgs.Empfty);
    }

}