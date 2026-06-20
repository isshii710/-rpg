# グリッドシステム + 農業システム セットアップ手順

## スクリプト構成

```
Assets/Scripts/Grid/
├── CropStage.cs       ← 作物の成長段階 enum（None/Seed/Sprout/Mature）
├── GridCell.cs        ← 1マス分のデータ（建物・畑・作物をまとめて管理）
├── GridManager.cs     ← グリッド全体を管理するシングルトン
├── GridPlacer.cs      ← マウス入力を受け取って配置・削除・種まきを行う
├── GridVisualizer.cs  ← ゲーム実行中にグリッド線を描画する（オプション）
├── FarmManager.cs     ← 作物の成長コルーチンを管理するシングルトン
└── SETUP.md           ← この手順書
```

---

## 【操作まとめ】

| 操作 | 効果 |
|------|------|
| 数字キー 1〜9 | アイテム切り替え |
| 左クリック（建物選択中） | 建物を配置 |
| 左クリック（畑選択中） | 畑タイルを配置（茶色の土） |
| 左クリック（種選択中） | 畑マスに種を植える |
| 右クリック（Mature の作物の上） | 収穫してアイテムを入手 |
| 右クリック（それ以外） | 建物・畑タイルを撤去 |

---

## ステップ1：スクリプトをプロジェクトに追加する

1. Unity で `Assets/Scripts/Grid/` フォルダを作成
2. 6つの `.cs` ファイルをすべてそのフォルダに置く

---

## ステップ2：地面（Ground）を作る

1. Hierarchy → 右クリック → **3D Object → Plane** を作成
2. Transform：Position `(5, 0, 5)`、Scale `(1, 1, 1)`
3. **Groundレイヤーを作る**
   - Inspector 上部「Layer」→ **Add Layer** → 空きスロットに `Ground` と入力
4. Plane を選択 → Layer を **Ground** に変更

---

## ステップ3：GridManager を配置する

1. Hierarchy → 右クリック → **Create Empty** → 名前を `GridManager`
2. Inspector → Add Component → **GridManager** をアタッチ
3. 設定値：

| 項目 | 値 |
|------|----|
| Width | 10 |
| Height | 10 |
| Cell Size | 1 |
| Origin Position | (0, 0, 0) |

4. （オプション）同じオブジェクトに **GridVisualizer** もアタッチ → ゲーム中もグリッド線が見える

---

## ステップ4：プレハブを3種類作る

### 「家」プレハブ
1. Hierarchy → **3D Object → Cube** を作成
2. Scale を `(0.8, 1.5, 0.8)` に（家っぽく縦長に）
3. マテリアルで **青色** に変更
4. `Assets/Prefabs/` フォルダにドラッグしてプレハブ化 → 名前を `House`
5. Hierarchy の Cube は削除してOK

### 「畑タイル」プレハブ
1. **3D Object → Cube** → Scale `(0.95, 0.05, 0.95)` に（薄い板状）
2. マテリアルで **茶色** に変更
3. プレハブ化 → 名前を `Farmland`

### 「種」「若葉」「収穫可能」プレハブ（3つ）
| 名前 | Scale | 色 |
|------|-------|-----|
| `CropSeed`   | (0.2, 0.2, 0.2) | 黄色 |
| `CropSprout` | (0.2, 0.5, 0.2) | 明るい緑 |
| `CropMature` | (0.3, 0.8, 0.3) | 濃い緑 |

---

## ステップ5：FarmManager を配置する

1. Hierarchy → **Create Empty** → 名前を `FarmManager`
2. **FarmManager** をアタッチ
3. Inspector の設定：

| 項目 | 設定 |
|------|------|
| Seed Prefab | `CropSeed` をドラッグ |
| Sprout Prefab | `CropSprout` をドラッグ |
| Mature Prefab | `CropMature` をドラッグ |
| Seed To Sprout Seconds | 5（テスト時は 2 など小さくすると確認しやすい） |
| Sprout To Mature Seconds | 10（テスト時は 3 など） |

---

## ステップ6：GridPlacer を配置する

1. Hierarchy → **Create Empty** → 名前を `GridPlacer`
2. **GridPlacer** をアタッチ
3. Inspector の **Items** リストを設定：

| インデックス | Label | Type | Prefab |
|------------|-------|------|--------|
| 0 | 家 | Building | House |
| 1 | 畑 | Farmland | Farmland |
| 2 | 種 | Seed | CropSeed |

4. **Ground Layer** を `Ground` に設定

---

## ステップ7：カメラを調整する

Main Camera の Transform：
- Position: `(5, 12, -4)`
- Rotation: `(55, 0, 0)`

---

## ステップ8：動作確認

1. **Play ボタン**を押す
2. **キー「2」** を押して畑タイルを選択 → 地面の上で左クリック → 茶色の板が置かれる
3. **キー「3」** を押して種を選択 → 畑の上で左クリック → 黄色の小さなキューブが現れる
4. 数秒後 → 緑の若葉（CropSprout）に変化する
5. さらに数秒後 → 濃い緑（CropMature）に変化 + Consoleに「収穫可能」と表示
6. CropMature の上で **右クリック** → 収穫。Console に「収穫成功！」と表示されて畑が空に戻る
7. **キー「1」** で家を選択 → 空いているマスに左クリック → 家が配置される

---

## 今後の拡張ポイント

| やりたいこと | どこをどう拡張するか |
|------------|---------------------|
| 水やりが必要な農業 | `GridCell` に `IsWatered` フラグを追加。FarmManagerのコルーチンで `IsWatered` を確認してからのみ成長させる |
| 収穫アイテムをインベントリに追加 | `FarmManager.TryHarvest()` の `// TODO` 行にアイテム追加処理を書く |
| 季節・天気による成長速度変化 | `FarmManager` の `seedToSproutSeconds` を GameManager から動的に変更する |
| 線路の敷設 | `ItemType` に `Rail` を追加し、`GridCell` に `RailDirection` enum を追加。`HandleLeftClick` に `Rail` のケースを追加する |
| 複数マス占有の建物 | `GridManager.TryPlace` に `sizeX, sizeZ` 引数を追加し、複数セルをまとめてロックする |
| セーブ・ロード | `GridCell` の全フィールドをJSON化して `Application.persistentDataPath` に保存する |
