using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 夜に出現する遠距離攻撃型の機械系敵（Delivery Robot）。
/// EnemyBase を継承し、Animator による歩行・攻撃・死亡アニメーションと
/// プレイヤーへの弾丸発射処理を連携。
/// </summary>
[RequireComponent(typeof(Animator))]
public class EnemyNight : EnemyBase
{
    [Header("EnemyNight Settings")]
    [Tooltip("発射する弾のプレハブ")]
    public GameObject bulletPrefab;
    [Tooltip("弾を発射する位置（未設定なら自身の正面・高さ0.6mから発射）")]
    public Transform shootPoint;
    [Tooltip("攻撃モーション開始から弾が発射されるまでの遅延（秒）")]
    public float shootDelay = 0.25f;
    [Tooltip("攻撃後の硬直時間（秒）")]
    public float postAttackDelay = 0.45f;
    [Tooltip("弾のダメージ")]
    public int bulletDamage = 2;
    [Tooltip("発射時の効果音（任意）")]
    public AudioClip shootSound;
    [Tooltip("旋回速度")]
    public float rotationSpeed = 180f;

    [Header("Debug / Test Settings")]
    [Tooltip("trueにすると昼夜チェックを無視して常時活動（テスト配置時に昼でも自己消滅しない）")]
    public bool testIgnoreTimeCondition = false;
    [Tooltip("trueにすると弾のダメージを0にして安全に動作確認可能")]
    public bool testDisableDamage = false;

    private Animator animator;
    private AudioSource audioSource;
    private bool isAttacking = false;
    private bool isDead = false;

    // Animator Parameter Hashes
    private static readonly int WalkingHash = Animator.StringToHash("Walking");

    protected override void Start()
    {
        // テスト用フラグがONなら常時活動（Any）、そうでなければ夜専用（Night）
        if (testIgnoreTimeCondition)
        {
            activeTime = TimeCondition.Any;
            Debug.Log($"[{gameObject.name}] テストモード: 昼夜チェックを無効化（常時活動）しました。");
        }
        else
        {
            activeTime = TimeCondition.Night;
        }

        // 遠距離攻撃のため、停止距離を広めに設定（インスペクターで上書き可）
        if (stoppingDistance <= 1.5f)
            stoppingDistance = 5.0f;

        base.Start();

        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1.0f; // 3D音響
            audioSource.playOnAwake = false;
        }

        Debug.Log($"[{gameObject.name}] ロボ敵初期化完了: 停止距離={stoppingDistance}, 速度={speed}, HP={GetComponent<EnemyHealth>()?.maxHealth}");
    }

    protected override void Update()
    {
        if (isDead) return;

        base.Update();

        UpdateAnimator();

        // 攻撃中または接近停止時はプレイヤー方向を向く
        if (player != null && (isAttacking || (agent != null && agent.remainingDistance <= stoppingDistance)))
        {
            RotateTowards(player.position);
        }
    }

    private void UpdateAnimator()
    {
        if (animator == null) return;

        if (isDead)
        {
            animator.SetBool(WalkingHash, false);
            return;
        }

        // NavMeshAgentの速度に応じて歩行アニメーションを切り替え
        bool isMoving = !isAttacking && agent != null && agent.isOnNavMesh && !agent.isStopped && agent.velocity.magnitude > 0.15f;
        animator.SetBool(WalkingHash, isMoving);
    }

    /// <summary>
    /// プレイヤーが射程内に入ったときに呼ばれる攻撃処理。
    /// </summary>
    /// <param name="target">攻撃対象（Player）</param>
    protected override void OnAttackTarget(GameObject target)
    {
        if (isAttacking || isDead) return;

        StartCoroutine(AttackRoutine());
    }

    private IEnumerator AttackRoutine()
    {
        isAttacking = true;

        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
        }

        // プレイヤー方向へ即座に向きを合わせる
        if (player != null)
        {
            RotateTowards(player.position);
        }

        // 攻撃アニメーションをブレンド再生（Attack または Attack2）
        if (animator != null)
        {
            animator.SetBool(WalkingHash, false);
            string attackAnim = (Random.value < 0.5f) ? "Attack" : "Attack2";
            animator.CrossFadeInFixedTime(attackAnim, 0.12f);
            Debug.Log($"[{gameObject.name}] 攻撃アニメーション再生: {attackAnim}");
        }

        // モーションに合わせたタイミングで弾を発射
        yield return new WaitForSeconds(shootDelay);

        if (!isDead)
        {
            Shoot();
        }

        // 攻撃後の硬直時間
        yield return new WaitForSeconds(postAttackDelay);

        if (!isDead && agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
        }

        isAttacking = false;
    }

    private void Shoot()
    {
        if (bulletPrefab == null)
        {
            Debug.LogWarning($"[{gameObject.name}] bulletPrefab が未設定です。");
            return;
        }

        // 発射位置の決定
        Vector3 spawnPos = shootPoint != null
            ? shootPoint.position
            : transform.position + Vector3.up * 0.6f + transform.forward * 0.6f;

        // プレイヤーの胸元（高さ0.8m）を狙う
        Vector3 targetPos = (player != null)
            ? player.position + Vector3.up * 0.8f
            : spawnPos + transform.forward * 10f;

        // わずかな散弾ブレ（0.15m）
        targetPos += Random.insideUnitSphere * 0.15f;

        Vector3 shootDir = (targetPos - spawnPos).normalized;
        Quaternion spawnRot = Quaternion.LookRotation(shootDir);

        GameObject bulletObj = Instantiate(bulletPrefab, spawnPos, spawnRot);

        // 弾のパラメータを設定
        Bullet bullet = bulletObj.GetComponent<Bullet>();
        if (bullet == null) bullet = bulletObj.GetComponentInChildren<Bullet>();
        if (bullet == null) bullet = bulletObj.GetComponentInParent<Bullet>();

        if (bullet != null)
        {
            bullet.targetTag = "Player";
            bullet.damage = testDisableDamage ? 0 : bulletDamage;
            bullet.SetShooter(gameObject);
        }

        // 発射音
        if (audioSource != null && shootSound != null)
        {
            audioSource.PlayOneShot(shootSound, 0.7f);
        }

        Debug.Log($"[{gameObject.name}] 弾を発射しました (Damage: {(testDisableDamage ? 0 : bulletDamage)})");
    }

    /// <summary>
    /// 目標座標に向けてスムーズに旋回する。
    /// </summary>
    private void RotateTowards(Vector3 targetPos)
    {
        Vector3 dir = targetPos - transform.position;
        dir.y = 0;
        if (dir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }
    }

    /// <summary>
    /// 死亡時の処理。
    /// </summary>
    public override void OnDeath()
    {
        base.OnDeath();
        isDead = true;
        isAttacking = false;

        Debug.Log($"[{gameObject.name}] 死亡アニメーション再生 (Die)");

        if (animator != null)
        {
            animator.SetBool(WalkingHash, false);
            animator.CrossFadeInFixedTime("Die", 0.1f);
        }

        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.enabled = false;
        }
    }
}
