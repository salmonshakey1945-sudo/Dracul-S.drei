using UnityEngine;
using Dracul.Player;
using Dracul.PhysicsEffects;

/// <summary>
/// プレイヤーを一定時間追尾し、追尾時間経過後は直進して着弾・爆発する追尾ミサイル。
/// 地面や建物、ボックス、木などのあらゆる遮蔽物に着弾して爆発します。
/// </summary>
public class HomingMissile : MonoBehaviour
{
    [Header("Missile Settings")]
    [Tooltip("ミサイルの移動速度")]
    public float speed = 16f;

    [Tooltip("旋回追尾の速度（度/秒）")]
    public float rotateSpeed = 170f;

    [Tooltip("プレイヤーを追尾する時間（秒）。この時間を超えると追尾を終了し直進する")]
    public float homingDuration = 1.0f;

    [Tooltip("発射直後の安全マージン時間（秒）。空へ高く打ち上がり自機から離れるまでの時間")]
    public float safetyLaunchTime = 0.4f;

    [Tooltip("ミサイルが自爆するまでの最大寿命（秒）")]
    public float lifeTime = 6f;

    [Header("Explosion Settings")]
    [Tooltip("テスト用: ダメージを無効化する（HPを減らさない）")]
    public bool testDisableDamage = true;

    [Tooltip("直撃・爆発時のダメージ")]
    public int damage = 25;

    [Tooltip("爆発の効果半径")]
    public float explosionRadius = 4.5f;

    [Tooltip("爆発によるノックバック力")]
    public float knockbackForce = 16.0f;

    [Tooltip("上方向のノックバック補正")]
    public float knockbackUpwards = 1.0f;

    [Header("Effects & Audio")]
    [Tooltip("爆発時に生成するプレハブ（パーティクルなど）")]
    public GameObject explosionPrefab;

    [Tooltip("爆発時のサウンド")]
    public AudioClip explosionSound;

    [Header("Target & Shooter")]
    [Tooltip("追尾対象のタグ")]
    public string targetTag = "Player";

    /// <summary>
    /// 発射元（Mech等）のGameObject
    /// </summary>
    public GameObject shooter;

    private Transform targetTransform;
    private float elapsedTime = 0f;
    private bool hasExploded = false;
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = false;
        }

        GameObject playerObj = GameObject.FindGameObjectWithTag(targetTag);
        if (playerObj != null)
        {
            targetTransform = playerObj.transform;
        }

        // 発射元との衝突を物理エンジンレベルでも無視
        if (shooter != null)
        {
            Collider myCol = GetComponent<Collider>();
            if (myCol != null)
            {
                Collider[] shooterCols = shooter.GetComponentsInChildren<Collider>();
                foreach (Collider sc in shooterCols)
                {
                    Physics.IgnoreCollision(myCol, sc, true);
                }
            }
        }
    }

    void Update()
    {
        elapsedTime += Time.deltaTime;

        // 最大寿命チェック
        if (elapsedTime >= lifeTime)
        {
            Explode();
            return;
        }

        // 【放射状打ち上げ後の追尾旋回】
        // safetyLaunchTime（約0.4秒）で空高く放射状に上昇した後、homingDuration（1.0秒）の間プレイヤーを追尾
        if (elapsedTime >= safetyLaunchTime && elapsedTime < (safetyLaunchTime + homingDuration) && targetTransform != null)
        {
            Vector3 targetCenter = targetTransform.position + Vector3.up * 1.0f;
            Vector3 direction = (targetCenter - transform.position).normalized;

            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotateSpeed * Time.deltaTime);
            }
        }

        // 前進移動
        transform.position += transform.forward * speed * Time.deltaTime;
    }

    void OnCollisionEnter(Collision collision)
    {
        HandleCollision(collision.gameObject);
    }

    void OnTriggerEnter(Collider other)
    {
        HandleCollision(other.gameObject);
    }

    private void HandleCollision(GameObject hitObj)
    {
        if (hasExploded) return;

        // 発射直後のセーフティ時間中は自爆しない（空へ登る）
        if (elapsedTime < safetyLaunchTime) return;

        // 発射元自身、発射元の子パーツ、他のEnemy、ミサイル同士は無視
        if (hitObj.CompareTag("Enemy") || hitObj.transform.root.CompareTag("Enemy")) return;
        if (hitObj.GetComponent<HomingMissile>() != null) return;
        if (shooter != null && (hitObj == shooter || hitObj.transform.IsChildOf(shooter.transform))) return;

        // 見えないトリガーゾーン（HellZone、ShadeZone、Area判定など）は無視
        Collider col = hitObj.GetComponent<Collider>();
        if (col != null && col.isTrigger)
        {
            string colName = hitObj.name.ToLower();
            if (colName.Contains("zone") || colName.Contains("area") || colName.Contains("volume") || colName.Contains("shade") || colName.Contains("trigger"))
            {
                return;
            }
        }

        // 【遮蔽物着弾】地面・壁・建物・ボックス・木など、あらゆる遮蔽物またはプレイヤーに着弾して大爆発！
        Explode();
    }

    /// <summary>
    /// 爆発処理
    /// </summary>
    public void Explode()
    {
        if (hasExploded) return;
        hasExploded = true;

        Vector3 explodePos = transform.position;

        // 爆発エフェクトの生成
        if (explosionPrefab != null)
        {
            GameObject fx = Instantiate(explosionPrefab, explodePos, Quaternion.identity);
            Destroy(fx, 3.5f);
        }

        // 爆発音の再生
        if (explosionSound != null)
        {
            AudioSource.PlayClipAtPoint(explosionSound, explodePos);
        }

        // 爆発範囲内の判定
        Collider[] hits = Physics.OverlapSphere(explodePos, explosionRadius);
        foreach (Collider col in hits)
        {
            if (col.CompareTag(targetTag) || col.transform.root.CompareTag(targetTag))
            {
                var playerStats = col.GetComponent<PlayerStats>();
                if (playerStats == null) playerStats = col.GetComponentInParent<PlayerStats>();
                if (playerStats == null) playerStats = col.GetComponentInChildren<PlayerStats>();

                // テスト用ダメージ無効化チェック
                if (playerStats != null && !testDisableDamage && damage > 0)
                {
                    playerStats.TakeDamage(damage);
                }

                var knockback = col.GetComponent<IKnockbackable>();
                if (knockback == null) knockback = col.GetComponentInParent<IKnockbackable>();
                if (knockback == null) knockback = col.GetComponentInChildren<IKnockbackable>();

                if (knockback != null)
                {
                    knockback.ApplyKnockback(knockbackForce, explodePos, explosionRadius, knockbackUpwards);
                }
            }
        }

        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}
