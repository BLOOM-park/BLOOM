using UnityEngine;
using System.Collections;

public class TreeGrowth : MonoBehaviour
{
    public GameObject youngPrefab;
    public GameObject maturePrefab;
    public float secondsPerStage = 10f; // 10 min

    // internal stage tracker: 0 = sapling, 1 = young
    private int stage = 0;

    private void Start()
    {
        StartCoroutine(GrowthCycle());
    }

    IEnumerator GrowthCycle()
    {
        yield return new WaitForSeconds(secondsPerStage);
        if (stage == 0 && youngPrefab != null)
        {
            ReplaceWithNext(youngPrefab, 1);
        }
        else if (stage == 1 && maturePrefab != null)
        {
            ReplaceWithNext(maturePrefab, 2);
        }
    }

    // Called when replacing prefab
    void ReplaceWithNext(GameObject nextPrefab, int nextStage)
    {
        GameObject newTree = Instantiate(nextPrefab, transform.position, transform.rotation);

        // transfer TreeGrowth script for continued growth
        TreeGrowth newGrowth = newTree.AddComponent<TreeGrowth>();
        newGrowth.youngPrefab = youngPrefab;
        newGrowth.maturePrefab = maturePrefab;
        newGrowth.secondsPerStage = secondsPerStage;
        newGrowth.stage = nextStage;

        // keep layer consistent
        newTree.layer = gameObject.layer;
        foreach (Transform t in newTree.GetComponentsInChildren<Transform>(true))
            t.gameObject.layer = gameObject.layer;

        Destroy(gameObject);
        Debug.Log("Tree grew to next stage.");
    }
}
