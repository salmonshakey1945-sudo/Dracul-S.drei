using UnityEngine;

namespace Dracul.Core
{
    public class TimeManager : MonoBehaviour
    {
        public static TimeManager Instance;

        [Header("Time Settings")]
        [SerializeField] private float _dayDurationSeconds = 300f; // 5 minutes per full cycle
        [SerializeField, Range(0, 1)] private float _currentTime = 0.5f; // 0=Midnight, 0.5=Noon

        public int Hours { get; private set; }
        public int Minutes { get; private set; }
        public float CurrentTime => _currentTime;

        private void Awake()
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

        [Header("Sun Settings")]
        [SerializeField] private Light _sunLight;
        public Light SunLight => _sunLight;
        [SerializeField] private float _intensityDay = 1.0f;
        [SerializeField] private float _intensityNight = 0.0f;
        [SerializeField] private AnimationCurve _lightIntensityCurve;
        
        [Header("Skybox Settings")]
        [SerializeField] private Material _skyboxMaterial;
        [SerializeField] private Color _daySkyTint = new Color(0.5f, 0.5f, 0.5f);
        [SerializeField] private Color _nightSkyTint = new Color(0.1f, 0.1f, 0.2f);
        [SerializeField] private float _dayExposure = 1.0f;
        [SerializeField] private float _nightExposure = 0.2f;
        [SerializeField] private float _skyTransitionSpeed = 1.0f;
        
        // simple IsDay check: 0.25 to 0.75 is roughly day
        public bool IsDay => (_currentTime > 0.25f && _currentTime < 0.75f);

        private void Update()
        {
            float timeStep = Time.deltaTime / _dayDurationSeconds;
            _currentTime += timeStep;

            if (_currentTime >= 1.0f)
                _currentTime -= 1.0f; // Using -= to maintain smooth timing

            // Calculate hours and minutes for UI
            float timeInHours = _currentTime * 24f;
            Hours = Mathf.FloorToInt(timeInHours);
            Minutes = Mathf.FloorToInt((timeInHours - Hours) * 60f);

            // Update UI
            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateTimeText(Hours, Minutes);
            }

            UpdateSunRotation();
            UpdateLighting();
        }

        private void UpdateSunRotation()
        {
            if (_sunLight != null)
            {
                // Rotate sun 360 degrees based on time
                // -90 (midnight) -> 90 (noon) -> 270 (midnight)
                float angle = (_currentTime * 360f) - 90f;
                _sunLight.transform.rotation = Quaternion.Euler(angle, 170f, 0); 
            }
        }

        private void UpdateLighting()
        {
            if (_sunLight != null)
            {
                // Evaluate intensity
                // If using curve: _sunLight.intensity = _lightIntensityCurve.Evaluate(_currentTime) * _intensityDay;
                // Simple lerp:
                if (IsDay)
                    _sunLight.intensity = Mathf.Lerp(_sunLight.intensity, _intensityDay, Time.deltaTime);
                else
                    _sunLight.intensity = Mathf.Lerp(_sunLight.intensity, _intensityNight, Time.deltaTime);
            }

            if (_skyboxMaterial != null)
            {
                Color targetTint = IsDay ? _daySkyTint : _nightSkyTint;
                float targetExposure = IsDay ? _dayExposure : _nightExposure;

                // Cubemapは _Tint, Proceduralは _SkyTint というプロパティ名を持っています
                if (_skyboxMaterial.HasProperty("_Tint"))
                {
                    Color currentTint = _skyboxMaterial.GetColor("_Tint");
                    _skyboxMaterial.SetColor("_Tint", Color.Lerp(currentTint, targetTint, Time.deltaTime * _skyTransitionSpeed));
                }
                else if (_skyboxMaterial.HasProperty("_SkyTint"))
                {
                    Color currentTint = _skyboxMaterial.GetColor("_SkyTint");
                    _skyboxMaterial.SetColor("_SkyTint", Color.Lerp(currentTint, targetTint, Time.deltaTime * _skyTransitionSpeed));
                }

                if (_skyboxMaterial.HasProperty("_Exposure"))
                {
                    float currentExposure = _skyboxMaterial.GetFloat("_Exposure");
                    _skyboxMaterial.SetFloat("_Exposure", Mathf.Lerp(currentExposure, targetExposure, Time.deltaTime * _skyTransitionSpeed));
                }
            }
        }
    }
}
