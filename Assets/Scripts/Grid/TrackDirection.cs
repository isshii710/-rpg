/// <summary>
/// 線路マスが隣接マスとどの方向に接続しているかを表すビットフラグ。
/// 複数方向を同時に持てる（例：North | East = 北と東に接続）。
/// </summary>
[System.Flags]
public enum TrackDirection
{
    None  = 0,
    North = 1 << 0,  // +Z 方向
    East  = 1 << 1,  // +X 方向
    South = 1 << 2,  // -Z 方向
    West  = 1 << 3,  // -X 方向
}
