# Subjiシーン 設定ガイド

対象シーン：`Assets/Scenes/subji/jikkennsupace.unity`

この資料では、これまで追加した道路、プレイヤー、ダッシュゲージ、夜間視野、カメラ、敵、タイマーの設定場所をまとめています。基本的にはコードを書き換えず、UnityのInspectorから変更できます。

## 最初に覚える操作

1. Unityで `jikkennsupace` シーンを開きます。
2. 左側のHierarchyから設定したいオブジェクトを選びます。
3. 右側のInspectorに表示されるコンポーネントの数値を変更します。
4. 再生ボタンを押して動作を確認します。

ゲーム中の基本操作は次のとおりです。

| 操作 | 初期キー |
|---|---|
| 移動 | `WASD` または矢印キー |
| ダッシュ | 移動しながら左Shift |
| 夜間視野の暗さをON/OFF | `P` |

主に使うオブジェクトは次の4つです。

| 設定したいもの | Hierarchyで選ぶもの | コンポーネント |
|---|---|---|
| 道路・ミニマップ・敵の追加 | `Subji Road Map` | `Subji Road Map` / `Subji Enemy Spawner` |
| プレイヤー | `Player` | `Subji Player Movement` |
| カメラ | `Main Camera` | `Subji Camera Follow` / `Camera` |
| 最初からいる敵 | `Enemy` | `Subji Enemy Chase` |

## 道路とミニマップ

Hierarchyで `Subji Road Map` を選び、`Subji Road Map` コンポーネントを開きます。

### 道路の設定

- `Field Size`
  - マップ一辺の長さです。現在は `60` です。
- `Road Width`
  - 道路の太さです。現在は `6` です。
- `Horizontal Roads`
  - 横道路の位置です。
  - `-20, 0, 20` なら、中央から下に20、中央、上に20の位置へ道路ができます。
- `Vertical Roads`
  - 縦道路の位置です。
  - `-20, 0, 20` なら、中央から左に20、中央、右に20の位置へ道路ができます。

道路を増やす場合は、配列の `Size` を増やして新しい座標を入力します。道路を減らす場合は `Size` を減らします。

例：横道路を5本にする場合

```text
Horizontal Roads
Size = 5
Element 0 = -24
Element 1 = -12
Element 2 = 0
Element 3 = 12
Element 4 = 24
```

道路位置は `Field Size` の半分を超えないようにしてください。`Field Size = 60` の場合、おおむね `-30～30` の範囲です。

### ミニマップの設定

- `Minimap Size`
  - 右上のミニマップの大きさです。
- `Minimap Margin`
  - 画面端からミニマップまでの隙間です。
- `Minimap Player Color`
  - プレイヤーの印の色です。
- `Minimap Enemy Color`
  - 敵の印の色です。

### ミニマップの発見範囲（デバッグ表示）

`Subji Road Map` コンポーネントの「デバッグ表示」から変更します。

- `Show Detection Ranges On Minimap`
  - オンにすると、各敵の発見範囲をミニマップ上へ円で表示します。
  - オフにすると円を隠します。
- `Enable Debug Toggle Key`
  - オンにすると、ゲーム実行中にキーで表示を切り替えられます。
- `Toggle Detection Ranges Key`
  - 表示切り替えに使うキーです。初期設定は `F3` です。
- `Minimap Detection Range Color`
  - ミニマップ上の発見範囲の色と透明度です。

発見範囲は、プレイヤーの状態に合わせて `Moving Detection Radius` と `Idle Detection Radius` のどちらかを自動的に表示します。本番用に隠す場合は `Show Detection Ranges On Minimap` をオフにしてください。

## プレイヤー

Hierarchyで `Player` を選び、`Subji Player Movement` コンポーネントを開きます。

- `Move Speed`
  - プレイヤーの移動速度です。大きくすると速くなります。
- `Speed Boost Key`
  - 押している間、移動速度を上げるキーです。
  - 初期設定は `Left Shift`（左Shift）です。Inspectorの選択欄から別のキーへ変更できます。
- `Speed Boost Multiplier`
  - 速度アップ中に `Move Speed` へ掛ける倍率です。初期設定は `2`です。
  - 例えば `Move Speed = 5`、`Speed Boost Multiplier = 2` なら、キーを押している間の移動速度は `10` になります。
- `Speed Boost Duration`
  - 満タンから連続でダッシュできる時間です。初期設定は `3` 秒です。
- `Speed Boost Recharge Delay`
  - Shiftを離してからゲージの回復が始まるまでの時間です。初期設定は `1` 秒です。
- `Speed Boost Recharge Duration`
  - 空のゲージが満タンになるまでの時間です。初期設定は `5` 秒です。
- `Speed Boost Gauge Offset`
  - プレイヤーを基準にしたゲージの表示位置です。
- `Speed Boost Gauge Size`
  - ゲージの横幅と縦幅です。
- `Speed Boost Gauge Color`
  - ゲージ本体の色と透明度です。
- `Speed Boost Gauge Background Color`
  - ゲージ背景の色と透明度です。
- `Enemy Spawn Offset`
  - 最初からいる敵の希望出現位置です。
  - マップ中央を `X = 0, Y = 0` とした相対座標です。
  - 道路外を指定した場合は最寄りの道路へ自動補正されます。

操作キーは `WASD` または矢印キーです。初期設定では、移動しながら左Shiftを押すと速く移動します。停止中はゲージを消費しません。ゲージは初めて使用した時に頭上へ現れ、ダッシュ中に減少し、Shiftを離すと設定時間後に回復します。満タンになると自動的に消えます。プレイヤーは道路の外へ移動できません。

### 夜間視野（実験機能）

プレイヤーの周囲だけを明るく残し、外側へ向かって徐々に暗くします。暗闇とグラデーションはゲーム開始時に自動生成されるため、HierarchyへUIを追加する必要はありません。

- `Enable Night Vision`
  - オンならゲーム開始時から暗闇を表示します。
- `Night Vision Toggle Key`
  - ゲーム中に暗闇をON/OFFするキーです。初期設定は `P` です。
- `Player Vision Radius`
  - 暗くならずに見える半径です。初期設定は `4.5` です。
  - 敵の移動中索敵半径の初期値 `5` より少し小さく設定されています。
- `Vision Gradient Width`
  - 明るい範囲から視野外の暗さへ変化する幅です。大きいほど境界が柔らかくなります。
- `Outside Darkness`
  - 視野外の暗さです。`0` は透明、`1` は指定色を最大濃度で表示します。
- `Darkness Color`
  - 視野外へ重ねる色と透明度です。夜は黒、霧の実験なら灰色などに変更できます。
- `Darkness Overlay Size`
  - 暗闇が覆う一辺の長さです。カメラに暗闇の端が映る場合は大きくします。
- `Darkness Texture Resolution`
  - グラデーション画像の解像度です。大きいほど滑らかになりますが、開始時の生成負荷とメモリ使用量が増えます。

夜間視野の設定はゲーム開始時に画像へ反映されます。色や半径を変更した場合は、一度再生を停止してから再度再生してください。

プレイヤーの `Rigidbody 2D` は移動時の小刻みな揺れを防ぐため、`Interpolate` が有効になっています。滑らかな表示に必要な設定なので、通常は `None` に戻さないでください。

### 敵との接触カウント

- `Enemy Overlap Threshold`
  - プレイヤーと敵を接触扱いにする重なり率です。
  - `0.3` はプレイヤーの表示面積の30%です。

左上のタイマーの下に `HIT` として接触回数が表示されます。同じ敵と重なり続けている間は1回だけ数え、一度離れてから再び30%以上重なるともう一度加算されます。

将来ゲームオーバー判定を追加する場合は、`SubjiPlayerMovement` の `EnemyContactCount` から現在の接触回数を取得できます。カウントが変わった瞬間を受け取りたい場合は `EnemyContactCountChanged` イベントを利用できます。

## カメラ

Hierarchyで `Main Camera` を選びます。

### 追従の滑らかさ

`Subji Camera Follow` コンポーネントの設定です。

- `Player`
  - 追いかける対象です。通常は `Player` のまま変更しません。
- `Smooth Time`
  - カメラ追従の滑らかさです。
  - 小さいほど素早く追従し、大きいほどゆっくり追従します。
  - おすすめは `0.1～0.25` です。現在は `0.15` です。

### 画面に映る範囲

`Camera` コンポーネントの設定です。

- `Orthographic Size`
  - 大きくすると広い範囲、小さくすると狭い範囲が映ります。
  - 現在は `5` です。

## 敵の追跡設定

Hierarchyで `Enemy` を選び、`Subji Enemy Chase` コンポーネントを開きます。

### 行動タイプと徘徊

- `Movement Type`
  - `Patrol And Chase`：未発見時は道路を徘徊し、発見後に追跡します。
  - `Wait Until Player Found`：発見するまで停止し、発見後に追跡します。
  - `Completely Stationary`：発見しても移動しない完全停止型です。
- `Patrol Speed`
  - 未発見時に道路を徘徊する速度です。
- `Minimum Patrol Wait`
  - 徘徊先へ到着した時に停止する最短時間です。
- `Maximum Patrol Wait`
  - 徘徊先へ到着した時に停止する最長時間です。

徘徊型の敵は道路上からランダムに目的地を選び、道路内の最短経路を使って移動します。

### 発見と追跡

- `Moving Detection Radius`
  - プレイヤーが動いている時の発見範囲です。
- `Idle Detection Radius`
  - プレイヤーが止まっている時の発見範囲です。
- `Chase Speed`
  - 敵が追いかける速度です。
- `Chase Memory Seconds`
  - プレイヤーが発見範囲から出た後も追跡を続ける時間です。
- `Circle Width`
  - 発見範囲を示す円の線の太さです。
- `Player`
  - 追跡対象です。通常は `Player` のまま変更しません。

敵は道路内で最短経路を計算し、道路外を横切らずに追跡します。

10秒ごとに追加される敵は、最初の `Enemy` の見た目、速度、発見範囲をコピーします。そのため、追加される全敵の基本設定を変えたい場合は、最初の `Enemy` を変更してください。

### 追加される敵へランダムな個体差を付ける

`Subji Road Map` → `Subji Enemy Spawner` の「追加される敵の個体差」を変更します。

- `Randomize Enemy Variation`
  - オンにすると、追加される敵ごとに行動タイプと速度が変化します。
- `Waiting Enemy Chance`
  - 発見するまで停止する個体が選ばれる確率です。
  - `0.2` は20%です。
- `Minimum Speed Multiplier`
  - 元の速度へ掛ける倍率の最小値です。
- `Maximum Speed Multiplier`
  - 元の速度へ掛ける倍率の最大値です。

例：`0.5～2.0` にすると、元の半分から2倍までの速度を持つ敵が追加されます。個体差を使用しない場合は `Randomize Enemy Variation` をオフにしてください。

## 敵の追加タイマーと出現場所

Hierarchyで `Subji Road Map` を選び、`Subji Enemy Spawner` コンポーネントを開きます。

### タイマー

- `Spawn Interval`
  - 敵を追加する間隔です。現在は `10` 秒です。
- `Maximum Enemies`
  - 同時に存在できる敵の最大数です。
  - `0` は無制限です。

左上の `NEXT` 表示が、次の敵が追加されるまでの秒数です。

### ランダムな道路上へ出す

`Spawn Mode` を `Random On Road` にします。縦道路または横道路からランダムに選ばれ、必ず道路上へ出現します。

### 決まった場所へ出す

1. `Spawn Mode` を `Fixed Points` にします。
2. `Fixed Spawn Points` の `Size` を出現地点の数にします。
3. 各 `Element` のX・Yへ座標を入力します。

敵は登録した地点へ上から順番に出現し、最後まで進むと最初の地点へ戻ります。

- `Snap Fixed Points To Road` がオン
  - 指定座標が道路外でも最寄りの道路へ補正します。
- `Snap Fixed Points To Road` がオフ
  - 入力した座標をそのまま使用します。

通常は道路外への出現を防ぐため、オンがおすすめです。

## よく使う調整例

### 敵を5秒ごとに追加する

`Subji Road Map` → `Subji Enemy Spawner` → `Spawn Interval = 5`

### 敵を最大10体にする

`Subji Road Map` → `Subji Enemy Spawner` → `Maximum Enemies = 10`

### 敵を速くする

`Enemy` → `Subji Enemy Chase` → `Chase Speed` を大きくします。

### 速度アップを右Shiftの3倍速にする

`Player` → `Subji Player Movement` → `Speed Boost Key = Right Shift`、`Speed Boost Multiplier = 3`

### 夜間視野を青みのある暗さにする

`Player` → `Subji Player Movement` → `Darkness Color` を暗い青色に変更します。暗さは `Outside Darkness` で調整します。

### カメラの遅れを減らす

`Main Camera` → `Subji Camera Follow` → `Smooth Time` を小さくします。

### 道を太くする

`Subji Road Map` → `Subji Road Map` → `Road Width` を大きくします。

## 関連するスクリプト

通常はInspectorから設定すればよいため、コードを直接変更する必要はありません。

| ファイル | 役割 |
|---|---|
| `Assets/Scripts/Subji/SubjiRoadMap.cs` | 道路Mesh、道路内判定、最短経路、ミニマップ |
| `Assets/Scripts/Subji/SubjiPlayerMovement.cs` | プレイヤー入力と道路上の移動 |
| `Assets/Scripts/Subji/SubjiCameraFollow.cs` | カメラの滑らかな追従 |
| `Assets/Scripts/Subji/SubjiEnemyChase.cs` | 敵の発見範囲と追跡 |
| `Assets/Scripts/Subji/SubjiEnemySpawner.cs` | タイマーと敵の追加出現 |

## 注意点

- 道路設定を変更したら、一度再生を停止してから再度再生してください。
- `Player`、`Enemy`、`Subji Road Map` の名前や参照は、不明な場合は変更しないでください。
- 数値を大きく変更した時は、敵やプレイヤーが道路内に収まるか再生して確認してください。
- シーンを変更した後は `Ctrl + S` で保存してください。
