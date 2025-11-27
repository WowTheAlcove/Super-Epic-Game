using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public enum ItemType {
    None,
    Weapon,
    Armor,
    Consumable,
    Miscellaneous
}

[CreateAssetMenu(fileName = "NewInventoryItemSO", menuName = "Scriptable Objects/InventoryItemSO", order = 1)]
public class ItemSO : ScriptableObject
{
    public int itemID;
    public string itemName;
    public string description;
    public ItemType itemType;
    public Sprite icon;
    public GameObject worldItemPrefab;
    public GameObject inventoryItemPrefab;
    public GameObject behaviourItemPrefab;
}
