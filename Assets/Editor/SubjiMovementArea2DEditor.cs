using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[CustomEditor(typeof(SubjiMovementArea2D))]
public sealed class SubjiMovementArea2DEditor : Editor
{
    private void OnSceneGUI()
    {
        SubjiMovementArea2D area = (SubjiMovementArea2D)target;
        Transform t = area.transform;
        Vector3 a = t.TransformPoint(area.pointA);
        Vector3 b = t.TransformPoint(area.pointB);
        EditorGUI.BeginChangeCheck();
        a = Handles.PositionHandle(a, Quaternion.identity);
        b = Handles.PositionHandle(b, Quaternion.identity);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(area, "Move movement area endpoint");
            area.pointA = t.InverseTransformPoint(a);
            area.pointB = t.InverseTransformPoint(b);
            area.MarkUnconfirmed();
            EditorUtility.SetDirty(area);
        }
        Bounds bounds = area.GetWorldBounds();
        Handles.color = area.IsConfirmed ? Color.green : new Color(1f, 0.65f, 0f);
        Handles.DrawWireCube(bounds.center, bounds.size);
        Event current = Event.current;
        if (current.type == EventType.KeyDown && (current.keyCode == KeyCode.Return || current.keyCode == KeyCode.KeypadEnter))
        {
            Undo.RecordObject(area, "Confirm movement area");
            area.Confirm();
            EditorUtility.SetDirty(area);
            EditorSceneManager.MarkSceneDirty(area.gameObject.scene);
            SceneView.RepaintAll();
            current.Use();
        }
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        SubjiMovementArea2D area = (SubjiMovementArea2D)target;
        Bounds bounds = area.GetWorldBounds();
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("行動範囲", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("状態", area.IsConfirmed ? "確定済み" : "未確定");
        EditorGUILayout.LabelField("横", bounds.size.x.ToString("0.##"));
        EditorGUILayout.LabelField("縦", bounds.size.y.ToString("0.##"));
        EditorGUILayout.HelpBox("Sceneビューで2つの端点を動かし、Enterで確定します。", MessageType.Info);
    }

    [MenuItem("GameObject/2D Object/Subji Movement Area", false, 21)]
    private static void CreateMovementArea()
    {
        GameObject go = new("Subji Movement Area");
        go.AddComponent<SubjiMovementArea2D>();
        Undo.RegisterCreatedObjectUndo(go, "Create Subji Movement Area");
        Selection.activeGameObject = go;
    }
}
