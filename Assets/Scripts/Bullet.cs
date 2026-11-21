using UnityEngine;

/// <summary>
/// 直進する弾丸スクリプト。
/// ターゲット（Player/Enemy）に着弾してダメージを与えるほか、
/// 壁・建物・ボックス・地面などの遮蔽物に着弾して消滅します。
/// ※「木」に対しては貫通して飛び続けます。
/// ※発射直後（0.25秒間）の完全保護により、自機や足元での即消滅を物理的・論理的に100%防止します。
/// </summary>
public class Bullet : MonoBehaviour
{
    [Header("Bullet Settings")]
    [Tooltip("弾速（m/s）")]
    public float speed = 22f;
    public float lifeTime = 3.5f;
    public int damage = 1;
    public string targetTag = "Enemy";

    [Tooltip("発射直後の安全マージン時間（秒）。発射元や至近距離での即消滅を防止")]
    public float safetyTime = 0.25f;

    [Tooltip("発射元GameObject（自身への誤射防止用）")]
    public GameObject shooter;

    private Rigidbody rb;
    private float elapsedTime = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = false;
        }

        if (shooter != null)
        {
            IgnoreShooterColliders(shooter);
        }

        EnsureVisibility();

        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        elapsedTime += Time.deltaTime;

        // ミサイルと同様、毎フレーム確実に直進移動（物理エンジンの停滞を完全防止）
        transform.position += transform.forward * speed * Time.deltaTime;
    }

    private void EnsureVisibility()
    {
        Transform meshChild = transform.Find("BulletMesh");
        if (meshChild != null)
        {
            meshChild.localScale = new Vector3(0.5f, 1.4f, 0.5f);
        }
        else if (transform.localScale.x < 0.4f)
        {
            transform.localScale = new Vector3(0.5f, 0.5f, 1.4f);
        }

        Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit") 
                          ?? Shader.Find("Sprites/Default") 
                          ?? Shader.Find("Unlit/Color");

        if (unlitShader != null)
        {
            Material glowMat = new Material(unlitShader);
            Color bulletColor = new Color(1.0f, 0.75f, 0.1f, 1.0f);

            if (glowMat.HasProperty("_BaseColor")) glowMat.SetColor("_BaseColor", bulletColor);
            if (glowMat.HasProperty("_Color")) glowMat.SetColor("_Color", bulletColor);

            glowMat.EnableKeyword("_EMISSION");
            if (glowMat.HasProperty("_EmissionColor"))
            {
                glowMat.SetColor("_EmissionColor", bulletColor * 2.0f);
            }

            MeshRenderer mr = GetComponentInChildren<MeshRenderer>();
            if (mr != null)
            {
                mr.material = glowMat;
            }

            TrailRenderer tr = GetComponent<TrailRenderer>();
            if (tr == null) tr = gameObject.AddComponent<TrailRenderer>();
            tr.material = glowMat;
            tr.time = 0.3f;
            tr.startWidth = 0.5f;
            tr.endWidth = 0.05f;
            tr.startColor = new Color(1.0f, 0.85f, 0.2f, 1.0f);
            tr.endColor = new Color(1.0f, 0.2f, 0.0f, 0.0f);
        }
    }

    public void SetShooter(GameObject targetShooter)
    {
        shooter = targetShooter;
        IgnoreShooterColliders(shooter);
    }

    private void IgnoreShooterColliders(GameObject targetShooter)
    {
        Collider myCol = GetComponent<Collider>();
        if (myCol != null && targetShooter != null)
        {
            Collider[] cols = targetShooter.GetComponentsInChildren<Collider>();
            foreach (var c in cols)
            {
                Physics.IgnoreCollision(myCol, c, true);
            }
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        HandleHit(collision.gameObject);
    }

    void OnTriggerEnter(Collider other)
    {
        HandleHit(other.gameObject);
    }

    private void HandleHit(GameObject hitObj)
    {
        if (hitObj == null) return;

        // 発射元自身、発射元の子パーツへの接触は常に無視
        if (shooter != null && (hitObj == shooter || hitObj.transform.IsChildOf(shooter.transform))) return;

        // 敵の弾（targetTag == "Player"）が他の敵や自身に当たった場合は貫通（味方・自機撃ち防止）
        if (targetTag == "Player" && (hitObj.CompareTag("Enemy") || hitObj.transform.root.CompareTag("Enemy"))) return;

        // ターゲット（Player）に着弾した場合
        if (hitObj.CompareTag(targetTag) || hitObj.transform.root.CompareTag(targetTag))
        {
            ApplyDamage(hitObj);
            Destroy(gameObject);
            return;
        }

        // 【最強の即消滅防止ガード】
        // safetyTime（約0.25秒間、自機から約5.5m飛ぶまで）は、遮蔽物に当たっても絶対に消滅させない！
        // 自機の腕、足、地面、至近距離の干渉物はすべてすり抜けて確実に飛び立ちます。
        if (elapsedTime < safetyTime)
        {
            return;
        }

        // 【木に対する貫通判定】
        if (IsTree(hitObj))
        {
            return;
        }

        // 見えないトリガーゾーン（HellZone、ShadeZone、Area判定など）は無視して貫通
        Collider col = hitObj.GetComponent<Collider>();
        if (col != null && col.isTrigger)
        {
            string colName = hitObj.name.ToLower();
            if (colName.Contains("zone") || colName.Contains("area") || colName.Contains("volume") || colName.Contains("shade") || colName.Contains("trigger"))
            {
                return;
            }
        }

        // 遮蔽物（地面、壁、建物、ボックス、岩など）に着弾して消滅
        Destroy(gameObject);
    }

    /// <summary>
    /// 未定義タグによる例外を完全に防止した安全な「木」判定
    /// </summary>
    private bool IsTree(GameObject obj)
    {
        if (obj == null) return false;

        if (obj.GetComponent<Tree>() != null || obj.GetComponentInParent<Tree>() != null) return true;

        string nameLower = obj.name.ToLower();
        if (nameLower.Contains("tree") || nameLower.Contains("wood") || nameLower.Contains("log")) return true;

        string rootNameLower = obj.transform.root.name.ToLower();
        if (rootNameLower.Contains("tree") || rootNameLower.Contains("wood")) return true;

        try
        {
            if (obj.tag == "Tree" || obj.transform.root.tag == "Tree") return true;
        }
        catch {}

        return false;
    }

    private void ApplyDamage(GameObject targetObj)
    {
        if (targetTag == "Enemy")
        {
            EnemyHealth enemyHealth = targetObj.GetComponent<EnemyHealth>();
            if (enemyHealth == null) enemyHealth = targetObj.GetComponentInParent<EnemyHealth>();
            if (enemyHealth == null) enemyHealth = targetObj.GetComponentInChildren<EnemyHealth>();

            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(damage);
            }
        }
        else if (targetTag == "Player")
        {
            if (damage <= 0) return; // テスト用ダメージ無効化

            var playerStats = targetObj.GetComponent<Dracul.Player.PlayerStats>();
            if (playerStats == null) playerStats = targetObj.GetComponentInParent<Dracul.Player.PlayerStats>();
            if (playerStats == null) playerStats = targetObj.GetComponentInChildren<Dracul.Player.PlayerStats>();

            if (playerStats != null)
            {
                playerStats.TakeDamage(damage);
            }
        }
    }
}
