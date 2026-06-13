using UnityEngine;

/// <summary>
/// 敵のスポーンを管理する。SpawnEntry のリストに登録した分だけ、
/// 昼夜条件・同時存在数上限を守りながら生成する。
/// 新しい敵を追加するときはこのスクリプトを変更せず、
/// インスペクターの SpawnEntries リストに Prefab を追加するだけでよい。
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    /// <summary>
    /// スポーンするエントリ1件分の設定。インスペクターで複数登録できる。
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
    }

    [Header("Spawn Entries")]
    [Tooltip("スポーンする敵の種類リスト。ここに Prefab を追加するだけで新しい敵に対応できる。")]
    public SpawnEntry[] spawnEntries;

    [Header("Spawn Points")]
    [Tooltip("敵を生成する位置のリスト")]
    public Transform[] spawnPoints;

    [Header("Spawn Timing")]
    [Tooltip("スポーンを試みる間隔（秒）")]
    public float spawnInterval = 10.0f;

    private float timer;

    void Update()
    {
        if (spawnEntries == null || spawnEntries.Length == 0) return;
        if (spawnPoints == null || spawnPoints.Length == 0) return;

        timer += Time.deltaTime;

        if (timer >= spawnInterval)
        {
            TrySpawnAll();
            timer = 0f;
        }
    }

    /// <summary>
    /// 全エントリに対してスポーン条件をチェックし、条件を満たすものを生成する。
    /// </summary>
    private void TrySpawnAll()
    {
        var tm = Dracul.Core.TimeManager.Instance;
        bool isDay = (tm != null) ? tm.IsDay : true;

        foreach (var entry in spawnEntries)
        {
            if (entry.prefab == null) continue;

            // 時間帯チェック
            bool timeOk = entry.spawnTime == TimeCondition.Any
                || (entry.spawnTime == TimeCondition.Day && isDay)
                || (entry.spawnTime == TimeCondition.Night && !isDay);

            if (!timeOk) continue;

            // 同時存在数チェック（同じプレハブ名で数える）
            int currentCount = CountEnemyByName(entry.prefab.name);
            if (currentCount >= entry.maxCount) continue;

            // スポーン実行
            SpawnEnemy(entry.prefab);
        }
    }

    /// <summary>
    /// 指定Prefab名の敵が現在何体存在するかを数える。
    /// </summary>
    private int CountEnemyByName(string prefabName)
    {
        int count = 0;
        // "(Clone)" を除いたオブジェクト名で比較
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
        Debug.Log($"[EnemySpawner] {prefab.name} をスポーンしました。");
    }
}
