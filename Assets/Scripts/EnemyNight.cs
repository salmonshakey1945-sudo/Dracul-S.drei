using UnityEngine;

public class EnemyNight : MonoBehaviour
{
    [Tooltip("敵の移動スピード")]
    public float speed = 3.0f;
    [Tooltip("プレイヤーとの適切な距離（この距離を維持する）")]
    public float stoppingDistance = 5.0f;

    [Header("Shooting Settings")]
    [Tooltip("発射する弾のプレハブ")]
    public GameObject bulletPrefab;
    [Tooltip("弾を発射する間隔（秒）")]
    public float shootInterval = 2.0f;
    [Tooltip("弾の発射位置（未設定なら自身のアタッチ位置）")]
    public Transform shootPoint;

    private Transform player;
    private float shootTimer;

    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
        else
        {
            Debug.LogWarning("Playerタグを持つオブジェクトが見つかりません。");
        }
        shootTimer = shootInterval; // 初回射撃までの準備時間
    }

    void Update()
    {
        if (player != null)
        {
            Vector3 direction = player.position - transform.position;
            direction.y = 0; // 2D平面上の動きを想定

            float distance = direction.magnitude;

            if (distance > 0.1f)
            {
                direction.Normalize();
                
                // プレイヤーの方向を向く
                transform.rotation = Quaternion.LookRotation(direction);

                // 距離が停止距離より遠い場合のみ近づく
                if (distance > stoppingDistance)
                {
                    transform.position += direction * speed * Time.deltaTime;
                }
            }

            // 射撃処理
            shootTimer -= Time.deltaTime;
            if (shootTimer <= 0f)
            {
                Shoot();
                shootTimer = shootInterval;
            }
        }
    }

    void Shoot()
    {
        if (bulletPrefab == null) return;

        Vector3 spawnPos = shootPoint != null ? shootPoint.position : transform.position + transform.forward * 1.0f;
        Quaternion spawnRot = transform.rotation;

        GameObject bulletObj = Instantiate(bulletPrefab, spawnPos, spawnRot);
        
        // 弾のターゲットをPlayerに設定する
        Bullet bullet = bulletObj.GetComponent<Bullet>();
        if (bullet == null) bullet = bulletObj.GetComponentInChildren<Bullet>();
        if (bullet == null) bullet = bulletObj.GetComponentInParent<Bullet>();

        if (bullet != null)
        {
            bullet.targetTag = "Player";
        }
    }
}
