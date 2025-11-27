using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/ItemDatabase")]
public class ItemDatabase : ScriptableObject {
    public List<ItemSO> allItems;

    private Dictionary<string, ItemSO> idToItem;



    /// Initializing the dictionary, so that scripts can quickly access items by their ID.
    private void OnEnable() {
        idToItem = new Dictionary<string, ItemSO>();

        if(allItems == null) {
            return;
        }

        foreach (var item in allItems) {
            idToItem[item.itemName] = item;
        }
    }

    //The Function scripts will use to retrive an inventory item SO by its ID
    public ItemSO GetInventoryItemSOByID(string id) {
        if(idToItem.TryGetValue(id, out var item)) {
            //if the item exists in the dictionary, return it's SO
            return item;
        } else {
            //if the item ID is wrong, return null
            Debug.LogError("Tried to get an InvetoryItemSO with an ID that does not exist: " + id);
            return null;
        }
    }
}
