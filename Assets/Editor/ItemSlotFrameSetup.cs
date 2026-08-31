using System.Linq;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class ItemSlotFrameSetup
{
    private const string ScenePath = "Assets/Scenes/subji/jikkennsupace.unity";
    private const string ArtPath = "Assets/Art/UI/アイテムスロット枠.aseprite";
    private const string CommentArtPath = "Assets/Art/UI/コメント.aseprite";
    private const string LightArtPath = "Assets/Art/UI/ライト.aseprite";
    private const string DrinkArtPath = "Assets/Art/UI/缶.aseprite";
    private const string PrefabPath = "Assets/Prefabs/UI/ItemSlotFrame.prefab";

    [InitializeOnLoadMethod]
    private static void SetupOnceAfterCompile()
    {
        if (!File.ReadAllText(ScenePath).Contains("m_Name: ItemSlotCanvas"))
            EditorApplication.delayCall += Setup;

        EditorApplication.delayCall += AddCommentItemIconIfNeeded;
        EditorApplication.delayCall += AddLightItemIconIfNeeded;
        EditorApplication.delayCall += ApplyItemIconScale;
        EditorApplication.delayCall += SetupDrinkSystemIfNeeded;
        EditorApplication.delayCall += RemoveLegacyVendingPoint;
    }

    private static void AddCommentItemIconIfNeeded()
    {
        var canvasObject = GameObject.Find("ItemSlotCanvas");
        if (canvasObject == null || canvasObject.GetComponentInChildren<Transform>(true) == null)
            return;

        if (canvasObject.GetComponentsInChildren<Transform>(true)
            .Any(child => child.name == "CommentIcon"))
            return;

        AddCommentItemIcon();
    }

    private static void AddLightItemIconIfNeeded()
    {
        var canvasObject = GameObject.Find("ItemSlotCanvas");
        if (canvasObject == null)
            return;

        if (canvasObject.GetComponentsInChildren<Transform>(true)
            .Any(child => child.name == "LightIcon"))
            return;

        AddLightItemIcon();
    }

    private static void ApplyItemIconScale()
    {
        bool changed = false;
        foreach (var image in Object.FindObjectsByType<Image>(FindObjectsSortMode.None))
        {
            if (image.name != "CommentIcon" && image.name != "LightIcon")
                continue;

            if (image.rectTransform.localScale != Vector3.one * 0.9f)
            {
                image.rectTransform.localScale = Vector3.one * 0.9f;
                changed = true;
            }
        }

        if (changed)
        {
            var canvasObject = GameObject.Find("ItemSlotCanvas");
            if (canvasObject != null)
            {
                EditorSceneManager.MarkSceneDirty(canvasObject.scene);
                EditorSceneManager.SaveScene(canvasObject.scene);
            }
        }
    }

    private static void SetupDrinkSystemIfNeeded()
    {
        var canvasObject = GameObject.Find("ItemSlotCanvas");
        if (canvasObject == null || canvasObject.GetComponent<DrinkItemController>() != null)
            return;

        SetupDrinkSystem();
    }

    private static void RemoveLegacyVendingPoint()
    {
        var oldPoint = GameObject.Find("Drink Vending Machine Point");
        if (oldPoint == null)
            return;

        var scene = oldPoint.scene;
        Object.DestroyImmediate(oldPoint);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    [MenuItem("Tools/UI/Setup Item Slot Frame")]
    public static void Setup()
    {
        AssetDatabase.ImportAsset(ArtPath, ImportAssetOptions.ForceSynchronousImport);
        var sprite = AssetDatabase.LoadAllAssetsAtPath(ArtPath).OfType<Sprite>().FirstOrDefault();
        if (sprite == null)
            throw new System.InvalidOperationException($"Spriteを読み込めませんでした: {ArtPath}");

        var prefabSource = new GameObject("ItemSlotFrame", typeof(RectTransform), typeof(Image));
        var image = prefabSource.GetComponent<Image>();
        image.sprite = sprite;
        image.color = new Color(1f, 1f, 1f, 0.8f);
        image.preserveAspect = true;
        image.raycastTarget = false;

        var rect = prefabSource.GetComponent<RectTransform>();
        rect.sizeDelta = sprite.rect.size;
        PrefabUtility.SaveAsPrefabAsset(prefabSource, PrefabPath);
        Object.DestroyImmediate(prefabSource);

        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var oldCanvas = GameObject.Find("ItemSlotCanvas");
        if (oldCanvas != null)
            Object.DestroyImmediate(oldCanvas);

        var canvasObject = new GameObject(
            "ItemSlotCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));

        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        instance.transform.SetParent(canvasObject.transform, false);

        var instanceRect = instance.GetComponent<RectTransform>();
        instanceRect.anchorMin = new Vector2(0.5f, 0f);
        instanceRect.anchorMax = new Vector2(0.5f, 0f);
        instanceRect.pivot = new Vector2(0.5f, 0f);
        instanceRect.anchoredPosition = new Vector2(0f, 24f);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("jikkennsupaceの画面下部にItemSlotFrameを1個配置しました。");
    }

    [MenuItem("Tools/UI/Add Item Slot Selector")]
    public static void AddSelector()
    {
        var canvasObject = GameObject.Find("ItemSlotCanvas");
        if (canvasObject == null)
            throw new System.InvalidOperationException("ItemSlotCanvasが見つかりません。");

        if (canvasObject.GetComponent<ItemSlotSelector>() == null)
            Undo.AddComponent<ItemSlotSelector>(canvasObject);

        EditorSceneManager.MarkSceneDirty(canvasObject.scene);
        EditorSceneManager.SaveScene(canvasObject.scene);
        Debug.Log("ItemSlotCanvasにItemSlotSelectorを追加しました。");
    }

    [MenuItem("Tools/UI/Add Comment Item Icon")]
    public static void AddCommentItemIcon()
    {
        AssetDatabase.ImportAsset(CommentArtPath, ImportAssetOptions.ForceSynchronousImport);
        var sprite = AssetDatabase.LoadAllAssetsAtPath(CommentArtPath).OfType<Sprite>().FirstOrDefault();
        if (sprite == null)
            throw new System.InvalidOperationException($"Spriteを読み込めませんでした: {CommentArtPath}");

        var canvasObject = GameObject.Find("ItemSlotCanvas");
        if (canvasObject == null)
            throw new System.InvalidOperationException("ItemSlotCanvasが見つかりません。");

        var slots = canvasObject.GetComponentsInChildren<RectTransform>(true)
            .Where(rect => rect.parent == canvasObject.transform)
            .OrderBy(rect => rect.anchoredPosition.x)
            .ToArray();
        if (slots.Length == 0)
            throw new System.InvalidOperationException("アイテムスロットが見つかりません。");

        var oldIcon = slots[0].Find("CommentIcon");
        if (oldIcon != null)
            Object.DestroyImmediate(oldIcon.gameObject);

        var iconObject = new GameObject("CommentIcon", typeof(RectTransform), typeof(Image));
        iconObject.transform.SetParent(slots[0], false);
        var iconRect = iconObject.GetComponent<RectTransform>();
        iconRect.anchorMin = Vector2.zero;
        iconRect.anchorMax = Vector2.one;
        iconRect.offsetMin = new Vector2(12f, 12f);
        iconRect.offsetMax = new Vector2(-12f, -12f);
        iconRect.localScale = Vector3.one * 0.9f;

        var iconImage = iconObject.GetComponent<Image>();
        iconImage.sprite = sprite;
        iconImage.preserveAspect = true;
        iconImage.raycastTarget = false;

        EditorSceneManager.MarkSceneDirty(canvasObject.scene);
        EditorSceneManager.SaveScene(canvasObject.scene);
        Debug.Log("一番左のアイテムスロットにコメント画像を追加しました。");
    }

    [MenuItem("Tools/UI/Add Light Item Icon")]
    public static void AddLightItemIcon()
    {
        AssetDatabase.ImportAsset(LightArtPath, ImportAssetOptions.ForceSynchronousImport);
        var sprite = AssetDatabase.LoadAllAssetsAtPath(LightArtPath).OfType<Sprite>().FirstOrDefault();
        if (sprite == null)
            throw new System.InvalidOperationException($"Spriteを読み込めませんでした: {LightArtPath}");

        var canvasObject = GameObject.Find("ItemSlotCanvas");
        if (canvasObject == null)
            throw new System.InvalidOperationException("ItemSlotCanvasが見つかりません。");

        var slots = canvasObject.GetComponentsInChildren<RectTransform>(true)
            .Where(rect => rect.parent == canvasObject.transform)
            .OrderBy(rect => rect.anchoredPosition.x)
            .ToArray();
        if (slots.Length < 2)
            throw new System.InvalidOperationException("2つ目のアイテムスロットが見つかりません。");

        var oldIcon = slots[1].Find("LightIcon");
        if (oldIcon != null)
            Object.DestroyImmediate(oldIcon.gameObject);

        var iconObject = new GameObject("LightIcon", typeof(RectTransform), typeof(Image));
        iconObject.transform.SetParent(slots[1], false);
        var iconRect = iconObject.GetComponent<RectTransform>();
        iconRect.anchorMin = Vector2.zero;
        iconRect.anchorMax = Vector2.one;
        iconRect.offsetMin = new Vector2(12f, 12f);
        iconRect.offsetMax = new Vector2(-12f, -12f);
        iconRect.localScale = Vector3.one * 0.9f;

        var iconImage = iconObject.GetComponent<Image>();
        iconImage.sprite = sprite;
        iconImage.preserveAspect = true;
        iconImage.raycastTarget = false;

        EditorSceneManager.MarkSceneDirty(canvasObject.scene);
        EditorSceneManager.SaveScene(canvasObject.scene);
        Debug.Log("左から2つ目のアイテムスロットにライト画像を追加しました。");
    }

    [MenuItem("Tools/UI/Setup Drink Item System")]
    public static void SetupDrinkSystem()
    {
        AssetDatabase.ImportAsset(DrinkArtPath, ImportAssetOptions.ForceSynchronousImport);
        var sprite = AssetDatabase.LoadAllAssetsAtPath(DrinkArtPath).OfType<Sprite>().FirstOrDefault();
        if (sprite == null)
            throw new System.InvalidOperationException($"Spriteを読み込めませんでした: {DrinkArtPath}");

        var canvasObject = GameObject.Find("ItemSlotCanvas");
        if (canvasObject == null)
            throw new System.InvalidOperationException("ItemSlotCanvasが見つかりません。");

        var slots = canvasObject.GetComponentsInChildren<RectTransform>(true)
            .Where(rect => rect.parent == canvasObject.transform)
            .OrderBy(rect => rect.anchoredPosition.x)
            .ToArray();
        if (slots.Length < 4)
            throw new System.InvalidOperationException("3番・4番のアイテムスロットが見つかりません。");

        for (int i = 2; i <= 3; i++)
        {
            var oldIcon = slots[i].Find("DrinkIcon");
            if (oldIcon != null)
                Object.DestroyImmediate(oldIcon.gameObject);

            var iconObject = new GameObject("DrinkIcon", typeof(RectTransform), typeof(Image));
            iconObject.transform.SetParent(slots[i], false);
            var iconRect = iconObject.GetComponent<RectTransform>();
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.offsetMin = new Vector2(12f, 12f);
            iconRect.offsetMax = new Vector2(-12f, -12f);
            iconRect.localScale = Vector3.one * 0.9f;

            var iconImage = iconObject.GetComponent<Image>();
            iconImage.sprite = sprite;
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
            iconObject.SetActive(false);
        }

        var controller = canvasObject.GetComponent<DrinkItemController>();
        if (controller == null)
            controller = canvasObject.AddComponent<DrinkItemController>();

        var serializedController = new SerializedObject(controller);
        serializedController.FindProperty("drinkSprite").objectReferenceValue = sprite;
        serializedController.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(canvasObject.scene);
        EditorSceneManager.SaveScene(canvasObject.scene);
        Debug.Log("ドリンク購入・スロット格納・スタミナ保護システムを追加しました。");
    }
}
