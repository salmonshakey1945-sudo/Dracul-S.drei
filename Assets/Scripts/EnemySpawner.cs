using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 敵のスポーンを管理する。
/// 昼夜条件・同時存在数上限・出現確率の重み（Weight）を守りながら生成する。
/// 新しい敵を追加するときはインスペクターの SpawnEntries リストに追加するだけでよい。
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    /// <summary>
    /// スポーンするエントリー1件分の設定。
    /// </summary>
    [System.Serializable]
    public class SpawnEntry
    {
        [Tooltip("生成する敵のプレハブ（EnemyBase を持つもの）")]
        public GameObject prefab;
        [Tooltip("スポーンする時間帯（Day=昼, Night=夜, Any=常時）")]
        public TimeCondition spawnTime = TimeCondition.Any;
        [Tooltip("この種類の敵の同時存在数の上限")]
        public int maxCount = 5;
        [Tooltip("出現確率の重み（例: 敵ロボ=10, 大型ボス=1 で10:1の比率で抽選）")]
        public float spawnWeight = 1.0f;
    }

    [Header("Spawn Entries")]
    [Tooltip("スポーンする敵の種類リスト。ここに Prefab を追加するだけで新しい敵に対応できる。")]
    public SpawnEntry[] spawnEntries;

    [Header("Spawn Points")]
    [Tooltip("敵を生成する位置のリスト")]
    public Transform[] spawnPoints;

    [Header("Spawn Timing")]
    [Tooltip("スポーンを試みる間隔（秒）")]
    public float spawnInterval = 7.0f;

    private float timer;

    void Update()
    {
        if (spawnEntries == null || spawnEntries.Length == 0) return;
        if (spawnPoints == null || spawnPoints.Length == 0) return;

        timer += Time.deltaTime;

        if (timer >= spawnInterval)
        {
            TrySpawnOne();
            timer = 0f;
        }
    }

    /// <summary>
    /// 現在の時間帯に適合し、上限数に達していないエントリーから
    /// 重み（spawnWeight）に基づいた抽選を行い、1体スポーンする。
    /// </summary>
    private void TrySpawnOne()
    {
        var tm = Dracul.Core.TimeManager.Instance;
        bool isDay = (tm != null) ? tm.IsDay : true;

        // 1. 現在スポーン可能なエントリー（時間帯OK かつ 上限未満）を収集
        List<SpawnEntry> availableEntries = new List<SpawnEntry>();
        float totalWeight = 0f;

        foreach (var entry in spawnEntries)
        {
            if (entry.prefab == null) continue;

            // 時間帯チェック
            bool timeOk = entry.spawnTime == TimeCondition.Any
                || (entry.spawnTime == TimeCondition.Day && isDay)
                || (entry.spawnTime == TimeCondition.Night && !isDay);

            if (!timeOk) continue;

            // 同時存在数チェック
            int currentCount = CountEnemyByName(entry.prefab.name);
            if (currentCount >= entry.maxCount) continue;

            // スポーン可能
            float weight = Mathf.Max(0.01f, entry.spawnWeight);
            availableEntries.Add(entry);
            totalWeight += weight;
        }

        if (availableEntries.Count == 0 || totalWeight <= 0f) return;

        // 2. 重み付けランダム抽選（Weighted Random）
        float roll = Random.Range(0f, totalWeight);
        float currentSum = 0f;
        SpawnEntry selectedEntry = availableEntries[0];

        foreach (var entry in availableEntries)
        {
            float weight = Mathf.Max(0.01f, entry.spawnWeight);
            currentSum += weight;
            if (roll <= currentSum)
            {
                selectedEntry = entry;
                break;
            }
        }

        // 3. 抽選された敵をスポーン
        SpawnEnemy(selectedEntry.prefab);
    }

    /// <summary>
    /// 指定Prefab名の敵が現在何体存在するかを数える。
    /// </summary>
    private int CountEnemyByName(string prefabName)
    {
        int count = 0;
        foreach (var enemy in Object.FindObjectsByType<EnemyBase>(FindObjectsSortMode.None))
        {
            string objName = enemy.gameObject.name.Replace("(Clone)", "").Trim();
            if (objName == prefabName) count++;
        }
        return count;
    }

    /// <summary>
    /// ランダムなスポーンポイントに敵を生成する。
    /// </summary>
    private void SpawnEnemy(GameObject prefab)
    {
        int randomIndex = Random.Range(0, spawnPoints.Length);
        Transform spawnPoint = spawnPoints[randomIndex];

        Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);
        Debug.Log($"[EnemySpawner] {prefab.name} をスポーンしました (時刻: {(Dracul.Core.TimeManager.Instance?.IsDay == true ? "昼" : "夜")})");
    }
}
