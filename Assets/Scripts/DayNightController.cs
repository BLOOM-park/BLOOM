using UnityEngine;

public class DayNightController : MonoBehaviour
{
    public static DayNightController instance;

    public Light sun;
    public float dayDurationSeconds = 120f; // Real seconds for a full day (very fast for testing)
    
    [Range(0, 24)]
    public float currentTime = 6f; // Start at 6 AM

    public float timeMultiplier = 1f;

    private void Awake()
    {
        if (instance != null) Destroy(gameObject);
        else instance = this;
    }

    private void Update()
    {
        // Update time
        currentTime += (Time.deltaTime / dayDurationSeconds) * 24f * timeMultiplier;
        if (currentTime >= 24f)
        {
            currentTime = 0f;
            // In a real day/night cycle, this would wrap around.
            // But here, we might want the day to 'hang' at night until the player sleeps.
            // For now, let's just loop it visually, but the numeric 'Day' is advanced by QuestManager.
        }

        UpdateSunPosition();
    }

    void UpdateSunPosition()
    {
        if (sun != null)
        {
            // Map 0-24 to 0-360 rotation
            // 6 AM = 0 rotation (Horizon)
            // 12 PM = 90 rotation (Zenith)
            // 6 PM = 180 rotation
            // 12 AM = 270 (-90) rotation
            
            float rotation = (currentTime - 6f) * 15f; // 360 / 24 = 15 degrees per hour
            sun.transform.rotation = Quaternion.Euler(rotation, 0f, 0f);
        }
    }

    public void SetTime(float time)
    {
        currentTime = time;
        UpdateSunPosition();
    }
}
