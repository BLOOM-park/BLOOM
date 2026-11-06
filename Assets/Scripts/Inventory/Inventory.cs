using System.Collections;
using System.Collections.Generic;
using UnityEngine;
 
public class Inventory : MonoBehaviour
{
    public static Inventory instance;
    public List<Item> items = new List<Item>();

    [Header("Trash Bag Icons")]
    public Sprite recyclingBagIcon;
    public Sprite glassBagIcon;
    public Sprite organicBagIcon;
 
    void Awake()
    {
        if (instance != null)
            Destroy(gameObject);
        else
            instance = this;
    }
 
    public delegate void OnItemChanged();
    public OnItemChanged onItemChangedCallback;

    public void AddItem(Item itemToAdd)
    {
        bool itemExists = false;
 
        foreach (Item item in items)
        {
            if(item.name == itemToAdd.name)
            {
                item.count += itemToAdd.count;
                // Fix: If the existing item lacks a model but the new one has it, update it.
                if (item.model == null && itemToAdd.model != null)
                {
                    item.model = itemToAdd.model;
                }
                
                // Keep trash flags updated
                if (itemToAdd.isTrash) item.isTrash = true;
                if (itemToAdd.isRiverTrash) item.isRiverTrash = true;
                itemExists = true;
                break;
            }
        }
        if(!itemExists)
        {
            items.Add(itemToAdd);
        }
        
        if(onItemChangedCallback != null)
            onItemChangedCallback.Invoke();
            
        Debug.Log(itemToAdd.count + " " + itemToAdd.name + " added to inventory.");
    }
 
    public void RemoveItem(Item itemToRemove)
    {
        foreach (var item in items)
        {
            if(item.name == itemToRemove.name)
            {
                item.count -= itemToRemove.count;
                if(item.count <= 0)
                {
                    items.Remove(item);
                }
                break;
            }
        }

        if(onItemChangedCallback != null)
            onItemChangedCallback.Invoke();

        Debug.Log(itemToRemove.count + " " + itemToRemove.name + " removed from inventory.");
    }
}
