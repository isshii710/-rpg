using UnityEngine;

public enum NpcShopType { None, Weapon, Armor, Inn }

/// <summary>
/// NPC 制御コンポーネント（村人・商店主・宿屋など）。
///
/// 【操作】
///   E キー : 近くの NPC と話す / ショップを開く / 宿屋で回復
///
/// 【設定】
///   npcName       : NPC の名前
///   shopType      : None=台詞のみ / Weapon=武器屋 / Armor=防具屋 / Inn=宿屋
///   dialogueLines : 会話台詞（None 時にランダム or 順番に表示）
///   canWander     : true で原点周辺をウロウロする
/// </summary>
public class NpcController : MonoBehaviour
{
    [Header("NPC 設定")]
    [SerializeField] public string npcName = "村人";
    [SerializeField] public NpcShopType shopType = NpcShopType.None;
    [SerializeField] public string[] dialogueLines = { "こんにちは！" };
    [SerializeField] float interactRadius = 1.8f;

    [Header("徘徊")]
    [SerializeField] bool canWander = false;
    [SerializeField] float wanderRadius = 3f;
    [SerializeField] float moveSpeed = 1.4f;

    Transform playerTransform;
    bool inRange;
    int dialogueIndex;

    Vector3 origin;
    Vector3 wanderTarget;
    float wanderTimer;

    static GUIStyle hintStyle;
    static bool hintStyleReady;

    void Start()
    {
        var player = GameObject.Find("Player");
        if (player != null) playerTransform = player.transform;
        origin = transform.position;
        wanderTarget = origin;
    }

    void Update()
    {
        if (playerTransform == null) return;

        inRange = Vector3.Distance(transform.position, playerTransform.position) <= interactRadius;

        if (inRange && Input.GetKeyDown(KeyCode.E))
            Interact();

        if (canWander && !inRange)
            UpdateWander();
    }

    void OnGUI()
    {
        if (!inRange) return;
        if (!hintStyleReady) InitHintStyle();

        var cam = Camera.main;
        if (cam == null) return;

        Vector3 sp = cam.WorldToScreenPoint(transform.position + Vector3.up * 1.6f);
        if (sp.z <= 0) return;

        float sx = sp.x - 80f;
        float sy = Screen.height - sp.y - 14f;
        GUI.Label(new Rect(sx, sy, 160f, 24f), $"[E] {npcName}", hintStyle);
    }

    void Interact()
    {
        switch (shopType)
        {
            case NpcShopType.Weapon:
                if (NpcShopUI.Instance != null) NpcShopUI.Instance.OpenShop(NpcShopType.Weapon, npcName);
                break;

            case NpcShopType.Armor:
                if (NpcShopUI.Instance != null) NpcShopUI.Instance.OpenShop(NpcShopType.Armor, npcName);
                break;

            case NpcShopType.Inn:
                HealAtInn();
                break;

            default:
                ShowDialogue();
                break;
        }
    }

    void HealAtInn()
    {
        const int COST = 50;
        var inv = InventoryManager.Instance;
        if (inv == null) return;

        if (inv.Gold < COST)
        {
            NpcDialogueUI.Show(npcName, $"一泊 {COST}G いただきます。ゴールドが足りません。");
            return;
        }

        inv.AddGold(-COST);

        var pm = PartyManager.Instance;
        if (pm != null)
            foreach (var m in pm.Members)
                m.currentHp = m.maxHp;

        NpcDialogueUI.Show(npcName, $"ゆっくりお休みください。（-{COST}G）パーティが全回復しました！");
    }

    void ShowDialogue()
    {
        if (dialogueLines == null || dialogueLines.Length == 0) return;
        string line = dialogueLines[dialogueIndex % dialogueLines.Length];
        NpcDialogueUI.Show(npcName, line);
        dialogueIndex = (dialogueIndex + 1) % dialogueLines.Length;
    }

    void UpdateWander()
    {
        wanderTimer -= Time.deltaTime;
        if (wanderTimer <= 0f)
        {
            float angle = Random.value * Mathf.PI * 2f;
            float r = Random.Range(0f, wanderRadius);
            wanderTarget = origin + new Vector3(Mathf.Cos(angle) * r, 0f, Mathf.Sin(angle) * r);
            wanderTimer = Random.Range(2f, 5f);
        }

        if (Vector3.Distance(transform.position, wanderTarget) > 0.1f)
        {
            transform.position = Vector3.MoveTowards(transform.position, wanderTarget, Time.deltaTime * moveSpeed);
            Vector3 dir = wanderTarget - transform.position;
            if (dir.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Slerp(transform.rotation,
                    Quaternion.LookRotation(new Vector3(dir.x, 0f, dir.z)), Time.deltaTime * 5f);
        }
    }

    static void InitHintStyle()
    {
        hintStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize  = 14,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
        };
        hintStyle.normal.textColor = new Color(1.0f, 0.95f, 0.3f);
        hintStyleReady = true;
    }
}
