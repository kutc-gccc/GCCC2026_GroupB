using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>既存シーンに対象参照と編集可能な操作地点を設定する。</summary>
public static class PhaseTwoTaskSetup
{
    private const string ScenePath = "Assets/Scenes/subji/jikkennsupace.unity";

    [MenuItem("Tools/Tasks/フェーズ2までの対象を設定")]
    public static void Configure()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var scene = EditorSceneManager.OpenScene(ScenePath);
        var stream = UnityEngine.Object.FindFirstObjectByType<JikkenCommentStream>();
        if (stream == null) throw new InvalidOperationException("JikkenCommentStreamがありません");
        var all = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true)).ToArray();
        Transform fountain = all.FirstOrDefault(t => t.name.StartsWith("噴水"));
        Transform freeFall = all.FirstOrDefault(t => t.name.StartsWith("フリーフォール"));
        var portal = UnityEngine.Object.FindObjectsByType<SubjiScenePortal>(FindObjectsInactive.Include,
            FindObjectsSortMode.None).FirstOrDefault(p => !p.returnToMap);
        if (fountain == null || freeFall == null || portal == null)
            throw new InvalidOperationException("噴水・フリーフォール・お化け屋敷入口を確認してください");

        Transform points = all.FirstOrDefault(t => t.name == "Phase 1-2 Task Points");
        if (points == null) points = new GameObject("Phase 1-2 Task Points").transform;
        Vector3 fountainGround = GroundPoint(fountain);
        stream.fountainDestination = Point(points, "01 Fountain Destination", fountainGround);
        stream.freeFallBoardingPoint = Point(points, "06 Free Fall Boarding", GroundPoint(freeFall));
        stream.freeFallExitPoint = Point(points, "06 Free Fall Exit", stream.freeFallBoardingPoint.position + Vector3.right * 2f);
        stream.signLocation = Point(points, "05 Sign Wall Interaction", fountainGround + Vector3.left * 6f);
        Transform train = all.FirstOrDefault(t => t.name == "いろいろ2_14");
        if (train == null) throw new InvalidOperationException("既存の汽車（いろいろ2_14）を設定してください");
        stream.trainWhistlePoint = Point(points, "08 Train Whistle Interaction", GroundPoint(train));
        stream.hauntedHouseEntrance = portal;
        stream.whistleClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Resources/Audio/train_whistle.mp3");
        stream.enablePhaseTwoTasks = true;
        stream.vendingSeconds = 1f;
        stream.bushSeconds = 0.5f;
        stream.freeFallSeconds = 3f;
        stream.whistleSeconds = 4f;
        stream.whistleAlertSeconds = 2f;
        stream.signSequence = new[] { UnityEngine.InputSystem.Key.UpArrow, UnityEngine.InputSystem.Key.DownArrow,
            UnityEngine.InputSystem.Key.LeftArrow, UnityEngine.InputSystem.Key.DownArrow,
            UnityEngine.InputSystem.Key.DownArrow, UnityEngine.InputSystem.Key.RightArrow };
        EditorUtility.SetDirty(stream);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Validate();
        Debug.Log("PHASE_TWO_SETUP_OK");
    }

    private static Vector3 GroundPoint(Transform target)
    {
        var renderer = target.GetComponent<SpriteRenderer>();
        return renderer == null ? target.position + Vector3.down * 2f :
            new Vector3(renderer.bounds.center.x, renderer.bounds.min.y - 1f, 0f);
    }

    private static Transform Point(Transform parent, string name, Vector3 position)
    {
        var existing = parent.Find(name);
        if (existing != null) return existing;
        var point = new GameObject(name).transform;
        point.SetParent(parent, false);
        point.position = position;
        return point;
    }

    [MenuItem("Tools/Tasks/フェーズ2設定を検証")]
    public static void Validate()
    {
        var stream = UnityEngine.Object.FindFirstObjectByType<JikkenCommentStream>();
        if (stream == null || stream.fountainDestination == null || stream.signLocation == null ||
            stream.freeFallBoardingPoint == null || stream.freeFallExitPoint == null ||
            stream.trainWhistlePoint == null || stream.hauntedHouseEntrance == null || stream.whistleClip == null)
            throw new InvalidOperationException("フェーズタスクの対象参照が不足しています");
        var enemies = UnityEngine.Object.FindObjectsByType<SubjiEnemyChase>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (enemies.Count(e => e.movementType == SubjiEnemyChase.MovementType.GuardPost) < 3 ||
            enemies.Count(e => e.movementType != SubjiEnemyChase.MovementType.GuardPost) < 4)
            throw new InvalidOperationException("フェーズ2の敵数が不足しています");
        if (!EditorBuildSettings.scenes.Any(s => s.enabled && s.path == stream.hauntedHouseEntrance.interiorScenePath))
            throw new InvalidOperationException("お化け屋敷がBuild Settingsにありません");
        Debug.Log("PHASE_TWO_VALIDATION_OK");
    }
}
