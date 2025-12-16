using UnityEngine;
using System;

public class DialogueEvents
{
    public class DialogueEventArgs : EventArgs
    {
        public string KnotName;
    }
    
    public event EventHandler<DialogueEventArgs> OnEnterDialogue;
    public void InvokeOnEnterDialogue(object sender, string knotName)
    {
        OnEnterDialogue?.Invoke(sender, new DialogueEventArgs()
        {
            KnotName = knotName
        });
    }

}