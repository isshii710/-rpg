# グリッドシステム セットアップ手順

## スクリプト構成

```
Assets/Scripts/Grid/
├── GridCell.cs        ← 1マス分のデータ（MonoBehaviour不使用・純粋なデータクラス）
├── GridManager.cs     ← グリッド全体を管理するシングルトン
├── GridPlacer.cs      ← マウス入力を受け取って配置・削除を行う
├── GridVisualizer.cs  ← ゲーム実行中にグリッド線を描画する（オプション）
└── SETUP.md           ← この手順書
```

---

## Unity上でのセットアップ手順

### ステップ1：プロジェクトを用意する

1. Unity Hub で新規3Dプロジェクトを作成（テンプレート：**3D Core**）
2. `Assets/Scripts/Grid/` フォルダを作成し、4つの `.cs` ファイルをすべて配置する

---

### ステップ2：地面（Ground）を作る

1. Hierarchy ウィンドウで右クリック → **3D Object → Plane** を作成
2. Inspector で Transform を以下に設定：
   - Position: `(5, 0, 5)` ← 10×10グリッドの中央に合わせる
   - Scale: `(1, 1, 1)`
3. **Groundレイヤーを作る**
   - Inspector 上部の「Layer」プルダウン → **Add Layer** をクリック
   - 空いているスロットに `Ground` と入力
4. 作った Plane を選択 → Layer を **Ground** に変更

---

### ステップ3：GridManager を配置する

1. Hierarchy で右クリック → **Create Empty** → 名前を `GridManager` にする
2. `GridManager` を選択し、Inspector で **Add Component** → `GridManager` を検索してアタッチ
3. Inspector の設定：

   | 項目 | 値 | 説明 |
   |------|-----|------|
   | Width | 10 | 横マス数 |
   | Height | 10 | 縦マス数 |
   | Cell Size | 1 | 1マスのサイズ |
   | Origin Position | (0, 0, 0) | グリッドの左下の原点 |

4. **（オプション）** `GridVisualizer` も同じ `GridManager` オブジェクトにアタッチすると、ゲーム実行中もグリッド線が見える

---

### ステップ4：プレハブを作る

配置できるオブジェクトのプレハブを3種類（家・畑・線路の仮置き）作ります。

**家のプレハブを例に：**

1. Hierarchy で右クリック → **3D Object → Cube** を作成
2. Transform の Scale を `(0.8, 0.8, 0.8)` にする（1マスより少し小さく）
3. マテリアルで色を変える（青 = 家、緑 = 畑、赤 = 線路 など）
4. `Assets/Prefabs/` フォルダを作成し、Hierarchy の Cube を Project ウィンドウにドラッグ → プレハブ化
5. Hierarchy 上の元の Cube は削除してOK
6. 同様に畑・線路用プレハブも作成する

---

### ステップ5：GridPlacer を配置する

1. Hierarchy で右クリック → **Create Empty** → 名前を `GridPlacer` にする
2. `GridPlacer` スクリプトをアタッチ
3. Inspector の設定：

   | 項目 | 設定方法 |
   |------|---------|
   | Placement Prefabs | サイズを 3 にして、ステップ4で作ったプレハブを 0〜2 にドラッグ |
   | Ground Layer | ドロップダウンから **Ground** を選択 |

---

### ステップ6：カメラを調整する

1. Main Camera を選択
2. Transform を以下にするとグリッドが見やすい：
   - Position: `(5, 12, -4)`
   - Rotation: `(55, 0, 0)`

---

### ステップ7：動作確認

1. **Playボタン** を押してゲームを実行
2. マウスを地面に向けて動かすと、半透明のプレビューが表示される
   - 置けるマス → **緑**
   - 置けないマス → **赤**
3. **左クリック** → オブジェクトを配置
4. **右クリック** → オブジェクトを削除
5. **数字キー 1〜3** → 配置するプレハブを切り替え

---

## 操作まとめ

| 操作 | 機能 |
|------|------|
| マウス移動 | 配置先のマスを選択（プレビュー表示） |
| 左クリック | オブジェクトを配置 |
| 右クリック | オブジェクトを削除 |
| 1〜9キー | 配置するプレハブを切り替え |

---

## 今後の拡張ポイント

| 機能 | どこを拡張するか |
|------|----------------|
| 植物の成長 | `GridCell` に `GrowthStage` フィールドを追加し、コルーチンで更新 |
| 線路のルート探索（A*など） | `GridManager.GetCell()` を使って隣接マスを探索するクラスを追加 |
| 建物の複数マス占有 | `TryPlace` に `int sizeX, int sizeZ` 引数を追加し、複数セルをまとめてロック |
| セーブ・ロード | `GridCell` の状態をJSON化して `Application.persistentDataPath` に保存 |
| マルチプレイヤー | `GridManager` の配置・削除をRPCで同期（Photon / Unity Netcode） |
