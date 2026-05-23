using UnityEngine;

public class DayNightCycle : MonoBehaviour
{
    [Header("Lighting")]
    public Light directionalLight;

    [Header("Cycle Settings")]
    [Tooltip("The time in hours when the sun rises (e.g., 6.0 for 6 AM)")]
    public float sunriseTime = 6f;
    [Tooltip("The time in hours when the sun sets (e.g., 18.0 for 6 PM)")]
    public float sunsetTime = 18f;

    [Header("Colors")]
    public Gradient lightColor;
    public AnimationCurve lightIntensity;

    void Update()
    {
        if (TimeManager.Instance == null || directionalLight == null) return;

        float timeOfDay = TimeManager.Instance.currentTimeOfDay;
        float timePercent = timeOfDay / 24f;

        UpdateLighting(timePercent);
    }

    void UpdateLighting(float timePercent)
    {
        // Rotate light: 
        // 0 at 6 AM (sunrise), 180 at 6 PM (sunset), 360 at 6 AM next day.
        // Let's assume timePercent ranges from 0 to 1 (00:00 to 24:00).
        // 00:00 -> -90 (midnight)
        // 06:00 -> 0 (sunrise)
        // 12:00 -> 90 (noon)
        // 18:00 -> 180 (sunset)
        
        float sunAngle = (timePercent * 360f) - 90f;
        directionalLight.transform.localRotation = Quaternion.Euler(sunAngle, 170f, 0f);

        // Update color and intensity if configured
        if (lightColor != null && lightColor.colorKeys.Length > 0)
        {
            directionalLight.color = lightColor.Evaluate(timePercent);
        }

        if (lightIntensity != null && lightIntensity.keys.Length > 0)
        {
            directionalLight.intensity = lightIntensity.Evaluate(timePercent);
        }
    }
}
