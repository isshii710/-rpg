using UnityEngine;

/// <summary>
/// 修行クエスト1件分の進捗を保持する。
/// StoryManager がフィールドとして持つ。
/// </summary>
[System.Serializable]
public class QuestStep
{
    public string title;
    public int    required;
    public int    current;

    public bool IsComplete => current >= required;

    public QuestStep(string title, int required)
    {
        this.title    = title;
        this.required = required;
        this.current  = 0;
    }

    /// <summary>
    /// 進捗を amount 分進める。
    /// すでに完了済みなら何もしない。
    /// 完了した瞬間なら true を返す。
    /// </summary>
    public bool Advance(int amount = 1)
    {
        if (IsComplete) return false;

        current = Mathf.Min(required, current + amount);

        if (IsComplete)
            Debug.Log($"[Story] ✓ クリア: {title}");
        else
            Debug.Log($"[Story] {title}: {current}/{required}");

        return IsComplete;
    }
}
