using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 出現する時間帯の条件。
/// </summary>
public enum TimeCondition
{
    Day,   // 昼のみ
    Night, // 夜のみ
    Any    // 常時
}

/// <summary>
/// 全ての敵キャラクターの基底クラス。
/// NavMeshAgent による移動・時間帯チェック・攻撃範囲判定を共通実装する。
/// 新しい敵を追加する場合はこのクラスを継承し、OnAttackTarget() を実装するだけでよい。
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public abstract class EnemyBase : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("移動速度")]
    public float speed = 3.0f;
    [Tooltip("プレイヤーへの追尾を停止する距離（攻撃射程）")]
    public float stoppingDistance = 1.5f;

    [Header("Attack Settings")]
    [Tooltip("攻撃のインターバル（秒）")]
    public float attackInterval = 1.5f;

    [Header("Time Condition")]
    [Tooltip("この敵が活動できる時間帯")]
    public TimeCondition activeTime = TimeCondition.Any;

    // --- Protected / Internal ---
    protected NavMeshAgent agent;
    protected Transform player;
    protected float attackTimer = 0f;

    protected virtual void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = speed;
        agent.stoppingDistance = stoppingDistance;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
        else
        {
            Debug.LogWarning($"[{gameObject.name}] Playerタグのオブジェクトが見つかりません。");
        }
    }

    protected virtual void Update()
    {
        CheckTimeCondition();
        if (player == null) return;

        MoveTowardPlayer();
        HandleAttackTimer();
    }

    // ── 時間帯チェック ──────────────────────────────────────

    /// <summary>
    /// 現在の時間帯が activeTime と一致しない場合、この敵を自己破棄する。
    /// </summary>
    private void CheckTimeCondition()
    {
        if (activeTime == TimeCondition.Any) return;

        var tm = Dracul.Core.TimeManager.Instance;
        if (tm == null) return;

        bool isDay = tm.IsDay;
        bool shouldBeActive = (activeTime == TimeCondition.Day) ? isDay : !isDay;

        if (!shouldBeActive)
        {
            Destroy(gameObject);
        }
    }

    // ── 移動 ────────────────────────────────────────────────

    /// <summary>
    /// NavMeshAgent を使ってプレイヤーへ追尾する。
    /// stoppingDistance 以内に入ると停止する（NavMeshAgent の設定で制御）。
    /// </summary>
    private void MoveTowardPlayer()
    {
        if (agent == null || !agent.isOnNavMesh) return;
        agent.SetDestination(player.position);
    }

    // ── 攻撃タイマー ────────────────────────────────────────

    /// <summary>
    /// プレイヤーが攻撃射程内に入り、かつインターバルが経過したら OnAttackTarget() を呼ぶ。
    /// </summary>
    private void HandleAttackTimer()
    {
        attackTimer -= Time.deltaTime;

        float dist = Vector3.Distance(transform.position, player.position);
        if (dist <= stoppingDistance && attackTimer <= 0f)
        {
            OnAttackTarget(player.gameObject);
            attackTimer = attackInterval;
        }
    }

    // ── 抽象メソッド（各敵が実装） ──────────────────────────

    /// <summary>
    /// プレイヤーが攻撃射程内に入ったときに呼ばれる。
    /// 近接敵はダメージを与え、遠距離敵は弾を発射するなど、各敵が実装する。
    /// </summary>
    /// <param name="target">攻撃対象のGameObject（Player）</param>
    protected abstract void OnAttackTarget(GameObject target);

    /// <summary>
    /// 死亡時に呼ばれる（任意でオーバーライド可）。演出処理などに使用。
    /// EnemyHealth.cs の DeathSequence から呼ばれる。
    /// </summary>
    public virtual void OnDeath() { }
}
