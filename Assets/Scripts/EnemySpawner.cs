using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Tooltip("生成する敵のプレハブ")]
    public GameObject enemyPrefab;

    [Tooltip("敵を生成する位置のリスト")]
    public Transform[] spawnPoints;

    [Tooltip("敵を生成する間隔（秒）")]
    public float spawnInterval = 10.0f;

    private float timer;

    void Update()
    {
        // プレハブが設定されていない、または生成位置リストが空の場合は何もしない
        if (enemyPrefab == null || spawnPoints == null || spawnPoints.Length == 0) return;

        timer += Time.deltaTime;

        // 指定した間隔ごとに敵を生成
        if (timer >= spawnInterval)
        {
            SpawnEnemy();
            
            // タイマーをリセット
            timer = 0f;
        }
    }

    void SpawnEnemy()
    {
        // 生成位置をランダムに選ぶ
        int randomIndex = Random.Range(0, spawnPoints.Length);
        Transform spawnPoint = spawnPoints[randomIndex];

        // 敵のプレハブを生成
        Instantiate(enemyPrefab, spawnPoint.position, spawnPoint.rotation);
    }
}
