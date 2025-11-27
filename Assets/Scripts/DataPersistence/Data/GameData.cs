using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GameData {

    public SerializableDictionary<string, Vector3> kickablePotPositions;
    public string currentBoundingShape;
    public SerializableDictionary<string, InventorySaveData> allInventoriesData;
    public SerializableDictionary<string, Vector3> allSavedPositions;
    public SerializableDictionary<string, bool> allSavedActivenesses;
    public SerializableDictionary<string, string> allDynamicPrefabs;
    public SerializableDictionary<string, PlayerData> allPlayerData;
    public SerializableDictionary<string, QuestSaveData> allQuestData;
    public SerializableDictionary<string, QuestStepData> allQuestStepData;

    //the values in the constructor will be default values for new game
    public GameData() {
        this.kickablePotPositions = new SerializableDictionary<string, Vector3>();
        this.currentBoundingShape = "Town1";
        this.allInventoriesData = new SerializableDictionary<string, InventorySaveData>();
        this.allSavedPositions = new SerializableDictionary<string, Vector3>();
        this.allSavedActivenesses = new SerializableDictionary<string, bool>();
        this.allPlayerData = new SerializableDictionary<string, PlayerData>();
        this.allDynamicPrefabs = new SerializableDictionary<string, string>();
        this.allQuestData = new SerializableDictionary<string, QuestSaveData>();
        this.allQuestStepData = new SerializableDictionary<string, QuestStepData>();

    }
}

#region Custom Data Classes

[System.Serializable]
public class InventorySaveData {
    public List<InventorySlotSaveData> items = new();
}

[System.Serializable]
public class InventorySlotSaveData {
    public int itemID;
}

[System.Serializable]
public class PlayerData {
    public Vector3 playerPositionData;
    public int bingoBongoCountData;
    public InventorySaveData hotbarData;
    public InventorySaveData inventoryData;

    public PlayerData() {
        hotbarData = new InventorySaveData();
        inventoryData = new InventorySaveData();
    }
}

[System.Serializable]
public class QuestSaveData {
    public QuestState state;
    public int currentStepIndex;
}

[System.Serializable]
public class QuestStepData {
    public string questId;
    public int playerIndex;
    public string questStepState;
}

#endregion