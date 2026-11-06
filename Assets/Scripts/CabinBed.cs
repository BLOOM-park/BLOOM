using UnityEngine;

public class CabinBed : MonoBehaviour
{
    public float interactionDistance = 3f;
    public GameObject[] lightProcessing;
    private Transform playerTransform;
    private int lightsState = 0;

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
                TrySleep();
            }
        }
    }

    void TrySleep()
    {
        if (QuestManager.instance != null)
        {
            QuestManager.instance.TrySleep();
            
            // Cycle light processing states safely
            if (lightProcessing != null && lightProcessing.Length > 0)
            {
                lightsState = (lightsState + 1) % (lightProcessing.Length + 1);
                for (int i = 0; i < lightProcessing.Length; i++)
                {
                    if (lightProcessing[i] != null)
                    {
                        lightProcessing[i].SetActive(i == lightsState - 1);
                    }
                }
            }
        }
    }
}
