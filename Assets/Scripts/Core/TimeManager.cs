using UnityEngine;

public class TimeManager : MonoBehaviour
{
    public static TimeManager Instance;

    [Header("Time Settings")]
    [Tooltip("How many real-time seconds it takes for a full in-game day to pass.")]
    public float timeMultiplier = 3600f; // Default: 1 hour real time = 24 hours in-game (so 1 sec = 24 in-game seconds, wait, 3600 / 24 = 150 times faster? Actually let's use a simple multiplier).
    
    // Better logic: 1 day = 24 hours. Let's define the multiplier as:
    // (real seconds per day) = 86400 / timeMultiplier
    [Tooltip("Multiplier for time passage. 1 means real-time. 60 means 60x faster.")]
    public float timeScale = 60f; 

    [Range(0, 24)]
    public float currentTimeOfDay = 6f; // Start at 6:00 AM

    public int Hours { get; private set; }
    public int Minutes { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        // Update time
        currentTimeOfDay += (Time.deltaTime / 3600f) * timeScale;
        
        if (currentTimeOfDay >= 24f)
        {
            currentTimeOfDay %= 24f;
        }

        // Calculate hours and minutes
        Hours = Mathf.FloorToInt(currentTimeOfDay);
        Minutes = Mathf.FloorToInt((currentTimeOfDay - Hours) * 60f);

        // Update UI if available
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateTimeText(Hours, Minutes);
        }
    }
}
