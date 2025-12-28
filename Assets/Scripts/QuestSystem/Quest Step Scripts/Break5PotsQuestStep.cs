using System;
using UnityEngine;

public class Break5PotsQuestStep : QuestStep
{
    private int amtOfPotsBroken = 0;

    private void Awake()
    {
        GameEventsManager.Instance.miscEvents.OnPotBroken += MiscEvents_OnPotBroken; 
        
    }

    private void MiscEvents_OnPotBroken(object sender, EventArgs e)
    {
        amtOfPotsBroken++;
        CheckIfDone();
    }

    private void CheckIfDone()
    {
        if (amtOfPotsBroken >= 5)
        {
            FinishQuestStep();
        }
    }
    
    [Serializable]
    private struct SavedData {
        public int potsBroken;
    }
    protected override string SerializeDataToString()
    {
        SavedData savedData = new SavedData();
        savedData.potsBroken = amtOfPotsBroken;
        return JsonUtility.ToJson(savedData);
    }

    protected override void DeserializeStringToData(string serializedState)
    {
        SavedData savedData = JsonUtility.FromJson<SavedData>(serializedState);
        amtOfPotsBroken = savedData.potsBroken;
    }
}