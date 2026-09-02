#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class SubjiGreenGuardSceneSetup
{
    static SubjiGreenGuardSceneSetup()
    {
        EditorApplication.delayCall += PlaceIfTargetSceneIsOpen;
    }

    private static void PlaceIfTargetSceneIsOpen()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        if (EditorSceneManager.GetActiveScene().path ==
            "Assets/Scenes/subji/jikkennsupace.unity")
            PlaceGreenGuard(false);
    }

    [MenuItem("Tools/Subji/監視敵をシーン配置")]
    public static void PlaceGreenGuard()
    {
        PlaceGreenGuard(true);
    }

    private static void PlaceGreenGuard(bool openTargetScene)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("監視敵のシーン配置はPlay Mode終了後に実行してください。");
            return;
        }

        const string scenePath = "Assets/Scenes/subji/jikkennsupace.unity";
        Scene scene = EditorSceneManager.GetActiveScene();
        if (scene.path != scenePath)
        {
            if (!openTargetScene)
                return;
            scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        }
        JikkenCommentStream stream = Object.FindFirstObjectByType<JikkenCommentStream>();
        if (stream == null)
        {
            Debug.LogError("JikkenCommentStream が見つかりません。");
            return;
        }

        GameObject container = FindSceneObject(
            "Green Guards (Activate after ID6+7)", scene);
        if (container == null)
        {
            container = new GameObject("Green Guards (Activate after ID6+7)");
            SceneManager.MoveGameObjectToScene(container, scene);
        }

        GameObject guard = FindSceneObject("Green Guard Post (Scene Editable)", scene);
        SubjiEnemyChase enemy;
        if (guard == null)
        {
            guard = new GameObject("Green Guard Post (Scene Editable)");
            SceneManager.MoveGameObjectToScene(guard, scene);
            guard.transform.SetParent(container.transform);
            guard.transform.position = new Vector3(-10f, -3f, 0f);
            enemy = guard.AddComponent<SubjiEnemyChase>();
        }
        else
        {
            enemy = guard.GetComponent<SubjiEnemyChase>();
            if (enemy == null)
                enemy = guard.AddComponent<SubjiEnemyChase>();
        }
        guard.transform.SetParent(container.transform, true);

        enemy.spriteResourcePath = "guard_green_enemy";
        enemy.movementType = SubjiEnemyChase.MovementType.GuardPost;
        enemy.matchPlayerMoveSpeed = true;
        enemy.returnSpeed = 1f;
        enemy.guardLookInterval = 1.5f;
        enemy.useRadialDetection = false;
        enemy.ApplyAppearanceAndCollider();
        enemy.spawnTiming = SubjiEnemyChase.SpawnTiming.AfterTasks6And7;

        // 通常敵と同じく、シーンのSpriteRendererにもスプライト実体を保存する。
        SpriteRenderer renderer = guard.GetComponent<SpriteRenderer>();
        SpriteRenderer normalEnemyRenderer = FindNormalEnemyRenderer(enemy, scene);
        if (normalEnemyRenderer != null)
        {
            renderer.sharedMaterial = normalEnemyRenderer.sharedMaterial;
            renderer.color = normalEnemyRenderer.color;
            renderer.sortingLayerID = normalEnemyRenderer.sortingLayerID;
            renderer.sortingOrder = normalEnemyRenderer.sortingOrder;
        }
        else
        {
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(
                "Assets/Resources/guard_green_enemy.aseprite"))
            {
                if (asset is Sprite sprite)
                {
                    renderer.sprite = sprite;
                    break;
                }
            }
        }

        SerializedObject serializedStream = new SerializedObject(stream);
        serializedStream.FindProperty("greenGuardContainer").objectReferenceValue = container;
        serializedStream.ApplyModifiedPropertiesWithoutUndo();

        guard.SetActive(true);
        container.SetActive(true);

        GameObject afterTask12Guard = FindSceneObject(
            "Green Guard Post (After ID12)", scene);
        if (afterTask12Guard == null)
        {
            afterTask12Guard = Object.Instantiate(guard, container.transform);
            afterTask12Guard.name = "Green Guard Post (After ID12)";
            afterTask12Guard.transform.position = new Vector3(-7f, -3f, 0f);
        }
        afterTask12Guard.GetComponent<SubjiEnemyChase>().spawnTiming =
            SubjiEnemyChase.SpawnTiming.AfterTask12;
        afterTask12Guard.SetActive(true);

        EditorUtility.SetDirty(stream);
        EditorUtility.SetDirty(guard);
        EditorUtility.SetDirty(container);
        EditorUtility.SetDirty(afterTask12Guard);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("監視敵をシーンへ配置し、JikkenCommentStream に設定しました。");
    }

    private static GameObject FindSceneObject(string objectName, Scene scene)
    {
        foreach (GameObject candidate in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (candidate.scene == scene && candidate.name == objectName)
                return candidate;
        }
        return null;
    }

    private static SpriteRenderer FindNormalEnemyRenderer(
        SubjiEnemyChase guardEnemy, Scene scene)
    {
        foreach (SubjiEnemyChase candidate in
            Resources.FindObjectsOfTypeAll<SubjiEnemyChase>())
        {
            if (candidate != guardEnemy && candidate.gameObject.scene == scene &&
                candidate.movementType != SubjiEnemyChase.MovementType.GuardPost)
            {
                SpriteRenderer candidateRenderer =
                    candidate.GetComponent<SpriteRenderer>();
                if (candidateRenderer != null && candidateRenderer.sprite != null)
                    return candidateRenderer;
            }
        }
        return null;
    }
}
#endif
