# Codex共有メモ

最終確認：2026-10-29。このプロジェクトの別チャット向け。会話全文の再取得は不要。現物の変更が優先。

## 次のチャットへ（現在の優先事項）

- 最優先の未確認：ドリンク使用不可の再報告を受け、左クリックから選択枠へ直接使用する修正済みだが成功未確認。敵の大小差も実機未確認。直近依頼はID12クリア表示が消えてから「脱出せよ」を表示する変更（実装・コンパイル済み）。詳細は末尾の現行状態を参照。

- 本編コメントの現行原本は `F:/gameseisaku/設計図 (1).xlsx` の「本編コメント表」。下記の旧設計図パスと混同しない。更新確認を求められたら保存済み原本を確認する。現在No.109まで実装（No.47・48は自販機失敗時のランダム発言）。
- ナビゲーターはユーザー希望で一時非表示（showTaskSelectionReminder=false）。勝手に再表示しない。
- ID1達成後、No.40でID2提示。ID2達成後No.49～58→No.59でID8提示（選択待ち）→No.60～72、ID8達成後No.73～84、汽笛増援の追跡が途切れて3秒後No.85～94→No.95でID12提示・選択待ち→No.96～104→ID12達成後No.105で脱出表示・ゴール有効化→No.106～109。No.65はID8操作開始待ち。その他の自動提示・フェーズ進行は停止中（autoAdvancePhaseTasks=false）。続きの流れ・タスクの提示時期はユーザーが決めるため、指示前に追加・再開しない。
- ブッシュはユーザー指示で一時無効（BushHideSpot2D.interactionEnabled=false）。操作・範囲判定・操作Triggerを停止。勝手に復帰しない。
- 全敵のlocalScaleはX=1.5、Y=1.5、Z=1（倍率ではなく絶対値）。既存敵の親Enemiesは1.3倍。追加敵もひな形と同じ親へ置くことで見た目を揃える。
- 汽笛増援は開始2秒後に出現。終了約2秒後に索敵範囲へ入る距離・速度で接近する。「終了後に停止して時間で強制追跡」の意味ではない。
- 本編導入は後で削除する可能性があり、MainStoryIntroのpartialへ分離済み。playMainStoryIntroオフで導入を省略できるが、タスク自動進行停止は別設定として維持する。
- 会話は通常の標準語で簡潔に。ユーザーの関西弁をまねしない。最新変更はコンパイル成功（既存警告4件）、Unity再生確認は未実施。未コミット変更・Unity側の変更を破棄しない。

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

- フェーズ1：ID1 噴水へ到達 → ID2 自販機でEを押すたび抽選（初回必ず失敗、以降1/8でドリンク入手すると達成）→ ID3 ブッシュ0.5秒。報酬はID1・3が100人、ID2が1,000人。失敗時は「まだ揺らしが足りないようだ、もう少し揺らしてみよう」を表示。通常本編の自販機に適用、専用チュートリアルは従来どおり。ID4 警備員をまくタスクは保留・欠番。
- フェーズ2：ID5～8から3つ。ID7を必ず含め、残り2つを抽選し、出現順をシャッフル。報酬は各5,000人。3つ達成で止める。フェーズ3・エンディングは追加対象外。
- ID5 サイン：矢印キー ↑ ↓ ← ↓ ↓ →。現在は範囲内でE開始、誤入力で先頭へ、WASDでキャンセル、制限時間なし。
- ID6 フリーフォール：E開始後3秒で完了。途中下車不可。乗車中は隠れ状態と敵停止で接触を防ぐ。終了時に戻す。
- フリーフォールアニメ（2026-10-27）：本編の既存本体を指定prefabと同じ座席なし画像に変更し、座席の子と指定controllerのAnimatorを追加。freeFallAnimator／freeFallSeatを参照。乗車3秒でクリップ1周、終了・キャンセルで座席の開始位置へ復帰。配置・スケール維持。コンパイル成功（既存警告4件）、Unity再生未確認。
- フリーフォール乗車カメラ（2026-10-27）：SubjiCameraFollow.FocusAttractionで本体と開始時座席の描画範囲の中心へ切替。縦横比に合わせ全体＋15%の余白が入るOrthographic Sizeへ拡大（通常より寄らない）。終了・キャンセルで元のSizeとプレイヤー追従へ復帰。コンパイル成功（既存警告4件）、Unity再生未確認。
- フリーフォールをユーザーが移動済み。`AlignFreeFallInteractionPoints`が再生開始時に本体画像の下へ乗車地点を合わせ、降車地点の相対差を維持し、本体の子にして追従させる。保存済みシーンだけから移動していないと断定しない。
- ID7 お化け屋敷：既存のゴール到達だけで達成。`SubjiScenePortal.GoalReturned`で帰還を通知。調査地点・人形・追加フラグは禁止。以前追加したものは削除済み。
- ID8 汽笛：E開始後、進行中に音をループ。2秒で近隣の敵へ調査通知＋通常敵を3～6体（25%で10体）出現、4秒で達成。追加敵は共通SubjiEnemySpawner.SpawnEnemyAtとSubjiEnemyChaseの時間付き騒音調査で接近し、汽笛終了の約2秒後に索敵範囲へ入るよう、出現距離と接近速度を調整する。停止待機・強制追跡は廃止し、接近中も通常索敵を維持。再試行で追加波を増やさない。完了・移動キャンセルで停止。長押しは不要。
- 汽笛アニメ（2026-10-27）：タスク進行中、Eを押している間だけ指定の汽車.anim／汽車_0.controllerを再生。離す・達成・キャンセル時は煙を非表示。既存汽車の子にassettryの煙5個とAnimatorを追加し、trainWhistleAnimatorで参照。タスク自体は従来のE1回開始を維持。コンパイル成功（既存警告4件）、Unity再生・見た目は未確認。
- 煙の配置修正（2026-10-27）：元画像は線路込みのため画像全体の縮尺換算を撤回。煙ルートのlocalPosition=(-0.08,1.25,0)、localScale=(1,1,1)で既存汽車の煙突付近へ調整。再生開始時にRebindして非表示の煙へのバインドを更新。Unityでの見た目は未確認。
- 汽車は既存の`いろいろ2_14`（灰色の蒸気機関車）を使う。追加した`Task Train`と仮の`Sign Wall Surface`は削除済み。勝手に再生成しない。
- 本編カメラ・タスク範囲（2026-10-27）：jikkennsupaceのMain CameraのOrthographic Sizeは6.25（元の5の1.25倍）。操作範囲を元の3倍：phaseInteractionRange=9、汽車はtrainInteractionRangeMultiplier=1で半径9を維持、自販機purchaseRange=6、ブッシュinteractionDistance=4.5、到達taskArrivalDistance=3。屋敷入口は以前に拡大済みのためinteractionRadius=6へ戻した。敵・物理衝突判定は変更なし。コンパイル成功（既存警告4件）、Unityでの見た目は未確認。
- 汽笛音：`Assets/Resources/Audio/train_whistle.mp3`。元音源はDownloadsの`蒸気機関車の汽笛1.mp3`。
- 敵の合計：ID1達成までは0体、達成時に通常2体を有効化（自動進行停止中も適用）。フェーズ1 通常2体、フェーズ2 通常4体＋監視3体。進行側が既存敵の有効化とスポーナー停止を管理。
- 登録者の毎秒自然増加は既存仕様を維持。タスク報酬の検証では分けて計算する。

## コメント時間の変更場所

- 共通未選択案内：`JikkenCommentStream.TaskSelectionReminder.cs`。現在はshowTaskSelectionReminder=falseで非表示。再度オンにした場合は通常本編の全タスク共通で、追加から3秒未選択ならナビゲーター「メッセージ機能のタスク欄確認してる？」を別枠で選択まで表示。文言・送り主・秒数はInspector。チュートリアル対象外。選択・待機の管理は共通CreateSelectableTaskComment／SpawnComment／SelectTaskに接続。

- タスクコメント枠（2026-10-28）：指定のF:/gameseisaku/taskcomment.asepriteをAssets/Resources/taskcomment.asepriteに取込。共通ShowPopupのisTaskCommentでタスク提示時のみ切替（通常・プレイヤー発言は各既存枠）。タスク提示の送り主はリスナー。コンパイル成功（既存警告4件）、Unityでの見た目未確認。控え：Backups/before_task_comment_frame_20261028。

- 本編コメント（2026-10-28）：`JikkenCommentStream.MainStoryIntro.cs`に仮導入を分離。設計図 (1).xlsx No.1～94対応（No.26は共通案内）。No.3後に中央「配信開始」2秒。No.25はリスナーA＋タスク枠でID1追加、選択まで維持。選択後No.27～32、ID1達成後No.33～39、その後No.40でリスナーBがID2提示（選択まで維持）、No.41～46、ID2達成後No.49～58、No.59でID8提示・選択待ち、No.60～72、ID8達成後No.73～84、汽笛増援の追跡が途切れて3秒後No.85～94→No.95でID12提示・選択待ち→No.96～104→ID12達成後No.105で脱出表示・ゴール有効化→No.106～109。No.65はID8操作開始待ち。autoAdvancePhaseTasks=falseでその他の自動提示・フェーズ進行を停止。失敗案内はタスクバー上に3秒表示。No.47/48は失敗ごとにランダム0.5秒の割込み発言（vendingFailureLines）。早期達成も記録して順序維持。画面を開いている間は時間送りを待機。InspectorのmainStoryLinesで本文・秒数・senderName・taskId・waitForTaskSelection・waitForCompletedTaskId・waitForStartedTaskId・waitForWhistleEscapeを調整。playMainStoryIntroオフで従来開始。Pはplayercomment、Lはcomment。移動・敵は維持。共通側の選択通知とPhaseTasksの開始・更新・達成フックで接続。コンパイル成功（既存警告4件）、Unity再生未確認。控え：Backups/before_main_story39_20261028。

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

## 現在の追加設定・確認箇所（2026-10-29）

- デバッグ：`debugSpaceSkipComments=true`で本編の通常コメント・配信開始をSpace送り。タスク選択・達成・操作開始・逃走待ちは飛ばさない。
- ID2：E1回につき`TimedWorldInteraction`で0.25秒後に抽選。初回失敗、以降1/8、報酬1,000人。失敗案内「まだ揺らしが足りないようだ、もう少し揺らしてみよう」はタスクバー直上に3秒（メッセージ画面中は非表示）。
- ID8逃走判定：`JikkenCommentStream.PhaseTasks.cs`の`UpdateWhistleEscape`。汽笛で生成した敵だけ0.1秒間隔で確認。一度でも追跡された後、全員の`IsChasingPlayer`がfalseのまま3秒でNo.85の待機解除。再追跡でリセット。`whistleEscapeSeconds`はInspector。
- 敵生成：`SubjiEnemySpawner.SpawnEnemyAt`を共用。ひな形と同じ親・見た目を使い、`SubjiEnemyChase`の生成・再有効化・外観反映時にScale=(1.5,1.5,1)へ統一。本編・Tutorialの保存済み敵も統一済み。Recoveryは触らない。
- 汽笛の接近は`SpawnWhistleEnemies`で残り操作時間＋2秒を目安に設定し、`SubjiEnemyChase.InvestigateNoise`で通常索敵を続けながら移動。実際の発見時刻はプレイヤー移動・道路迂回・視野で変動。再試行で増援を追加しない。
- 最新検証：No.1～94の番号重複・欠番なし、No.73～94の22件の本文・待機条件を保存済み原本と照合。C#コンパイル成功（既存警告4件）。Unityで会話・逃走再開・増援の見た目と接近タイミングは未確認。次の依頼で必要な箇所だけ確認する。
- 直近の控え：`Backups/before_story94_escape_20261029`（追加前の会話・進行・敵基盤・本編シーン）、`Backups/before_pause_bush_20261029`、`Backups/before_spawn_enemy_parent_fix_20261029`。未コミット変更やUnityの未保存変更を破棄しない。
- 敵配置：本編の配置済み敵は一時非アクティブ（配置・参照は保持）。ID1達成時の通常2体とID8汽笛増援は維持。勝手に配置を戻さない。
- ID12・脱出：No.95リスナーZでID12提示・選択待ち。既存の城をfireworksPointに設定。画像の端から距離9以内でE1回→TimedWorldInteractionで3秒操作（移動キャンセル）、報酬100000。No.105はID12達成と既存クリア表示1.3秒の終了を待ち、中央「脱出せよ」を1秒表示して(0,-13)のゴール・ミニマップを有効化（受注不要）。No.106～109後に会話停止。ゴール接触でCLEAR。花火映像演出は未追加。
- 脱出待機の実装：MainStoryIntro.CanPresentMainStoryLineのshowEscapeGoal条件でSubjiPlayerMovement.IsTaskCompleteMessageVisibleを参照。固定の追加待ち時間はない。
- 敵の見た目：SubjiEnemyChase.ApplyAppearanceAndColliderでSprites/Defaultの共有Material・白を適用。保存済み本編・Tutorialの敵43体ずつはlocalScale=(1.5,1.5,1)、親込み1.95を確認。CreateEnemyは非アクティブの既存Enemyも再利用。ApplyUniformScaleは実行中の親なし敵を同シーンのEnemiesへ所属させ、再有効化・親変更時もlocalScaleを統一。大小差の実機解消は未確認。
- タスクバー重複：本編・TutorialのItemSlotCanvas (1)を非アクティブ化し、ItemSlotCanvasだけを使用。重複枠がドリンクを覆っていたため。勝手に重複側を再有効化しない。DrinkItemControllerは初期化・入手時の色を白、不透明度1へ設定。
- ドリンク使用【要再生確認】：ユーザーから使用不可の再報告あり。最新修正ではItemSlotSelectorの左クリックで同じCanvasのDrinkItemController.TryUseDrink(slots[selectedIndex])を直接呼ぶ。選択枠直下の有効なDrinkIconを参照し、4秒スタミナ消費停止・アイコン非表示。旧イベント購読による使用と使用時の番号辞書参照は撤去。入手用キャッシュはselector.Slots順。修正後の使用成功は未確認。直近控え：Backups/before_direct_drink_use_*。
- E操作中ライト：SubjiPlayerMovement.UpdateSlotLightはinteractionMovementLockedで早期終了しない。移動停止中もマウス追従・左点灯・右消灯を許可。メッセージ画面・ゲーム停止時の制限は維持。
- 最新検証：コンパイル成功（既存警告4件）。No.1～109の本文・話者と追加15件の時間・待機設定を原本・コード・シーンで照合済み。Unity再生でのドリンク使用、敵サイズ、E操作中ライト、ID12達成表示→脱出の順番は未確認。未コミット・Unity未保存変更を破棄しない。

## クリア画面・通常ライト（2026-10-29、現行）

- ユーザー自身で編集するため、専用シーン `Assets/Scenes/Clear.unity` を使用。単独再生可能、Build Settings登録済み。本編の `SubjiGameClearGoal.ReachGoal` から遷移。旧 `Resources/ClearScreenArtwork.prefab` は撤去済み。
- `Clear Canvas` 配下に背景、`Fireworks/Firework 1～6`、`Castle Upper Half`、`CLEAR Label`、`Restart Button`、`Ending Caption` を保存。位置・大きさはシーン上で編集でき、起動時に固定配置へ戻さない。花火制御は `Assets/Scripts/Subji/SubjiClearScreen.cs`、`Clear Screen Settings` のInspectorで素材・秒数・上昇距離を編集。
- 黒背景、城の上半分、周囲の繰り返し花火。城は本編と同じSpriteをUI Imageで直接参照し、下半分を画面外へ出す。子Canvasの描画順は背景-20、花火0、城10、文字・ボタン20。城のMaterialは `Assets/Materials/ClearCastleDim.mat`、Shaderは `Assets/Shaders/ClearCastleDim.shader`、Brightness=0.6（透明度は維持）。
- 中央基準でCLEAR Y=0、Restart Y=-110、Ending Caption Y=-185。文章は「彼女のその後は誰も知らない」（NotoSansJP・28pt）。Restartは `Title` シーンへ戻る。
- 通常ライトは `Assets/Scripts/Subji/SubjiPlayerMovement.cs` の `lightEdgeAngle=3` 度、`lightDropoff=15`。本編シーンにも保存。`Assets/Resources/SubjiStaticDarkness.shader` で照射角度の内側3度にpow減衰。屋内は周縁角度0を送信し従来表示、照射角度・距離は変更なし。
- 最新C#コンパイル成功（既存警告4件）。Unityバックグラウンド起動は終了コード1で停止。Unityの再生・シェーダーコンパイル・実表示は未確認。城が見えないとの再報告後、Image方式へ変更済みだが表示成功の明示確認はない。
- 控え：`Backups/before_clear_scene_20261029`、`before_clear_visibility_20261029`、`before_clear_castle_sprite_20261029`、`before_clear_dim_layout_20261029`、`before_clear_caption_20261029`、`before_light_edge_20261029`。ユーザーや他チャットの変更・Unity未保存変更を上書きしない。

- ID12後の手動敵配置：JikkenCommentStream.PhaseTasks.cs の fireworksClearEnemies（Inspector『ID12クリア後に出現する敵』）へシーン配置済みのSubjiEnemyChaseを登録。開始時に無効化し通常フェーズの人数対象から除外、ID12達成時に有効化。配置・個体設定はユーザーが編集。未登録なら従来どおり。
- 現行：PキーのID12後ジャンプ・暗さ切替を削除。Space送りは維持し、案内は共通Canvasの画面下中央（下端140、タスクバー上）に固定。本編会話中のみ表示。緑敵のplayer未設定時の自動取得と、ID12までの敵をまとめて有効化する修正は維持。コンパイル成功、案内の実表示は未確認。
- 選択後の共通案内：タスク選択後、操作開始・達成なしで8秒（メッセージ画面中と停止中は計時停止）経つとナビゲーター「目的地はミニマップに書いてあるよ。近くでEを押すと反応するよ」を別コメント枠に4秒、タスクごと1回表示。共通TimedWorldInteraction操作中・QTE開始・隠れ操作で抑止。未選択時案内の無効設定は維持。コンパイル成功、Unity再生未確認。
- 終了操作：GameQuitConfirmation.csを起動時に1つ生成し全シーンで共用。左上「Escでゲームを閉じる」、Escで一時停止して「ゲームを終了しますか？」と「はい／いいえ」。はいでビルド版はApplication.Quit、Editorでは再生停止。いいえ／再Escで元の時間倍率・EventSystemへ復帰。日本語はWindowsのYu Gothic／Meiryoフォントを使用。コンパイル成功。
