using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
public class AnimalRoaming : MonoBehaviour
{
    [Header("Roaming Settings")]
    [Tooltip("The range (half-extent) of the square area to roam within from the starting position.")]
    [SerializeField] private float roamRange = 10f;
    [Tooltip("Minimum time to wait after reaching a destination.")]
    [SerializeField] private float minWaitTime = 2f;
    [Tooltip("Maximum time to wait after reaching a destination.")]
    [SerializeField] private float maxWaitTime = 5f;
    
    [Header("Animal Sounds")]
    [Tooltip("Audio clips for the animal sounds")]
    [SerializeField] private AudioClip[] animalSounds;
    [Tooltip("Minimum time between animal sounds")]
    [SerializeField] private float minSoundInterval = 5f;
    [Tooltip("Maximum time between animal sounds")]
    [SerializeField] private float maxSoundInterval = 15f;
    [Tooltip("Volume of the animal sounds")]
    [SerializeField] private float soundVolume = 0.5f;
    [Tooltip("Maximum distance at which player can hear the animal sounds")]
    [SerializeField] private float hearingDistance = 30f;
    
    [Header("Debug")]
    [Tooltip("If true, the range gizmo will always be visible in the scene view.")]
    [SerializeField] private bool showGizmosAlways = false;

    private NavMeshAgent agent;
    private Vector3 startPosition;
    private AudioSource audioSource;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        // Use the initial position as the center of the roaming square
        startPosition = transform.position;
        
        // Get or create AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.volume = soundVolume;
    }

    private void Start()
    {
        if (agent != null)
        {
            agent.updateRotation = true;
            agent.updatePosition = true;
        }

        StartCoroutine(RoamRoutine());
        StartCoroutine(PlaySoundsRoutine());
    }

    private IEnumerator RoamRoutine()
    {
        while (true)
        {
            // 1. Wait
            float waitTime = Random.Range(minWaitTime, maxWaitTime);
            yield return new WaitForSeconds(waitTime);

            // 2. Move
            if (agent != null && agent.isOnNavMesh && TryGetRandomPoint(out Vector3 destination))
            {
                agent.SetDestination(destination);

                // Wait until path is calculated or we start moving
                yield return null; 
                
                // Wait until we reach the destination
                while (agent != null && agent.isOnNavMesh && (agent.pathPending || agent.remainingDistance > agent.stoppingDistance))
                {
                    yield return null;
                }
            }
        }
    }

    private bool TryGetRandomPoint(out Vector3 result)
    {
        // Try multiple times to find a valid point on the NavMesh
        for (int i = 0; i < 30; i++)
        {
            // Random point in a square around startPosition
            float randomX = Random.Range(-roamRange, roamRange);
            float randomZ = Random.Range(-roamRange, roamRange);
            
            Vector3 randomPoint = startPosition + new Vector3(randomX, 0, randomZ);

            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomPoint, out hit, 2.0f, NavMesh.AllAreas))
            {
                result = hit.position;
                return true;
            }
        }

        result = Vector3.zero;
        return false;
    }

    private IEnumerator PlaySoundsRoutine()
    {
        while (true)
        {
            // Wait for random interval before playing next sound
            float soundInterval = Random.Range(minSoundInterval, maxSoundInterval);
            yield return new WaitForSeconds(soundInterval);

            // Find player and check distance
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null && audioSource != null)
            {
                float distanceToPlayer = Vector3.Distance(transform.position, playerObject.transform.position);
                
                // Only play sound if player is within hearing distance
                if (distanceToPlayer <= hearingDistance)
                {
                    // Play random animal sound if we have any
                    if (animalSounds != null && animalSounds.Length > 0)
                    {
                        int randomIndex = Random.Range(0, animalSounds.Length);
                        if (animalSounds[randomIndex] != null)
                        {
                            // Calculate volume based on distance (closer = louder)
                            float distanceVolume = soundVolume * (1f - (distanceToPlayer / hearingDistance));
                            audioSource.PlayOneShot(animalSounds[randomIndex], distanceVolume);
                        }
                    }
                }
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (showGizmosAlways)
        {
            DrawGizmos();
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!showGizmosAlways)
        {
            DrawGizmos();
        }
    }

    private void DrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Vector3 center = Application.isPlaying ? startPosition : transform.position;
        Gizmos.DrawWireCube(center, new Vector3(roamRange * 2, 1f, roamRange * 2));
    }
}
