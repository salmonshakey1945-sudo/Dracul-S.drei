using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    // 1. 最大体力 (maxHealth) と現在の体力 (currentHealth) を持つ
    public int maxHealth = 10;
    public int currentHealth;

    void Start()
    {
        // 初期化：現在の体力を最大体力に設定
        currentHealth = maxHealth;
        // UI更新
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateLifeText(currentHealth);
        }
    }

    // 2. ダメージを受けるメソッド TakeDamage(int amount) を持つ
    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        Debug.Log("Player took damage: " + amount + " Current Health: " + currentHealth);

        // UI更新
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateLifeText(currentHealth);
        }

        // 3. 体力が0以下になったら、GameManager.instance.GameOver() を呼び出してゲームオーバーにする
        if (currentHealth <= 0)
        {
            // 体力が負にならないように0に固定（表示上の都合など）
            currentHealth = 0;
            Die();
        }
    }

    void Die()
    {
        // GameManagerのGameOverメソッドを呼び出す
        // GameManager側のプロパティ名がCapital Case (Instance) であることを確認済みのため修正して使用
        if (GameManager.Instance != null)
        {
            GameManager.Instance.GameOver();
        }
        else
        {
            Debug.LogError("GameManager Instance not found!");
        }

        // プレイヤーを非アクティブにするなどの追加処理があればここに記述
        // gameObject.SetActive(false); 
    }
}
