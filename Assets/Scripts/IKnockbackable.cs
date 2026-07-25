using UnityEngine;

namespace Dracul.PhysicsEffects
{
    /// <summary>
    /// 爆発などによる吹き飛ばし（ノックバック）を受けることができるオブジェクトのインターフェース
    /// </summary>
    public interface IKnockbackable
    {
        /// <summary>
        /// ノックバックを適用します。
        /// </summary>
        /// <param name="force">吹き飛ばす力の強さ</param>
        /// <param name="explosionPos">爆発の発生源の座標</param>
        /// <param name="explosionRadius">爆発の最大半径</param>
        /// <param name="upwardsModifier">上方向に持ち上げる力の補正値（0だと水平に吹き飛ぶ）</param>
        void ApplyKnockback(float force, Vector3 explosionPos, float explosionRadius, float upwardsModifier = 0.0f);
    }
}
