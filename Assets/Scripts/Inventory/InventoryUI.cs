using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    public Transform itemsParent;
    public GameObject inventoryUI;

    private Inventory inventory;
    private InventorySlot[] slots;

    void Start()
    {
        InitializeInventory();
    }

    void InitializeInventory()
    {
        if (inventory == null)
        {
            inventory = Inventory.instance;
            if (inventory != null)
            {
                inventory.onItemChangedCallback -= UpdateUI;
                inventory.onItemChangedCallback += UpdateUI;
            }
        }

        if (slots == null || slots.Length == 0)
        {
            if (itemsParent != null)
                slots = itemsParent.GetComponentsInChildren<InventorySlot>();
            else
                slots = GetComponentsInChildren<InventorySlot>();
        }
    }

    void Update()
    {
        if (PauseMenu.isPaused) return;

        // Ensure initialization in case Inventory.instance initialized after Start
        if (inventory == null && Inventory.instance != null)
        {
            InitializeInventory();
            UpdateUI();
        }

        if (Input.GetKeyDown(KeyCode.I))
        {
            if (inventoryUI != null)
            {
                inventoryUI.SetActive(!inventoryUI.activeSelf);
            }
        }
    }

    void UpdateUI()
    {
        if (slots == null) return;
        if (inventory == null) inventory = Inventory.instance;
        if (inventory == null) return;

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null) continue;

            if (i < inventory.items.Count)
            {
                slots[i].AddItem(inventory.items[i]);
            }
            else
            {
                slots[i].ClearSlot();
            }
        }
    }

    private void OnDestroy()
    {
        if (inventory != null)
        {
            inventory.onItemChangedCallback -= UpdateUI;
        }
    }
}
