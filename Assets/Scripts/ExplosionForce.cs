using System.Collections.Generic;
using UnityEngine;

namespace Dracul.PhysicsEffects
{
    /// <summary>
    /// 徐々に広がる球状のトリガーを用いて、周囲のオブジェクトを吹き飛ばすスクリプト。
    /// 爆発エフェクトのプレハブ等にアタッチして使用します。
    /// </summary>
    public class ExplosionForce : MonoBehaviour
    {
        [Tooltip("吹き飛ばす力の強さ（基本値・プレイヤーや敵向け）")]
        public float force = 200f;

        [Tooltip("アイテム（Rigidbody）を吹き飛ばす際の力の倍率（1.0で基本値と同じ）")]
        public float itemForceMultiplier = 0.05f;

        [Tooltip("爆発の最大半径")]
        public float maxRadius = 5f;

        [Tooltip("最大半径に到達するまでの時間（秒）")]
        public float duration = 0.2f;

        [Tooltip("上方向に持ち上げる力の補正値")]
        public float upwardsModifier = 1.0f;

        private SphereCollider _sphereCollider;
        private float _elapsedTime = 0f;
        private HashSet<Collider> _affectedColliders = new HashSet<Collider>();

        void Start()
        {
            // SphereColliderを取得するか、なければ追加する
            _sphereCollider = GetComponent<SphereCollider>();
            if (_sphereCollider == null)
            {
                _sphereCollider = gameObject.AddComponent<SphereCollider>();
            }

            // トリガーとして設定し、初期半径を0にする
            _sphereCollider.isTrigger = true;
            _sphereCollider.radius = 0f;
        }

        void Update()
        {
            if (_elapsedTime < duration)
            {
                _elapsedTime += Time.deltaTime;
                // 時間経過に応じてコライダーの半径を拡大する
                float t = Mathf.Clamp01(_elapsedTime / duration);
                // easing (例: ease-out quad) をかけるとより自然になるかもしれません
                float easedT = t * (2 - t); 
                _sphereCollider.radius = Mathf.Lerp(0, maxRadius, easedT);
            }
            else
            {
                // 拡大が完了したらコライダーを無効化する
                _sphereCollider.enabled = false;
            }
        }

        void OnTriggerEnter(Collider other)
        {
            // 既に処理済みのコライダーは無視する
            if (_affectedColliders.Contains(other))
            {
                return;
            }
            _affectedColliders.Add(other);

            // 1. IKnockbackableを実装しているコンポーネント（プレイヤーや敵）か確認
            IKnockbackable knockbackable = other.GetComponentInParent<IKnockbackable>();
            if (knockbackable != null)
            {
                knockbackable.ApplyKnockback(force, transform.position, maxRadius, upwardsModifier);
                return;
            }

            // 2. なければ、Rigidbody（アイテムなど）に直接力を加える
            Rigidbody rb = other.attachedRigidbody;
            if (rb != null && !rb.isKinematic)
            {
                // アイテム用の倍率をかけて威力を調整する
                float itemForce = force * itemForceMultiplier;
                rb.AddExplosionForce(itemForce, transform.position, maxRadius, upwardsModifier, ForceMode.Impulse);
            }
        }
    }
}
