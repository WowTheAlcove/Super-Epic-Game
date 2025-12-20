using UnityEngine;
using Ink.Runtime;
using System;
using System.Collections.Generic;

public class DialogueEvents
{
    public class DialogueStartedEventArgs : EventArgs
    {
        public string KnotName;
    }
    public event EventHandler<DialogueStartedEventArgs> OnEnterDialogue;
    public void InvokeOnEnterDialogueRequested(object sender, string knotName)
    {
        OnEnterDialogue?.Invoke(sender, new DialogueStartedEventArgs()
        {
            KnotName = knotName,
        });
    }

    public event EventHandler OnDialogueStarted;

    public void InvokeOnDialogueStarted(object sender)
    {
        OnDialogueStarted?.Invoke(sender, EventArgs.Empty);
    }
    public event EventHandler OnDialogueEnded;

    public void InvokeOnDialogueEnded(object sender)
    {
        OnDialogueEnded?.Invoke(sender, EventArgs.Empty);
    }
    
    public class DialogueDisplayedEventArgs : EventArgs
    {
        public string DialogueLine;
        public List<Choice> CurrentChoices;
    }
    public event EventHandler<DialogueDisplayedEventArgs> OnDialogueDisplayed;

    public void InvokeOnDialogueDisplayed(object sender, string dialogueLine, List<Choice> currentChoices)
    {
        OnDialogueDisplayed?.Invoke(sender, new DialogueDisplayedEventArgs()
        {
            DialogueLine = dialogueLine,
            CurrentChoices =  currentChoices
        });
    }

    public class ChoiceChosenEventArgs : EventArgs
    {
        public int ChosenChoiceIndex;
    }
    public event EventHandler<ChoiceChosenEventArgs> onChoiceIndexChosen;

    public void InvokeOnChoiceIndexChosen(object sender, int choiceIndex)
    {
        onChoiceIndexChosen?.Invoke(sender, new ChoiceChosenEventArgs()
        {
            ChosenChoiceIndex = choiceIndex
        });
    }

    public class BindInkExternalFunctionEventArgs : EventArgs
    {
        public string functionName;
        public Action action;
    }
    public event EventHandler<BindInkExternalFunctionEventArgs> OnBindInkExternalFunction;

    public void InvokeOnBindInkExternalFunction(object sender, string passedFunctionName, Action passedAction)
    {
        OnBindInkExternalFunction?.Invoke(sender, new BindInkExternalFunctionEventArgs()
        {
            functionName = passedFunctionName,
            action = passedAction
        });
    }

    public class UnbindInkExternalFunctionEventArgs : EventArgs
    {
        public string functionName;
    }
    public  event EventHandler<UnbindInkExternalFunctionEventArgs> OnUnbindInkExternalFunction;

    public void InvokeOnUnbindInkExternalFunction(object sender, string passedFunctionName)
    {
        OnUnbindInkExternalFunction?.Invoke(sender, new UnbindInkExternalFunctionEventArgs()
        {
            functionName = passedFunctionName
        });
    }

    public class UpdateInkVariableEventArgs : EventArgs
    {
        public string variableName;
        public Ink.Runtime.Object newValue;
    }
    public event EventHandler<UpdateInkVariableEventArgs> OnUpdateInkVariable;

    public void InvokeOnUpdateInkVariable(object sender, string passedVariableName, Ink.Runtime.Object passedNewValue)
    {
        OnUpdateInkVariable?.Invoke(sender, new UpdateInkVariableEventArgs()
        {
            variableName = passedVariableName,
            newValue = passedNewValue
        });
    }
}