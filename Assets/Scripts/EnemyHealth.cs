using UnityEngine;
using System.Collections;

public class EnemyHealth : MonoBehaviour
{
    [Tooltip("敵の最大HP")]
    public int maxHealth = 3;

    [Header("Effects")]
    public GameObject explosionPrefab; // 1回目の爆発エフェクト
    [Tooltip("1回目の爆発エフェクトが消滅するまでの時間（秒）")]
    public float explosionLifeTime = 1.0f;

    public GameObject finalExplosionPrefab; // 完全消滅時の2回目の爆発エフェクト
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
    [HideInInspector]
    public bool isAbsorbable = false;
    
    private int currentHealth;
    private bool isDead = false;

    void Start()
    {
        // 生成時にHPを最大値に初期化
        currentHealth = maxHealth;
    }

    // ダメージを受ける処理
    public void TakeDamage(int damage)
    {
        if (isDead) return; // 既に死んでいる場合は処理しない

        currentHealth -= damage;
        
        // HPが0以下になったら死亡処理を呼ぶ
        if (currentHealth <= 0)
        {
            isDead = true;
            StartCoroutine(DeathSequence());
        }
    }

    // 完全に吸血されきった時に呼ばれる
    public void CompleteAbsorption()
    {
        if (!isAbsorbable) return;
        isAbsorbable = false;

        // 即座に2回目の爆発エフェクトを出して消滅させる
        if (finalExplosionPrefab != null)
        {
            GameObject explosion2 = Instantiate(finalExplosionPrefab, transform.position, Quaternion.identity);
            Destroy(explosion2, finalExplosionLifeTime);
        }
        Destroy(gameObject);
    }

    IEnumerator DeathSequence()
    {
        /* --- 1. スクリプト・当たり判定の無効化 --- */
        Enemy enemyScript = GetComponent<Enemy>();
        if (enemyScript != null) enemyScript.enabled = false;
        
        Collider col = GetComponent<Collider>();
        if (col != null) 
        {
            // 物理衝突を消し、トリガー（吸血検知用）にする
            col.isTrigger = true;
        }

        // 吸血可能状態にする
        isAbsorbable = true;

        /* --- 2. 1回目の爆発エフェクト --- */
        if (explosionPrefab != null)
        {
            GameObject explosion1 = Instantiate(explosionPrefab, transform.position, Quaternion.identity);
            Destroy(explosion1, explosionLifeTime);
        }

        /* --- 3. ゆっくり倒れるアニメーション --- */
        Quaternion startRotation = transform.rotation;
        // 現在のY軸の向き（向いている方向）を維持したまま、後ろ（または前）に90度倒れる
        Quaternion targetRotation = Quaternion.Euler(90, transform.eulerAngles.y, 0);

        float elapsedTime = 0f;
        while (elapsedTime < fallDownDuration)
        {
            // 時間の経過に合わせて現在の角度から目標の角度へ少しずつ回転させる
            transform.rotation = Quaternion.Lerp(startRotation, targetRotation, elapsedTime / fallDownDuration);
            elapsedTime += Time.deltaTime;
            yield return null; // 1フレーム待機
        }
        transform.rotation = targetRotation; // 最後に確実な角度に設定

        /* --- 4. その場でしばらく待機 --- */
        yield return new WaitForSeconds(deadStayTime);

        /* --- 5. 2回目の爆発エフェクト（完全消滅時） --- */
        if (isAbsorbable) // 吸血されていなければ実行
        {
            if (finalExplosionPrefab != null)
            {
                GameObject explosion2 = Instantiate(finalExplosionPrefab, transform.position, Quaternion.identity);
                Destroy(explosion2, finalExplosionLifeTime);
            }
            
            /* --- 6. 敵自身を完全に削除 --- */
            Destroy(gameObject);
        }
    }
}
