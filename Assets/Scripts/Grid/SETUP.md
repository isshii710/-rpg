# グリッドシステム + 農業 + 建築 + 鉄道システム セットアップ手順

## スクリプト構成

```
Assets/Scripts/Grid/
├── CropStage.cs       ← 作物の成長段階 enum（None/Seed/Sprout/Mature）
├── BuildingType.cs    ← 建物カテゴリ enum（Residential/Workshop/Storage/Decoration）
├── TrackDirection.cs  ← 線路の接続方向フラグ enum（North/East/South/West）
├── BuildingData.cs    ← 建物定義 ScriptableObject（名前・サイズ・プレハブ）
├── GridCell.cs        ← 1マスのデータ（建物・畑・作物・線路・複数マス占有をすべて管理）
├── GridManager.cs     ← グリッド全体を管理するシングルトン
├── GridPlacer.cs      ← マウス入力・配置・農業操作・建物回転
├── GridVisualizer.cs  ← ゲーム実行中にグリッド線を描画する（オプション）
├── FarmManager.cs     ← 作物の成長コルーチンを管理するシングルトン
├── BuildingManager.cs ← 複数マス占有の建物配置・撤去を管理するシングルトン
├── RailManager.cs     ← 線路配置・接続検出・BFS経路探索を管理するシングルトン
├── TrainController.cs ← 線路ルートに沿って列車を自動移動させるコンポーネント
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
| 左クリック（Track） | 線路を敷設（隣接線路の形が自動更新） |
| 右クリック（Mature の作物の上） | 収穫 |
| 右クリック（線路の上） | 線路を撤去（隣接線路の形が自動更新） |
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

### 線路プレハブ（鉄道用）

RailManager はプレハブを差し替えるだけで自動的に正しい向きに配置する。

| 名前 | Shape | Scale（目安） | 色 | 備考 |
|------|-------|-------------|-----|------|
| `TrackStraight` | ┃ 直線 | (0.3, 0.1, 0.95) | 灰色 | **必須**。デフォルト向きをZ軸方向（南北）にすること |
| `TrackCurve` | ┐ 曲線 | 任意 | 灰色 | 省略可（Straight で代用される）。デフォルト向きを「南と東を繋ぐ」にすること |
| `TrackTJunction` | ├ T字 | 任意 | 灰色 | 省略可。デフォルト向きを「北・南・東を繋ぐ（西欠け）」にすること |
| `TrackCross` | ┼ 十字 | 任意 | 灰色 | 省略可 |

---

## ステップ6：RailManager を配置する

1. Hierarchy → **Create Empty** → 名前を `RailManager`
2. **RailManager** スクリプトをアタッチ
3. Inspector の設定：

| 項目 | 設定 |
|------|------|
| Straight Prefab | `TrackStraight` をドラッグ（**必須**） |
| Curve Prefab | `TrackCurve` をドラッグ（省略時は Straight で代用） |
| T Junction Prefab | `TrackTJunction` をドラッグ（省略可） |
| Cross Prefab | `TrackCross` をドラッグ（省略可） |

---

## ステップ7：GridPlacer の Items を設定する

GridPlacer オブジェクトを選択 → Inspector の **Items** リストを設定：

| インデックス | Label | Type | 設定 |
|------------|-------|------|------|
| 0 | 家（2x2） | Building | Building Data に `House_2x2` をドラッグ |
| 1 | 畑 | Farmland | Prefab に `Farmland` をドラッグ |
| 2 | 種 | Seed | Prefab に `CropSeed` をドラッグ |
| 3 | 線路 | Track | Prefab に `TrackStraight` をドラッグ（プレビュー用） |

**Ground Layer** を `Ground` に設定することも忘れずに。

---

## ステップ8：列車を作る

1. Hierarchy → **3D Object → Cube** → Scale `(0.6, 0.4, 0.9)` → 赤色マテリアル
2. プレハブ化 → 名前を `Train`
3. `Train` プレハブに **TrainController** スクリプトをアタッチ
4. Inspector の設定：

| 項目 | 例 | 説明 |
|------|----|------|
| Start X / Start Z | 2 / 0 | 線路の始点グリッド座標 |
| End X / End Z | 2 / 7 | 線路の終点グリッド座標 |
| Speed | 4 | 移動速度（マス/秒） |
| Height Offset | 0.5 | 地面からの浮かせ高さ |
| Mode | Bounce | Bounce=往復 / Loop=ループ / OneWay=片道 |

---

## ステップ9：動作確認（鉄道）

1. **Play ボタン**を押す
2. **キー「4」**（線路）を選択 → 地面上に左クリックで線路を敷設
3. 隣接マスに追加するたびに、線路が直線・カーブ・T字・十字に自動で切り替わる
4. 線路の上で **右クリック** → 線路を撤去（隣の線路も自動で形が変わる）
5. `Train` プレハブをシーンに配置 → Inspector で Start/End を線路マスのグリッド座標に設定
6. Play 後、自動でルートを検索して走り始める
7. Inspector 右クリック → **「経路を再構築」** でルートをリセットできる

---

## Scene ビューでの確認

Train オブジェクトを選択すると：
- **黄色の線** → 現在のルート
- **緑の球** → 始点
- **赤の球** → 終点

---

## 今後の拡張ポイント

| やりたいこと | どこをどう拡張するか |
|------------|---------------------|
| 複数の列車 | `Train` プレハブをシーンに複数配置し、それぞれ Start/End を別の線路区間に設定 |
| 列車の停車・発車 | `TrainController` に `Stop()` / `Go()` メソッドを追加。ステーション建物の `OnTriggerEnter` から呼ぶ |
| 信号機・分岐 | `GridCell` に `IsSignal` フラグを追加。`TrainController` が信号マスで一時停止する処理を追加 |
| 建物の建設コスト | `BuildingData` に `cost` フィールドを追加。`BuildingManager.TryPlace` の前に所持金チェックを入れる |
| セーブ・ロード | `GridCell` の全フィールドを JSON 化して `Application.persistentDataPath` に保存 |
| 収穫アイテムをインベントリに追加 | `FarmManager.TryHarvest()` の末尾に `InventoryManager.Instance.AddItem(...)` を呼ぶ |
