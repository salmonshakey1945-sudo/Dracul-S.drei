using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 上位敵（ボス級）メカのAIスクリプト。
/// プレイヤーと一定の距離（中距離）を維持しながら、
/// 機関銃（小キャノン連射）と追尾ミサイル（大キャノン）による遠距離攻撃を行う。
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(AudioSource))]
public class EnemyMech : EnemyBase
{
    [Header("Debug Settings")]
    [Tooltip("テスト用: 弾やミサイルのダメージを無効化する（HPを減らさない。テスト後にチェックを外してください）")]
    public bool testDisableDamage = true;

    [Header("Damage Settings")]
    [Tooltip("機関銃の1発あたりのダメージ数")]
    public int machineGunDamage = 4;

    [Tooltip("追尾ミサイルの1発あたりの直撃・爆発ダメージ数")]
    public int missileDamage = 25;

    [Header("Mech Distance & Kiting Settings")]
    [Tooltip("プレイヤーと保ちたい理想の距離")]
    public float idealDistance = 11.0f;

    [Tooltip("この距離よりプレイヤーが近づいたら後退する")]
    public float minDistance = 7.0f;

    [Tooltip("この距離よりプレイヤーが離れたら接近する")]
    public float maxDistance = 16.0f;

    [Tooltip("追跡時の移動速度")]
    public float chaseSpeed = 4.0f;

    [Tooltip("後退時の移動速度")]
    public float retreatSpeed = 3.5f;

    [Tooltip("旋回速度（度/秒）")]
    public float rotationSpeed = 140.0f;

    [Header("Weapons & Projectiles")]
    [Tooltip("機関銃の弾丸プレハブ（Bulletコンポーネント付き）")]
    public GameObject machineGunBulletPrefab;

    [Tooltip("追尾ミサイルのプレハブ（HomingMissileコンポーネント付き）")]
    public GameObject homingMissilePrefab;

    [Tooltip("ミサイルの追尾時間（秒）。これを過ぎると直進する")]
    public float missileHomingDuration = 1.0f;

    [Header("Muzzle Transforms")]
    public Transform muzzleSmallL;
    public Transform muzzleSmallR;
    public Transform muzzleBigL;
    public Transform muzzleBigR;

    [Header("Audio Clips")]
    public AudioClip audioSmallCanon;
    public AudioClip audioBigCanon;
    public AudioClip audioFootStep;

    [Header("Aiming & Animation Settings")]
    [Tooltip("上半身ボーンを水平に旋回させてプレイヤーを常に照準するか")]
    public bool enableBodyAim = true;

    [Tooltip("照準を行う上半身（Body）ボーン")]
    public Transform bodyBone;

    [Tooltip("左右の照準方向を反転するか（逆を向く場合にチェックを入れてください）")]
    public bool invertAimYaw = false;

    [Tooltip("ミサイル発射時の仰角の向きを反転するか（下を向く場合にチェックを入れてください）")]
    public bool invertMissilePitch = false;

    [Tooltip("上半身の最大左右照準角度（度）。首が回りすぎるのを防止")]
    public float maxAimYawAngle = 60.0f;

    [Tooltip("上半身の追従速度")]
    public float aimSpeed = 8.0f;

    [Tooltip("ミサイル発射時に上半身を空へ向かせる仰角角度（度）")]
    public float missileAimUpAngle = 25.0f;

    // --- Private Variables ---
    private Animator animator;
    private AudioSource audioSource;
    private bool isAttacking = false;
    private float currentCooldown = 0f;
    private bool useLeftSmallMuzzle = false;
    private bool isRetreating = false;

    // 水平照準角の補間変数
    private float currentAimYaw = 0f;

    // ミサイル発射時の仰角アニメーション制御
    private bool isPitchingBody = false;
    private float currentPitchAngle = 0f;

    // Animator Parameter Hashes
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int ShootSmallTrigger = Animator.StringToHash("ShootSmall");
    private static readonly int ShootBigTrigger = Animator.StringToHash("ShootBig");
    private static readonly int HitTrigger = Animator.StringToHash("Hit");

    protected override void Start()
    {
        activeTime = TimeCondition.Any;

        base.Start();

        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();

        if (bodyBone == null)
        {
            bodyBone = transform.Find("Mech/Root/Pelvis/Body");
        }

        agent.speed = chaseSpeed;
        agent.stoppingDistance = 0.5f;
        currentCooldown = attackInterval;
    }

    protected override void Update()
    {
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
            if (player == null) return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (currentCooldown > 0f)
        {
            currentCooldown -= Time.deltaTime;
        }

        // 間合い管理 & 移動処理
        HandleDistanceAndMovement(distanceToPlayer);

        // 攻撃判定
        if (!isAttacking && currentCooldown <= 0f && distanceToPlayer <= maxDistance + 2.0f)
        {
            DecideAndExecuteAttack(distanceToPlayer);
        }

        UpdateAnimator();
    }

    protected virtual void LateUpdate()
    {
        if (bodyBone == null) return;

        // ミサイル射撃中の仰角（Pitch）
        float pitchVal = isPitchingBody ? currentPitchAngle : 0f;
        if (invertMissilePitch) pitchVal = -pitchVal;

        // 通常時のプレイヤー水平追従計算
        if (enableBodyAim && player != null && !isPitchingBody)
        {
            Vector3 toPlayer = player.position - transform.position;
            toPlayer.y = 0;
            if (toPlayer != Vector3.zero)
            {
                float angle = Vector3.SignedAngle(transform.forward, toPlayer.normalized, Vector3.up);
                if (invertAimYaw) angle = -angle;

                float targetYaw = Mathf.Clamp(angle, -maxAimYawAngle, maxAimYawAngle);
                currentAimYaw = Mathf.Lerp(currentAimYaw, targetYaw, aimSpeed * Time.deltaTime);
            }
        }
        else if (isPitchingBody)
        {
            // ミサイル発射中は正面（0度）を基準に上を向く
            currentAimYaw = Mathf.Lerp(currentAimYaw, 0f, 10f * Time.deltaTime);
        }

        // 【確定解】
        // X軸 = 左右旋回（Yaw）
        // Y軸 = 上下仰角（Pitch）※Z軸に入れると右に傾いてしまうため、上下軸であるY軸で上を向かせる！
        // Z軸 = ロール（左右傾き）※0度で水平を保ち、右に倒れるのを完全に防止！
        bodyBone.localRotation = Quaternion.Euler(currentAimYaw, 180f - pitchVal, 0f);
    }

    // ── 間合い管理 & 移動 ───────────────────────────────────

    private void HandleDistanceAndMovement(float distanceToPlayer)
    {
        if (agent == null || !agent.isOnNavMesh) return;

        // 攻撃動作中の場合は完全停止してプレイヤーを向く
        if (isAttacking)
        {
            agent.isStopped = true;
            isRetreating = false;
            RotateTowardsPlayer();
            return;
        }

        if (distanceToPlayer < minDistance)
        {
            // 【後退（カイト行動）】
            isRetreating = true;
            agent.isStopped = true;

            Vector3 retreatDir = (transform.position - player.position).normalized;
            retreatDir.y = 0;

            if (retreatDir != Vector3.zero)
            {
                agent.Move(retreatDir * retreatSpeed * Time.deltaTime);
            }

            RotateTowardsPlayer();
        }
        else if (distanceToPlayer > idealDistance + 1.5f)
        {
            // 【接近】
            isRetreating = false;
            agent.isStopped = false;
            agent.speed = chaseSpeed;
            agent.SetDestination(player.position);
        }
        else
        {
            // 【適正距離】
            isRetreating = false;
            agent.isStopped = true;
            RotateTowardsPlayer();
        }
    }

    private void RotateTowardsPlayer()
    {
        if (player == null) return;

        Vector3 lookDir = (player.position - transform.position).normalized;
        lookDir.y = 0;
        if (lookDir != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(lookDir);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }
    }

    // ── 攻撃ロジック ────────────────────────────────────────

    private void DecideAndExecuteAttack(float distanceToPlayer)
    {
        bool shootMissile = false;
        if (distanceToPlayer >= idealDistance)
        {
            shootMissile = Random.value < 0.65f;
        }
        else
        {
            shootMissile = Random.value < 0.35f;
        }

        if (shootMissile && homingMissilePrefab != null)
        {
            StartCoroutine(AttackMissileRoutine());
        }
        else if (machineGunBulletPrefab != null)
        {
            StartCoroutine(AttackMachineGunRoutine());
        }
    }

    /// <summary>
    /// 機関銃攻撃ルーチン。
    /// 発射直前にプレイヤーの方向へ素早くしっかり向き直ってから、バースト射撃を開始します。
    /// </summary>
    private IEnumerator AttackMachineGunRoutine()
    {
        isAttacking = true;
        agent.isStopped = true;

        // 【発射直前】プレイヤーの方向へ素早くしっかりと向き直る（約0.3秒間）
        float aimDuration = 0.3f;
        float elapsed = 0f;
        while (elapsed < aimDuration)
        {
            elapsed += Time.deltaTime;
            if (player != null)
            {
                Vector3 lookDir = (player.position - transform.position).normalized;
                lookDir.y = 0;
                if (lookDir != Vector3.zero)
                {
                    Quaternion targetRot = Quaternion.LookRotation(lookDir);
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, 360f * Time.deltaTime);
                }
            }
            yield return null;
        }

        // ロボが正面を向いた後に射撃モーション開始
        if (animator != null)
        {
            animator.SetTrigger(ShootSmallTrigger);
        }

        yield return new WaitForSeconds(0.15f);

        // 6発バースト射撃（射撃中もプレイヤーを追従照準）
        int burstCount = 6;
        for (int i = 0; i < burstCount; i++)
        {
            RotateTowardsPlayer();
            FireSmallCanonBullet();
            yield return new WaitForSeconds(0.12f);
        }

        yield return new WaitForSeconds(0.4f);
        isAttacking = false;
        currentCooldown = attackInterval;
    }

    /// <summary>
    /// 追尾ミサイル攻撃シーケンス。
    /// 上半身が上空25度へ起き上がり、ミサイル発射後、水平へ戻ります。
    /// </summary>
    private IEnumerator AttackMissileRoutine()
    {
        isAttacking = true;
        agent.isStopped = true;

        if (animator != null)
        {
            animator.SetTrigger(ShootBigTrigger);
        }

        isPitchingBody = true;
        currentPitchAngle = 0f;

        // 1. 上半身が25度起き上がる（約0.35秒）
        float liftDuration = 0.35f;
        float elapsed = 0f;
        while (elapsed < liftDuration)
        {
            elapsed += Time.deltaTime;
            currentPitchAngle = Mathf.SmoothStep(0f, missileAimUpAngle, elapsed / liftDuration);
            yield return null;
        }
        currentPitchAngle = missileAimUpAngle;

        yield return new WaitForSeconds(0.1f);

        // 2. 25度起き上がった状態でミサイル発射！
        FireBigCanonMissile(isLeft: true);
        yield return new WaitForSeconds(0.35f);
        FireBigCanonMissile(isLeft: false);

        yield return new WaitForSeconds(0.3f);

        // 3. ミサイルがすべて撃ち終わった後、通常の角度へ戻る
        float returnDuration = 0.35f;
        elapsed = 0f;
        while (elapsed < returnDuration)
        {
            elapsed += Time.deltaTime;
            currentPitchAngle = Mathf.SmoothStep(missileAimUpAngle, 0f, elapsed / returnDuration);
            yield return null;
        }
        currentPitchAngle = 0f;
        isPitchingBody = false;

        yield return new WaitForSeconds(0.2f);

        // 4. ミサイル攻撃終了（移動再開可能）
        isAttacking = false;
        currentCooldown = attackInterval + 1.0f;
    }

    private void FireSmallCanonBullet()
    {
        if (machineGunBulletPrefab == null) return;

        Transform muzzle = useLeftSmallMuzzle ? muzzleSmallL : muzzleSmallR;
        useLeftSmallMuzzle = !useLeftSmallMuzzle;

        Vector3 basePos = (muzzle != null) ? muzzle.position : transform.position + Vector3.up * 1.5f;

        // プレイヤーへの射撃方向（胸元を狙う）
        Vector3 targetPoint = (player != null) ? player.position + Vector3.up * 1.0f : transform.position + transform.forward * 10f;
        targetPoint += Random.insideUnitSphere * 0.2f; // 散布ブレ
        Vector3 shootDir = (targetPoint - basePos).normalized;

        // ロボの当たり判定（半径1.3m）を完全に超えた安全な前方から発射
        Vector3 spawnPos = basePos + shootDir * 2.2f;
        // 地面に埋まらないよう高さを保証（足元+1.2m以上）
        spawnPos.y = Mathf.Max(spawnPos.y, transform.position.y + 1.2f);

        Quaternion spawnRot = Quaternion.LookRotation(shootDir);

        GameObject bullet = Instantiate(machineGunBulletPrefab, spawnPos, spawnRot);
        Bullet b = bullet.GetComponent<Bullet>();
        if (b != null)
        {
            b.targetTag = "Player";
            b.SetShooter(gameObject);
            b.damage = testDisableDamage ? 0 : machineGunDamage;
        }

        if (audioSource != null && audioSmallCanon != null)
        {
            audioSource.PlayOneShot(audioSmallCanon, 0.8f);
        }
    }

    private void FireBigCanonMissile(bool isLeft)
    {
        if (homingMissilePrefab == null) return;

        Transform muzzle = isLeft ? muzzleBigL : muzzleBigR;
        Vector3 basePos = (muzzle != null) ? muzzle.position : transform.position + Vector3.up * 2.5f;

        Vector3 spreadRight = (isLeft ? -transform.right : transform.right) * 0.6f;
        Vector3 launchDir = (transform.forward * 0.25f + Vector3.up * 1.15f + spreadRight).normalized;

        Vector3 spawnPos = basePos + launchDir * 1.5f;
        Quaternion spawnRot = Quaternion.LookRotation(launchDir);

        GameObject missileObj = Instantiate(homingMissilePrefab, spawnPos, spawnRot);
        HomingMissile missile = missileObj.GetComponent<HomingMissile>();
        if (missile != null)
        {
            missile.homingDuration = missileHomingDuration;
            missile.shooter = gameObject;
            missile.targetTag = "Player";
            missile.testDisableDamage = testDisableDamage;
            missile.damage = missileDamage;
        }

        if (audioSource != null && audioBigCanon != null)
        {
            audioSource.PlayOneShot(audioBigCanon, 1.0f);
        }
    }

    // ── アニメーションイベント受取 ──────────────────────────

    public void ShootSmallCanonA() => FireSmallCanonBullet();
    public void ShootSmallCanonB() => FireSmallCanonBullet();
    public void ShootBigCanonA() => FireBigCanonMissile(true);
    public void ShootBigCanonB() => FireBigCanonMissile(false);

    public void FootStep()
    {
        if (audioSource != null && audioFootStep != null)
        {
            audioSource.PlayOneShot(audioFootStep, 0.4f);
        }
    }
    public void EndOfRun() { }
    public void EndOfRunJump() { }

    public void PlayHitReaction()
    {
        if (animator != null)
        {
            animator.SetTrigger(HitTrigger);
        }
    }

    public override void OnDeath()
    {
        base.OnDeath();
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
        }
    }

    protected override void OnAttackTarget(GameObject target) { }

    private void UpdateAnimator()
    {
        if (animator == null) return;

        float speedVal = isRetreating ? 1.0f : ((agent != null && !agent.isStopped) ? agent.velocity.magnitude : 0f);
        animator.SetFloat(SpeedHash, speedVal);
    }
}
