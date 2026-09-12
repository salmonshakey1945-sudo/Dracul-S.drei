using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Dracul.Environment
{
    /// <summary>
    /// 木などのオブジェクトをプロシージャル（手続き型）に配置するコンポーネント。
    /// 指定されたエリア内に地面レイキャストを行い、自然な回転・スケールで木を配置します。
    /// また、日光遮蔽・ダメージ軽減用の ShadeObject / コライダーの自動設定にも対応しています。
    /// </summary>
    [ExecuteAlways]
    public class ProceduralTreeSpawner : MonoBehaviour
    {
        public enum ShadeColliderShape
        {
            Capsule,
            Box,
            MeshConvex
        }

        [Header("Tree Prefabs")]
        [Tooltip("配置する木のプレハブ一覧（Tree9_2, Tree9_3, Tree9_4, Tree9_5 など）")]
        public GameObject[] treePrefabs;

        [Header("Spawn Settings")]
        [Tooltip("配置する木の目標本数")]
        public int treeCount = 50;

        [Tooltip("配置エリアのサイズ (X幅, Z奥行き)")]
        public Vector2 spawnAreaSize = new Vector2(100f, 100f);

        [Tooltip("配置エリアの中心オフセット（Transformの位置からのオフセット）")]
        public Vector3 spawnAreaOffset = Vector3.zero;

        [Header("Scale & Rotation")]
        [Tooltip("木の基本スケール（デフォルト: 0.5, 0.5, 0.5）")]
        public Vector3 baseScale = new Vector3(0.5f, 0.5f, 0.5f);

        [Tooltip("スケールのランダムな揺らぎ幅（0で固定、0.1なら ±0.05 の範囲でランダム）")]
        [Range(0f, 0.5f)]
        public float scaleVariation = 0.1f;

        [Tooltip("Y軸（向き）をランダムに回転させるか")]
        public bool randomYRotation = true;

        [Tooltip("地面の傾斜（法線）に合わせるか（falseの場合は常に真上向き）")]
        public bool alignToGroundNormal = false;

        [Header("Ground & Height Alignment")]
        [Tooltip("配置位置のY座標オフセット（木の上下位置の微調整。浮いている場合はマイナス、埋まっている場合はプラス）")]
        public float yOffset = 0f;

        [Tooltip("地面探索用のRaycastを発射する高さ（SpawnerのY座標基準）")]
        public float raycastHeight = 50f;

        [Tooltip("Raycastの最大距離")]
        public float raycastMaxDistance = 150f;

        [Tooltip("地面判定を行うレイヤーマスク（Raycast用）")]
        public LayerMask groundLayer = ~0; // デフォルトはEverything

        [Header("Placement Rules")]
        [Tooltip("木同士の最小間隔（重なり防止）")]
        public float minDistance = 3.0f;

        [Tooltip("1本あたりの最大配置試行回数")]
        public int maxAttemptsPerTree = 30;

        [Tooltip("木を配置したくない障害物レイヤー（建物・道など）")]
        public LayerMask obstacleLayer = 0;

        [Tooltip("障害物との干渉チェック半径")]
        public float obstacleCheckRadius = 1.0f;

        [Header("Leaf Shade & Sun Mitigation")]
        [Tooltip("生成した木に日光遮蔽・軽減用の ShadeObject とコライダーを自動追加するか")]
        public bool addShadeSystem = true;

        [Tooltip("葉の影に入った時のペナルティ倍率（0 = 完全無効, 0.3 = 70%カット, 1 = 軽減なし）")]
        [Range(0f, 1f)]
        public float leafPenaltyMultiplier = 0.3f;

        [Tooltip("影判定用コライダーの形状（MeshConvexが最も葉の形状に正確です）")]
        public ShadeColliderShape shadeColliderShape = ShadeColliderShape.MeshConvex;

        [Header("Random Seed")]
        [Tooltip("シード値を固定して再現性を持たせるか")]
        public bool useRandomSeed = false;

        [Tooltip("固定シード値")]
        public int customSeed = 12345;

        [Header("Lifecycle & Container")]
        [Tooltip("ゲーム開始時（Start）に自動生成するか")]
        public bool spawnOnStart = false;

        [Tooltip("生成した木を格納する親オブジェクトの名前")]
        public string containerName = "TreeContainer";

        [Tooltip("生成時の詳細ログを出力するか（ヒットした地面の名前や座標を確認できます）")]
        public bool showDebugLog = true;

        /// <summary>
        /// 配置エリアの中心ワールド座標を取得
        /// </summary>
        public Vector3 AreaCenter => transform.position + spawnAreaOffset;

        private void Start()
        {
            if (Application.isPlaying && spawnOnStart)
            {
                GenerateTrees();
            }
        }

        /// <summary>
        /// 木をプロシージャルに生成・配置します。
        /// </summary>
        [ContextMenu("Generate Trees")]
        public void GenerateTrees()
        {
            if (treePrefabs == null || treePrefabs.Length == 0)
            {
                Debug.LogWarning("[ProceduralTreeSpawner] Tree Prefabs が設定されていません。", this);
                return;
            }

            // シード設定
            if (useRandomSeed)
            {
                Random.InitState(customSeed);
            }

            // 既存の木コンテナをクリア
            ClearTrees();

            Transform container = GetOrCreateContainer();
            List<Vector3> placedPositions = new List<Vector3>();

            int successfulCount = 0;
            Vector3 center = AreaCenter;
            float halfWidth = spawnAreaSize.x * 0.5f;
            float halfDepth = spawnAreaSize.y * 0.5f;

            string lastHitColliderName = "None";

            for (int i = 0; i < treeCount; i++)
            {
                for (int attempt = 0; attempt < maxAttemptsPerTree; attempt++)
                {
                    // ランダムなX, Z位置を決定
                    float randomX = center.x + Random.Range(-halfWidth, halfWidth);
                    float randomZ = center.z + Random.Range(-halfDepth, halfDepth);
                    Vector3 rayOrigin = new Vector3(randomX, center.y + raycastHeight, randomZ);

                    // RaycastAll で Trigger を無視し、自身や生成済みの木以外の有効な地面を探す
                    RaycastHit[] hits = Physics.RaycastAll(rayOrigin, Vector3.down, raycastMaxDistance, groundLayer, QueryTriggerInteraction.Ignore);

                    if (hits != null && hits.Length > 0)
                    {
                        // 距離順にソート（最も上にあるコライダーから順にチェック）
                        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

                        RaycastHit? validHit = null;
                        for (int h = 0; h < hits.Length; h++)
                        {
                            var hit = hits[h];

                            // 自分自身（Spawner）やコンテナ内のオブジェクトは地面として無視
                            if (hit.transform == transform || hit.transform.IsChildOf(transform) || hit.transform.IsChildOf(container))
                            {
                                continue;
                            }

                            validHit = hit;
                            break;
                        }

                        if (!validHit.HasValue) continue;

                        RaycastHit groundHit = validHit.Value;
                        lastHitColliderName = groundHit.collider.gameObject.name;
                        Vector3 targetPos = groundHit.point;

                        // 障害物チェック
                        if (obstacleLayer != 0 && Physics.CheckSphere(targetPos, obstacleCheckRadius, obstacleLayer))
                        {
                            continue;
                        }

                        // 既存の木との距離チェック（重なり防止）
                        bool isTooClose = false;
                        for (int p = 0; p < placedPositions.Count; p++)
                        {
                            if (Vector3.Distance(targetPos, placedPositions[p]) < minDistance)
                            {
                                isTooClose = true;
                                break;
                            }
                        }

                        if (isTooClose) continue;

                        // Y Offset を適用した位置に配置
                        Vector3 spawnPos = targetPos + Vector3.up * yOffset;
                        SpawnSingleTree(spawnPos, groundHit.normal, container);
                        placedPositions.Add(targetPos);
                        successfulCount++;
                        break;
                    }
                }
            }

            if (showDebugLog)
            {
                Debug.Log($"[ProceduralTreeSpawner] 木の配置が完了しました。（目標: {treeCount}本, 配置成功: {successfulCount}本 / 接地ヒット例: '{lastHitColliderName}'）", this);
            }
        }

        /// <summary>
        /// 1本の木をインスタンス化して配置
        /// </summary>
        private void SpawnSingleTree(Vector3 position, Vector3 normal, Transform parent)
        {
            // ランダムにプレハブを選択
            int prefabIndex = Random.Range(0, treePrefabs.Length);
            GameObject selectedPrefab = treePrefabs[prefabIndex];
            if (selectedPrefab == null) return;

            // 回転の決定
            Quaternion rotation;
            if (alignToGroundNormal)
            {
                rotation = Quaternion.FromToRotation(Vector3.up, normal);
                if (randomYRotation)
                {
                    rotation *= Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                }
            }
            else
            {
                float yAngle = randomYRotation ? Random.Range(0f, 360f) : 0f;
                rotation = Quaternion.Euler(0f, yAngle, 0f);
            }

            // インスタンス化（エディタ時はプレハブリンクを維持）
            GameObject treeObj;
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                treeObj = (GameObject)PrefabUtility.InstantiatePrefab(selectedPrefab, parent);
                treeObj.transform.position = position;
                treeObj.transform.rotation = rotation;
                Undo.RegisterCreatedObjectUndo(treeObj, "Spawn Procedural Tree");
            }
            else
            {
                treeObj = Instantiate(selectedPrefab, position, rotation, parent);
            }
#else
            treeObj = Instantiate(selectedPrefab, position, rotation, parent);
#endif

            // スケール計算
            float scaleMod = (scaleVariation > 0f) ? Random.Range(-scaleVariation * 0.5f, scaleVariation * 0.5f) : 0f;
            Vector3 finalScale = new Vector3(
                Mathf.Max(0.01f, baseScale.x + scaleMod),
                Mathf.Max(0.01f, baseScale.y + scaleMod),
                Mathf.Max(0.01f, baseScale.z + scaleMod)
            );
            treeObj.transform.localScale = finalScale;

            // 影システム（ShadeObject / 判定コライダー）のセットアップ
            if (addShadeSystem)
            {
                SetupTreeShade(treeObj);
            }
        }

        /// <summary>
        /// 木オブジェクトに影判定用コライダーと ShadeObject を設定
        /// </summary>
        private void SetupTreeShade(GameObject treeObj)
        {
            // ShadeObject コンポーネントの付与
            ShadeObject shade = treeObj.GetComponent<ShadeObject>();
            if (shade == null)
            {
                shade = treeObj.AddComponent<ShadeObject>();
            }
            shade.penaltyMultiplier = leafPenaltyMultiplier;
            shade.shadeDescription = "Tree Leaves";

            // コライダーの付与（プレイヤーの歩行を邪魔しないよう Trigger に設定）
            if (treeObj.GetComponent<Collider>() == null)
            {
                MeshFilter mf = treeObj.GetComponent<MeshFilter>();

                if (shadeColliderShape == ShadeColliderShape.MeshConvex && mf != null && mf.sharedMesh != null)
                {
                    MeshCollider mc = treeObj.AddComponent<MeshCollider>();
                    mc.sharedMesh = mf.sharedMesh;
                    mc.convex = true;
                    mc.isTrigger = true;
                }
                else if (shadeColliderShape == ShadeColliderShape.Box && mf != null && mf.sharedMesh != null)
                {
                    BoxCollider bc = treeObj.AddComponent<BoxCollider>();
                    bc.center = mf.sharedMesh.bounds.center;
                    bc.size = mf.sharedMesh.bounds.size;
                    bc.isTrigger = true;
                }
                else
                {
                    // Capsule (樹冠全体を覆う滑らかなコライダー)
                    CapsuleCollider cc = treeObj.AddComponent<CapsuleCollider>();
                    if (mf != null && mf.sharedMesh != null)
                    {
                        Bounds bounds = mf.sharedMesh.bounds;
                        cc.center = bounds.center;
                        cc.height = bounds.size.y;
                        cc.radius = Mathf.Max(bounds.extents.x, bounds.extents.z);
                    }
                    else
                    {
                        cc.center = new Vector3(0, 4f, 0);
                        cc.height = 8f;
                        cc.radius = 3f;
                    }
                    cc.isTrigger = true;
                }
            }
        }

        /// <summary>
        /// 生成した木をすべて削除します。
        /// </summary>
        [ContextMenu("Clear Trees")]
        public void ClearTrees()
        {
            Transform container = transform.Find(containerName);
            if (container != null)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    Undo.DestroyObjectImmediate(container.gameObject);
                }
                else
                {
                    Destroy(container.gameObject);
                }
#else
                Destroy(container.gameObject);
#endif
            }
        }

        /// <summary>
        /// 木をまとめる親オブジェクトを取得または作成
        /// </summary>
        private Transform GetOrCreateContainer()
        {
            Transform container = transform.Find(containerName);
            if (container == null)
            {
                GameObject obj = new GameObject(containerName);
                obj.transform.SetParent(transform, false);
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    Undo.RegisterCreatedObjectUndo(obj, "Create Tree Container");
                }
#endif
                container = obj.transform;
            }
            return container;
        }

        private void OnDrawGizmosSelected()
        {
            // 配置領域の Gizmo 描画
            Gizmos.color = new Color(0.2f, 0.8f, 0.3f, 0.4f);
            Vector3 center = AreaCenter;
            Gizmos.DrawWireCube(center, new Vector3(spawnAreaSize.x, 2.0f, spawnAreaSize.y));

            Gizmos.color = new Color(0.2f, 0.8f, 0.3f, 0.1f);
            Gizmos.DrawCube(center, new Vector3(spawnAreaSize.x, 0.1f, spawnAreaSize.y));

            // Raycast 発射高度の目安線
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(center, center + Vector3.up * raycastHeight);
            Gizmos.DrawWireCube(center + Vector3.up * raycastHeight, new Vector3(spawnAreaSize.x * 0.1f, 0.1f, spawnAreaSize.y * 0.1f));
        }
    }
}
