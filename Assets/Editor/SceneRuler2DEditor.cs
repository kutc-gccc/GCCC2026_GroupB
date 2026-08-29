using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[CustomEditor(typeof(SceneRuler2D))]
public sealed class SceneRuler2DEditor : Editor
{
    private void OnSceneGUI()
    {
        SceneRuler2D ruler = (SceneRuler2D)target;
        Transform t = ruler.transform;
        Vector3 a = t.TransformPoint(ruler.pointA);
        Vector3 b = t.TransformPoint(ruler.pointB);

        EditorGUI.BeginChangeCheck();
        a = Handles.PositionHandle(a, Quaternion.identity);
        b = Handles.PositionHandle(b, Quaternion.identity);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(ruler, "Move ruler endpoint");
            ruler.pointA = t.InverseTransformPoint(a);
            ruler.pointB = t.InverseTransformPoint(b);
            EditorUtility.SetDirty(ruler);
        }

        Handles.color = Color.cyan;
        Handles.DrawDottedLine(a, b, 4f);

        Event current = Event.current;
        if (current.type == EventType.KeyDown &&
            (current.keyCode == KeyCode.Return || current.keyCode == KeyCode.KeypadEnter))
        {
            CreateWall(ruler, a, b);
            current.Use();
        }
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        SceneRuler2D ruler = (SceneRuler2D)target;
        Vector3 a = ruler.transform.TransformPoint(ruler.pointA);
        Vector3 b = ruler.transform.TransformPoint(ruler.pointB);
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("計測結果", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("横の長さ", Mathf.Abs(b.x - a.x).ToString("0.##"));
        EditorGUILayout.LabelField("縦の長さ", Mathf.Abs(b.y - a.y).ToString("0.##"));
        Vector3 center = (a + b) * 0.5f;
        EditorGUILayout.LabelField("中央 X", center.x.ToString("0.##"));
        EditorGUILayout.LabelField("中央 Y", center.y.ToString("0.##"));
        EditorGUILayout.HelpBox("SceneビューでEnter：この範囲に透明壁を生成", MessageType.Info);
    }

    private static void CreateWall(SceneRuler2D ruler, Vector3 a, Vector3 b)
    {
        const string prefabPath = "Assets/Prefabs/InvisibleWall2D.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogError($"透明壁Prefabが見つかりません: {prefabPath}");
            return;
        }

        GameObject wall = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        Undo.RegisterCreatedObjectUndo(wall, "Create Invisible Wall");
        wall.transform.position = (a + b) * 0.5f;
        GameObject container = GameObject.Find("Invisible Walls");
        if (container == null)
        {
            container = new GameObject("Invisible Walls");
            Undo.RegisterCreatedObjectUndo(container, "Create Invisible Walls container");
        }

        foreach (InvisibleWall2D existingWall in Object.FindObjectsByType<InvisibleWall2D>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (existingWall.transform.parent != container.transform)
                Undo.SetTransformParent(existingWall.transform, container.transform, "Group Invisible Wall");
        }

        Undo.SetTransformParent(wall.transform, container.transform, "Group Invisible Wall");
        BoxCollider2D box = wall.GetComponent<BoxCollider2D>();
        box.size = new Vector2(
            Mathf.Max(Mathf.Abs(b.x - a.x), 0.1f),
            Mathf.Max(Mathf.Abs(b.y - a.y), 0.1f));
        Selection.activeGameObject = wall;
        EditorSceneManager.MarkSceneDirty(wall.scene);
        EditorSceneManager.SaveScene(wall.scene);
    }

    [MenuItem("GameObject/2D Object/Scene Ruler", false, 20)]
    private static void CreateRuler()
    {
        GameObject go = new("Scene Ruler");
        go.AddComponent<SceneRuler2D>();
        Undo.RegisterCreatedObjectUndo(go, "Create Scene Ruler");
        Selection.activeGameObject = go;
    }
}
