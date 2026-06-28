using UnityEngine;

public enum BuildPieceCategory { Floor, Wall, Roof, Pillar }

/// <summary>
/// モジュール建築の1パーツ定義。Assets/Resources/BuildPieces/ に置く。
/// BuildModeManager が Resources.LoadAll で自動読み込みする。
/// </summary>
[CreateAssetMenu(fileName = "BuildPiece", menuName = "RPG/Build Piece")]
public class BuildPieceData : ScriptableObject
{
    public string        pieceName;
    public BuildPieceCategory category;
    public GameObject    prefab;
    public int           cost;      // 0 = 無料
}
