using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Tooltip("敵の移動スピード")]
    public float speed = 3.0f;
    
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
}
