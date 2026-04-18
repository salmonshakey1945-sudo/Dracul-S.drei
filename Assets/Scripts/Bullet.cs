using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("Bullet Settings")]
    public float speed = 20f;
    public float lifeTime = 3f;
    public int damage = 1; // 弾が与えるダメージ量

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
        // 敵に当たったかチェック
        if (collision.gameObject.CompareTag("Enemy"))
        {
            EnemyHealth enemyHealth = collision.gameObject.GetComponent<EnemyHealth>();
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(damage);
            }

            // 何かに当たったら弾自身を消す
            Destroy(gameObject);
        }
    }

    // コライダーが「Is Trigger」オンの場合呼ばれる
    void OnTriggerEnter(Collider other)
    {
        // 敵に当たったかチェック
        if (other.CompareTag("Enemy"))
        {
            EnemyHealth enemyHealth = other.GetComponent<EnemyHealth>();
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(damage);
            }

            // 何かに当たったら弾自身を消す
            Destroy(gameObject);
        }
    }
}
