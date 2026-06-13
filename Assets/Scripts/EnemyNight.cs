using UnityEngine;

/// <summary>
/// 夜に出現する遠距離攻撃型の機械系敵。
/// EnemyBase を継承し、一定間隔でプレイヤーに向けて弾を発射する。
/// 吸血不可・アイテムドロップ可能（EnemyHealth インスペクターで設定）。
/// </summary>
public class EnemyNight : EnemyBase
{
    [Header("EnemyNight Settings")]
    [Tooltip("発射する弾のプレハブ")]
    public GameObject bulletPrefab;
    [Tooltip("弾を発射する位置（未設定なら自身の正面1mから発射）")]
    public Transform shootPoint;

    protected override void Start()
    {
        // 夜専用敵として時間帯を設定
        activeTime = TimeCondition.Night;

        // 遠距離攻撃のため、停止距離を広めに設定（インスペクターで上書き可）
        if (stoppingDistance <= 1.5f)
            stoppingDistance = 5.0f;

        base.Start();
    }

    /// <summary>
    /// プレイヤーが攻撃射程内に入ったときに呼ばれる射撃処理。
    /// </summary>
    /// <param name="target">攻撃対象（Player）</param>
    protected override void OnAttackTarget(GameObject target)
    {
        Shoot();
    }

    private void Shoot()
    {
        if (bulletPrefab == null)
        {
            Debug.LogWarning("[EnemyNight] bulletPrefab が未設定です。");
            return;
        }

        Vector3 spawnPos = shootPoint != null
            ? shootPoint.position
            : transform.position + transform.forward * 1.0f;
        Quaternion spawnRot = transform.rotation;

        GameObject bulletObj = Instantiate(bulletPrefab, spawnPos, spawnRot);

        // 弾のターゲットをPlayerに設定
        Bullet bullet = bulletObj.GetComponent<Bullet>();
        if (bullet == null) bullet = bulletObj.GetComponentInChildren<Bullet>();
        if (bullet == null) bullet = bulletObj.GetComponentInParent<Bullet>();

        if (bullet != null)
        {
            bullet.targetTag = "Player";
        }

        Debug.Log("[EnemyNight] 弾を発射！");
    }
}
