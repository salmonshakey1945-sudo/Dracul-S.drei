using UnityEngine;

namespace Dracul.Core
{
    public class TimeManager : MonoBehaviour
    {
        [Header("Time Settings")]
        [SerializeField] private float _dayDurationSeconds = 300f; // 5 minutes per full cycle
        [SerializeField, Range(0, 1)] private float _currentTime = 0.5f; // 0=Midnight, 0.5=Noon

        [Header("Sun Settings")]
        [SerializeField] private Light _sunLight;
        [SerializeField] private float _intensityDay = 1.0f;
        [SerializeField] private float _intensityNight = 0.0f;
        [SerializeField] private AnimationCurve _lightIntensityCurve;
        
        // simple IsDay check: 0.25 to 0.75 is roughly day
        public bool IsDay => (_currentTime > 0.25f && _currentTime < 0.75f);

        private void Update()
        {
            float timeStep = Time.deltaTime / _dayDurationSeconds;
            _currentTime += timeStep;

            if (_currentTime >= 1.0f)
                _currentTime = 0.0f;

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
        }
    }
}
