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
- 屋内ライト：`HauntedHouse.unity`の`Haunted House`にある`SubjiInteriorStage`「お化け屋敷内のライト設定」で照射角度・距離・ライトの色（白は従来表示、Aで強さ）・視野外の暗さ・色を独立調整。通常入場・屋内直接再生の両方で適用。初期値は従来どおり。Unity再生確認は未実施。
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
- フリーフォールアニメ（2026-10-27）：本編の既存本体を指定prefabと同じ座席なし画像に変更し、座席の子と指定controllerのAnimatorを追加。freeFallAnimator／freeFallSeatを参照。乗車3秒でクリップ1周、終了・キャンセルで座席の開始位置へ復帰。配置・スケール維持。コンパイル成功（既存警告4件）、Unity再生未確認。
- フリーフォール乗車カメラ（2026-10-27）：SubjiCameraFollow.FocusAttractionで本体と開始時座席の描画範囲の中心へ切替。縦横比に合わせ全体＋15%の余白が入るOrthographic Sizeへ拡大（通常より寄らない）。終了・キャンセルで元のSizeとプレイヤー追従へ復帰。コンパイル成功（既存警告4件）、Unity再生未確認。
- フリーフォールをユーザーが移動済み。`AlignFreeFallInteractionPoints`が再生開始時に本体画像の下へ乗車地点を合わせ、降車地点の相対差を維持し、本体の子にして追従させる。保存済みシーンだけから移動していないと断定しない。
- ID7 お化け屋敷：既存のゴール到達だけで達成。`SubjiScenePortal.GoalReturned`で帰還を通知。調査地点・人形・追加フラグは禁止。以前追加したものは削除済み。
- ID8 汽笛：E開始後、進行中に音をループ。2秒で近隣の敵へ調査通知、4秒で達成。完了・移動キャンセルで停止。長押しは不要。
- 汽笛アニメ（2026-10-27）：タスク進行中、Eを押している間だけ指定の汽車.anim／汽車_0.controllerを再生。離す・達成・キャンセル時は煙を非表示。既存汽車の子にassettryの煙5個とAnimatorを追加し、trainWhistleAnimatorで参照。タスク自体は従来のE1回開始を維持。コンパイル成功（既存警告4件）、Unity再生・見た目は未確認。
- 煙の配置修正（2026-10-27）：元画像は線路込みのため画像全体の縮尺換算を撤回。煙ルートのlocalPosition=(-0.08,1.25,0)、localScale=(1,1,1)で既存汽車の煙突付近へ調整。再生開始時にRebindして非表示の煙へのバインドを更新。Unityでの見た目は未確認。
- 汽車は既存の`いろいろ2_14`（灰色の蒸気機関車）を使う。追加した`Task Train`と仮の`Sign Wall Surface`は削除済み。勝手に再生成しない。
- 本編カメラ・タスク範囲（2026-10-27）：jikkennsupaceのMain CameraのOrthographic Sizeは6.25（元の5の1.25倍）。操作範囲を元の3倍：phaseInteractionRange=9、汽車はtrainInteractionRangeMultiplier=1で半径9を維持、自販機purchaseRange=6、ブッシュinteractionDistance=4.5、到達taskArrivalDistance=3。屋敷入口は以前に拡大済みのためinteractionRadius=6へ戻した。敵・物理衝突判定は変更なし。コンパイル成功（既存警告4件）、Unityでの見た目は未確認。
- 汽笛音：`Assets/Resources/Audio/train_whistle.mp3`。元音源はDownloadsの`蒸気機関車の汽笛1.mp3`。
- 敵の合計：フェーズ1 通常2体、フェーズ2 通常4体＋監視3体。進行側が既存敵の有効化とスポーナー停止を管理。
- 登録者の毎秒自然増加は既存仕様を維持。タスク報酬の検証では分けて計算する。

## コメント時間の変更場所

- タスクコメント枠（2026-10-28）：指定のF:/gameseisaku/taskcomment.asepriteをAssets/Resources/taskcomment.asepriteに取込。共通ShowPopupのisTaskCommentでタスク提示時のみ切替（通常・プレイヤー発言は各既存枠）。タスク提示の送り主はリスナー。コンパイル成功（既存警告4件）、Unityでの見た目未確認。控え：Backups/before_task_comment_frame_20261028。

- 本編開始コメント（2026-10-28）：`JikkenCommentStream.MainStoryIntro.cs`に仮導入を分離。設計図 (1).xlsx「本編コメント表」No.1～25を時間送りし、No.3の後に中央「配信開始」（初期2秒）、No.25「リスナーA」のタスク発言を3秒表示し、表示開始時に既存ID1をタスク欄へ追加（重複提示なし）。本編シーンで有効。Inspector「本編開始コメントを再生」をオフで従来開始に戻る。本文・送り主・秒数はmainStoryLines。senderNameで名前、taskIdで追加タスクを指定。Pはplayercomment、Lはcomment。移動・敵は維持。完全削除時はPhaseTasks.csのBeginMainStoryIntro／UpdateMainStoryIntroの2箇所を戻し、分離ファイルとシーンのplayMainStoryIntroを削除。コンパイル成功（既存警告4件）、Unity再生未確認。控え：Backups/before_main_story_intro_20261028。

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

## メッセージ画面（2026-10-25）
- JikkenCommentStream：右にコメント、左に開始前・進行中・完了のタスクを個別表示。左のタスクをクリックで開始（同時1件）。進行中は黄、完了は緑。右上のキャンセル画像ボタンで画面を閉じる。画像は Assets/Resources/cancel_button.aseprite。通常マップの左右X座標を交換。
- dotnet build成功（既存警告4件）。Unityでの画像取込・見た目・クリックの再生確認は未実施。変更前控え：Backups/before_message_layout_20261025。
- 配置修正：起動・プレビュー時に保存済みRectTransformへInspector配置を無条件適用しない。Unity上で調整した枠を保持する。メッセージ画面中はSubjiRoadMap.MessageScreenOpenでミニマップと付随タスク表示を隠し、閉じると復帰。

## タスクバー（2026-10-26）
- 左からライト、メッセージ、飲料の順。ItemSlotSelector.commentSlotIndex=1、SubjiPlayerMovement.lightSlotNumber=1。通常マップ内の両スロットCanvasとアイコン追加メニューも対応。再生確認は未実施。

## 専用チュートリアル（2026-10-26）
- 共通ポップアップの送り主はpopupSenderLabelで画像左上の小枠内、本文は下段へ分離。切り出された画像78×37に合わせ送り主のアンカーを(0.14,0.75)～(0.47,0.96)、中央揃え・8～25pt。本文アンカーは(0.06,0.22)～(0.96,0.61)（2026-10-27）。
- 解像度対応：IMGUIの座標・登録者・ミニマップ・操作ゲージ・QTE・帰還・終了表示をSubjiPlayerMovement.ReferenceGuiScopeで既存CanvasScalerと同じ1920×1080・縦横比0.5の倍率へ統一。異なる縦横比では表示領域の広さは変わる。dotnet build成功（既存警告4件）、Unityでの見た目・複数解像度確認は未実施。
- コメント画像（2026-10-27）：Assets/Resources/comment.asepriteを指定元F:/gameseisaku/comment.asepriteの新版で上書き。本編・チュートリアル共用、旧画像の別ファイルは残さない。本編のプレイヤー発言には色の異なるplayercomment.asepriteを使う方針。Assets/Resources/playercomment.asepriteへ取込済み、発言への適用は今後の本編コメント変更時。
- チュートリアル中は座標・登録者のHUDを非表示。Space送りの案内は白の太字＋太い黒縁（outlineWidth=0.5、OUTLINE_ON有効、2026-10-27）。
- 設計図No.12のライト操作後、No.13の終了案内前に「本番は真っ暗だからライトで警備員を見つけて上手に逃げよう」を3秒表示（2026-10-27）。
- シーン：Assets/Scenes/subji/Tutorial.unity。既存マップ・UIを再利用。単独再生可能で、標準は完了時に本編へ進まない。Jikken Comment Streamの「専用チュートリアル」で文言・秒数・色・完了後遷移を調整。
- 進行：JikkenCommentStream.Tutorial.cs（同じpartial）。移動累計4秒→ダッシュ累計2秒→画面を開く→練習タスク受注→ライト点灯。Spaceの押下1回で1工程送り。練習報酬0。暗闇生成なし、敵オブジェクト非アクティブ・スポーナー停止・追加敵生成なし。
- TitleMenuのSTARTからプレイ／スキップを選択。タイトル経由の練習完了後は5秒暗転して本編へ。本編はplayIntroTutorial=falseでID1開始。両経路とも本編シーンの既存初期配置を使用。Build SettingsにTutorial登録済み。
- 変更前控え：Backups/before_tutorial_20261026。dotnet build成功（既存警告4件）。Unityのバックグラウンド起動は終了コード1で停止し、Play Mode・見た目の確認は未実施。
- コメント表示更新：Tutorialも本編のcomment.aseprite画像枠（CreatePopupUi）を共用。指定元F:/gameseisaku/comment.asepriteと同一画像。メッセージ画面中も案内を維持。コメント出現音を共用し、問題OK時にも本編の達成音。説明・OK待ちは3秒（移動4秒・ダッシュ2秒・暗転5秒は維持）。コンパイル成功、再生確認未実施。
