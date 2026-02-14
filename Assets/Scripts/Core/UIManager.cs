using UnityEngine;
using UnityEngine.UI;
using Dracul.Player;

namespace Dracul.Core
{
    public class UIManager : MonoBehaviour
    {
        [Header("Player References")]
        [SerializeField] private PlayerStats _playerStats;

        [Header("UI Elements")]
        [SerializeField] private Slider _healthSlider;
        [SerializeField] private Slider _bloodSlider;
        [SerializeField] private Image _bloodFillImage; // Optional: to change color when bleeding

        private void Start()
        {
            // Auto-find player if not assigned
            if (_playerStats == null)
            {
                _playerStats = FindObjectOfType<PlayerStats>();
            }

            if (_healthSlider != null && _playerStats != null)
            {
                _healthSlider.maxValue = _playerStats.MaxHealth;
                _healthSlider.value = _playerStats.CurrentHealth;
            }

            if (_bloodSlider != null && _playerStats != null)
            {
                _bloodSlider.maxValue = _playerStats.MaxBlood;
                _bloodSlider.value = _playerStats.CurrentBlood;
            }
        }

        private void Update()
        {
            if (_playerStats == null) return;

            // Update Sliders
            if (_healthSlider != null)
            {
                _healthSlider.value = Mathf.Lerp(_healthSlider.value, _playerStats.CurrentHealth, Time.deltaTime * 5f);
            }

            if (_bloodSlider != null)
            {
                _bloodSlider.value = Mathf.Lerp(_bloodSlider.value, _playerStats.CurrentBlood, Time.deltaTime * 5f);
            }

            // Visual Feedback for Bleeding (Optional)
            /*
            if (_bloodFillImage != null)
            {
                // logic to pulse color if bleeding, etc.
            }
            */
        }
    }
}
