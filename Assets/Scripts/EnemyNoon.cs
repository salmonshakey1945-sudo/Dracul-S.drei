using UnityEngine;

public class EnemyNoon : MonoBehaviour
{
    [Tooltip("敵の移動スピード")]
    public float speed = 3.0f;
    [Tooltip("敵の攻撃力（ぶつかった時のダメージ）")]
    public float damage = 10f;
    
    private Transform player;

    void Start()
    {
        // 「Player」タグを持つオブジェクトを探す
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            // プレイヤーのTransformを取得
            player = playerObj.transform;
        }
        else
        {
            Debug.LogWarning("Playerタグを持つオブジェクトが見つかりません。");
        }
    }

    void Update()
    {
        if (player != null)
        {
            // プレイヤーの方向を向くベクトルを計算
            Vector3 direction = player.position - transform.position;
            
            // 2Dアクションを想定し、Y軸（高さ）方向の移動は制限する
            direction.y = 0;
            
            // 正規化して長さを1にし、一定のスピードで移動させる
            if (direction.magnitude > 0.1f) // 重なり防止のために少し距離を空ける
            {
                direction.Normalize();
                
                // プレイヤーの方向を向く
                transform.rotation = Quaternion.LookRotation(direction);
                
                transform.position += direction * speed * Time.deltaTime;
            }
        }
    }
    // 衝突判定（物理的な当たり判定）
    private void OnCollisionEnter(Collision collision)
    {
        if (!this.enabled) return; // ダウン状態（スクリプト無効化時）はダメージを与えない

        // ぶつかったオブジェクトの名前をログに出力（デバッグ用）
        Debug.Log($"[Enemy] OnCollisionEnter: {collision.gameObject.name} にぶつかりました");

        if (collision.gameObject.CompareTag("Player"))
        {
            DealDamageToPlayer(collision.gameObject);
        }
    }

    private void OnCollisionStay(Collision collision)
    {
    }

    // 衝突判定（トリガー設定の当たり判定）
    private void OnTriggerEnter(Collider other)
    {
        if (!this.enabled) return; // ダウン状態（スクリプト無効化時）はダメージを与えない

        // ぶつかったオブジェクトの名前をログに出力（デバッグ用）
        Debug.Log($"[Enemy] OnTriggerEnter: {other.gameObject.name} に侵入しました");

        if (other.CompareTag("Player"))
        {
            DealDamageToPlayer(other.gameObject);
        }
    }

    // プレイヤーにダメージを与える共通処理
    private void DealDamageToPlayer(GameObject playerObject)
    {
        // 同じオブジェクトだけでなく、親や子オブジェクトにある場合も探す
        var playerStats = playerObject.GetComponent<Dracul.Player.PlayerStats>();
        if (playerStats == null) playerStats = playerObject.GetComponentInParent<Dracul.Player.PlayerStats>();
        if (playerStats == null) playerStats = playerObject.GetComponentInChildren<Dracul.Player.PlayerStats>();

        if (playerStats != null)
        {
            playerStats.TakeDamage(damage);
            Debug.Log($"[Enemy] プレイヤーに {damage} のダメージを与えました！");
        }
        else
        {
            Debug.LogWarning($"[Enemy] {playerObject.name} はPlayerタグがついていますが、PlayerStatsコンポーネントが見つかりません！");
        }
    }
}
