/// <summary>
/// 作物の成長段階。
/// 将来「水やり済み」「病気」などの状態を追加するときはここを拡張する。
/// </summary>
public enum CropStage
{
    None,    // 何も植えられていない
    Seed,    // 種（発芽前）
    Sprout,  // 若葉（成長中）
    Mature   // 収穫可能
}
