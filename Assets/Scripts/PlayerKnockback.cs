using UnityEngine;
using StarterAssets; // もしPlayerがThirdPersonControllerを使用している場合

namespace Dracul.PhysicsEffects
{
    /// <summary>
    /// プレイヤーにアタッチし、CharacterControllerを利用して吹き飛ばしを適用するクラス
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerKnockback : MonoBehaviour, IKnockbackable
    {
        [Tooltip("ノックバック速度の減衰率（ドラッグ）")]
        public float drag = 5f;

        [Tooltip("ノックバック中の追加重力")]
        public float gravity = -15f;

        [Tooltip("ノックバックとみなす最小速度")]
        public float minVelocity = 0.1f;

        private CharacterController _controller;
        private Vector3 _knockbackVelocity = Vector3.zero;

        // もし必要であれば、ノックバック中に一時的に通常の操作入力を無効化するための参照
        // private ThirdPersonController _thirdPersonController;

        void Start()
        {
            _controller = GetComponent<CharacterController>();
            // _thirdPersonController = GetComponent<ThirdPersonController>();
        }

        void Update()
        {
            if (_knockbackVelocity.sqrMagnitude > minVelocity * minVelocity)
            {
                // ノックバック中の移動処理
                _controller.Move(_knockbackVelocity * Time.deltaTime);

                // 水平方向の減衰（ドラッグ）
                _knockbackVelocity.x = Mathf.Lerp(_knockbackVelocity.x, 0, drag * Time.deltaTime);
                _knockbackVelocity.z = Mathf.Lerp(_knockbackVelocity.z, 0, drag * Time.deltaTime);

                // 垂直方向（重力）の適用
                if (!_controller.isGrounded)
                {
                    _knockbackVelocity.y += gravity * Time.deltaTime;
                }
                else
                {
                    // 接地していればY方向の速度も減衰
                    if (_knockbackVelocity.y < 0)
                    {
                        _knockbackVelocity.y = 0;
                    }
                }
            }
            else
            {
                _knockbackVelocity = Vector3.zero;
            }
        }

        public void ApplyKnockback(float force, Vector3 explosionPos, float explosionRadius, float upwardsModifier = 0.0f)
        {
            // 爆発中心からプレイヤーへの方向ベクトル
            Vector3 direction = transform.position - explosionPos;

            // upwardsModifierを適用して上方向に浮きやすくする
            direction.y += upwardsModifier;
            
            // 距離に応じた減衰（中心に近いほど力が強い）
            float distance = direction.magnitude;
            float forceMultiplier = 1f - Mathf.Clamp01(distance / explosionRadius);

            // 質量としてダミーの値を仮定（必要であればフィールドで設定可能にする）
            float mass = 70f;
            
            // 初速を決定 (F = ma => v = F/m * dt 的な近似, Impulseなので F/m)
            // CharacterControllerにはAddForceがないため、直接速度ベクトルを計算する
            Vector3 initialVelocity = direction.normalized * (force * forceMultiplier / mass);

            // 現在のノックバック速度を上書き（または加算）
            _knockbackVelocity = initialVelocity;

            // TODO: ここで _thirdPersonController を数秒間無効化するなどの処理を入れると
            //       操作不能時間を演出できます。
        }
    }
}
