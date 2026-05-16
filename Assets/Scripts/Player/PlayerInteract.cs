using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

namespace Dracul.Player
{
    public class PlayerInteract : MonoBehaviour
    {
        [Header("Interact Settings")]
        [Tooltip("調べる（吸血する）半径")]
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
        private Coroutine absorbCoroutine;
        private bool isAbsorbing = false;
        private GameObject currentAbsorbParticle;

        void Start()
        {
            playerStats = GetComponent<PlayerStats>();
            playerController = GetComponent<PlayerController>(); // 移動キャンセル用
        }

        void Update()
        {
            if (Keyboard.current == null) return;

            // 吸血中の場合
            if (isAbsorbing)
            {
                // キャンセル判定 (WASD, 矢印キー, Space, Fキー)
                if (CheckCancelInput())
                {
                    CancelAbsorption();
                }
                return; // 吸血中ならこれ以上のインタラクトは行わない
            }

            // Fキーが押されたかどうかの判定
            if (Keyboard.current.fKey.wasPressedThisFrame)
            {
                TryAbsorbBlood();
            }
        }

        private bool CheckCancelInput()
        {
            // Fキーが離されたらキャンセルとみなす
            if (!Keyboard.current.fKey.isPressed) return true;

            // いずれかの移動キーが入力されたらキャンセルとみなす
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

        private void TryAbsorbBlood()
        {
            // トリガー設定されたコライダーも確実に検知するように QueryTriggerInteraction.Collide を指定
            Collider[] colliders = Physics.OverlapSphere(transform.position, interactRadius, interactLayer, QueryTriggerInteraction.Collide);

            EnemyHealth closestEnemy = null;
            float closestDistance = float.MaxValue;

            foreach (var col in colliders)
            {
                // 親や子にEnemyHealthがついている場合も考慮
                EnemyHealth enemyHealth = col.GetComponentInParent<EnemyHealth>();
                if (enemyHealth == null) enemyHealth = col.GetComponentInChildren<EnemyHealth>();

                // isAbsorbable が true で、血が残っている敵のみを対象とする
                if (enemyHealth != null && enemyHealth.isAbsorbable && enemyHealth.bloodGiveAmount > 0)
                {
                    float distance = Vector3.Distance(transform.position, col.transform.position);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestEnemy = enemyHealth;
                    }
                }
            }

            if (closestEnemy != null && playerStats != null)
            {
                Debug.Log("[PlayerInteract] 吸血開始！");
                // 吸血処理のコルーチンを開始
                absorbCoroutine = StartCoroutine(AbsorbRoutine(closestEnemy));
            }
            else
            {
                Debug.Log($"[PlayerInteract] 吸血できる敵が範囲内(半径{interactRadius})にいません。");
            }
        }

        private IEnumerator AbsorbRoutine(EnemyHealth enemy)
        {
            isAbsorbing = true;

            // プレイヤーの動きを止める（コンポーネントを一時的にオフ）
            if (playerController != null) playerController.enabled = false;
            
            // 勢いで滑らないように速度をゼロにする（Y軸＝落下は維持）
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
            }

            // 吸血パーティクルの生成
            if (absorbParticlePrefab != null)
            {
                currentAbsorbParticle = Instantiate(absorbParticlePrefab, enemy.transform.position, Quaternion.identity, enemy.transform);
            }

            float elapsedTime = 0f;
            float totalBloodToGive = enemy.bloodGiveAmount;
            // 1秒あたりの血の回復量
            float bloodPerSec = totalBloodToGive / absorbDuration;

            while (elapsedTime < absorbDuration)
            {
                // フレームごとの吸収量を計算
                float frameBlood = bloodPerSec * Time.deltaTime;
                if (enemy.bloodGiveAmount < frameBlood)
                {
                    frameBlood = enemy.bloodGiveAmount;
                }

                enemy.bloodGiveAmount -= frameBlood;
                playerStats.Feed(frameBlood);

                elapsedTime += Time.deltaTime;
                yield return null; // 1フレーム待つ
            }

            // 完全に吸い切った場合
            enemy.bloodGiveAmount = 0; 
            enemy.CompleteAbsorption();
            
            // 全て完了したら状態をリセット
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
            
            // プレイヤーの動きを再開
            if (playerController != null) playerController.enabled = true;

            // パーティクルが残っていれば削除
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
