using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.Collections.Generic;

[CustomEditor(typeof(SceneRuler2D))]
public sealed class SceneRuler2DEditor : Editor
{
    private const float JoinDistance = 0.35f;

    private void OnSceneGUI()
    {
        SceneRuler2D ruler = (SceneRuler2D)target;
        Transform t = ruler.transform;
        Vector3 a = t.TransformPoint(ruler.pointA);
        Vector3 b = t.TransformPoint(ruler.pointB);
        bool endpointsOverlap = Vector2.Distance(a, b) <= JoinDistance;

        EditorGUI.BeginChangeCheck();
        Handles.color = endpointsOverlap ? Color.magenta : Color.white;
        a = Handles.PositionHandle(a, Quaternion.identity);
        Handles.color = endpointsOverlap ? Color.magenta : Color.white;
        b = Handles.PositionHandle(b, Quaternion.identity);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(ruler, "Move ruler endpoint");
            ruler.pointA = t.InverseTransformPoint(a);
            ruler.pointB = t.InverseTransformPoint(b);
            if (Vector2.Distance(a, b) > JoinDistance)
                ruler.isLoopConnected = false;
            EditorUtility.SetDirty(ruler);
        }

        for (int i = 0; i < ruler.additionalPoints.Count; i++)
        {
            Vector3 point = t.TransformPoint(ruler.additionalPoints[i]);
            EditorGUI.BeginChangeCheck();
            point = Handles.PositionHandle(point, Quaternion.identity);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(ruler, "Move ruler point");
                ruler.additionalPoints[i] = t.InverseTransformPoint(point);
                EditorUtility.SetDirty(ruler);
            }
        }

        Handles.color = Color.cyan;
        List<Vector3> worldPoints = GetWorldPoints(ruler);
        for (int i = 0; i < worldPoints.Count - 1; i++)
            Handles.DrawDottedLine(worldPoints[i], worldPoints[i + 1], 4f);

        Event current = Event.current;
        if (current.type == EventType.KeyDown && current.keyCode == KeyCode.R)
        {
            Undo.RecordObject(ruler, "Reset ruler points");
            ruler.additionalPoints.Clear();
            ruler.isLoopConnected = false;
            EditorUtility.SetDirty(ruler);
            EditorSceneManager.MarkSceneDirty(ruler.gameObject.scene);
            SceneView.RepaintAll();
            current.Use();
        }
        else if (current.type == EventType.KeyDown && current.keyCode == KeyCode.O && current.shift)
        {
            a = t.TransformPoint(ruler.pointA);
            b = t.TransformPoint(ruler.pointB);
            if (Vector2.Distance(a, b) <= JoinDistance)
            {
                Undo.RecordObject(ruler, "Connect ruler endpoints");
                ruler.pointB = ruler.pointA;
                ruler.isLoopConnected = true;
                EditorUtility.SetDirty(ruler);
                EditorSceneManager.MarkSceneDirty(ruler.gameObject.scene);
                SceneView.RepaintAll();
            }
            current.Use();
        }
        else if (current.type == EventType.KeyDown && current.keyCode == KeyCode.O)
        {
            Undo.RecordObject(ruler, "Add ruler point on left");
            ruler.additionalPoints.Insert(0, ruler.pointA);
            ruler.pointA += Vector2.left;
            ruler.isLoopConnected = false;
            EditorUtility.SetDirty(ruler);
            EditorSceneManager.MarkSceneDirty(ruler.gameObject.scene);
            SceneView.RepaintAll();
            current.Use();
        }

        if (current.type == EventType.KeyDown &&
            (current.keyCode == KeyCode.Return || current.keyCode == KeyCode.KeypadEnter))
        {
            worldPoints = GetWorldPoints(ruler);
            for (int i = 0; i < worldPoints.Count - 1; i++)
                CreateWall(ruler, worldPoints[i], worldPoints[i + 1]);
            if (ruler.isLoopConnected && worldPoints.Count > 2 &&
                Vector2.Distance(worldPoints[0], worldPoints[^1]) > 0.01f)
                CreateWall(ruler, worldPoints[^1], worldPoints[0]);
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
        EditorGUILayout.LabelField("追加頂点", ruler.additionalPoints.Count.ToString());
        EditorGUILayout.LabelField("端点の連結", ruler.isLoopConnected ? "連結済み" : "未連結");
        EditorGUILayout.HelpBox("Scene Ruler選択中のみ有効\nOキー：左端側に頂点を追加\n左右の端点が重なると紫色\n重なった状態でShift+O：端点を連結\nRキー：追加頂点と連結をリセット\nEnter：透明壁を生成", MessageType.Info);
    }

    private static List<Vector3> GetWorldPoints(SceneRuler2D ruler)
    {
        List<Vector3> points = new() { ruler.transform.TransformPoint(ruler.pointA) };
        foreach (Vector2 point in ruler.additionalPoints)
            points.Add(ruler.transform.TransformPoint(point));
        points.Add(ruler.transform.TransformPoint(ruler.pointB));
        return points;
    }

    private static void CreateWall(SceneRuler2D ruler, Vector3 a, Vector3 b)
    {
        const string prefabPath = "Assets/Prefabs/MapObjects/InvisibleWall2D.prefab";
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
