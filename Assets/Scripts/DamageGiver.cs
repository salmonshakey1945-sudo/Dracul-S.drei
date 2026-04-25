using UnityEngine;
using Dracul.Player;

public class DamageGiver : MonoBehaviour
{
    // ダメージ量
    public int damageAmount = 1;

    // 衝突判定 (Rigidbody同士、または通常のコライダー)
    void OnCollisionEnter(Collision collision)
    {
        ApplyDamage(collision.gameObject);
    }

    // トリガー接触判定 (CharacterControllerやIsTriggerを使用している場合)
    void OnTriggerEnter(Collider other)
    {
        ApplyDamage(other.gameObject);
    }

    // ダメージ適用の共通処理
    private void ApplyDamage(GameObject target)
    {
        // 相手が "Player" タグを持っているか確認
        if (target.CompareTag("Player"))
        {
            // PlayerStatsコンポーネントを取得
            PlayerStats playerStats = target.GetComponent<PlayerStats>();

            // コンポーネントが存在すればダメージを与える
            if (playerStats != null)
            {
                playerStats.TakeDamage(damageAmount);
            }
        }
    }
}
