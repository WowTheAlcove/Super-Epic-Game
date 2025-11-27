using UnityEngine;
using System;
using Unity.Netcode;

public class QuestEvents
{
    public class QuestEventArgs : EventArgs {
        public string QuestID;
        public int PlayerIndex;
    }

    public class QuestStateChangeEventArgs : EventArgs {
        public string QuestID;
        public QuestState NewQuestState;
    }

    public event EventHandler<QuestEventArgs> OnStartQuest;
    public event EventHandler<QuestEventArgs> OnAdvanceQuest;
    public event EventHandler<QuestEventArgs> OnFinishQuest;

    public void InvokeStartQuest(object sender, string ID, int playerIndex) {
        OnStartQuest?.Invoke(null, new QuestEventArgs() { QuestID = ID, PlayerIndex = playerIndex });
    }
    public void InvokeAdvanceQuest(object sender, string ID, int playerIndex) {
        OnAdvanceQuest?.Invoke(null, new QuestEventArgs() { QuestID = ID, PlayerIndex = playerIndex });
    }
    public void InvokeFinishQuest(object sender, string ID, int playerIndex) {
        OnFinishQuest?.Invoke(null, new QuestEventArgs() { QuestID = ID, PlayerIndex = playerIndex});
    }

    public event EventHandler<QuestStateChangeEventArgs> OnQuestStateChange;

    public void InvokeQuestStateChange(object sender, string questId, QuestState newQuestState) {
        OnQuestStateChange?.Invoke(sender, new QuestStateChangeEventArgs() { QuestID = questId, NewQuestState = newQuestState});
    }
}
