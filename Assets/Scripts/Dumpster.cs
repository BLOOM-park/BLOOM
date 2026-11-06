using UnityEngine;

public class Dumpster : MonoBehaviour
{
    public TrashType dumpsterType;
    public float interactionDistance = 3f;
    private Transform playerTransform;

    private void Start()
    {
        if (GetComponent<MapMarker>() == null)
        {
            MapMarker marker = gameObject.AddComponent<MapMarker>();
            marker.markerType = MapMarker.MarkerType.Dumpster;
            marker.iconSize = 2.3f;
            marker.heightOffset = 8f;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (playerTransform == null)
            {
                GameObject p = GameObject.FindGameObjectWithTag("Player");
                if (p) playerTransform = p.transform;
            }

            if (playerTransform != null && Vector3.Distance(transform.position, playerTransform.position) <= interactionDistance)
            {
                TryDisposeTrash();
            }
        }
    }

    void TryDisposeTrash()
    {
        if (Inventory.instance == null || playerTransform == null) return;

        PlayerPickUpDrop player = playerTransform.GetComponent<PlayerPickUpDrop>();
        if (player == null) return;

        // Check if player is holding the correct bag for this dumpster
        if (player.IsHoldingCorrectBag(dumpsterType))
        {
            // Identify the bag item name to remove
            string bagName = "";
            switch (dumpsterType)
            {
                case TrashType.Recyclable: bagName = "Recycling Bag"; break;
                case TrashType.Glass: bagName = "Glass Bag"; break;
                case TrashType.Organic: bagName = "Organic Bag"; break;
            }

            // Find the bag BEFORE removing to get its flags
            Item bagInInventory = Inventory.instance.items.Find(item => item.name == bagName);
            if (bagInInventory == null)
            {
                Debug.LogError($"Bag '{bagName}' not found in inventory!");
                return;
            }

            bool isTrash = bagInInventory.isTrash;
            bool isRiverTrash = bagInInventory.isRiverTrash;

            // Remove 1 count of that bag
            Item bagToRemove = new Item(bagName, 1, isTrash, isRiverTrash);
            Inventory.instance.RemoveItem(bagToRemove);

            // Unequip only if this was the last bag of that type
            bool hasBagLeft = Inventory.instance.items.Exists(item => item.name == bagName);
            if (!hasBagLeft)
            {
                player.UnequipItem();
            }

            if (QuestManager.instance != null)
            {
                QuestManager.instance.HandleTrashDisposed(isRiverTrash);
            }
        }
        else
        {
            Debug.Log($"Wrong bag! You need a {dumpsterType} bag.");
        }
    }
}
