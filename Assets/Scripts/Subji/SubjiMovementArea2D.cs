using UnityEngine;

/// <summary>プレイヤーと敵が移動できる長方形の範囲。</summary>
public sealed class SubjiMovementArea2D : MonoBehaviour
{
    public Vector2 pointA = new(-50f, -45f);
    public Vector2 pointB = new(50f, 45f);
    [SerializeField, HideInInspector] private bool isConfirmed;

    public bool IsConfirmed => isConfirmed;

    public Bounds GetWorldBounds()
    {
        Vector3 a = transform.TransformPoint(pointA);
        Vector3 b = transform.TransformPoint(pointB);
        Bounds bounds = new((a + b) * 0.5f, Vector3.zero);
        bounds.Encapsulate(a);
        bounds.Encapsulate(b);
        return bounds;
    }

    public void Confirm() => isConfirmed = true;
    public void MarkUnconfirmed() => isConfirmed = false;

    private void OnDrawGizmos()
    {
        Bounds bounds = GetWorldBounds();
        Gizmos.color = isConfirmed ? new Color(0.2f, 1f, 0.35f, 0.85f) : new Color(1f, 0.75f, 0.1f, 0.85f);
        Gizmos.DrawWireCube(bounds.center, bounds.size);
    }
}
