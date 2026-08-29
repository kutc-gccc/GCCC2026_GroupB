using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public sealed class InvisibleWall2D : MonoBehaviour
{
    [SerializeField] private Color sceneColor = new(1f, 0.25f, 0.1f, 0.28f);

    private void OnDrawGizmos()
    {
        BoxCollider2D box = GetComponent<BoxCollider2D>();
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
