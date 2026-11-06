using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Item
{
    public string name;
    public int count;
    public Sprite icon;
 
    public GameObject model;

    public bool isTrash;
    public bool isRiverTrash;
 
    public Item(string itemName, int itemCount, bool trash, bool riverTrash, Sprite itemIcon = null, GameObject itemModel = null)
    {
        name = itemName;
        count = itemCount;
        icon = itemIcon;
        model = itemModel;
        isTrash = trash;
        isRiverTrash = riverTrash;
    }
}
