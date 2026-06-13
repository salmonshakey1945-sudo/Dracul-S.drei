using UnityEngine;

/// <summary>
/// 昼に出現する近接攻撃型の生物系敵。
/// EnemyBase を継承し、接触時に PlayerStats へ直接ダメージを与える。
/// 吸血可能（EnemyHealth.canBeAbsorbed = true）。
/// </summary>
public class EnemyNoon : EnemyBase
{
    [Header("EnemyNoon Settings")]
    [Tooltip("一回の攻撃で与えるダメージ量")]
    public float damage = 10f;

    protected override void Start()
    {
        // 昼専用敵として時間帯を設定
        activeTime = TimeCondition.Day;
        base.Start();
    }

    /// <summary>
    /// プレイヤーが攻撃射程内に入ったときに呼ばれる近接攻撃処理。
    /// </summary>
    /// <param name="target">攻撃対象（Player）</param>
    protected override void OnAttackTarget(GameObject target)
    {
        var playerStats = target.GetComponent<Dracul.Player.PlayerStats>();
        if (playerStats == null) playerStats = target.GetComponentInParent<Dracul.Player.PlayerStats>();
        if (playerStats == null) playerStats = target.GetComponentInChildren<Dracul.Player.PlayerStats>();

        if (playerStats != null)
        {
            playerStats.TakeDamage(damage);
            Debug.Log($"[EnemyNoon] プレイヤーに {damage} のダメージ！");
        }
    }
}
