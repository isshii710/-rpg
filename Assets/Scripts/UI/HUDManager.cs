using UnityEngine;

/// <summary>
/// OnGUI を使った HUD 表示。Canvas・パッケージ不要。
/// </summary>
public class HUDManager : MonoBehaviour
{
    public static HUDManager Instance { get; private set; }

    // GUIStyle は OnGUI 内で初回だけ生成する（GUI.skin が使える唯一のタイミング）
    GUIStyle styleNormal;
    GUIStyle styleGold;
    GUIStyle styleGreen;
    GUIStyle styleItem;
    bool stylesReady;

    void Awake()
    {
        Instance = this;
    }

    void OnGUI()
    {
        if (!stylesReady) InitStyles();

        DrawGold();
        DrawQuests();
        DrawPartyHP();
        DrawSelectedItem();
    }

    void InitStyles()
    {
        styleNormal = new GUIStyle(GUI.skin.label)
        {
            fontSize  = 20,
            fontStyle = FontStyle.Bold,
        };
        styleNormal.normal.textColor = Color.white;

        styleGold = new GUIStyle(styleNormal) { alignment = TextAnchor.UpperRight };
        styleGold.normal.textColor = new Color(1f, 0.9f, 0.1f);

        styleGreen = new GUIStyle(styleNormal);
        styleGreen.normal.textColor = new Color(0.5f, 1f, 0.5f);

        styleItem = new GUIStyle(styleNormal) { fontSize = 26, alignment = TextAnchor.LowerCenter };
        styleItem.normal.textColor = new Color(1f, 1f, 0.5f);

        stylesReady = true;
    }

    void DrawGold()
    {
        var inv  = InventoryManager.Instance;
        int gold = inv != null ? inv.Gold : 0;
        GUI.Label(new Rect(Screen.width - 210, 10, 200, 36), $"G  {gold}", styleGold);
    }

    void DrawQuests()
    {
        var sm = StoryManager.Instance;
        if (sm == null) return;

        float y = 10f;
        if (sm.HarvestQuest != null) { GUI.Label(new Rect(10, y, 520, 30), QuestLine(sm.HarvestQuest), styleNormal); y += 32; }
        if (sm.MiningQuest  != null) { GUI.Label(new Rect(10, y, 520, 30), QuestLine(sm.MiningQuest),  styleNormal); y += 32; }
        if (sm.CombatQuest  != null) { GUI.Label(new Rect(10, y, 520, 30), QuestLine(sm.CombatQuest),  styleNormal); y += 32; }

        string leaveLabel = sm.CanLeaveVillage ? "旅立ち：解放済み" : "旅立ち：修行中...";
        GUI.Label(new Rect(10, y, 400, 30), leaveLabel, styleGreen);
    }

    void DrawPartyHP()
    {
        var pm = PartyManager.Instance;
        if (pm == null) return;

        float y = Screen.height - 130f;
        foreach (var m in pm.Members)
        {
            GUI.Label(new Rect(10, y, 300, 28), $"{m.characterName}  HP {m.currentHp}/{m.maxHp}", styleNormal);
            y += 30;
        }
    }

    void DrawSelectedItem()
    {
        var gp = GridPlacer.Instance;
        if (gp == null) return;
        string label = gp.SelectedItemLabel;
        if (string.IsNullOrEmpty(label)) return;

        float w = 300f;
        GUI.Label(new Rect((Screen.width - w) * 0.5f, Screen.height - 50, w, 44), $"[ {label} ]", styleItem);
    }

    static string QuestLine(QuestStep q)
        => $"{(q.IsComplete ? "[完]" : "[ ]")} {q.title}  {q.current}/{q.required}";
}
