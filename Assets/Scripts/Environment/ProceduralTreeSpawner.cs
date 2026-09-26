using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Dracul.Environment
{
    /// <summary>
    /// 木や草などのオブジェクトをプロシージャル（手続き型）に配置するコンポーネント。
    /// 指定されたエリア内に地面レイキャストを行い、自然な回転・スケールで配置します。
    /// 木のように真上向きに生やすことも、草のように地面の傾斜（法線）に対して垂直に生やすことも設定可能。
    /// また、Perlin Noise や Cluster による密度の「濃淡（群生・粗密）」表現にも対応しています。
    /// </summary>
    [ExecuteAlways]
    public class ProceduralTreeSpawner : MonoBehaviour
    {
        public enum DistributionMode
        {
            [InspectorName("Uniform (均等ランダム)")]
            Uniform,
            [InspectorName("Perlin Noise (自然なまだら模様の濃淡)")]
            PerlinNoise,
            [InspectorName("Clusters (島状の群生・パッチ)")]
            Clusters,
            [InspectorName("Tree Proximity (木の根元・下草に密集)")]
            TreeProximity,
            [InspectorName("Tree Ring (幹を避け樹冠のフチ・木漏れ日ゾーンに群生)")]
            TreeRing,
            [InspectorName("Tree Avoidance (木を避けて開けた平地に密集)")]
            TreeAvoidance
        }

        public enum ShadeColliderShape
        {
            Capsule,
            Box,
            MeshConvex
        }

        [Header("Object Prefabs")]
        [Tooltip("配置するオブジェクト（木や草・低木など）のプレハブ一覧")]
        public GameObject[] treePrefabs;

        [Header("Spawn Settings")]
        [Tooltip("配置するオブジェクトの目標数")]
        public int treeCount = 50;

        [Tooltip("配置エリアのサイズ (X幅, Z奥行き)")]
        public Vector2 spawnAreaSize = new Vector2(100f, 100f);

        [Tooltip("配置エリアの中心オフセット（Transformの位置からのオフセット）")]
        public Vector3 spawnAreaOffset = Vector3.zero;

        [Header("Distribution & Clustering (密度の濃淡・群生)")]
        [Tooltip("配置分布のアルゴリズム。\n・Uniform: 全体に均等に散らばる\n・PerlinNoise: 自然界のような滑らかなまだら模様の濃淡\n・Clusters: 指定した数の塊（群生）を作って密集")]
        public DistributionMode distributionMode = DistributionMode.Uniform;

        [Tooltip("【PerlinNoise用】ノイズのスケール（値が小さいほど広大な塊、大きいほど細かなまだら模様）")]
        [Range(0.005f, 0.2f)]
        public float noiseScale = 0.04f;

        [Tooltip("【PerlinNoise用】密度の最低閾値（この値以下のエリアには草が生えず、地面が露出します）")]
        [Range(0f, 0.8f)]
        public float noiseThreshold = 0.3f;

        [Tooltip("【PerlinNoise用】濃淡のメリハリ（値が大きいほど密集部と薄い部の差がハッキリします）")]
        [Range(0.5f, 3.0f)]
        public float noiseContrast = 1.2f;

        [Tooltip("【PerlinNoise用】ノイズのオフセット（シード値によって自動変化も可能）")]
        public Vector2 noiseOffset = Vector2.zero;

        [Tooltip("【Clusters用】群生（塊）の数")]
        [Range(1, 50)]
        public int clusterCount = 8;

        [Tooltip("【Clusters用】1つの群生の広がり半径")]
        [Range(1f, 50f)]
        public float clusterRadius = 10f;

        [Tooltip("【Clusters用】群生の外にまばらに散らす草の割合（0 = 完全に群生内のみ, 0.2 = 20%は全体に散乱）")]
        [Range(0f, 0.5f)]
        public float scatterRatio = 0.15f;

        [Header("Tree Relation Settings (木との連動・下草設定)")]
        [Tooltip("参照する木々の親コンテナ（Transform）。未設定時はシーン内の 'TreeSpawner' または 'TreeContainer' を自動探索します")]
        public Transform treeSourceContainer;

        [Tooltip("【Tree連動用】木周辺に集中させる草の割合（0.85なら85%が木の周り、15%が全体にまばらに散乱して自然な散らばりを作ります）")]
        [Range(0f, 1f)]
        public float treeProximityFocus = 0.85f;

        [Tooltip("木の幹直下の除外半径（木の幹の中に草が生えるのを防止する最小距離）")]
        [Range(0f, 5f)]
        public float treeInnerRadius = 0.8f;

        [Tooltip("木の影響が及ぶ外側半径（木の根元からどこまで草を広げるか）")]
        [Range(1f, 30f)]
        public float treeOuterRadius = 6.0f;

        [Tooltip("木周辺での密度の偏り度合い（1.0 = 均等, 2.0 = 幹に近いほど高密度, 0.5 = 外側ほど高密度）")]
        [Range(0.2f, 3.0f)]
        public float treeFalloff = 1.5f;

        [Tooltip("どの分布モード（UniformやPerlinNoise含む）でも、木の幹(treeInnerRadius)へのめり込み配置を防止するか")]
        public bool preventTrunkOverlap = true;

        [Header("Scale & Rotation")]
        [Tooltip("基本スケール（木: 0.5, 草: 1.0 など）")]
        public Vector3 baseScale = new Vector3(0.5f, 0.5f, 0.5f);

        [Tooltip("スケールのランダムな揺らぎ幅（0で固定、0.1なら ±0.05 の範囲でランダム）")]
        [Range(0f, 1f)]
        public float scaleVariation = 0.1f;

        [Tooltip("Y軸（向き）をランダムに回転させるか")]
        public bool randomYRotation = true;

        [Header("Alignment to Ground (地面への角度合わせ)")]
        [Tooltip("地面の傾斜（法線）に対して垂直に生やすか。\n・草や岩の場合: チェックON（地面に垂直に生える）\n・木の場合: チェックOFF（重力に逆らって常に真上に向かって生える）")]
        public bool alignToGroundNormal = false;

        [Tooltip("法線への合わせ具合（1.0 = 地面に完全垂直, 0.0 = 常にワールド真上）")]
        [Range(0f, 1f)]
        public float normalAlignmentStrength = 1.0f;

        [Tooltip("配置を許可する最大傾斜角（度）。これ以上の急斜面・崖には配置しません")]
        [Range(0f, 90f)]
        public float maxSlopeAngle = 60f;

        [Header("Ground & Height Alignment")]
        [Tooltip("配置位置のY座標オフセット（上下位置の微調整。浮いている場合はマイナス、埋まっている場合はプラス）")]
        public float yOffset = 0f;

        [Tooltip("オブジェクトのスケール・高さに応じて、自動的に地面に何％埋め込むか（0 = 地表ぴったり, 0.35 = 35%埋める）\n岩をドッシリ接地させるのに最適です")]
        [Range(0f, 0.8f)]
        public float groundSinkRatio = 0.0f;

        [Tooltip("地面探索用のRaycastを発射する高さ（SpawnerのY座標基準）")]
        public float raycastHeight = 50f;

        [Tooltip("Raycastの最大距離")]
        public float raycastMaxDistance = 150f;

        [Tooltip("地面判定を行うレイヤーマスク（Raycast用）")]
        public LayerMask groundLayer = ~0; // デフォルトはEverything

        [Header("Parent-Child Satellite Spawn (親子ペア配置・岩用機能)")]
        [Tooltip("親オブジェクト（大きな岩）の周囲に、小さな子オブジェクト（小石）を添えるように配置するか")]
        public bool spawnChildSatellites = false;

        [Tooltip("親1個あたりに添える子オブジェクトの数")]
        [Range(1, 4)]
        public int satelliteChildCount = 2;

        [Tooltip("親からの距離範囲 (最小〜最大)")]
        public Vector2 satelliteDistanceRange = new Vector2(0.6f, 1.8f);

        [Tooltip("親に対する子オブジェクトのスケール比率 (最小〜最大)")]
        public Vector2 satelliteScaleMultiplier = new Vector2(0.25f, 0.45f);

        [Header("Placement Rules")]
        [Tooltip("オブジェクト同士の最小間隔（重なり防止。草なら 0.2〜0.4、木なら 3.0 など）")]
        public float minDistance = 3.0f;

        [Tooltip("1個あたりの最大配置試行回数")]
        public int maxAttemptsPerTree = 50;

        [Tooltip("配置したくない障害物レイヤー（建物・道など）")]
        public LayerMask obstacleLayer = 0;

        [Tooltip("障害物との干渉チェック半径")]
        public float obstacleCheckRadius = 1.0f;

        [Header("Leaf Shade & Sun Mitigation (木用機能)")]
        [Tooltip("生成したオブジェクトに日光遮蔽・軽減用の ShadeObject とコライダーを自動追加するか（草の場合はOFF推奨）")]
        public bool addShadeSystem = true;

        [Tooltip("葉の影に入った時のペナルティ倍率（0 = 完全無効, 0.3 = 70%カット, 1 = 軽減なし）")]
        [Range(0f, 1f)]
        public float leafPenaltyMultiplier = 0.3f;

        [Tooltip("影判定用コライダーの形状（MeshConvexが最も葉の形状に正確です）")]
        public ShadeColliderShape shadeColliderShape = ShadeColliderShape.MeshConvex;

        [Header("Optimization / Performance (草用機能)")]
        [Tooltip("プレハブに含まれるコライダーを自動削除するか（大量の草でプレイヤーが引っかかるのを防止したい場合にON）")]
        public bool removeColliders = false;

        [Header("Random Seed")]
        [Tooltip("シード値を固定して再現性を持たせるか")]
        public bool useRandomSeed = false;

        [Tooltip("固定シード値")]
        public int customSeed = 12345;

        [Header("Lifecycle & Container")]
        [Tooltip("ゲーム開始時（Start）に自動生成するか")]
        public bool spawnOnStart = false;

        [Tooltip("生成したオブジェクトを格納する親オブジェクトの名前")]
        public string containerName = "TreeContainer";

        [Tooltip("生成時の詳細ログを出力するか")]
        public bool showDebugLog = true;

        /// <summary>
        /// 配置エリアの中心ワールド座標を取得
        /// </summary>
        public Vector3 AreaCenter => transform.position + spawnAreaOffset;

        // Clustersモード用の一時キャッシュ
        private List<Vector2> _cachedClusterCenters = new List<Vector2>();

        // 木との連動用の一時キャッシュ
        private List<Vector3> _cachedTreePositions = new List<Vector3>();

        private void Start()
        {
            if (Application.isPlaying && spawnOnStart)
            {
                GenerateTrees();
            }
        }

        /// <summary>
        /// オブジェクトをプロシージャルに生成・配置します。
        /// </summary>
        [ContextMenu("Generate Objects")]
        public void GenerateTrees()
        {
            if (treePrefabs == null || treePrefabs.Length == 0)
            {
                Debug.LogWarning("[ProceduralTreeSpawner] Prefabs が設定されていません。", this);
                return;
            }

#if UNITY_EDITOR
            int undoGroup = -1;
            if (!Application.isPlaying)
            {
                Undo.IncrementCurrentGroup();
                undoGroup = Undo.GetCurrentGroup();
                Undo.SetCurrentGroupName("Generate Procedural Objects");
            }
#endif

            // シード設定
            if (useRandomSeed)
            {
                Random.InitState(customSeed);
            }

            // ノイズオフセットが未設定ならランダムオフセット
            Vector2 effectiveNoiseOffset = noiseOffset;
            if (effectiveNoiseOffset == Vector2.zero)
            {
                effectiveNoiseOffset = new Vector2(Random.Range(-1000f, 1000f), Random.Range(-1000f, 1000f));
            }

            // モードに応じた事前準備
            SetupClusterCenters();
            CollectTreePositions();

            // 既存のコンテナをクリア
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
                    // 分布モードに応じた候補座標サンプリング
                    Vector2 samplePos = SampleCandidatePosition(center, halfWidth, halfDepth, effectiveNoiseOffset);

                    float randomX = samplePos.x;
                    float randomZ = samplePos.y;
                    Vector3 rayOrigin = new Vector3(randomX, center.y + raycastHeight, randomZ);

                    // RaycastAll で Trigger を無視し、自身や生成済みのオブジェクト以外の有効な地面を探す
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

                        // 傾斜角チェック
                        float slopeAngle = Vector3.Angle(groundHit.normal, Vector3.up);
                        if (slopeAngle > maxSlopeAngle)
                        {
                            continue;
                        }

                        lastHitColliderName = groundHit.collider.gameObject.name;
                        Vector3 targetPos = groundHit.point;

                        // 障害物チェック
                        if (obstacleLayer != 0 && Physics.CheckSphere(targetPos, obstacleCheckRadius, obstacleLayer))
                        {
                            continue;
                        }

                        // 既存の配置物との距離チェック（重なり防止）
                        if (minDistance > 0f)
                        {
                            bool isTooClose = false;
                            for (int p = 0; p < placedPositions.Count; p++)
                            {
                                if (Vector3.SqrMagnitude(targetPos - placedPositions[p]) < minDistance * minDistance)
                                {
                                    isTooClose = true;
                                    break;
                                }
                            }

                            if (isTooClose) continue;
                        }

                        // 木の幹との重なりチェック（草が木の幹を突き抜けるのを防止）
                        if (preventTrunkOverlap && _cachedTreePositions.Count > 0 && treeInnerRadius > 0f)
                        {
                            bool isInsideTrunk = false;
                            for (int t = 0; t < _cachedTreePositions.Count; t++)
                            {
                                Vector2 treeXZ = new Vector2(_cachedTreePositions[t].x, _cachedTreePositions[t].z);
                                Vector2 targetXZ = new Vector2(targetPos.x, targetPos.z);
                                if (Vector2.SqrMagnitude(targetXZ - treeXZ) < treeInnerRadius * treeInnerRadius)
                                {
                                    isInsideTrunk = true;
                                    break;
                                }
                            }
                            if (isInsideTrunk) continue;
                        }

                        // Y Offset を適用した位置に配置
                        Vector3 spawnPos = targetPos + Vector3.up * yOffset;
                        SpawnSingleTree(spawnPos, groundHit.normal, container);
                        placedPositions.Add(targetPos);
                        successfulCount++;

                        // 親子ペア配置（子石の自動添え置き）
                        if (spawnChildSatellites)
                        {
                            SpawnSatellites(targetPos, container, placedPositions);
                        }
                        break;
                    }
                }
            }

            if (showDebugLog)
            {
                Debug.Log($"[ProceduralTreeSpawner] 配置が完了しました。（目標: {treeCount}個, 配置成功: {successfulCount}個 / 分布: {distributionMode} / 接地ヒット例: '{lastHitColliderName}'）", this);
            }

#if UNITY_EDITOR
            if (!Application.isPlaying && undoGroup != -1)
            {
                Undo.CollapseUndoOperations(undoGroup);
            }
#endif
        }

        /// <summary>
        /// 分布モードに応じた配置候補座標を計算
        /// </summary>
        private Vector2 SampleCandidatePosition(Vector3 center, float halfWidth, float halfDepth, Vector2 nOffset)
        {
            switch (distributionMode)
            {
                case DistributionMode.PerlinNoise:
                {
                    // リジェクションサンプリング：ノイズ密度に応じた確率判定
                    for (int retry = 0; retry < 15; retry++)
                    {
                        float candX = center.x + Random.Range(-halfWidth, halfWidth);
                        float candZ = center.z + Random.Range(-halfDepth, halfDepth);

                        float noise = Mathf.PerlinNoise((candX + nOffset.x) * noiseScale, (candZ + nOffset.y) * noiseScale);

                        if (noise >= noiseThreshold)
                        {
                            float normalizedDensity = (noise - noiseThreshold) / Mathf.Max(0.001f, 1f - noiseThreshold);
                            float acceptProbability = Mathf.Pow(normalizedDensity, noiseContrast);

                            if (Random.value <= acceptProbability)
                            {
                                return new Vector2(candX, candZ);
                            }
                        }
                    }
                    // リトライ上限時は一様ランダム
                    return new Vector2(center.x + Random.Range(-halfWidth, halfWidth), center.z + Random.Range(-halfDepth, halfDepth));
                }

                case DistributionMode.Clusters:
                {
                    if (_cachedClusterCenters.Count > 0 && Random.value > scatterRatio)
                    {
                        // いずれかのクラスタ中心の周囲に配置
                        int cIdx = Random.Range(0, _cachedClusterCenters.Count);
                        Vector2 cCenter = _cachedClusterCenters[cIdx];

                        // ガウス風（中心ほど高密度）の円内ランダム
                        Vector2 offset = Random.insideUnitCircle;
                        offset = offset * (offset.magnitude * clusterRadius);

                        float finalX = Mathf.Clamp(cCenter.x + offset.x, center.x - halfWidth, center.x + halfWidth);
                        float finalZ = Mathf.Clamp(cCenter.y + offset.y, center.z - halfDepth, center.z + halfDepth);
                        return new Vector2(finalX, finalZ);
                    }
                    else
                    {
                        // 散乱分：全体にまばらに配置
                        return new Vector2(center.x + Random.Range(-halfWidth, halfWidth), center.z + Random.Range(-halfDepth, halfDepth));
                    }
                }

                case DistributionMode.TreeProximity:
                {
                    // 木の周辺（根元・下草）に集中配置
                    if (_cachedTreePositions.Count > 0 && Random.value <= treeProximityFocus)
                    {
                        int tIdx = Random.Range(0, _cachedTreePositions.Count);
                        Vector3 tPos = _cachedTreePositions[tIdx];

                        float angle = Random.Range(0f, Mathf.PI * 2f);
                        // treeFalloff: 1.0=均等, >1.0=幹に近いほど密集
                        float t = Mathf.Pow(Random.value, treeFalloff);
                        float dist = Mathf.Lerp(treeInnerRadius, treeOuterRadius, t);

                        float candX = Mathf.Clamp(tPos.x + Mathf.Cos(angle) * dist, center.x - halfWidth, center.x + halfWidth);
                        float candZ = Mathf.Clamp(tPos.z + Mathf.Sin(angle) * dist, center.z - halfDepth, center.z + halfDepth);
                        return new Vector2(candX, candZ);
                    }
                    else
                    {
                        // こぼれ種（散乱分）：全体にまばらに配置
                        return new Vector2(center.x + Random.Range(-halfWidth, halfWidth), center.z + Random.Range(-halfDepth, halfDepth));
                    }
                }

                case DistributionMode.TreeRing:
                {
                    // 幹の直下と外側を避け、樹冠のフチ（木漏れ日ゾーン）にリング状に集中配置
                    if (_cachedTreePositions.Count > 0 && Random.value <= treeProximityFocus)
                    {
                        int tIdx = Random.Range(0, _cachedTreePositions.Count);
                        Vector3 tPos = _cachedTreePositions[tIdx];

                        float angle = Random.Range(0f, Mathf.PI * 2f);
                        // 三角分布（中央値付近が最も高密度になる山型）
                        float t = (Random.value + Random.value) * 0.5f;
                        float dist = Mathf.Lerp(treeInnerRadius, treeOuterRadius, t);

                        float candX = Mathf.Clamp(tPos.x + Mathf.Cos(angle) * dist, center.x - halfWidth, center.x + halfWidth);
                        float candZ = Mathf.Clamp(tPos.z + Mathf.Sin(angle) * dist, center.z - halfDepth, center.z + halfDepth);
                        return new Vector2(candX, candZ);
                    }
                    else
                    {
                        return new Vector2(center.x + Random.Range(-halfWidth, halfWidth), center.z + Random.Range(-halfDepth, halfDepth));
                    }
                }

                case DistributionMode.TreeAvoidance:
                {
                    // 木がある場所を避け、開けた平地に集中配置
                    if (_cachedTreePositions.Count > 0)
                    {
                        for (int retry = 0; retry < 20; retry++)
                        {
                            float candX = center.x + Random.Range(-halfWidth, halfWidth);
                            float candZ = center.z + Random.Range(-halfDepth, halfDepth);
                            Vector2 candPos = new Vector2(candX, candZ);

                            // 最寄りの木との距離を算出
                            float minTreeDist = float.MaxValue;
                            for (int t = 0; t < _cachedTreePositions.Count; t++)
                            {
                                Vector2 treeXZ = new Vector2(_cachedTreePositions[t].x, _cachedTreePositions[t].z);
                                float d = Vector2.Distance(candPos, treeXZ);
                                if (d < minTreeDist) minTreeDist = d;
                            }

                            if (minTreeDist >= treeOuterRadius)
                            {
                                return candPos;
                            }
                            else if (minTreeDist > treeInnerRadius)
                            {
                                float norm = (minTreeDist - treeInnerRadius) / Mathf.Max(0.001f, treeOuterRadius - treeInnerRadius);
                                if (Random.value <= Mathf.Pow(norm, treeFalloff))
                                {
                                    return candPos;
                                }
                            }
                        }
                    }
                    return new Vector2(center.x + Random.Range(-halfWidth, halfWidth), center.z + Random.Range(-halfDepth, halfDepth));
                }

                default: // Uniform
                    return new Vector2(center.x + Random.Range(-halfWidth, halfWidth), center.z + Random.Range(-halfDepth, halfDepth));
            }
        }

        /// <summary>
        /// クラスタ中心をランダム決定
        /// </summary>
        private void SetupClusterCenters()
        {
            _cachedClusterCenters.Clear();
            if (distributionMode != DistributionMode.Clusters) return;

            Vector3 center = AreaCenter;
            float halfWidth = spawnAreaSize.x * 0.5f;
            float halfDepth = spawnAreaSize.y * 0.5f;

            for (int i = 0; i < clusterCount; i++)
            {
                float cx = center.x + Random.Range(-halfWidth * 0.85f, halfWidth * 0.85f);
                float cz = center.z + Random.Range(-halfDepth * 0.85f, halfDepth * 0.85f);
                _cachedClusterCenters.Add(new Vector2(cx, cz));
            }
        }

        /// <summary>
        /// 参照する木々の位置を収集・キャッシュ
        /// </summary>
        private void CollectTreePositions()
        {
            _cachedTreePositions.Clear();

            // 木連動モード または 幹めり込み防止が有効な場合に木を探索
            bool needsTrees = (distributionMode == DistributionMode.TreeProximity ||
                               distributionMode == DistributionMode.TreeRing ||
                               distributionMode == DistributionMode.TreeAvoidance ||
                               preventTrunkOverlap);

            if (!needsTrees) return;

            Transform targetContainer = treeSourceContainer;

            // 未設定時はシーン内から自動探索
            if (targetContainer == null)
            {
                // 1. 他の ProceduralTreeSpawner を探す
                ProceduralTreeSpawner[] spawners = Object.FindObjectsByType<ProceduralTreeSpawner>(FindObjectsSortMode.None);
                foreach (var spawner in spawners)
                {
                    if (spawner != this)
                    {
                        Transform found = spawner.transform.Find(spawner.containerName);
                        if (found != null && found.childCount > 0)
                        {
                            targetContainer = found;
                            break;
                        }
                    }
                }

                // 2. それでも見つからなければ一般的な名前で検索
                if (targetContainer == null)
                {
                    GameObject go = GameObject.Find("TreeSpawner/TreeContainer") ?? GameObject.Find("TreeContainer");
                    if (go != null && go.transform != transform.Find(containerName))
                    {
                        targetContainer = go.transform;
                    }
                }
            }

            if (targetContainer != null)
            {
                for (int i = 0; i < targetContainer.childCount; i++)
                {
                    Transform child = targetContainer.GetChild(i);
                    _cachedTreePositions.Add(child.position);
                }
            }

            if (_cachedTreePositions.Count == 0 && (distributionMode == DistributionMode.TreeProximity || distributionMode == DistributionMode.TreeRing || distributionMode == DistributionMode.TreeAvoidance))
            {
                Debug.LogWarning("[ProceduralTreeSpawner] 木オブジェクトが見つかりませんでした。先に木を生成するか、'Tree Source Container' をインスペクターで指定してください。", this);
            }
        }

        /// <summary>
        /// 1つのオブジェクトをインスタンス化して配置
        /// </summary>
        private GameObject SpawnSingleTree(Vector3 position, Vector3 normal, Transform parent, float scaleMultiplier = 1.0f)
        {
            // ランダムにプレハブを選択
            int prefabIndex = Random.Range(0, treePrefabs.Length);
            GameObject selectedPrefab = treePrefabs[prefabIndex];
            if (selectedPrefab == null) return null;

            // 回転の決定
            Quaternion rotation;
            if (alignToGroundNormal)
            {
                // 地面法線に合わせて傾ける（草など）
                Vector3 targetUp = Vector3.Slerp(Vector3.up, normal, normalAlignmentStrength);
                Quaternion normalRotation = Quaternion.FromToRotation(Vector3.up, targetUp);

                if (randomYRotation)
                {
                    Quaternion randomRot = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                    rotation = normalRotation * randomRot;
                }
                else
                {
                    rotation = normalRotation;
                }
            }
            else
            {
                // 常にワールド真上向き（木など）
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
            }
            else
            {
                treeObj = Instantiate(selectedPrefab, position, rotation, parent);
            }
#else
            treeObj = Instantiate(selectedPrefab, position, rotation, parent);
#endif

            // スケール計算（親子ペアの子の場合は scaleMultiplier で縮小）
            float scaleMod = (scaleVariation > 0f) ? Random.Range(-scaleVariation * 0.5f, scaleVariation * 0.5f) : 0f;
            Vector3 finalScale = new Vector3(
                Mathf.Max(0.01f, (baseScale.x + scaleMod) * scaleMultiplier),
                Mathf.Max(0.01f, (baseScale.y + scaleMod) * scaleMultiplier),
                Mathf.Max(0.01f, (baseScale.z + scaleMod) * scaleMultiplier)
            );
            treeObj.transform.localScale = finalScale;

            // 地面への埋め込み補正（スケールに応じた自然な沈み込み）
            if (groundSinkRatio > 0f)
            {
                treeObj.transform.position -= Vector3.up * (finalScale.y * groundSinkRatio);
            }

            // コライダー削除（草用オプション）
            if (removeColliders)
            {
                Collider[] colliders = treeObj.GetComponentsInChildren<Collider>();
                for (int c = 0; c < colliders.Length; c++)
                {
                    if (Application.isPlaying)
                    {
                        Destroy(colliders[c]);
                    }
                    else
                    {
                        DestroyImmediate(colliders[c]);
                    }
                }
            }

            // 影システム（ShadeObject / 判定コライダー）のセットアップ（木用）
            if (addShadeSystem)
            {
                SetupTreeShade(treeObj);
            }

            return treeObj;
        }

        /// <summary>
        /// 親オブジェクト（大きな岩など）の足元に小さな子オブジェクト（小石など）を添えるように配置
        /// </summary>
        private void SpawnSatellites(Vector3 parentPos, Transform container, List<Vector3> placedPositions)
        {
            for (int s = 0; s < satelliteChildCount; s++)
            {
                float angle = Random.Range(0f, Mathf.PI * 2f);
                float dist = Random.Range(satelliteDistanceRange.x, satelliteDistanceRange.y);
                float satX = parentPos.x + Mathf.Cos(angle) * dist;
                float satZ = parentPos.z + Mathf.Sin(angle) * dist;

                Vector3 rayOrigin = new Vector3(satX, parentPos.y + raycastHeight, satZ);
                RaycastHit[] hits = Physics.RaycastAll(rayOrigin, Vector3.down, raycastMaxDistance, groundLayer, QueryTriggerInteraction.Ignore);

                if (hits != null && hits.Length > 0)
                {
                    System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
                    RaycastHit? validHit = null;

                    for (int h = 0; h < hits.Length; h++)
                    {
                        var hit = hits[h];
                        if (hit.transform == transform || hit.transform.IsChildOf(transform) || hit.transform.IsChildOf(container))
                            continue;
                        validHit = hit;
                        break;
                    }

                    if (!validHit.HasValue) continue;
                    RaycastHit groundHit = validHit.Value;

                    if (Vector3.Angle(groundHit.normal, Vector3.up) > maxSlopeAngle) continue;

                    float childScaleMult = Random.Range(satelliteScaleMultiplier.x, satelliteScaleMultiplier.y);
                    Vector3 childSpawnPos = groundHit.point + Vector3.up * yOffset;

                    SpawnSingleTree(childSpawnPos, groundHit.normal, container, childScaleMult);
                    placedPositions.Add(groundHit.point);
                }
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
        /// 生成したオブジェクトをすべて削除します。
        /// </summary>
        [ContextMenu("Clear Objects")]
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
        /// オブジェクトをまとめる親オブジェクトを取得または作成
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
                    Undo.RegisterCreatedObjectUndo(obj, "Create Container");
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

            // Clustersモードのプレビューギズモ
            if (distributionMode == DistributionMode.Clusters && _cachedClusterCenters != null && _cachedClusterCenters.Count > 0)
            {
                Gizmos.color = new Color(1.0f, 0.8f, 0.2f, 0.6f);
                for (int i = 0; i < _cachedClusterCenters.Count; i++)
                {
                    Vector3 cPos = new Vector3(_cachedClusterCenters[i].x, center.y, _cachedClusterCenters[i].y);
                    Gizmos.DrawWireSphere(cPos, clusterRadius);
                }
            }

            // 木との連動モード（TreeProximity / TreeRing / TreeAvoidance）のプレビューギズモ
            if (distributionMode == DistributionMode.TreeProximity || distributionMode == DistributionMode.TreeRing || distributionMode == DistributionMode.TreeAvoidance)
            {
                if (_cachedTreePositions == null || _cachedTreePositions.Count == 0)
                {
                    CollectTreePositions();
                }

                if (_cachedTreePositions != null && _cachedTreePositions.Count > 0)
                {
                    for (int i = 0; i < _cachedTreePositions.Count; i++)
                    {
                        Vector3 tPos = _cachedTreePositions[i];

                        // 木の影響外側半径（草の広がり範囲）: 緑色
                        Gizmos.color = new Color(0.2f, 0.9f, 0.4f, 0.4f);
                        Gizmos.DrawWireSphere(tPos, treeOuterRadius);

                        // 幹直下の除外半径: 赤色
                        if (treeInnerRadius > 0f)
                        {
                            Gizmos.color = new Color(1.0f, 0.3f, 0.2f, 0.5f);
                            Gizmos.DrawWireSphere(tPos, treeInnerRadius);
                        }
                    }
                }
            }
        }
    }
}
