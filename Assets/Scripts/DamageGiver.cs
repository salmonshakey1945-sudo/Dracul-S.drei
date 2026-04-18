using UnityEngine;

public class DamageGiver : MonoBehaviour
{
    // ダメージ量
    public int damageAmount = 1;

    // 衝突判定
    void OnCollisionEnter(Collision collision)
    {
        // 相手が "Player" タグを持っているか確認
        if (collision.gameObject.CompareTag("Player"))
        {
            // PlayerHealthコンポーネントを取得
            PlayerHealth playerHealth = collision.gameObject.GetComponent<PlayerHealth>();

            // コンポーネントが存在すればダメージを与える
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damageAmount);
            }
        }
    }
}
