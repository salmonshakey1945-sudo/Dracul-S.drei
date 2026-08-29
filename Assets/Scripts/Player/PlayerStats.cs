using UnityEngine;

namespace Dracul.Player
{
    public class PlayerStats : MonoBehaviour
    {
        [Header("Status Settings")]
        [SerializeField] private float _maxHealth = 100f;
        [SerializeField] private float _maxBlood = 100f;
        [SerializeField] private float _sunlightBloodDecayRate = 1.0f; // 日光によるブラッドゲージ減少量
        [SerializeField] private float _healBloodDecayRate = 2.0f; // 体力回復のためのブラッドゲージ減少量
        [SerializeField] private float _healRate = 5.0f; // ブラッドゲージ減少による体力回復量
        [SerializeField] private float _bleedingRate = 2.0f; // Extra blood loss when bleeding

        [Header("Debug / View")]
        [SerializeField] private float _currentHealth;
        [SerializeField] private float _currentBlood;
        [SerializeField] private bool _isBleeding;
        [SerializeField] private bool _isWeakened; // From sunlight or low blood
        private bool _isDead = false;

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
            if (_isDead) return;

            HandleBloodDecay();
            HandleRegeneration();
            CheckWeakness();
        }

        private void HandleBloodDecay()
        {
            float decay = 0f;
            if (_isBleeding) decay += _bleedingRate;

            if (decay > 0f)
            {
                _currentBlood -= decay * Time.deltaTime;
                _currentBlood = Mathf.Clamp(_currentBlood, 0, _maxBlood);
            }

            if (_currentBlood <= 0)
            {
                // Trigger Starvation/Weakness State
                // TODO: Apply movement penalty
            }
        }

        private void HandleRegeneration()
        {
            if (_currentHealth < _maxHealth && _currentBlood > 0)
            {
                float healAmount = _healRate * Time.deltaTime;
                float bloodRequired = _healBloodDecayRate * Time.deltaTime;

                if (_currentHealth + healAmount > _maxHealth)
                {
                    float proportion = (_maxHealth - _currentHealth) / healAmount;
                    healAmount *= proportion;
                    bloodRequired *= proportion;
                }

                if (_currentBlood < bloodRequired)
                {
                    float proportion = _currentBlood / bloodRequired;
                    healAmount *= proportion;
                    bloodRequired = _currentBlood;
                }

                _currentBlood -= bloodRequired;
                _currentHealth += healAmount;

                _currentBlood = Mathf.Clamp(_currentBlood, 0, _maxBlood);
                _currentHealth = Mathf.Clamp(_currentHealth, 0, _maxHealth);
            }
        }

        private void CheckWeakness()
        {
             // 0 Blood or Sunlight (handled externally) triggers weakness
             _isWeakened = (_currentBlood <= 0);
        }

        public void TakeDamage(float amount)
        {
            if (_isDead) return;

            _currentHealth -= amount;
            // Chance to bleed? Or always bleed on hit?
            // "Damage causes bleeding"
            _isBleeding = true; 

            if (_currentHealth <= 0)
            {
                _isDead = true;
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
            
            _currentBlood -= _sunlightBloodDecayRate * Time.deltaTime;
            _currentBlood = Mathf.Clamp(_currentBlood, 0, _maxBlood);

            // Sunlight also causes weakness
            _isWeakened = true;
        }

        private void Die()
        {
            Debug.Log("Player Destroyed (Functionally Disabled)");
            EnableRagdoll();
            // TODO: Trigger Game Over or Respawn
        }

        private void EnableRagdoll()
        {
            // 1. アニメーターを無効化して物理演算に任せる
            Animator animator = GetComponentInChildren<Animator>();
            if (animator != null)
            {
                animator.enabled = false;
            }

            // 2. プレイヤーの操作・制御スクリプトを無効化する
            if (TryGetComponent<StarterAssets.ThirdPersonController>(out var thirdPersonController))
            {
                thirdPersonController.enabled = false;
            }
            if (TryGetComponent<StarterAssets.StarterAssetsInputs>(out var starterInputs))
            {
                starterInputs.enabled = false;
            }
            if (TryGetComponent<UnityEngine.InputSystem.PlayerInput>(out var playerInput))
            {
                playerInput.enabled = false;
            }
            if (TryGetComponent<PlayerAttack>(out var playerAttack))
            {
                playerAttack.enabled = false;
            }
            if (TryGetComponent<Dracul.PhysicsEffects.PlayerKnockback>(out var playerKnockback))
            {
                playerKnockback.enabled = false;
            }
            if (TryGetComponent<PlayerInteract>(out var playerInteract))
            {
                playerInteract.enabled = false;
            }
            if (TryGetComponent<GunIKController>(out var gunIK))
            {
                gunIK.enabled = false;
            }
            if (TryGetComponent<PlayerController>(out var controller))
            {
                controller.enabled = false;
            }

            // 3. 親のコライダー（CharacterController含む）とRigidbodyを無効化（骨のコライダーと干渉させないため）
            Collider mainCollider = GetComponent<Collider>();
            if (mainCollider != null)
            {
                mainCollider.enabled = false;
            }
            
            Rigidbody mainRb = GetComponent<Rigidbody>();
            if (mainRb != null)
            {
                mainRb.isKinematic = true;
                mainRb.detectCollisions = false;
            }

            // 4. 子オブジェクト（骨）の全Rigidbodyを有効化して重力で落下させる
            Rigidbody[] ragdollRigidbodies = GetComponentsInChildren<Rigidbody>();
            foreach (Rigidbody rb in ragdollRigidbodies)
            {
                if (rb == mainRb) continue;

                rb.isKinematic = false;
                rb.useGravity = true;
            }
        }
    }
}
