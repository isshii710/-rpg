using UnityEngine;

/// <summary>
/// NPC の台詞を画面下中央に表示するシングルトン UI。
/// NpcController から NpcDialogueUI.Show(name, text) で呼ぶ。
/// E キーか一定時間で自動消去。
/// </summary>
public class NpcDialogueUI : MonoBehaviour
{
    public static NpcDialogueUI Instance { get; private set; }

    string speakerName;
    string text;
    bool isVisible;
    float timer;

    Texture2D texBg;
    Texture2D texBorder;
    Texture2D texNameBg;
    GUIStyle styleSpeaker;
    GUIStyle styleText;
    GUIStyle styleHint;
    bool ready;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    /// <summary>台詞ウィンドウを表示する。duration 秒後に自動消去。</summary>
    public static void Show(string speaker, string msg, float duration = 4.5f)
    {
        if (Instance == null) return;
        Instance.speakerName = speaker;
        Instance.text        = msg;
        Instance.isVisible   = true;
        Instance.timer       = duration;
    }

    void Update()
    {
        if (!isVisible) return;
        timer -= Time.deltaTime;
        if (timer <= 0f || Input.GetKeyDown(KeyCode.E))
            isVisible = false;
    }

    void OnGUI()
    {
        if (!ready) Init();
        if (!isVisible) return;

        float pw = 600f;
        float ph = 92f;
        float px = (Screen.width - pw) * 0.5f;
        float py = Screen.height - ph - 58f;

        // パネル本体
        GUI.DrawTexture(new Rect(px, py, pw, ph), texBg);
        DrawBorder(px, py, pw, ph);

        // 名前タグ（パネル上部に張り出す）
        float nw = Mathf.Max(80f, speakerName.Length * 14f + 16f);
        GUI.DrawTexture(new Rect(px + 10f, py - 22f, nw, 24f), texNameBg);
        GUI.Label(new Rect(px + 14f, py - 22f, nw - 8f, 24f), speakerName, styleSpeaker);

        // 台詞
        GUI.Label(new Rect(px + 14f, py + 8f, pw - 28f, ph - 18f), text, styleText);

        // 操作ヒント
        GUI.Label(new Rect(px + pw - 130f, py + ph - 20f, 120f, 18f), "E: 閉じる", styleHint);
    }

    void DrawBorder(float x, float y, float w, float h)
    {
        float b = 2f;
        GUI.DrawTexture(new Rect(x,         y,         w, b), texBorder);
        GUI.DrawTexture(new Rect(x,         y + h - b, w, b), texBorder);
        GUI.DrawTexture(new Rect(x,         y,         b, h), texBorder);
        GUI.DrawTexture(new Rect(x + w - b, y,         b, h), texBorder);
    }

    void Init()
    {
        texBg     = MakeTex(new Color(0.04f, 0.04f, 0.18f, 0.93f));
        texBorder = MakeTex(new Color(0.80f, 0.62f, 0.10f, 1.00f));
        texNameBg = MakeTex(new Color(0.80f, 0.62f, 0.10f, 1.00f));

        styleSpeaker = new GUIStyle(GUI.skin.label)
        {
            fontSize  = 13,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft,
        };
        styleSpeaker.normal.textColor = new Color(0.08f, 0.04f, 0.00f);

        styleText = new GUIStyle(GUI.skin.label)
        {
            fontSize  = 15,
            fontStyle = FontStyle.Normal,
            alignment = TextAnchor.UpperLeft,
            wordWrap  = true,
        };
        styleText.normal.textColor = Color.white;

        styleHint = new GUIStyle(GUI.skin.label)
        {
            fontSize  = 10,
            alignment = TextAnchor.MiddleRight,
        };
        styleHint.normal.textColor = new Color(0.60f, 0.60f, 0.60f);

        ready = true;
    }

    static Texture2D MakeTex(Color c)
    {
        var t = new Texture2D(1, 1);
        t.SetPixel(0, 0, c);
        t.Apply();
        return t;
    }
}
