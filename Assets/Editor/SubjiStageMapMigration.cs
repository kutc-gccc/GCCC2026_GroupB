using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SubjiStageMapMigration
{
    private const string SourcePath = "Assets/Scenes/stage/stagescene.unity";
    private const string TargetPath = "Assets/Scenes/subji/jikkennsupace.unity";
    private const string MapRootName = "Stage Map (from stagen)";
    private const string SpriteMaterialPath = "Assets/Materials/SubjiStageSprite.mat";

    [MenuItem("Tools/Subji/Replace Map With Stage")]
    public static void ReplaceMap()
    {
        Scene target = EditorSceneManager.OpenScene(TargetPath, OpenSceneMode.Single);

        GameObject oldMapRoot = GameObject.Find(MapRootName);
        if (oldMapRoot != null)
            Object.DestroyImmediate(oldMapRoot);

        GameObject border = GameObject.Find("60x60 Field Border");
        if (border != null)
            Object.DestroyImmediate(border);

        SubjiRoadMap roadMap = Object.FindFirstObjectByType<SubjiRoadMap>();
        if (roadMap == null)
            throw new MissingComponentException("SubjiRoadMap was not found in the target scene.");

        roadMap.restrictMovementToRoads = false;
        roadMap.showRoadsInSceneView = false;
        SubjiEnemySpawner spawner = roadMap.GetComponent<SubjiEnemySpawner>();
        if (spawner != null)
        {
            spawner.spawnMode = SubjiEnemySpawner.SpawnMode.RandomOnStage;
            spawner.snapFixedPointsToRoad = false;
        }

        SubjiPlayerMovement playerMovement = Object.FindFirstObjectByType<SubjiPlayerMovement>();
        if (playerMovement != null)
        {
            playerMovement.enableNightVision = false;
            SpriteRenderer playerRenderer = playerMovement.GetComponent<SpriteRenderer>();
            ApplyCharacterRendering(playerRenderer, 100);
            EditorUtility.SetDirty(playerMovement);
        }

        SubjiEnemyChase enemyTemplate = Object.FindFirstObjectByType<SubjiEnemyChase>();
        if (enemyTemplate != null)
            ApplyCharacterRendering(enemyTemplate.GetComponent<SpriteRenderer>(), 90);

        // SubjiEnemyChaseへ置き換え済みなので、旧EnemyChaseは二重動作と
        // player未設定エラーの原因になるため削除する。
        foreach (EnemyChase oldEnemyChase in
            Object.FindObjectsByType<EnemyChase>(FindObjectsSortMode.None))
        {
            Object.DestroyImmediate(oldEnemyChase);
        }

        Scene source = EditorSceneManager.OpenScene(SourcePath, OpenSceneMode.Additive);
        GameObject mapRoot = new GameObject(MapRootName);
        SceneManager.MoveGameObjectToScene(mapRoot, target);

        foreach (GameObject root in source.GetRootGameObjects())
        {
            if (root.name == "Main Camera" || root.name == "Global Light 2D" ||
                root.name == "Player" || root.name == "Enemy" ||
                root.name == "60x60 Field Border")
                continue;

            GameObject copy = Object.Instantiate(root);
            copy.name = root.name;
            SceneManager.MoveGameObjectToScene(copy, target);
            copy.transform.SetParent(mapRoot.transform, true);
        }

        Material defaultSpriteMaterial = AssetDatabase.LoadAssetAtPath<Material>(SpriteMaterialPath);
        if (defaultSpriteMaterial == null)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Materials"))
                AssetDatabase.CreateFolder("Assets", "Materials");
            defaultSpriteMaterial = new Material(Shader.Find("Sprites/Default"));
            defaultSpriteMaterial.name = "Subji Stage Sprite";
            AssetDatabase.CreateAsset(defaultSpriteMaterial, SpriteMaterialPath);
        }
        foreach (SpriteRenderer spriteRenderer in mapRoot.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (defaultSpriteMaterial != null)
            {
                spriteRenderer.sharedMaterial = defaultSpriteMaterial;
                EditorUtility.SetDirty(spriteRenderer);
            }
        }

        EditorSceneManager.CloseScene(source, true);
        EditorUtility.SetDirty(roadMap);
        if (spawner != null)
            EditorUtility.SetDirty(spawner);
        EditorSceneManager.MarkSceneDirty(target);
        EditorSceneManager.SaveScene(target, TargetPath);
        Debug.Log("Subji map replaced with stagescene map. Border removed and stage spawning enabled.");
    }

    private static void ApplyCharacterRendering(SpriteRenderer spriteRenderer, int sortingOrder)
    {
        if (spriteRenderer == null)
            return;

        Material spriteMaterial = AssetDatabase.LoadAssetAtPath<Material>(SpriteMaterialPath);
        if (spriteMaterial != null)
            spriteRenderer.sharedMaterial = spriteMaterial;
        spriteRenderer.sortingLayerName = "Player";
        spriteRenderer.sortingOrder = sortingOrder;
        EditorUtility.SetDirty(spriteRenderer);
    }
}
