using UnityEngine;
using System.Collections;
using Dracul.Item;

public class EnemyHealth : MonoBehaviour
{
    [Tooltip("敵の最大HP")]
    public int maxHealth = 3;

    [Header("Effects")]
    public GameObject explosionPrefab;
    [Tooltip("1回目の爆発エフェクトが消滅するまでの時間（秒）")]
    public float explosionLifeTime = 1.0f;

    public GameObject finalExplosionPrefab;
    [Tooltip("2回目の爆発エフェクトが消滅するまでの時間（秒）")]
    public float finalExplosionLifeTime = 1.5f;

    [Header("Death Settings")]
    [Tooltip("倒れるモーションにかかる時間（秒）")]
    public float fallDownDuration = 2.0f;
    [Tooltip("倒れ終えてから完全に消滅するまでの待機時間（秒）")]
    public float deadStayTime = 90.0f;

    [Header("Absorption Settings")]
    [Tooltip("吸血された際に回復するBlood量")]
    public float bloodGiveAmount = 30f;
    [Tooltip("この敵を吸血できるか（生物系=true, 機械系=false）")]
    public bool canBeAbsorbed = true;
    [HideInInspector]
    public bool isAbsorbable = false;

    [Header("Investigation Settings")]
    [Tooltip("この敵の死体を調べられるか")]
    public bool canBeInvestigated = false;
    [Range(0f, 1f)]
    [Tooltip("アイテムがドロップされる確率 (0〜1)")]
    public float itemDropProbability = 0.5f;
    [Tooltip("ドロップするアイテムのPickupプレハブ（ItemPickupコンポーネントを持つもの）")]
    public GameObject itemDropPrefab;
    [HideInInspector]
    public bool isInvestigable = false;

    private int currentHealth;
    private bool isDead = false;

    void Start()
    {
        currentHealth = maxHealth;
    }

    /// <summary>
    /// ダメージを受ける処理。HP が 0 以下になったら DeathSequence を開始する。
    /// </summary>
    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            isDead = true;
            StartCoroutine(DeathSequence());
        }
    }

    /// <summary>
    /// 完全に吸血されきったときに呼ばれる。
    /// </summary>
    public void CompleteAbsorption()
    {
        if (!isAbsorbable) return;
        isAbsorbable = false;

        if (finalExplosionPrefab != null)
        {
            GameObject explosion2 = Instantiate(finalExplosionPrefab, transform.position, Quaternion.identity);
            Destroy(explosion2, finalExplosionLifeTime);
        }
        Destroy(gameObject);
    }

    IEnumerator DeathSequence()
    {
        /* --- 1. EnemyBase（および派生クラス）を無効化 --- */
        // EnemyBase を参照するだけでよいため、新しい敵を追加しても変更不要
        EnemyBase enemyScript = GetComponent<EnemyBase>();
        if (enemyScript != null)
        {
            enemyScript.OnDeath();
            enemyScript.enabled = false;
        }

        // NavMeshAgent も停止させる
        var navAgent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (navAgent != null) navAgent.enabled = false;

        // コライダーを物理衝突なし・トリガーのみに変更
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }

        // 状態フラグを設定
        if (canBeAbsorbed) isAbsorbable = true;
        if (canBeInvestigated) isInvestigable = true;

        /* --- 2. アイテムドロップ（確率判定） --- */
        if (canBeInvestigated && itemDropPrefab != null)
        {
            if (Random.value <= itemDropProbability)
            {
                // 死体の少し上にアイテムをスポーン
                Vector3 dropPos = transform.position + Vector3.up * 0.5f;
                Instantiate(itemDropPrefab, dropPos, Quaternion.identity);
                Debug.Log($"[EnemyHealth] {gameObject.name} がアイテムをドロップしました。");
            }
        }

        /* --- 3. 1回目の爆発エフェクト --- */
        if (explosionPrefab != null)
        {
            GameObject explosion1 = Instantiate(explosionPrefab, transform.position, Quaternion.identity);
            Destroy(explosion1, explosionLifeTime);
        }

        /* --- 4. ゆっくり倒れるアニメーション --- */
        Quaternion startRotation = transform.rotation;
        Quaternion targetRotation = Quaternion.Euler(90, transform.eulerAngles.y, 0);

        float elapsedTime = 0f;
        while (elapsedTime < fallDownDuration)
        {
            transform.rotation = Quaternion.Lerp(startRotation, targetRotation, elapsedTime / fallDownDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        transform.rotation = targetRotation;

        /* --- 5. しばらく待機（吸血・調査の猶予時間） --- */
        yield return new WaitForSeconds(deadStayTime);

        /* --- 6. 2回目の爆発エフェクト（完全消滅） --- */
        if (finalExplosionPrefab != null)
        {
            GameObject explosion2 = Instantiate(finalExplosionPrefab, transform.position, Quaternion.identity);
            Destroy(explosion2, finalExplosionLifeTime);
        }

        Destroy(gameObject);
    }
}
