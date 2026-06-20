# グリッドシステム + 農業 + 建築システム セットアップ手順

## スクリプト構成

```
Assets/Scripts/Grid/
├── CropStage.cs       ← 作物の成長段階 enum（None/Seed/Sprout/Mature）
├── BuildingType.cs    ← 建物カテゴリ enum（Residential/Workshop/Storage/Decoration）
├── BuildingData.cs    ← 建物定義 ScriptableObject（名前・サイズ・プレハブ）
├── GridCell.cs        ← 1マスのデータ（建物・畑・作物・複数マス占有をすべて管理）
├── GridManager.cs     ← グリッド全体を管理するシングルトン
├── GridPlacer.cs      ← マウス入力・配置・農業操作・建物回転
├── GridVisualizer.cs  ← ゲーム実行中にグリッド線を描画する（オプション）
├── FarmManager.cs     ← 作物の成長コルーチンを管理するシングルトン
├── BuildingManager.cs ← 複数マス占有の建物配置・撤去を管理するシングルトン
└── SETUP.md           ← この手順書
```

---

## 【操作まとめ】

| 操作 | 効果 |
|------|------|
| 数字キー 1〜9 | アイテム切り替え |
| R キー | 建物の向きを 90° 回転（Building タイプのみ） |
| 左クリック（Building） | 建物を配置（複数マス対応） |
| 左クリック（Farmland） | 畑タイルを配置 |
| 左クリック（Seed） | 畑マスに種を植える |
| 右クリック（Mature の作物の上） | 収穫 |
| 右クリック（建物の上） | 建物全体を撤去（どのマスをクリックしてもOK） |
| 右クリック（畑タイルの上） | 畑タイルを撤去（作物も一緒に消える） |

---

## ステップ1：スクリプトをプロジェクトに追加する

1. Unity で `Assets/Scripts/Grid/` フォルダを作成
2. すべての `.cs` ファイルをそのフォルダに置く

---

## ステップ2：地面（Ground）を作る

1. Hierarchy → 右クリック → **3D Object → Plane** を作成
2. Transform：Position `(5, 0, 5)`、Scale `(1, 1, 1)`
3. **Ground レイヤーを作る**
   - Inspector 上部「Layer」→ **Add Layer** → 空きスロットに `Ground` と入力
4. Plane を選択 → Layer を **Ground** に変更

---

## ステップ3：各マネージャーを配置する

| GameObject 名 | アタッチするスクリプト | 設定 |
|--------------|----------------------|------|
| `GridManager` | GridManager | Width=10, Height=10, CellSize=1 |
| `FarmManager` | FarmManager | 種・若葉・収穫のプレハブをセット |
| `BuildingManager` | BuildingManager | 設定なし（シングルトン） |
| `GridPlacer` | GridPlacer | Items と Ground Layer を設定 |

（オプション）`GridManager` に **GridVisualizer** もアタッチするとゲーム中もグリッド線が見える。

---

## ステップ4：BuildingData を作る（建物の定義）

ScriptableObject を使って建物の設定をアセットとして保存する。

1. Project ウィンドウ → `Assets/Data/` フォルダを作成
2. 右クリック → **Create → RPG → Building Data**
3. 名前を `House_2x2` などにする
4. Inspector で設定：

| 項目 | 例（2x2 の家） |
|------|---------------|
| Building Name | 家 |
| Type | Residential |
| Prefab | 家のプレハブ |
| Size X | 2 |
| Size Z | 2 |

5. 同様に小屋（1x1）や倉庫（2x3）などを作る

---

## ステップ5：プレハブを作る

### 建物プレハブの作り方（2x2 の家を例に）

1. Hierarchy → **3D Object → Cube** を作成
2. Scale を `(2, 2, 2)` に（2マス×2マスのサイズ感）
3. マテリアルで **青色** に変更
4. `Assets/Prefabs/` フォルダにドラッグしてプレハブ化 → 名前を `House_2x2`
5. Hierarchy の Cube は削除

> **ポイント**：プレハブの Scale は占有マス数に合わせる。1マス=1単位なので、2x2 なら Scale=(2, ?, 2)。

### 畑・作物プレハブ（農業用）

| 名前 | Scale | 色 |
|------|-------|-----|
| `Farmland` | (0.95, 0.05, 0.95) | 茶色 |
| `CropSeed` | (0.2, 0.2, 0.2) | 黄色 |
| `CropSprout` | (0.2, 0.5, 0.2) | 明るい緑 |
| `CropMature` | (0.3, 0.8, 0.3) | 濃い緑 |

---

## ステップ6：GridPlacer の Items を設定する

GridPlacer オブジェクトを選択 → Inspector の **Items** リストを設定：

| インデックス | Label | Type | 設定 |
|------------|-------|------|------|
| 0 | 家（2x2） | Building | Building Data に `House_2x2` をドラッグ |
| 1 | 小屋（1x1） | Building | Building Data に `Hut_1x1` をドラッグ |
| 2 | 畑 | Farmland | Prefab に `Farmland` をドラッグ |
| 3 | 種 | Seed | Prefab に `CropSeed` をドラッグ |

**Ground Layer** を `Ground` に設定することも忘れずに。

---

## ステップ7：動作確認

1. **Play ボタン**を押す
2. **キー「1」**（家2x2）を選択 → 地面上にマウスを動かす → 2マス×2マス分の半透明プレビューが表示される
3. **R キー** を押すと 90° 回転する
4. 全マスが空いているとき緑、1マスでも埋まっていると赤になる
5. **左クリック** → 家が配置される
6. 配置した家の上で **右クリック** → どのマスでも建物全体が消える
7. **キー「3」**（畑）→ 空きマスに左クリック → 畑タイルが置かれる
8. **キー「4」**（種）→ 畑の上で左クリック → 種が植わる
9. 数秒後に成長 → CropMature の上で右クリック → 収穫

---

## 今後の拡張ポイント

| やりたいこと | どこをどう拡張するか |
|------------|---------------------|
| 建物の建設コスト | `BuildingData` に `cost` フィールドを追加。`BuildingManager.TryPlace` の前に所持金チェックを入れる |
| 建物の効果・機能 | `BuildingData.type` でカテゴリ判定。WorkshopなどはItemを生産するManagerを別途作成 |
| 住民の居住管理 | `BuildingData` に `maxResidents` を追加。別途 ResidentManager で管理 |
| 線路の敷設 | `ItemType.Rail` を追加。`GridCell` に `RailDirection` enum を追加。RailManagerで接続を管理 |
| セーブ・ロード | `GridCell` の全フィールドを JSON 化して `Application.persistentDataPath` に保存 |
| 収穫アイテムをインベントリに追加 | `FarmManager.TryHarvest()` の末尾に `InventoryManager.Instance.AddItem(...)` を呼ぶ |
