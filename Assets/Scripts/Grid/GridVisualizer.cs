using UnityEngine;

/// <summary>
/// ゲーム実行中にグリッドの線を描画する（LineRenderer使用）。
/// 開発中や、グリッドが常に見えるゲームUIが必要なときに使う。
/// 不要なら GridManager の OnDrawGizmos だけでも十分。
/// </summary>
[RequireComponent(typeof(GridManager))]
public class GridVisualizer : MonoBehaviour
{
    [Header("線の色と太さ")]
    [SerializeField] Color lineColor = new Color(1f, 1f, 1f, 0.3f);
    [SerializeField] float lineWidth = 0.02f;
    [SerializeField] Material lineMaterial;

    void Start()
    {
        BuildGridLines();
    }

    void BuildGridLines()
    {
        GridManager gm = GridManager.Instance;
        int w = gm.Width;
        int h = gm.Height;
        float cs = gm.CellSize;
        Vector3 origin = gm.OriginPosition;

        // 縦線
        for (int x = 0; x <= w; x++)
            CreateLine(
                origin + new Vector3(x * cs, 0.02f, 0),
                origin + new Vector3(x * cs, 0.02f, h * cs)
            );

        // 横線
        for (int z = 0; z <= h; z++)
            CreateLine(
                origin + new Vector3(0,      0.02f, z * cs),
                origin + new Vector3(w * cs, 0.02f, z * cs)
            );
    }

    void CreateLine(Vector3 from, Vector3 to)
    {
        var go = new GameObject("GridLine");
        go.transform.parent = transform;

        var lr = go.AddComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.SetPosition(0, from);
        lr.SetPosition(1, to);
        lr.startWidth = lineWidth;
        lr.endWidth   = lineWidth;
        lr.useWorldSpace = true;

        if (lineMaterial != null)
            lr.material = lineMaterial;
        else
        {
            // マテリアルが未設定の場合はUnlit/Colorで代用
            lr.material = new Material(Shader.Find("Unlit/Color"));
            lr.material.color = lineColor;
        }

        lr.startColor = lineColor;
        lr.endColor   = lineColor;
    }
}
