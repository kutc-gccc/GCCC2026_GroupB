# Codex共有メモ

最終確認：2026-10-23。このプロジェクトの別チャット向け。会話全文の再取得は不要。現物の変更が優先。

## ユーザーの作業方針

- 使用量を従来の半分以下に抑える目標。指定箇所だけ確認・変更し、報告は短くする。削減率は保証しない。
- 頼まれていない見た目・設置物・調査地点・機能・自動設定・テストツールを増やさない。
- 既存の共通基盤を使う。巨大シーン全文の出力、同じ内容の再検索、不要なGUI操作を避ける。
- バックアップを保持し、ユーザーの配置変更・未コミット変更を上書きしない。Unity上の未保存変更はディスクからは確認できない。

## 場所と設定

- Unity：6000.3.11f1。
- 通常マップ：`Assets/Scenes/subji/jikkennsupace.unity`。屋内：同ディレクトリの`HauntedHouse.unity`。
- Hierarchyの`Jikken Comment Stream`にある`JikkenCommentStream`で時間・QTE・音源・対象参照をまとめて設定。
- 共通UI・受諾・報酬・完了：`Assets/Scripts/Subji/JikkenCommentStream.cs`。
- フェーズ1・2の処理：同ディレクトリの`JikkenCommentStream.PhaseTasks.cs`（同じpartialクラス）。対象シーンは`enablePhaseTwoTasks`が有効。
- 時間操作：同ディレクトリの`TimedWorldInteraction.cs`。Eを1回押すと開始し、離しても継続。通常は移動でキャンセル。乗車時のみ移動キャンセル不可。
- 自販機：`Assets/Scripts/UI/DrinkItemController.cs`。ブッシュ：`Assets/Scripts/Subji/BushHideSpot2D.cs`。
- 敵共通：`Assets/Scripts/Subji/SubjiEnemyChase.cs`。個体差は設定で表す。
- 操作地点：Hierarchyの`Phase 1-2 Task Points`。フリーフォールの地点は再生時に本体の子へ移る。
- 設計図：`F:/gameseisaku/設計図/設計図.xlsx`。古い長押し・周回記述より、下記の後から確定した仕様を優先。
- 詳細手順：`Docs/フェーズ2までのタスク追加手順.md`。古い実装案も含むため、現行仕様はこのメモを優先する。

## 確定仕様

- フェーズ1：ID1 噴水へ到達 → ID2 自販機1秒 → ID3 ブッシュ0.5秒。報酬は各100人。ID4 警備員をまくタスクは保留・欠番。
- フェーズ2：ID5～8から3つ。ID7を必ず含め、残り2つを抽選し、出現順をシャッフル。報酬は各5,000人。3つ達成で止める。フェーズ3・エンディングは追加対象外。
- ID5 サイン：矢印キー ↑ ↓ ← ↓ ↓ →。現在は範囲内でE開始、誤入力で先頭へ、WASDでキャンセル、制限時間なし。
- ID6 フリーフォール：E開始後3秒で完了。途中下車不可。乗車中は隠れ状態と敵停止で接触を防ぐ。終了時に戻す。
- フリーフォールをユーザーが移動済み。`AlignFreeFallInteractionPoints`が再生開始時に本体画像の下へ乗車地点を合わせ、降車地点の相対差を維持し、本体の子にして追従させる。保存済みシーンだけから移動していないと断定しない。
- ID7 お化け屋敷：既存のゴール到達だけで達成。`SubjiScenePortal.GoalReturned`で帰還を通知。調査地点・人形・追加フラグは禁止。以前追加したものは削除済み。
- ID8 汽笛：E開始後、進行中に音をループ。2秒で近隣の敵へ調査通知、4秒で達成。完了・移動キャンセルで停止。長押しは不要。
- 汽車は既存の`いろいろ2_14`（灰色の蒸気機関車）を使う。追加した`Task Train`と仮の`Sign Wall Surface`は削除済み。勝手に再生成しない。
- 汽笛音：`Assets/Resources/Audio/train_whistle.mp3`。元音源はDownloadsの`蒸気機関車の汽笛1.mp3`。
- 敵の合計：フェーズ1 通常2体、フェーズ2 通常4体＋監視3体。進行側が既存敵の有効化とスポーナー停止を管理。
- 登録者の毎秒自然増加は既存仕様を維持。タスク報酬の検証では分けて計算する。

## コメント時間の変更場所

`Jikken Comment Stream`のInspector「導入コメントの時間」。表示名は日本語化済み。

- 最初の移動説明まで（秒）：`phaseIntroDelay`、初期値1。
- 移動を続ける時間（秒）：`phaseIntroMovementSeconds`、初期値3。
- ダッシュ後の説明間隔（秒）：`phaseIntroAfterDashDelay`、初期値1。
- うぽつコメントまで（秒）：`phaseIntroViewerDelay`、初期値0.5。
- タスク案内を繰り返す間隔（秒）：`tutorialMessageInterval`。

## バックアップと検証状況

- 初回実装前：`Backups/before_phase2_20261023_124554/project_backup.zip`。Assets全体・設定・Docs等1,160ファイル。CRC検査済み。復元説明とmanifestは同フォルダ。Library・未保存のUnity変更は対象外。
- 汽車・ゴール簡略化の前：`Backups/before_train_goal_fix.zip`（対象ソースとシーン）。
- 初回実装のコンパイル・Play Mode検証は成功（`PHASE_TWO_PLAYMODE_OK`）。汽車・屋内ゴールへの修正後とフリーフォール追従修正後はコンパイル確認のみで、再生テストは未実施。初回テスト成功を現行版の再生確認と混同しない。
- 既存の検証メニュー：`Tools > Tasks > フェーズ2の動作を検証（Play Mode）`、Ctrl+Shift+F9。必要な場合だけ使う。テスト中は敵を停止し、全4種類確認用に抽選順を一時差し替える。通常の経路・難易度を保証しない。
- `Assets/Editor/PhaseTwoTaskSetup.cs`は設定メニュー、`PhaseTwoPlayModeCheck.cs`は検証。通常の小修正で実行・拡張しない。
- 作業は未コミット。シーン・フォント等はユーザーやUnityによる変更もあり得る。まとめて破棄しない。

## タイトル画面
- 起動順先頭：Assets/Scenes/Title.unity。Assets/Title/TitleMenu.cs でSTARTボタンを再生時に生成し、jikkennsupaceへ移動。背景Canvasと画像はシーンに保存済みで編集時も表示。画像：Assets/Title/TitleBackground.png。画像のTextureImporter設定を明示し、Unityで編集時・再生時の背景とSTART表示を確認済み。ボタンからの遷移は未確認。
