using UnityEngine;
using Dracul.Core;
using Dracul.Player;

namespace Dracul.Environment
{
    /// <summary>
    /// プレイヤーにアタッチし、日光に晒されている場合に持続ダメージおよびブラッドゲージ減少を与えるスクリプト。
    /// マルチポイントレイキャスト（頭・胸・足・左右肩）による精密な遮蔽率サンプリングを行い、
    /// 「完全日陰ではゼロ、一部露出/木漏れ日では軽減減少、日向では通常減少」を実現します。
    /// </summary>
    [RequireComponent(typeof(PlayerStats))]
    public class SunDamage : MonoBehaviour
    {
        [Header("Sun Damage Settings")]
        [Tooltip("日光によって毎秒受ける基本ダメージ量")]
        public float damagePerSecond = 5f;

        [Tooltip("レイキャストで影となる障害物を判定するレイヤー")]
        public LayerMask shadowLayerMask = ~0;

        [Tooltip("レイキャストの最大探索距離")]
        public float maxRayDistance = 500f;

        [Header("Sampling Points (Offsets relative to Player)")]
        [Tooltip("頭部オフセット")]
        public Vector3 headOffset = new Vector3(0, 1.7f, 0);

        [Tooltip("胸部オフセット")]
        public Vector3 chestOffset = new Vector3(0, 1.0f, 0);

        [Tooltip("足元オフセット")]
        public Vector3 feetOffset = new Vector3(0, 0.2f, 0);

        [Tooltip("左半身オフセット")]
        public Vector3 leftOffset = new Vector3(-0.4f, 1.2f, 0);

        [Tooltip("右半身オフセット")]
        public Vector3 rightOffset = new Vector3(0.4f, 1.2f, 0);

        [Header("Debug")]
        [Tooltip("Sceneビューで各サンプリングRayを描画（赤: 直射日光, 黄: 葉の影/軽減, 緑: 完全遮蔽）")]
        public bool drawDebugRays = true;

        [Tooltip("現在の日光露出倍率（0 = 完全日陰/減少ゼロ, 0.1~0.9 = 軽減減少, 1.0 = 通常減少）")]
        [Range(0f, 1f)]
        public float currentExposureMultiplier = 0f;

        private PlayerStats playerStats;

        void Start()
        {
            playerStats = GetComponent<PlayerStats>();
        }

        void Update()
        {
            // TimeManager が存在しない、または「夜」の場合は判定を行わずリセット
            if (TimeManager.Instance == null || !TimeManager.Instance.IsDay)
            {
                currentExposureMultiplier = 0f;
                return;
            }

            Light sun = TimeManager.Instance.SunLight;
            if (sun == null)
            {
                currentExposureMultiplier = 0f;
                return;
            }

            // 太陽の方向ベクトル（Directional Light の逆方向）
            Vector3 sunDirection = -sun.transform.forward;

            // プレイヤーの向きに応じたサンプリング位置を算出
            Vector3[] samplePoints = new Vector3[]
            {
                transform.position + headOffset,
                transform.position + chestOffset,
                transform.position + feetOffset,
                transform.position + transform.rotation * leftOffset,
                transform.position + transform.rotation * rightOffset
            };

            float totalMultiplier = 0f;

            for (int i = 0; i < samplePoints.Length; i++)
            {
                Vector3 startPos = samplePoints[i];
                float pointMultiplier = EvaluatePointExposure(startPos, sunDirection);
                totalMultiplier += pointMultiplier;

                if (drawDebugRays)
                {
                    if (pointMultiplier >= 0.99f)
                    {
                        Debug.DrawRay(startPos, sunDirection * 10f, Color.red);
                    }
                    else if (pointMultiplier > 0.01f)
                    {
                        Debug.DrawRay(startPos, sunDirection * 10f, Color.yellow);
                    }
                    else
                    {
                        Debug.DrawRay(startPos, sunDirection * 10f, Color.green);
                    }
                }
            }

            // 全ポイントの平均露出度（0.0 〜 1.0）
            currentExposureMultiplier = totalMultiplier / samplePoints.Length;

            // 完全に影（0）なら減少ゼロ、部分露出/木漏れ日なら軽減減少、日向なら通常減少
            if (playerStats != null)
            {
                playerStats.ApplySunlightDamage(damagePerSecond, currentExposureMultiplier);
            }
        }

        /// <summary>
        /// 単一サンプリング点の日光露出ペナルティ倍率を判定
        /// </summary>
        private float EvaluatePointExposure(Vector3 origin, Vector3 sunDir)
        {
            RaycastHit[] hits = Physics.RaycastAll(origin, sunDir, maxRayDistance, shadowLayerMask, QueryTriggerInteraction.Collide);

            if (hits == null || hits.Length == 0)
            {
                // 何も遮るものがない = 直射日光 (1.0)
                return 1.0f;
            }

            // 距離順にソート
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            float bestPenalty = 1.0f;
            bool hitValidObstacle = false;

            for (int h = 0; h < hits.Length; h++)
            {
                var hit = hits[h];

                // プレイヤー自身（および子オブジェクト）は無視
                if (hit.transform == transform || hit.transform.IsChildOf(transform))
                {
                    continue;
                }

                hitValidObstacle = true;

                // 遮蔽物コンポーネントをチェック
                ShadeObject shade = hit.collider.GetComponentInParent<ShadeObject>();
                if (shade != null)
                {
                    // 葉の影などの軽減遮蔽
                    if (shade.penaltyMultiplier < bestPenalty)
                    {
                        bestPenalty = shade.penaltyMultiplier;
                    }
                }
                else
                {
                    // 通常の障害物（建物・地形・幹など）は完全遮蔽 (0.0)
                    return 0.0f;
                }
            }

            return hitValidObstacle ? bestPenalty : 1.0f;
        }
    }
}
