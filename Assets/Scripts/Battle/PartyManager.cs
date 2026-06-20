using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 最大4人のパーティを管理するシングルトン。
///
/// ストーリー設定
///   5人の友達のうち1人（バトルマスター役）が拉致された。
///   残った4人（アレン・リナ・テオ・カイ）が仲間となり冒険へ出発する。
///   最終目標：拉致された友達を救うこと。
/// </summary>
public class PartyManager : MonoBehaviour
{
    public static PartyManager Instance { get; private set; }

    public IReadOnlyList<CharacterStats> Members => members;

    readonly List<CharacterStats> members = new();

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        InitParty();
    }

    // ================================================================
    //  パーティ初期化（ストーリー固定メンバー）
    // ================================================================

    void InitParty()
    {
        //                    名前    職業                          HP  ATK DEF
        members.Add(new CharacterStats("アレン", CharacterClass.Warrior,       80, 18, 10));
        members.Add(new CharacterStats("リナ",   CharacterClass.Mage,           45, 25,  4));
        members.Add(new CharacterStats("テオ",   CharacterClass.Priest,         60, 12,  8));
        members.Add(new CharacterStats("カイ",   CharacterClass.MartialArtist,  65, 20,  6));
    }

    // ================================================================
    //  参照
    // ================================================================

    /// <summary>生存している先頭キャラの攻撃力（戦闘時の基準値）。</summary>
    public int GetLeaderAttack()
    {
        var front = members.Find(m => m.IsAlive);
        return front?.attackPower ?? 10;
    }

    /// <summary>パーティ全員が戦闘不能か。</summary>
    public bool IsAllDefeated() => members.TrueForAll(m => !m.IsAlive);

    // ================================================================
    //  ダメージ
    // ================================================================

    /// <summary>
    /// 敵からのダメージを先頭の生存キャラが受ける。
    /// 先頭キャラが倒れると次のキャラへ自動的に引き継がれる。
    /// </summary>
    public void TakeDamage(int rawDamage)
    {
        var front = members.Find(m => m.IsAlive);
        if (front == null) return;

        int actual = front.TakeDamage(rawDamage);
        Debug.Log($"[Party] {front.characterName} が {actual}ダメージ！  HP:{front.currentHp}/{front.maxHp}");

        if (!front.IsAlive)
        {
            Debug.Log($"[Party] {front.characterName} は戦闘不能になった！");
            if (IsAllDefeated())
                Debug.LogWarning("[Party] ── パーティ全滅！ ゲームオーバー ──");
        }
    }

    // ================================================================
    //  経験値
    // ================================================================

    /// <summary>生存している全員に経験値を配布する。</summary>
    public void AddExperience(int exp)
    {
        foreach (var m in members)
        {
            if (!m.IsAlive) continue;
            if (m.GainExperience(exp))
                Debug.Log($"[Party] ★ {m.characterName} がレベルアップ！ Lv.{m.level}");
        }
    }

    // ================================================================
    //  デバッグ
    // ================================================================

    [ContextMenu("パーティ状態をログ出力")]
    public void PrintParty()
    {
        Debug.Log("=== Party Status ===");
        foreach (var m in members)
            Debug.Log("  " + m.ToString());
    }
}
