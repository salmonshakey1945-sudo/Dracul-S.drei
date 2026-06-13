using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using Dracul.Item;

namespace Dracul.Player
{
    public class PlayerInteract : MonoBehaviour
    {
        [Header("Interact Settings")]
        [Tooltip("調べる・吸血・アイテム取得の有効半径")]
        public float interactRadius = 2.0f;
        [Tooltip("調べられる対象が含まれるレイヤー（何も指定しなければ全て）")]
        public LayerMask interactLayer = ~0;

        [Header("Absorb Process")]
        [Tooltip("完全に吸い切るのにかかる時間")]
        public float absorbDuration = 2.0f;
        [Tooltip("吸血中に再生されるパーティクルエフェクト")]
        public GameObject absorbParticlePrefab;

        private PlayerStats playerStats;
        private PlayerController playerController;
        private PlayerInventory playerInventory;
        private Coroutine absorbCoroutine;
        private bool isAbsorbing = false;
        private GameObject currentAbsorbParticle;

        void Start()
        {
            playerStats = GetComponent<PlayerStats>();
            playerController = GetComponent<PlayerController>();
            playerInventory = GetComponent<PlayerInventory>();

            if (playerInventory == null)
            {
                Debug.LogWarning("[PlayerInteract] PlayerInventory コンポーネントが見つかりません。アイテム取得が機能しません。");
            }
        }

        void Update()
        {
            if (Keyboard.current == null) return;

            // 吸血中の場合
            if (isAbsorbing)
            {
                if (CheckCancelInput())
                {
                    CancelAbsorption();
                }
                return;
            }

            // Fキーが押されたかどうかの判定
            if (Keyboard.current.fKey.wasPressedThisFrame)
            {
                TryInteract();
            }
        }

        private bool CheckCancelInput()
        {
            if (!Keyboard.current.fKey.isPressed) return true;

            return Keyboard.current.wKey.wasPressedThisFrame ||
                   Keyboard.current.aKey.wasPressedThisFrame ||
                   Keyboard.current.sKey.wasPressedThisFrame ||
                   Keyboard.current.dKey.wasPressedThisFrame ||
                   Keyboard.current.leftArrowKey.wasPressedThisFrame ||
                   Keyboard.current.rightArrowKey.wasPressedThisFrame ||
                   Keyboard.current.upArrowKey.wasPressedThisFrame ||
                   Keyboard.current.downArrowKey.wasPressedThisFrame ||
                   Keyboard.current.spaceKey.wasPressedThisFrame;
        }

        private void TryInteract()
        {
            Collider[] colliders = Physics.OverlapSphere(
                transform.position, interactRadius, interactLayer, QueryTriggerInteraction.Collide);

            EnemyHealth closestEnemyAbsorb = null;
            EnemyHealth closestEnemyInvestigate = null;
            ItemPickup closestItemPickup = null;

            float closestAbsorbDist = float.MaxValue;
            float closestInvestigateDist = float.MaxValue;
            float closestItemDist = float.MaxValue;

            foreach (var col in colliders)
            {
                float distance = Vector3.Distance(transform.position, col.transform.position);

                // ── 吸血・調査対象（敵死体）──
                EnemyHealth enemyHealth = col.GetComponentInParent<EnemyHealth>();
                if (enemyHealth == null) enemyHealth = col.GetComponentInChildren<EnemyHealth>();

                if (enemyHealth != null)
                {
                    if (enemyHealth.isAbsorbable && enemyHealth.bloodGiveAmount > 0)
                    {
                        if (distance < closestAbsorbDist)
                        {
                            closestAbsorbDist = distance;
                            closestEnemyAbsorb = enemyHealth;
                        }
                    }
                    else if (enemyHealth.isInvestigable)
                    {
                        if (distance < closestInvestigateDist)
                        {
                            closestInvestigateDist = distance;
                            closestEnemyInvestigate = enemyHealth;
                        }
                    }
                    continue; // 同じオブジェクトを ItemPickup として重複検出しないよう skip
                }

                // ── アイテム取得 ──
                ItemPickup pickup = col.GetComponentInParent<ItemPickup>();
                if (pickup == null) pickup = col.GetComponentInChildren<ItemPickup>();

                if (pickup != null && !pickup.isPickedUp)
                {
                    if (distance < closestItemDist)
                    {
                        closestItemDist = distance;
                        closestItemPickup = pickup;
                    }
                }
            }

            // ── 優先順位: 吸血 > 調査 > アイテム取得 ──
            if (closestEnemyAbsorb != null && playerStats != null)
            {
                Debug.Log("[PlayerInteract] 吸血開始！");
                absorbCoroutine = StartCoroutine(AbsorbRoutine(closestEnemyAbsorb));
            }
            else if (closestEnemyInvestigate != null)
            {
                InvestigateEnemy(closestEnemyInvestigate);
            }
            else if (closestItemPickup != null && playerInventory != null)
            {
                closestItemPickup.Pickup(playerInventory);
            }
            else
            {
                Debug.Log($"[PlayerInteract] インタラクトできる対象が範囲内（半径{interactRadius}m）にいません。");
            }
        }

        private void InvestigateEnemy(EnemyHealth enemy)
        {
            enemy.isInvestigable = false;
            Debug.Log($"[PlayerInteract] {enemy.gameObject.name} の死体を調べた。（アイテムは周囲に落ちているか確認してください）");
        }

        private IEnumerator AbsorbRoutine(EnemyHealth enemy)
        {
            isAbsorbing = true;

            if (playerController != null) playerController.enabled = false;

            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
            }

            if (absorbParticlePrefab != null)
            {
                currentAbsorbParticle = Instantiate(absorbParticlePrefab, enemy.transform.position, Quaternion.identity, enemy.transform);
            }

            float elapsedTime = 0f;
            float totalBloodToGive = enemy.bloodGiveAmount;
            float bloodPerSec = totalBloodToGive / absorbDuration;

            while (elapsedTime < absorbDuration)
            {
                float frameBlood = bloodPerSec * Time.deltaTime;
                if (enemy.bloodGiveAmount < frameBlood)
                {
                    frameBlood = enemy.bloodGiveAmount;
                }

                enemy.bloodGiveAmount -= frameBlood;
                playerStats.Feed(frameBlood);

                elapsedTime += Time.deltaTime;
                yield return null;
            }

            enemy.bloodGiveAmount = 0;
            enemy.CompleteAbsorption();

            EndAbsorption();
        }

        private void CancelAbsorption()
        {
            if (absorbCoroutine != null)
            {
                StopCoroutine(absorbCoroutine);
                absorbCoroutine = null;
            }

            Debug.Log("[PlayerInteract] 吸血がキャンセルされました。（残りの血は再度吸えます）");
            EndAbsorption();
        }

        private void EndAbsorption()
        {
            isAbsorbing = false;

            if (playerController != null) playerController.enabled = true;

            if (currentAbsorbParticle != null)
            {
                Destroy(currentAbsorbParticle);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, interactRadius);
        }
    }
}
