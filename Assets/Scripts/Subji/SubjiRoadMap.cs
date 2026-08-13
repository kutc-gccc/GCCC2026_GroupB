using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generates a simple road network and draws a matching minimap.
/// The component is created automatically by SubjiPlayerMovement.
/// </summary>
public class SubjiRoadMap : MonoBehaviour
{
    public float fieldSize = 60f;
    public float roadWidth = 6f;
    public float[] horizontalRoads = { -20f, 0f, 20f };
    public float[] verticalRoads = { -20f, 0f, 20f };

    private readonly List<LineRenderer> roadRenderers = new List<LineRenderer>();
    private Material roadMaterial;
    private Transform player;
    private Vector2 center;
    private GUIStyle minimapLabelStyle;

    public void Configure(Transform target, Vector2 mapCenter, float size)
    {
        player = target;
        center = mapCenter;
        fieldSize = size;
        BuildRoads();
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
        ClearRoads();

        if (roadMaterial == null)
            roadMaterial = new Material(Shader.Find("Sprites/Default"));

        float halfField = fieldSize * 0.5f;
        foreach (float y in horizontalRoads)
            CreateRoad(new Vector2(center.x - halfField, center.y + y),
                new Vector2(center.x + halfField, center.y + y));

        foreach (float x in verticalRoads)
            CreateRoad(new Vector2(center.x + x, center.y - halfField),
                new Vector2(center.x + x, center.y + halfField));
    }

    private void CreateRoad(Vector2 from, Vector2 to)
    {
        GameObject road = new GameObject("Road");
        road.transform.SetParent(transform, false);
        LineRenderer line = road.AddComponent<LineRenderer>();
        line.sharedMaterial = roadMaterial;
        line.useWorldSpace = true;
        line.positionCount = 2;
        line.startWidth = roadWidth;
        line.endWidth = roadWidth;
        line.startColor = new Color(0.18f, 0.2f, 0.23f, 1f);
        line.endColor = line.startColor;
        line.sortingOrder = -5;
        line.SetPosition(0, new Vector3(from.x, from.y, 0f));
        line.SetPosition(1, new Vector3(to.x, to.y, 0f));
        roadRenderers.Add(line);
    }

    private void ClearRoads()
    {
        // Include children left by an editor refresh, not only this instance's list.
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            GameObject child = transform.GetChild(i).gameObject;
            if (Application.isPlaying)
                Destroy(child);
            else
                DestroyImmediate(child);
        }

        roadRenderers.Clear();
    }

    private void OnGUI()
    {
        if (!Application.isPlaying || player == null)
            return;

        const float mapSize = 180f;
        const float margin = 16f;
        Rect map = new Rect(Screen.width - mapSize - margin, margin, mapSize, mapSize);

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
        GUI.color = new Color(0.15f, 0.9f, 1f, 1f);
        GUI.DrawTexture(new Rect(markerX - 5f, markerY - 5f, 10f, 10f), Texture2D.whiteTexture);
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
}
