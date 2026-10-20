using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

/// <summary>初期シーンの作成と、往復処理の実行確認。既存シーンは上書きしない。</summary>
public static class SubjiHauntedHouseSetup
{
    public const string ScenePath = "Assets/Scenes/subji/HauntedHouse.unity";
    private const string MainScenePath = "Assets/Scenes/subji/jikkennsupace.unity";

    [MenuItem("Tools/Subji/Open Haunted House")]
    public static void OpenStage()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;
        CreateStageIfMissing();
        EditorSceneManager.OpenScene(ScenePath);
    }

    public static void CreateStageIfMissing()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
            return;

        Scene previousScene = SceneManager.GetActiveScene();
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        GameObject root = new("Haunted House");
        SubjiInteriorStage stage = root.AddComponent<SubjiInteriorStage>();

        GameObject start = new("Start Point (入場地点)");
        start.transform.SetParent(root.transform);
        start.transform.position = new Vector3(2f, 0f, 0f);
        stage.startPoint = start.transform;

        GameObject areaObject = new("Movement Area (移動範囲)");
        areaObject.transform.SetParent(root.transform);
        SubjiMovementArea2D area = areaObject.AddComponent<SubjiMovementArea2D>();
        area.pointA = new Vector2(0f, -4f);
        area.pointB = new Vector2(24f, 4f);
        area.Confirm();
        stage.movementArea = area;

        GameObject grid = new("Grid");
        grid.transform.SetParent(root.transform);
        grid.AddComponent<Grid>();
        GameObject floor = new("Floor (編集用タイルマップ)");
        floor.transform.SetParent(grid.transform);
        Tilemap tilemap = floor.AddComponent<Tilemap>();
        TilemapRenderer floorRenderer = floor.AddComponent<TilemapRenderer>();
        floorRenderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(
            "Assets/Materials/SubjiStageSprite.mat");
        TileBase tile = AssetDatabase.LoadAssetAtPath<TileBase>("Assets/Tile/木タイル.asset");
        if (tile == null)
            throw new InvalidOperationException("屋敷の仮床用タイルが見つかりません。");
        for (int x = 0; x < 24; x++)
            for (int y = -4; y < 4; y++)
                tilemap.SetTile(new Vector3Int(x, y, 0), tile);

        GameObject exit = new("Exit (通常マップへ戻る終点)");
        exit.transform.SetParent(root.transform);
        exit.transform.position = new Vector3(22f, 0f, 0f);
        BoxCollider2D trigger = exit.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = new Vector2(2f, 8f);
        SubjiScenePortal portal = exit.AddComponent<SubjiScenePortal>();
        portal.returnToMap = true;

        // 出口はライトを当てると見える。画面上の案内は暗闇の影響を受けない。
        SpriteRenderer exitRenderer = exit.AddComponent<SpriteRenderer>();
        exitRenderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/木タイル.png");
        exitRenderer.sharedMaterial = floorRenderer.sharedMaterial;
        exitRenderer.color = new Color(0.2f, 1f, 0.35f);
        exitRenderer.sortingOrder = 1;
        EditorSceneManager.SaveScene(scene, ScenePath);
        if (previousScene.IsValid() && previousScene.isLoaded)
            SceneManager.SetActiveScene(previousScene);
        EditorSceneManager.CloseScene(scene, true);
        Debug.Log("HAUNTED_HOUSE_STAGE_CREATED");
    }

    private static SubjiScenePortal smokePortal;
    private static SubjiPlayerMovement smokePlayer;
    private static ItemSlotSelector smokeSlots;
    private static JikkenCommentStream smokeComments;
    private static SubjiEnemyChase smokeEnemy;
    private static int smokePlayerId;
    private static int smokeCommentsId;
    private static int smokeSlotIndex;
    private static int smokeStep;
    private static double deadline;
    private static Vector3 smokeReturn;
    private static bool smokeNight;
    private static float smokeDarkness;

    // -executeMethodでは-quitを付けず、非同期の往復確認が終わった時に終了する。
    [MenuItem("Tools/Subji/Verify Haunted House Round Trip")]
    public static void VerifyRoundTrip()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;
        if (Application.isBatchMode)
            EditorSceneManager.OpenScene(MainScenePath);
        else if (SceneManager.GetActiveScene().path != MainScenePath)
        {
            Debug.LogWarning("通常マップを開いてから往復確認を実行してください。");
            return;
        }
        SessionState.SetBool("HauntedHouseSmokeRunning", true);
        EditorApplication.isPlaying = true;
    }

    [InitializeOnLoadMethod]
    private static void ResumeSmokeTest()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state != PlayModeStateChange.EnteredPlayMode ||
                !SessionState.GetBool("HauntedHouseSmokeRunning", false))
                return;
            SessionState.SetBool("HauntedHouseSmokeRunning", false);
            smokeStep = 0;
            deadline = EditorApplication.timeSinceStartup + 90;
            EditorApplication.update += TickSmokeTest;
        };
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private static void TickSmokeTest()
    {
        try
        {
            Check(EditorApplication.timeSinceStartup < deadline, "往復確認がタイムアウトしました。");
            if (smokeStep == 0)
            {
                smokePlayer = UnityEngine.Object.FindFirstObjectByType<SubjiPlayerMovement>();
                smokePortal = UnityEngine.Object.FindObjectsByType<SubjiScenePortal>(FindObjectsSortMode.None)
                    .First(portal => !portal.returnToMap);
                smokeSlots = UnityEngine.Object.FindFirstObjectByType<ItemSlotSelector>();
                smokeComments = UnityEngine.Object.FindFirstObjectByType<JikkenCommentStream>();
                smokeEnemy = UnityEngine.Object.FindFirstObjectByType<SubjiEnemyChase>();
                Check(smokePlayer != null && smokePlayer.enabled, "プレイヤーが動作していません。");
                smokePlayerId = smokePlayer.GetInstanceID();
                smokeCommentsId = smokeComments.GetInstanceID();
                smokeSlots.Select(1);
                smokeSlotIndex = smokeSlots.SelectedIndex;
                smokeNight = smokePlayer.enableNightVision;
                smokeDarkness = smokePlayer.outsideDarkness;
                smokePlayer.TeleportTo(smokePortal.transform.position);
                smokeReturn = smokePlayer.transform.position;
                smokePortal.Enter(smokePlayer);
                smokeStep = 1;
            }
            else if (smokeStep == 1 && smokePortal.IsVisiting && !smokePortal.IsBusy)
            {
                Check(smokePlayer.IsInInterior, "屋内の移動・暗闇設定が有効ではありません。");
                Check(!smokeEnemy.gameObject.activeInHierarchy, "通常マップの敵が停止していません。");
                Check(!smokeComments.enabled, "屋外タスクの更新が停止していません。");
                Check(smokePlayer.GetInstanceID() == smokePlayerId &&
                    smokeSlots.SelectedIndex == smokeSlotIndex, "プレイヤーか所持品がリセットされました。");
                Check(!smokePlayer.IsWorldPositionIlluminated((Vector2)smokePlayer.transform.position + Vector2.up * 0.2f),
                    "屋内にライト以外の視野が残っています。");
                SpriteRenderer overlay = GameObject.Find("Player Vision Darkness").GetComponent<SpriteRenderer>();
                Check(overlay.gameObject.activeInHierarchy && overlay.material.GetColor("_Color").a == 1f &&
                    overlay.material.GetVector("_PlayerData").z < 0f, "完全暗闇の描画設定が不正です。");
                Check(overlay.material.shader.isSupported, "暗闇シェーダーがサポートされていません。");
                SubjiInteriorStage stage = UnityEngine.Object.FindFirstObjectByType<SubjiInteriorStage>();
                Check(Vector3.Distance(smokePlayer.transform.position, stage.startPoint.position) < 0.1f,
                    "入場地点に移動していません。");
                SubjiScenePortal exit = UnityEngine.Object.FindObjectsByType<SubjiScenePortal>(FindObjectsSortMode.None)
                    .First(portal => portal.returnToMap);
                smokePlayer.TeleportTo(exit.transform.position);
                smokeStep = 2; // 出口の実際のTriggerで帰還させる。
            }
            else if (smokeStep == 2 && !smokePortal.IsVisiting && !smokePortal.IsBusy)
            {
                Check(!smokePlayer.IsInInterior, "屋内設定が解除されていません。");
                Check(Vector3.Distance(smokePlayer.transform.position, smokeReturn) < 0.1f,
                    "通常マップの入場位置へ戻っていません。");
                Check(smokeEnemy.gameObject.activeInHierarchy && smokeComments.enabled,
                    "通常マップの敵・タスクが再開していません。");
                Check(smokeComments.GetInstanceID() == smokeCommentsId &&
                    smokeSlots.SelectedIndex == smokeSlotIndex, "タスクか所持品がリセットされました。");
                Check(smokePlayer.enableNightVision == smokeNight &&
                    smokePlayer.outsideDarkness == smokeDarkness, "通常マップの明るさ設定が変わりました。");
                Check(!SceneManager.GetSceneByPath(ScenePath).isLoaded, "屋内シーンが解放されていません。");
                Debug.Log("HAUNTED_HOUSE_ROUND_TRIP_PASSED");
                EditorApplication.update -= TickSmokeTest;
                FinishSmokeTest(true);
            }
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorApplication.update -= TickSmokeTest;
            FinishSmokeTest(false);
        }
    }

    private static void FinishSmokeTest(bool success)
    {
        System.IO.File.WriteAllText("Library/HauntedHouseSmoke.result", success ? "PASSED" : "FAILED");
        if (Application.isBatchMode)
            EditorApplication.Exit(success ? 0 : 1);
        else
            EditorApplication.isPlaying = false;
    }
}
