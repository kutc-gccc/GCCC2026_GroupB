using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Generates a simple road network and draws a matching minimap.
/// The component is created automatically by SubjiPlayerMovement.
/// </summary>
[DefaultExecutionOrder(-1000)]
[ExecuteAlways]
public class SubjiRoadMap : MonoBehaviour
{
    [Header("道路の設定")]
    [Min(10f)] public float fieldSize = 60f;
    [Min(1f)] public float roadWidth = 6f;
    [Tooltip("マップ中心からの上下方向の位置です")]
    public float[] horizontalRoads = { -20f, 0f, 20f };
    [Tooltip("マップ中心からの左右方向の位置です")]
    public float[] verticalRoads = { -20f, 0f, 20f };
    [Tooltip("Sceneビューに道路を表示します")]
    public bool showRoadsInSceneView = true;
    public Color roadColor = new Color(0.18f, 0.2f, 0.23f, 1f);

    [Header("ミニマップの設定")]
    [Range(100f, 400f)] public float minimapSize = 180f;
    [Range(0f, 50f)] public float minimapMargin = 16f;
    public Color minimapPlayerColor = new Color(0.15f, 0.9f, 1f, 1f);
    public Color minimapEnemyColor = new Color(1f, 0.2f, 0.2f, 1f);

    [Header("デバッグ表示")]
    [Tooltip("敵の発見範囲をミニマップ上に表示します")]
    public bool showDetectionRangesOnMinimap = true;
    [Tooltip("実行中にキーで発見範囲表示を切り替えます")]
    public bool enableDebugToggleKey = true;
    [Tooltip("発見範囲表示を切り替えるキー")]
    public Key toggleDetectionRangesKey = Key.F3;
    public Color minimapDetectionRangeColor = new Color(1f, 0.35f, 0.2f, 0.8f);

    private Material roadMaterial;
    private Mesh roadMesh;
    private Transform player;
    private Vector2 center;
    private GUIStyle minimapLabelStyle;

    public Vector2 Center => center;

    public bool IsReady { get; private set; }

    private void Awake()
    {
        center = transform.position;
        BuildRoads();
        IsReady = true;
    }

    private void OnEnable()
    {
        if (!Application.isPlaying)
            RefreshEditorRoads();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying || !isActiveAndEnabled)
            return;
        UnityEditor.EditorApplication.delayCall += RefreshEditorRoads;
    }
#endif

    private void RefreshEditorRoads()
    {
        if (this == null || Application.isPlaying)
            return;

        center = transform.position;
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        if (!showRoadsInSceneView)
        {
            if (renderer != null)
                renderer.enabled = false;
            return;
        }

        BuildRoads();
        renderer = GetComponent<MeshRenderer>();
        if (renderer != null)
            renderer.enabled = true;
    }

    private void Update()
    {
        if (!Application.isPlaying)
            return;

        if (!enableDebugToggleKey || Keyboard.current == null)
            return;

        if (Keyboard.current[toggleDetectionRangesKey].wasPressedThisFrame)
            showDetectionRangesOnMinimap = !showDetectionRangesOnMinimap;
    }

    public void RegisterPlayer(Transform target)
    {
        player = target;
    }

    public void Configure(Transform target, Vector2 mapCenter, float size)
    {
        center = mapCenter;
        fieldSize = size;
        BuildRoads();
        player = target;
        IsReady = true;
    }

    public Vector2 ConstrainToRoad(Vector2 currentPosition, Vector2 desiredPosition, Vector2 playerExtents)
    {
        float halfField = fieldSize * 0.5f;
        desiredPosition.x = Mathf.Clamp(desiredPosition.x,
            center.x - halfField + playerExtents.x,
            center.x + halfField - playerExtents.x);
        desiredPosition.y = Mathf.Clamp(desiredPosition.y,
            center.y - halfField + playerExtents.y,
            center.y + halfField - playerExtents.y);

        if (IsOnRoad(desiredPosition, playerExtents))
            return desiredPosition;

        // Sliding along an edge feels better than stopping both axes at once.
        Vector2 horizontalOnly = new Vector2(desiredPosition.x, currentPosition.y);
        if (IsOnRoad(horizontalOnly, playerExtents))
            return horizontalOnly;

        Vector2 verticalOnly = new Vector2(currentPosition.x, desiredPosition.y);
        if (IsOnRoad(verticalOnly, playerExtents))
            return verticalOnly;

        return currentPosition;
    }

    public Vector2 GetClosestPointOnRoad(Vector2 position, Vector2 extents)
    {
        float halfField = fieldSize * 0.5f;
        float mapMinX = center.x - halfField + extents.x;
        float mapMaxX = center.x + halfField - extents.x;
        float mapMinY = center.y - halfField + extents.y;
        float mapMaxY = center.y + halfField - extents.y;
        position.x = Mathf.Clamp(position.x, mapMinX, mapMaxX);
        position.y = Mathf.Clamp(position.y, mapMinY, mapMaxY);

        // すでにその個体が道路内へ収まる位置なら、座標を変えない。
        if (IsOnRoad(position, extents))
            return position;

        Vector2 best = position;
        float bestDistance = float.PositiveInfinity;

        foreach (float yOffset in horizontalRoads)
        {
            float roadY = center.y + yOffset;
            float usableHalfHeight = Mathf.Max(0f, roadWidth * 0.5f - extents.y);
            Vector2 candidate = new Vector2(
                Mathf.Clamp(position.x, mapMinX, mapMaxX),
                Mathf.Clamp(position.y, roadY - usableHalfHeight, roadY + usableHalfHeight));
            float distance = (candidate - position).sqrMagnitude;
            if (distance < bestDistance)
            {
                best = candidate;
                bestDistance = distance;
            }
        }

        foreach (float xOffset in verticalRoads)
        {
            float roadX = center.x + xOffset;
            float usableHalfWidth = Mathf.Max(0f, roadWidth * 0.5f - extents.x);
            Vector2 candidate = new Vector2(
                Mathf.Clamp(position.x, roadX - usableHalfWidth, roadX + usableHalfWidth),
                Mathf.Clamp(position.y, mapMinY, mapMaxY));
            float distance = (candidate - position).sqrMagnitude;
            if (distance < bestDistance)
            {
                best = candidate;
                bestDistance = distance;
            }
        }

        return best;
    }

    public Vector2 GetRandomPointOnRoad()
    {
        float halfField = fieldSize * 0.5f;
        bool useHorizontal = horizontalRoads != null && horizontalRoads.Length > 0 &&
            (verticalRoads == null || verticalRoads.Length == 0 || Random.value < 0.5f);

        if (useHorizontal)
        {
            float y = center.y + horizontalRoads[Random.Range(0, horizontalRoads.Length)];
            return new Vector2(Random.Range(center.x - halfField, center.x + halfField), y);
        }

        if (verticalRoads != null && verticalRoads.Length > 0)
        {
            float x = center.x + verticalRoads[Random.Range(0, verticalRoads.Length)];
            return new Vector2(x, Random.Range(center.y - halfField, center.y + halfField));
        }

        return center;
    }

    /// <summary>Returns the next intersection to use instead of cutting across blocks.</summary>
    public Vector2 GetNextPathPoint(Vector2 from, Vector2 target, float arrivalDistance = 0.15f)
    {
        bool fromHorizontal = GetNearestRoad(from, out float fromRoad, out bool fromIsHorizontal);
        bool targetHorizontal = GetNearestRoad(target, out float targetRoad, out bool targetIsHorizontal);
        fromHorizontal = fromIsHorizontal;
        targetHorizontal = targetIsHorizontal;

        if (fromHorizontal == targetHorizontal && Mathf.Approximately(fromRoad, targetRoad))
            return target;

        if (fromHorizontal != targetHorizontal)
        {
            Vector2 intersection = fromHorizontal
                ? new Vector2(targetRoad, fromRoad)
                : new Vector2(fromRoad, targetRoad);
            return Vector2.Distance(from, intersection) <= arrivalDistance ? target : intersection;
        }

        if (fromHorizontal)
        {
            float connectorX = GetNearestValue(verticalRoads, from.x, center.x);
            Vector2 first = new Vector2(connectorX, fromRoad);
            Vector2 second = new Vector2(connectorX, targetRoad);
            if (Vector2.Distance(from, first) > arrivalDistance)
                return first;
            return Vector2.Distance(from, second) > arrivalDistance ? second : target;
        }

        float connectorY = GetNearestValue(horizontalRoads, from.y, center.y);
        Vector2 verticalFirst = new Vector2(fromRoad, connectorY);
        Vector2 verticalSecond = new Vector2(targetRoad, connectorY);
        if (Vector2.Distance(from, verticalFirst) > arrivalDistance)
            return verticalFirst;
        return Vector2.Distance(from, verticalSecond) > arrivalDistance ? verticalSecond : target;
    }

    public Vector2 GetShortestPathPoint(Vector2 from, Vector2 target, Vector2 extents)
    {
        if (SegmentStaysOnRoad(from, target, extents))
            return target;

        List<Vector2> nodes = new List<Vector2> { from, target };
        float cornerX = Mathf.Max(0.05f, roadWidth * 0.5f - extents.x);
        float cornerY = Mathf.Max(0.05f, roadWidth * 0.5f - extents.y);

        foreach (float xOffset in verticalRoads)
        {
            foreach (float yOffset in horizontalRoads)
            {
                float x = center.x + xOffset;
                float y = center.y + yOffset;
                nodes.Add(new Vector2(x - cornerX, y - cornerY));
                nodes.Add(new Vector2(x - cornerX, y + cornerY));
                nodes.Add(new Vector2(x + cornerX, y - cornerY));
                nodes.Add(new Vector2(x + cornerX, y + cornerY));
            }
        }

        int count = nodes.Count;
        float[] distances = new float[count];
        int[] previous = new int[count];
        bool[] visited = new bool[count];
        for (int i = 0; i < count; i++)
        {
            distances[i] = float.PositiveInfinity;
            previous[i] = -1;
        }
        distances[0] = 0f;

        for (int step = 0; step < count; step++)
        {
            int current = -1;
            float best = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                if (!visited[i] && distances[i] < best)
                {
                    current = i;
                    best = distances[i];
                }
            }

            if (current < 0 || current == 1)
                break;

            visited[current] = true;
            for (int next = 0; next < count; next++)
            {
                if (visited[next] || next == current ||
                    !SegmentStaysOnRoad(nodes[current], nodes[next], extents))
                    continue;

                float candidate = distances[current] + Vector2.Distance(nodes[current], nodes[next]);
                if (candidate < distances[next])
                {
                    distances[next] = candidate;
                    previous[next] = current;
                }
            }
        }

        if (previous[1] < 0)
            return from;

        int waypoint = 1;
        while (previous[waypoint] > 0)
            waypoint = previous[waypoint];
        return nodes[waypoint];
    }

    private bool SegmentStaysOnRoad(Vector2 from, Vector2 to, Vector2 extents)
    {
        float distance = Vector2.Distance(from, to);
        int samples = Mathf.Max(2, Mathf.CeilToInt(distance / 0.2f));
        for (int i = 0; i <= samples; i++)
        {
            Vector2 point = Vector2.Lerp(from, to, i / (float)samples);
            if (!IsOnRoad(point, extents))
                return false;
        }
        return true;
    }

    private bool GetNearestRoad(Vector2 position, out float roadCoordinate, out bool isHorizontal)
    {
        float horizontal = GetNearestValue(horizontalRoads, position.y, center.y);
        float vertical = GetNearestValue(verticalRoads, position.x, center.x);
        isHorizontal = Mathf.Abs(position.y - horizontal) <= Mathf.Abs(position.x - vertical);
        roadCoordinate = isHorizontal ? horizontal : vertical;
        return isHorizontal;
    }

    private static float GetNearestValue(float[] offsets, float value, float origin)
    {
        if (offsets == null || offsets.Length == 0)
            return origin;

        float nearest = origin + offsets[0];
        float distance = Mathf.Abs(value - nearest);
        for (int i = 1; i < offsets.Length; i++)
        {
            float candidate = origin + offsets[i];
            float candidateDistance = Mathf.Abs(value - candidate);
            if (candidateDistance < distance)
            {
                nearest = candidate;
                distance = candidateDistance;
            }
        }
        return nearest;
    }

    private bool IsOnRoad(Vector2 position, Vector2 extents)
    {
        float usableHalfWidthX = Mathf.Max(0.05f, roadWidth * 0.5f - extents.x);
        float usableHalfWidthY = Mathf.Max(0.05f, roadWidth * 0.5f - extents.y);

        foreach (float y in horizontalRoads)
        {
            if (Mathf.Abs(position.y - (center.y + y)) <= usableHalfWidthY)
                return true;
        }

        foreach (float x in verticalRoads)
        {
            if (Mathf.Abs(position.x - (center.x + x)) <= usableHalfWidthX)
                return true;
        }

        return false;
    }

    private void BuildRoads()
    {
        if (roadMaterial == null)
        {
            roadMaterial = new Material(Shader.Find("Sprites/Default"));
            if (!Application.isPlaying)
                roadMaterial.hideFlags = HideFlags.DontSaveInEditor;
        }

        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null)
            meshFilter = gameObject.AddComponent<MeshFilter>();

        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer == null)
            meshRenderer = gameObject.AddComponent<MeshRenderer>();

        meshRenderer.sharedMaterial = roadMaterial;
        meshRenderer.sortingOrder = -5;

        if (roadMesh != null)
        {
            if (Application.isPlaying)
                Destroy(roadMesh);
            else
                DestroyImmediate(roadMesh);
        }

        roadMesh = new Mesh { name = "Complete Road Map Mesh" };
        if (!Application.isPlaying)
            roadMesh.hideFlags = HideFlags.DontSaveInEditor;
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Color> colors = new List<Color>();
        float halfField = fieldSize * 0.5f;
        foreach (float y in horizontalRoads)
            AddQuad(vertices, triangles, colors,
                new Rect(-halfField, y - roadWidth * 0.5f, fieldSize, roadWidth));

        foreach (float x in verticalRoads)
            AddQuad(vertices, triangles, colors,
                new Rect(x - roadWidth * 0.5f, -halfField, roadWidth, fieldSize));

        roadMesh.SetVertices(vertices);
        roadMesh.SetTriangles(triangles, 0);
        roadMesh.SetColors(colors);
        roadMesh.RecalculateBounds();
        meshFilter.sharedMesh = roadMesh;
    }

    private void AddQuad(List<Vector3> vertices, List<int> triangles,
        List<Color> colors, Rect rect)
    {
        int first = vertices.Count;
        vertices.Add(new Vector3(rect.xMin, rect.yMin, 0f));
        vertices.Add(new Vector3(rect.xMin, rect.yMax, 0f));
        vertices.Add(new Vector3(rect.xMax, rect.yMax, 0f));
        vertices.Add(new Vector3(rect.xMax, rect.yMin, 0f));
        triangles.Add(first); triangles.Add(first + 1); triangles.Add(first + 2);
        triangles.Add(first); triangles.Add(first + 2); triangles.Add(first + 3);
        colors.Add(roadColor); colors.Add(roadColor); colors.Add(roadColor); colors.Add(roadColor);
    }

    private void OnGUI()
    {
        if (!Application.isPlaying || player == null)
            return;

        float mapSize = Mathf.Min(minimapSize, Screen.width * 0.35f, Screen.height * 0.35f);
        Rect map = new Rect(Screen.width - mapSize - minimapMargin, minimapMargin, mapSize, mapSize);

        GUI.color = new Color(0.04f, 0.06f, 0.08f, 0.88f);
        GUI.Box(map, GUIContent.none);

        GUI.BeginGroup(map);
        float scale = mapSize / fieldSize;
        GUI.color = new Color(0.55f, 0.57f, 0.6f, 1f);

        foreach (float y in horizontalRoads)
        {
            float screenY = mapSize - ((y + fieldSize * 0.5f) * scale);
            GUI.DrawTexture(new Rect(0f, screenY - roadWidth * scale * 0.5f,
                mapSize, roadWidth * scale), Texture2D.whiteTexture);
        }

        foreach (float x in verticalRoads)
        {
            float screenX = (x + fieldSize * 0.5f) * scale;
            GUI.DrawTexture(new Rect(screenX - roadWidth * scale * 0.5f, 0f,
                roadWidth * scale, mapSize), Texture2D.whiteTexture);
        }

        Vector2 local = (Vector2)player.position - center;
        float markerX = (local.x + fieldSize * 0.5f) * scale;
        float markerY = mapSize - ((local.y + fieldSize * 0.5f) * scale);
        GUI.color = minimapPlayerColor;
        GUI.DrawTexture(new Rect(markerX - 5f, markerY - 5f, 10f, 10f), Texture2D.whiteTexture);

        SubjiEnemyChase[] enemies = FindObjectsByType<SubjiEnemyChase>(FindObjectsSortMode.None);
        GUI.color = minimapEnemyColor;
        foreach (SubjiEnemyChase enemy in enemies)
        {
            Vector2 enemyLocal = (Vector2)enemy.transform.position - center;
            float enemyX = (enemyLocal.x + fieldSize * 0.5f) * scale;
            float enemyY = mapSize - ((enemyLocal.y + fieldSize * 0.5f) * scale);
            if (showDetectionRangesOnMinimap)
            {
                GUI.color = minimapDetectionRangeColor;
                DrawMinimapCircle(new Vector2(enemyX, enemyY),
                    enemy.CurrentDetectionRadius * scale);
            }

            GUI.color = minimapEnemyColor;
            GUI.DrawTexture(new Rect(enemyX - 4f, enemyY - 4f, 8f, 8f), Texture2D.whiteTexture);
        }
        GUI.EndGroup();

        GUI.color = Color.white;
        if (minimapLabelStyle == null)
        {
            minimapLabelStyle = new GUIStyle(GUI.skin.label);
            minimapLabelStyle.alignment = TextAnchor.MiddleCenter;
            minimapLabelStyle.fontStyle = FontStyle.Bold;
            minimapLabelStyle.normal.textColor = Color.white;
        }
        GUI.Label(new Rect(map.x, map.y + map.height + 2f, map.width, 22f), "MINI MAP", minimapLabelStyle);
    }

    private static void DrawMinimapCircle(Vector2 centerPoint, float radius)
    {
        const int segments = 48;
        const float pointSize = 2f;
        for (int i = 0; i < segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;
            float x = centerPoint.x + Mathf.Cos(angle) * radius;
            float y = centerPoint.y + Mathf.Sin(angle) * radius;
            GUI.DrawTexture(new Rect(x - pointSize * 0.5f, y - pointSize * 0.5f,
                pointSize, pointSize), Texture2D.whiteTexture);
        }
    }
}
