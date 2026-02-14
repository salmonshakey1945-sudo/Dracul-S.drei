using UnityEngine;

namespace Dracul.Player
{
    public class PlayerStats : MonoBehaviour
    {
        [Header("Status Settings")]
        [SerializeField] private float _maxHealth = 100f;
        [SerializeField] private float _maxBlood = 100f;
        [SerializeField] private float _bloodDecayRate = 1.0f; // Blood loss per second
        [SerializeField] private float _bleedingRate = 2.0f; // Extra blood loss when bleeding
        [SerializeField] private float _healThreshold = 50f; // Blood needed to auto-heal
        [SerializeField] private float _healRate = 5.0f; // Health recovered per second

        [Header("Debug / View")]
        [SerializeField] private float _currentHealth;
        [SerializeField] private float _currentBlood;
        [SerializeField] private bool _isBleeding;
        [SerializeField] private bool _isWeakened; // From sunlight or low blood

        public float CurrentHealth => _currentHealth;
        public float CurrentBlood => _currentBlood;
        public float MaxHealth => _maxHealth;
        public float MaxBlood => _maxBlood;

        private void Start()
        {
            _currentHealth = _maxHealth;
            _currentBlood = _maxBlood;
        }

        private void Update()
        {
            HandleBloodDecay();
            HandleRegeneration();
            CheckWeakness();
        }

        private void HandleBloodDecay()
        {
            float decay = _bloodDecayRate;
            if (_isBleeding) decay += _bleedingRate;

            _currentBlood -= decay * Time.deltaTime;
            _currentBlood = Mathf.Clamp(_currentBlood, 0, _maxBlood);

            if (_currentBlood <= 0)
            {
                // Trigger Starvation/Weakness State
                // TODO: Apply movement penalty
            }
        }

        private void HandleRegeneration()
        {
            // Auto heal if blood is high enough and not at max health
            if (_currentBlood > _healThreshold && _currentHealth < _maxHealth)
            {
                // Consumes a small amount of extra blood to heal? Or just passive?
                // Reference says: "Heals over time if Blood Gauge is high."
                _currentHealth += _healRate * Time.deltaTime;
                _currentHealth = Mathf.Min(_currentHealth, _maxHealth);
            }
        }

        private void CheckWeakness()
        {
             // 0 Blood or Sunlight (handled externally) triggers weakness
             _isWeakened = (_currentBlood <= 0);
        }

        public void TakeDamage(float amount)
        {
            _currentHealth -= amount;
            // Chance to bleed? Or always bleed on hit?
            // "Damage causes bleeding"
            _isBleeding = true; 

            if (_currentHealth <= 0)
            {
                Die();
            }
        }

        public void Feed(float amount)
        {
            _currentBlood += amount;
            _currentBlood = Mathf.Min(_currentBlood, _maxBlood);
            _isBleeding = false; // Feeding stops bleeding? Design choice or separate item?
        }

        public void ApplySunlightDamage(float damagePerSecond)
        {
            TakeDamage(damagePerSecond * Time.deltaTime);
            // Sunlight also causes weakness
            _isWeakened = true;
        }

        private void Die()
        {
            Debug.Log("Player Destroyed (Functionally Disabled)");
            // TODO: Trigger Game Over or Respawn
        }
    }
}
