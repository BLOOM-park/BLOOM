using UnityEngine;
using UnityEngine.UI;

public class InventorySlot : MonoBehaviour
{
    public Image icon;
    public Button removeButton;

    Item item;

    public void AddItem(Item newItem)
    {
        item = newItem;
        if (icon != null)
        {
            icon.sprite = item != null ? item.icon : null;
            icon.enabled = icon.sprite != null;
        }
        if (removeButton != null)
        {
            removeButton.interactable = true;
        }
    }

    public void ClearSlot()
    {
        item = null;
        if (icon != null)
        {
            icon.sprite = null;
            icon.enabled = false;
        }
        if (removeButton != null)
        {
            removeButton.interactable = false;
        }
    }

    public void OnRemoveButton()
    {
        if (Inventory.instance != null && item != null)
        {
            Inventory.instance.RemoveItem(item);
        }
    }
}
