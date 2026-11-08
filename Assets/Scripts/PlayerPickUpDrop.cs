using UnityEngine;

public class PlayerPickUpDrop : MonoBehaviour
{
    [Header("References")]
    public Transform playerCameraTransform;
    public Collider objectGrabZone;
    public LayerMask pickUpLayerMask;
    public float pickUpDistance = 4f;

    [Header("Tools & Bags Hand Models")]
    public GameObject grabberModel;
    public GameObject recyclingBagModel;
    public GameObject glassBagModel;
    public GameObject organicBagModel;
    public GameObject wateringCanModel;
    public GameObject saplingModel;

    private int lastIndex = -1;
    private string currentEquippedItemName = "";
    private InventoryUI cachedInventoryUI;

    private void UpdateHotbarUI()
    {
        if (cachedInventoryUI == null) cachedInventoryUI = FindFirstObjectByType<InventoryUI>();
        if (cachedInventoryUI != null) cachedInventoryUI.SetSelectedSlot(lastIndex);
    }

    public string CurrentEquippedItemName => currentEquippedItemName;

    public bool IsHoldingGrabber => currentEquippedItemName == "Grabber";

    public bool IsHoldingItem(string itemName)
    {
        return currentEquippedItemName == itemName;
    }
    
    // Helper to reference the correct bag model for pickup script
    public bool IsHoldingCorrectBag(TrashType type)
    {
        switch (type)
        {
            case TrashType.Recyclable: return currentEquippedItemName == "Recycling Bag";
            case TrashType.Glass: return currentEquippedItemName == "Glass Bag";
            case TrashType.Organic: return currentEquippedItemName == "Organic Bag";
            default: return false;
        }
    }

    private void Start()
    {
        // Ensure all tools are disabled at start
        DisableAllTools();
        UpdateHotbarUI();
    }

    private void Update()
    {
        // Hotbar selection
        for (int i = 0; i < 7; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                EquipItem(i);
            }
        }
    }

    private void EquipItem(int index)
    {   
        if (index >= 0 && index < Inventory.instance.items.Count)
        {
            Item item = Inventory.instance.items[index];
            
            // If selecting the same item, unequip it
            if (lastIndex == index)
            {
                DisableAllTools();
                lastIndex = -1;
                currentEquippedItemName = "";
                UpdateHotbarUI();
                return;
            }

            // Switch tools
            DisableAllTools();
            lastIndex = index;
            currentEquippedItemName = item.name;
            UpdateHotbarUI();

            Debug.Log($"Equipping Item: '{item.name}' at index {index}");

            // Enable the correct model based on Item Name
            switch (item.name)
            {
                case "Grabber":
                    if (grabberModel) grabberModel.SetActive(true);
                    else Debug.LogWarning("Grabber Model is not assigned in Inspector!");
                    break;
                case "Recycling Bag":
                    if (recyclingBagModel) recyclingBagModel.SetActive(true);
                    else Debug.LogWarning("Recycling Bag Model is not assigned!");
                    break;
                case "Glass Bag":
                    if (glassBagModel) glassBagModel.SetActive(true);
                    else Debug.LogWarning("Glass Bag Model is not assigned!");
                    break;
                case "Organic Bag":
                    if (organicBagModel) organicBagModel.SetActive(true);
                    else Debug.LogWarning("Organic Bag Model is not assigned!");
                    break;
                case "Watering Can":
                    if (wateringCanModel) wateringCanModel.SetActive(true);
                    else Debug.LogWarning("Watering Can Model is not assigned!");
                    break;
                case "Sapling": 
                    if (saplingModel) saplingModel.SetActive(true);
                    else Debug.LogWarning("Sapling Model is not assigned!");
                    break;
                default:
                    Debug.Log($"Equipped '{item.name}', but it does not match 'Grabber' or 'Sapling' etc. Check exact spelling.");
                    break;
            }
        }
    }

    private void DisableAllTools()
    {
        if (grabberModel) grabberModel.SetActive(false);
        if (recyclingBagModel) recyclingBagModel.SetActive(false);
        if (glassBagModel) glassBagModel.SetActive(false);
        if (organicBagModel) organicBagModel.SetActive(false);
        if (wateringCanModel) wateringCanModel.SetActive(false);
        if (saplingModel) saplingModel.SetActive(false);
    }

    // Public method to unequip the current item
    public void UnequipItem()
    {
        DisableAllTools();
        lastIndex = -1;
        currentEquippedItemName = "";
        UpdateHotbarUI();
    }
}
