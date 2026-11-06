using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pickup : MonoBehaviour
{
    public Item item = new Item("Item Name", 1, false, false);
    public Sprite icon; 
    public GameObject itemModel;

    private bool isPlayerInRange = false;
    private PlayerPickUpDrop playerController;

    private TrashItem cachedTrashScript;
    private bool hasCheckedTrashScript = false;

    void Start()
    {
        // Cache trash script reference early
        GetTrashScript();
    }

    void Update()
    {
        if (isPlayerInRange)
        {
            bool canPick = CanBePickedUp();

            if (canPick)
            {
                if (PickupPromptManager.Instance != null)
                {
                    PickupPromptManager.Instance.RegisterPickup(this);
                }

                if (Input.GetKeyDown(KeyCode.E))
                {
                    AttemptPickup();
                }
            }
            else
            {
                if (PickupPromptManager.Instance != null)
                {
                    PickupPromptManager.Instance.UnregisterPickup(this);
                }
            }
        }
    }

    /// <summary>
    /// Checks whether the player is currently able to pick up this item.
    /// For trash items: player must be holding the Grabber tool.
    /// For normal items: player just needs to be in range.
    /// </summary>
    public bool CanBePickedUp()
    {
        if (!isPlayerInRange) return false;

        // Ensure playerController reference is available
        if (playerController == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerController = playerObj.GetComponentInParent<PlayerPickUpDrop>() 
                    ?? playerObj.GetComponent<PlayerPickUpDrop>();
            }
        }

        if (playerController == null) return false;

        TrashItem trashScript = GetTrashScript();
        if (trashScript != null)
        {
            // For trash, the player MUST have the Grabber equipped!
            return playerController.IsHoldingGrabber;
        }

        // Generic items can be picked up normally
        return true;
    }

    /// <summary>
    /// Returns a human-friendly name for this item.
    /// </summary>
    public string GetItemDisplayName()
    {
        if (item != null && !string.IsNullOrEmpty(item.name) && item.name != "Item Name")
        {
            return item.name;
        }

        TrashItem trashScript = GetTrashScript();
        if (trashScript != null)
        {
            switch (trashScript.trashType)
            {
                case TrashType.Recyclable: return "Recyclable Trash";
                case TrashType.Glass: return "Glass Trash";
                case TrashType.Organic: return "Organic Trash";
            }
        }

        if (itemModel != null) return itemModel.name;
        return gameObject.name.Replace("(Clone)", "").Trim();
    }

    /// <summary>
    /// Safely finds and caches any TrashItem component associated with this pickup.
    /// </summary>
    public TrashItem GetTrashScript()
    {
        if (hasCheckedTrashScript) return cachedTrashScript;

        hasCheckedTrashScript = true;

        if (itemModel != null)
        {
            cachedTrashScript = itemModel.GetComponent<TrashItem>()
                ?? itemModel.GetComponentInChildren<TrashItem>()
                ?? itemModel.GetComponentInParent<TrashItem>();
        }

        if (cachedTrashScript == null)
        {
            cachedTrashScript = GetComponent<TrashItem>()
                ?? GetComponentInChildren<TrashItem>()
                ?? GetComponentInParent<TrashItem>();
        }

        return cachedTrashScript;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = true;
            playerController = other.GetComponentInParent<PlayerPickUpDrop>() 
                ?? other.GetComponent<PlayerPickUpDrop>();

            if (CanBePickedUp() && PickupPromptManager.Instance != null)
            {
                PickupPromptManager.Instance.RegisterPickup(this);
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;
            playerController = null;

            if (PickupPromptManager.Instance != null)
            {
                PickupPromptManager.Instance.UnregisterPickup(this);
            }
        }
    }

    void OnDisable()
    {
        if (PickupPromptManager.HasInstance)
        {
            PickupPromptManager.Instance.UnregisterPickup(this);
        }
    }

    void OnDestroy()
    {
        if (PickupPromptManager.HasInstance)
        {
            PickupPromptManager.Instance.UnregisterPickup(this);
        }
    }

    void AttemptPickup()
    {
        Debug.Log("Pickup: Attempting pickup...");

        if (playerController == null) 
        {
            Debug.LogError("Pickup: playerController is null! Make sure the Player has PlayerPickUpDrop script and entered the trigger.");
            return;
        }

        TrashItem trashScript = GetTrashScript();

        if (trashScript != null) // It is trash
        {
            Debug.Log($"Pickup: Item is Trash (Type: {trashScript.trashType}). Player equipped: '{playerController.CurrentEquippedItemName}'");

            if (playerController.IsHoldingGrabber)
            {
                string bagName = "";
                switch (trashScript.trashType)
                {
                    case TrashType.Recyclable: bagName = "Recycling Bag"; break;
                    case TrashType.Glass: bagName = "Glass Bag"; break;
                    case TrashType.Organic: bagName = "Organic Bag"; break;
                }

                Debug.Log($"Pickup: Trash logic. Bag to give: '{bagName}'");

                if (!string.IsNullOrEmpty(bagName))
                {
                    Sprite bagIcon = null;
                    if (Inventory.instance != null)
                    {
                        switch (trashScript.trashType)
                        {
                            case TrashType.Recyclable: bagIcon = Inventory.instance.recyclingBagIcon; break;
                            case TrashType.Glass: bagIcon = Inventory.instance.glassBagIcon; break;
                            case TrashType.Organic: bagIcon = Inventory.instance.organicBagIcon; break;
                        }
                    }
                    else
                    {
                        Debug.LogError("Pickup: Inventory instance is null!");
                    }

                    Item bagItem = new Item(bagName, 1, trashScript.isTrash, trashScript.isRiverTrash, bagIcon); 
                    Inventory.instance.AddItem(bagItem);

                    // Unregister prompt before destroying
                    if (PickupPromptManager.HasInstance)
                    {
                        PickupPromptManager.Instance.UnregisterPickup(this);
                    }

                    Destroy(gameObject);
                }
                else
                {
                    Debug.LogError($"Pickup: Failed to map TrashType {trashScript.trashType} to a bag name.");
                }
            }
            else
            {
                Debug.Log("Pickup: FAIL - Player is NOT holding grabber.");
            }
        }
        else // Not trash (generic item pickup)
        {
            Debug.Log("Pickup: Item is NOT trash. Picking up normally. ItemModel: " + (itemModel != null ? itemModel.name : "null"));
            Inventory.instance.AddItem(item);

            if (PickupPromptManager.HasInstance)
            {
                PickupPromptManager.Instance.UnregisterPickup(this);
            }

            Destroy(gameObject);
        }
    }
}
