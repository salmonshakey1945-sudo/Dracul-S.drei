using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace Dracul.PhysicsEffects
{
    /// <summary>
    /// 敵キャラクターにアタッチし、ノックバック発生時に一時的にNavMeshAgentを無効化して物理挙動（Rigidbody）に任せるクラス
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(NavMeshAgent))]
    public class EnemyKnockback : MonoBehaviour, IKnockbackable
    {
        [Tooltip("ノックバック後に操作復帰（NavMesh再有効化）するまでの最小待機時間")]
        public float minRecoveryTime = 0.5f;

        private Rigidbody _rb;
        private NavMeshAgent _agent;
        private bool _isKnockedBack = false;

        void Start()
        {
            _rb = GetComponent<Rigidbody>();
            _agent = GetComponent<NavMeshAgent>();
            
            // 通常時は物理演算の影響を受けないようにする（NavMeshAgentで移動するため）
            if (_rb != null)
            {
                _rb.isKinematic = true;
            }
        }

        public void ApplyKnockback(float force, Vector3 explosionPos, float explosionRadius, float upwardsModifier = 0.0f)
        {
            if (_isKnockedBack) return; // 既にノックバック中なら無視

            StartCoroutine(KnockbackRoutine(force, explosionPos, explosionRadius, upwardsModifier));
        }

        private IEnumerator KnockbackRoutine(float force, Vector3 explosionPos, float explosionRadius, float upwardsModifier)
        {
            _isKnockedBack = true;

            // 1. NavMeshAgentを無効化
            if (_agent != null && _agent.enabled)
            {
                _agent.enabled = false;
            }

            // 2. Rigidbodyを物理演算有効にする
            if (_rb != null)
            {
                _rb.isKinematic = false;
                
                // 少しだけ上に浮かせてから力を加えると綺麗に吹き飛ぶ
                transform.position += Vector3.up * 0.1f;
                
                _rb.AddExplosionForce(force, explosionPos, explosionRadius, upwardsModifier, ForceMode.Impulse);
            }

            // 最低限待機
            yield return new WaitForSeconds(minRecoveryTime);

            // 3. 速度が落ち着き、接地するまで待機
            if (_rb != null)
            {
                while (_rb.linearVelocity.sqrMagnitude > 0.1f)
                {
                    yield return null;
                }
            }
            
            // 完全に静止させる
            if (_rb != null)
            {
                _rb.linearVelocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
                _rb.isKinematic = true;
            }

            // 4. NavMeshAgentを再有効化
            if (_agent != null)
            {
                // エージェントがNavMesh上に戻れるように位置を微調整する
                NavMeshHit hit;
                if (NavMesh.SamplePosition(transform.position, out hit, 2.0f, NavMesh.AllAreas))
                {
                    transform.position = hit.position;
                    _agent.enabled = true;
                }
                else
                {
                    Debug.LogWarning("[EnemyKnockback] NavMeshAgent could not find a valid position to snap back to.");
                    // 復帰不能な場合は何か処理を追加してもよい（即死させる等）
                }
            }

            _isKnockedBack = false;
        }
    }
}
