using UnityEngine;
using Dracul.Core;
using Dracul.Player;

namespace Dracul.Environment
{
    /// <summary>
    /// プレイヤーにアタッチし、日光に晒されている場合に持続ダメージを与えるスクリプト。
    /// TimeManager の昼夜判定と太陽（Directional Light）の向きを利用して、レイキャストで影の判定を行う。
    /// </summary>
    [RequireComponent(typeof(PlayerStats))]
    public class SunDamage : MonoBehaviour
    {
        [Header("Sun Damage Settings")]
        [Tooltip("日光によって毎秒受けるダメージ量")]
        public float damagePerSecond = 5f;
        
        [Tooltip("レイキャストで影となる障害物を判定するレイヤー")]
        public LayerMask shadowLayerMask = ~0; // デフォルトですべてのレイヤーを対象

        [Tooltip("レイを飛ばす開始位置の高さオフセット（足元からだと地面に当たるため）")]
        public float raycastOffset = 1.0f;

        private PlayerStats playerStats;

        void Start()
        {
            playerStats = GetComponent<PlayerStats>();
        }

        void Update()
        {
            // TimeManager が存在しない、または「夜」の場合はダメージ判定を行わない
            if (TimeManager.Instance == null || !TimeManager.Instance.IsDay)
            {
                return;
            }

            Light sun = TimeManager.Instance.SunLight;
            if (sun == null) return;

            // 太陽の方向ベクトル（Directional Light は Z軸の正方向を向いているので、その逆方向が太陽の方向）
            Vector3 sunDirection = -sun.transform.forward;

            // レイを飛ばす開始位置（プレイヤーの少し上）
            Vector3 rayStartPos = transform.position + Vector3.up * raycastOffset;

            // レイキャストで太陽の方向に障害物があるかチェック
            // 距離は1000fなど十分遠くを設定
            if (Physics.Raycast(rayStartPos, sunDirection, out RaycastHit hit, 1000f, shadowLayerMask))
            {
                // 何かにぶつかった = 影の中にいる
                Debug.DrawRay(rayStartPos, sunDirection * hit.distance, Color.green);
            }
            else
            {
                // 何にもぶつからない = 日光に当たっている（影から出ている）
                Debug.DrawRay(rayStartPos, sunDirection * 10f, Color.red);
                
                if (playerStats != null)
                {
                    playerStats.TakeDamage(damagePerSecond * Time.deltaTime);
                }
            }
        }
    }
}
