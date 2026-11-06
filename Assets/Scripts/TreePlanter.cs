using System.Collections;
using UnityEngine;

public class TreePlanter : MonoBehaviour
{
    public KeyCode plantKey = KeyCode.E;
    
    public static event System.Action OnTreePlanted;

    public GameObject saplingPrefab;
    public GameObject youngPrefab;
    public GameObject maturePrefab;

    [Header("Placement")]
    public float plantingOffset = 2f;
    public float plantingRange = 2f;
    public LayerMask plantingZoneLayer;
    public LayerMask treeLayer;
    public LayerMask groundLayer;       // NEW: only hits solid ground
    public float treeProximityRadius = 1f;
    public float groundCheckDistance = 1.2f;
    public float plantingCooldown = 0.25f;

    private float lastPlantTime = -10f;
    private Collider[] playerColliders;

    private void Awake()
    {
        playerColliders = GetComponentsInChildren<Collider>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(plantKey))
            TryPlantTree();
    }

    void TryPlantTree()
    {
        if (Time.time < lastPlantTime + plantingCooldown) return;

        // Check if player is holding Sapling
        PlayerPickUpDrop player = GetComponent<PlayerPickUpDrop>();
        if (player == null || !player.IsHoldingItem("Sapling"))
        {
            Debug.Log("Must hold Sapling to plant.");
            return;
        }

        if (!IsPlayerGrounded())
        {
            Debug.Log("Can't plant while in air.");
            return;
        }

        // compute forward planting point
        Vector3 forward = transform.forward;
        Vector3 candidate = transform.position + forward.normalized * plantingOffset;

        // raycast down, ignoring triggers and using groundLayer
        if (!Physics.Raycast(candidate + Vector3.up * 2f, Vector3.down, out RaycastHit hit, 5f, groundLayer, QueryTriggerInteraction.Ignore))
        {
            Debug.Log("No ground detected below planting point.");
            return;
        }
        candidate = hit.point;

        // check planting zone
        Collider[] zones = Physics.OverlapSphere(candidate, plantingRange * 0.5f, plantingZoneLayer, QueryTriggerInteraction.Collide);
        if (zones.Length == 0)
        {
            Debug.Log("Not inside a planting zone.");
            return;
        }

        // check for nearby trees
        Collider[] nearbyTrees = Physics.OverlapSphere(candidate, treeProximityRadius, treeLayer, QueryTriggerInteraction.Collide);
        if (nearbyTrees.Length > 0)
        {
            Debug.Log("Can't plant here, too close to another tree.");
            return;
        }

        // instantiate sapling
        GameObject newTree = Instantiate(saplingPrefab, candidate, Quaternion.identity);
        TreeGrowth growth = newTree.AddComponent<TreeGrowth>();
        growth.youngPrefab = youngPrefab;
        growth.maturePrefab = maturePrefab;

        // match layer safely
        int treeLayerIndex = treeLayer.value > 0 ? Mathf.RoundToInt(Mathf.Log(treeLayer.value, 2)) : gameObject.layer;
        if (treeLayerIndex < 0 || treeLayerIndex > 31) treeLayerIndex = gameObject.layer;
        SetGameObjectAndChildrenLayer(newTree, treeLayerIndex);

        // ignore collisions with player
        IgnoreCollisionsBetween(newTree, playerColliders);

        lastPlantTime = Time.time;
        Debug.Log("Tree planted successfully.");
        
        OnTreePlanted?.Invoke();
    }

    bool IsPlayerGrounded()
    {
        return Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, groundCheckDistance, groundLayer, QueryTriggerInteraction.Ignore);
    }

    void IgnoreCollisionsBetween(GameObject treeObj, Collider[] others)
    {
        Collider[] treeCols = treeObj.GetComponentsInChildren<Collider>();
        foreach (var t in treeCols)
            foreach (var o in others)
                if (t && o)
                    Physics.IgnoreCollision(t, o);
    }

    void SetGameObjectAndChildrenLayer(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform t in go.GetComponentsInChildren<Transform>(true))
            t.gameObject.layer = layer;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Vector3 candidate = transform.position + transform.forward * plantingOffset;
        Gizmos.DrawWireSphere(candidate, treeProximityRadius);
    }
}
