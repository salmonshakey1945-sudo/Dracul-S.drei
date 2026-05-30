using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("Bullet Settings")]
    public float speed = 20f;
    public float lifeTime = 3f;
    public int damage = 1; // 弾が与えるダメージ量
    public string targetTag = "Enemy"; // 攻撃対象のタグ

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // Apply forward force/velocity
        // Using 'linearVelocity' for Unity 6 compatibility (formerly 'velocity')
        if (rb != null)
        {
            rb.linearVelocity = transform.forward * speed;
        }

        // Automatically destroy the bullet after 'lifeTime' seconds
        Destroy(gameObject, lifeTime);
    }

    // コライダーが「Is Trigger」オフの場合呼ばれる
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag(targetTag))
        {
            ApplyDamage(collision.gameObject);
            Destroy(gameObject);
        }
    }

    // コライダーが「Is Trigger」オンの場合呼ばれる
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(targetTag))
        {
            ApplyDamage(other.gameObject);
            Destroy(gameObject);
        }
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
