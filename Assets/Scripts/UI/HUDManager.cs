using UnityEngine;
using TMPro;

/// <summary>
/// ゲーム中の HUD を更新する MonoBehaviour。
/// Canvas 直下の各 TextMeshProUGUI を Inspector でアサインする。
/// </summary>
public class HUDManager : MonoBehaviour
{
    public static HUDManager Instance { get; private set; }

    [Header("ゴールド（右上）")]
    [SerializeField] TMP_Text goldText;

    [Header("クエスト進捗（左上）")]
    [SerializeField] TMP_Text harvestQuestText;
    [SerializeField] TMP_Text miningQuestText;
    [SerializeField] TMP_Text combatQuestText;
    [SerializeField] TMP_Text leaveStatusText;

    [Header("パーティ HP（左下）")]
    [SerializeField] TMP_Text partyHPText;

    [Header("選択中アイテム（下中央）")]
    [SerializeField] TMP_Text selectedItemText;

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        UpdateGold();
        UpdateQuests();
        UpdatePartyHP();
        UpdateSelectedItem();
    }

    void UpdateGold()
    {
        if (goldText == null) return;
        var inv = InventoryManager.Instance;
        int gold = inv != null ? inv.Gold : 0;
        goldText.text = $"G {gold}";
    }

    void UpdateQuests()
    {
        var sm = StoryManager.Instance;
        if (sm == null) return;

        if (harvestQuestText != null && sm.HarvestQuest != null)
            harvestQuestText.text = QuestLine(sm.HarvestQuest);

        if (miningQuestText != null && sm.MiningQuest != null)
            miningQuestText.text = QuestLine(sm.MiningQuest);

        if (combatQuestText != null && sm.CombatQuest != null)
            combatQuestText.text = QuestLine(sm.CombatQuest);

        if (leaveStatusText != null)
            leaveStatusText.text = sm.CanLeaveVillage ? "旅立ち：解放済み ✓" : "旅立ち：修行中...";
    }

    void UpdatePartyHP()
    {
        if (partyHPText == null) return;
        var pm = PartyManager.Instance;
        if (pm == null) { partyHPText.text = ""; return; }

        var members = pm.Members;
        if (members == null || members.Count == 0) { partyHPText.text = ""; return; }

        var sb = new System.Text.StringBuilder();
        foreach (var m in members)
            sb.AppendLine($"{m.characterName}  HP {m.currentHp}/{m.maxHp}");

        partyHPText.text = sb.ToString().TrimEnd();
    }

    void UpdateSelectedItem()
    {
        if (selectedItemText == null) return;
        var gp = GridPlacer.Instance;
        selectedItemText.text = gp != null ? $"[{gp.SelectedItemLabel}]" : "";
    }

    static string QuestLine(QuestStep q)
        => $"{(q.IsComplete ? "✓" : "・")} {q.title}  {q.current}/{q.required}";
}
