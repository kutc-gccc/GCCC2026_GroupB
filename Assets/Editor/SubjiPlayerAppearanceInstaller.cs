using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Aseprite;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class SubjiPlayerAppearanceInstaller
{
    private const string SpritePath = "Assets/Sprites/Player/Sprite-0001.aseprite";
    private const string ScenePath = "Assets/Scenes/subji/jikkennsupace.unity";
    private const string MaterialPath = "Assets/Materials/SubjiStageSprite.mat";

    static SubjiPlayerAppearanceInstaller()
    {
        EditorSceneManager.sceneOpened += (_, _) => EditorApplication.delayCall += TryApply;
        EditorApplication.delayCall += TryApply;
    }

    private static void TryApply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        if (!scene.IsValid() || !scene.isLoaded)
            return;

        AsepriteImporter importer = AssetImporter.GetAtPath(SpritePath) as AsepriteImporter;
        if (importer != null && !Mathf.Approximately(importer.spritePixelsPerUnit, 32f))
        {
            // 32x32画像を1 Unity unitとして扱う。PlayerのScale 0.8は維持する。
            importer.spritePixelsPerUnit = 32f;
            importer.SaveAndReimport();
            EditorApplication.delayCall += TryApply;
            return;
        }

        Sprite sprite = AssetDatabase.LoadAllAssetsAtPath(SpritePath)
            .OfType<Sprite>()
            .OrderBy(asset => asset.name)
            .FirstOrDefault();
        if (sprite == null)
            return;

        GameObject player = scene.GetRootGameObjects()
            .FirstOrDefault(root => root.name == "Player");
        SpriteRenderer renderer = player != null ? player.GetComponent<SpriteRenderer>() : null;
        if (renderer == null || renderer.sprite == sprite)
            return;

        // TransformのScaleは変更せず、見た目だけを差し替える。
        renderer.sprite = sprite;
        renderer.sortingLayerName = "Player";
        renderer.sortingOrder = 100;
        Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material != null)
            renderer.sharedMaterial = material;

        EditorUtility.SetDirty(renderer);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Sprite-0001.asepriteをsubjiのPlayerへ適用しました。Transform Scaleは変更していません。");
    }
}
