using System;
using UnityEngine;

/// <summary>
/// パーティメンバー1人分のステータス。
/// MonoBehaviourではないので、PartyManagerがListで保持する。
/// </summary>
[Serializable]
public class CharacterStats
{
    public string characterName;
    public CharacterClass characterClass;
    public int level         = 1;
    public int maxHp;
    public int currentHp;
    public int attackPower;
    public int defense;
    public int experience;
    public int expToNextLevel;

    public bool IsAlive => currentHp > 0;

    public CharacterStats(string name, CharacterClass cls, int hp, int atk, int def)
    {
        characterName   = name;
        characterClass  = cls;
        maxHp           = currentHp = hp;
        attackPower     = atk;
        defense         = def;
        expToNextLevel  = 100;
    }

    // ----------------------------------------------------------------
    //  ダメージ計算（防御力で軽減、最低1ダメージ保証）
    // ----------------------------------------------------------------

    /// <summary>ダメージを受ける。実際に受けたダメージ量を返す。</summary>
    public int TakeDamage(int rawDamage)
    {
        int actual = Mathf.Max(1, rawDamage - defense);
        currentHp  = Mathf.Max(0, currentHp - actual);
        return actual;
    }

    // ----------------------------------------------------------------
    //  経験値・レベルアップ
    // ----------------------------------------------------------------

    /// <summary>経験値を加算し、レベルアップしたら true を返す。</summary>
    public bool GainExperience(int exp)
    {
        experience += exp;
        if (experience < expToNextLevel) return false;
        LevelUp();
        return true;
    }

    void LevelUp()
    {
        level++;
        experience    -= expToNextLevel;
        expToNextLevel = level * 100;

        // 職業ごとの成長量
        int hpGain  = characterClass switch
        {
            CharacterClass.Warrior       => 15,
            CharacterClass.Priest        => 10,
            CharacterClass.MartialArtist => 12,
            _                            =>  8,  // Mage
        };
        int atkGain = characterClass switch
        {
            CharacterClass.Mage          => 4,
            CharacterClass.MartialArtist => 3,
            _                            => 2,
        };

        maxHp       += hpGain;
        currentHp    = maxHp;   // レベルアップでHP全回復
        attackPower += atkGain;
        defense     += 1;
    }

    public override string ToString() =>
        $"[{characterClass}] {characterName} Lv.{level}  HP:{currentHp}/{maxHp}  ATK:{attackPower}  DEF:{defense}  EXP:{experience}/{expToNextLevel}";
}
