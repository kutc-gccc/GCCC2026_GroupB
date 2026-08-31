using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public sealed class InvisibleWall2D : MonoBehaviour
{
    private static readonly HashSet<InvisibleWall2D> activeWalls = new();
    [SerializeField] private Color sceneColor = new(1f, 0.25f, 0.1f, 0.28f);
    private BoxCollider2D cachedCollider;

    public static IReadOnlyCollection<InvisibleWall2D> ActiveWalls => activeWalls;
    public BoxCollider2D Collider => cachedCollider != null
        ? cachedCollider
        : cachedCollider = GetComponent<BoxCollider2D>();

    private void OnEnable() => activeWalls.Add(this);
    private void OnDisable() => activeWalls.Remove(this);

    private void OnDrawGizmos()
    {
        BoxCollider2D box = Collider;
        if (box == null)
            return;

        Matrix4x4 previousMatrix = Gizmos.matrix;
        Color previousColor = Gizmos.color;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = sceneColor;
        Gizmos.DrawCube(box.offset, box.size);
        Gizmos.color = new Color(sceneColor.r, sceneColor.g, sceneColor.b, 0.9f);
        Gizmos.DrawWireCube(box.offset, box.size);
        Gizmos.matrix = previousMatrix;
        Gizmos.color = previousColor;
    }
}
