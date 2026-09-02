using UnityEngine;

/// <summary>
/// 敵の正面に仮の視野を表示し、範囲内の点を判定します。
/// このコンポーネントを無効化・削除すれば、視野の表示と判定をまとめて外せます。
/// </summary>
public class SubjiEnemyVisionCone : MonoBehaviour
{
    [Header("仮の視野")]
    [Tooltip("扇形の端から端までの角度")]
    [Range(1f, 180f)] public float viewAngle = 30f;
    [Tooltip("視野の長さ。移動中の索敵半径5より少し長い値です")]
    [Min(0.1f)] public float viewDistance = 5.5f;
    [Tooltip("扇形の滑らかさ")]
    [Range(2, 64)] public int segments = 24;
    [Tooltip("視野の表示色")]
    public Color viewColor = new Color(1f, 0.75f, 0.15f, 0.22f);

    public Vector2 FacingDirection { get; private set; } = Vector2.right;

    private Mesh viewMesh;
    private MeshRenderer viewRenderer;
    private Transform visualTransform;
    private Vector3 previousPosition;
    private Vector2 lastMeshDirection;
    private Vector3[] meshVertices;
    private int[] meshTriangles;
    private float nextMeshUpdateTime;

    private void Awake()
    {
        previousPosition = transform.position;
        CreateVisual();
    }

    private void LateUpdate()
    {
        Vector2 movement = transform.position - previousPosition;
        if (movement.sqrMagnitude > 0.000001f)
            FacingDirection = movement.normalized;

        previousPosition = transform.position;
        if ((FacingDirection - lastMeshDirection).sqrMagnitude > 0.0001f &&
            Time.time >= nextMeshUpdateTime)
        {
            UpdateMesh();
            nextMeshUpdateTime = Time.time + 0.1f;
        }
    }

    private void OnEnable()
    {
        if (viewRenderer != null)
            viewRenderer.enabled = true;
    }

    private void OnDisable()
    {
        if (viewRenderer != null)
            viewRenderer.enabled = false;
    }

    private void OnDestroy()
    {
        if (viewMesh != null)
            Destroy(viewMesh);
        if (viewRenderer != null && viewRenderer.material != null)
            Destroy(viewRenderer.material);
    }

    public bool Contains(Vector2 worldPoint)
    {
        Vector2 toPoint = worldPoint - (Vector2)transform.position;
        if (toPoint.sqrMagnitude > viewDistance * viewDistance)
            return false;
        if (toPoint.sqrMagnitude <= Mathf.Epsilon)
            return true;

        return Vector2.Angle(FacingDirection, toPoint) <= viewAngle * 0.5f;
    }

    public bool ContainsDirection(Vector2 worldPoint)
    {
        Vector2 toPoint = worldPoint - (Vector2)transform.position;
        float forwardDot = Vector2.Dot(FacingDirection, toPoint);
        if (forwardDot <= 0f)
            return false;

        float cosine = Mathf.Cos(viewAngle * 0.5f * Mathf.Deg2Rad);
        return forwardDot * forwardDot >= toPoint.sqrMagnitude * cosine * cosine;
    }

    public void SetFacingDirection(Vector2 direction)
    {
        if (direction.sqrMagnitude <= Mathf.Epsilon)
            return;

        FacingDirection = direction.normalized;
        UpdateMesh();
    }

    private void CreateVisual()
    {
        GameObject visual = new GameObject("Temporary Vision Cone");
        visual.transform.SetParent(transform, false);
        visualTransform = visual.transform;
        KeepVisualAtWorldScale();

        MeshFilter filter = visual.AddComponent<MeshFilter>();
        viewRenderer = visual.AddComponent<MeshRenderer>();
        viewRenderer.sortingLayerID = GetComponent<SpriteRenderer>()?.sortingLayerID ?? 0;
        viewRenderer.sortingOrder = 4;
        viewRenderer.material = new Material(Shader.Find("Sprites/Default"));
        viewRenderer.material.color = viewColor;

        viewMesh = new Mesh { name = "Temporary Enemy Vision Cone" };
        filter.sharedMesh = viewMesh;
        UpdateMesh();
    }

    private void UpdateMesh()
    {
        if (viewMesh == null)
            return;

        int safeSegments = Mathf.Max(2, segments);
        if (meshVertices == null || meshVertices.Length != safeSegments + 2)
            meshVertices = new Vector3[safeSegments + 2];
        if (meshTriangles == null || meshTriangles.Length != safeSegments * 3)
            meshTriangles = new int[safeSegments * 3];
        meshVertices[0] = Vector3.zero;

        float facingAngle = Mathf.Atan2(FacingDirection.y, FacingDirection.x) * Mathf.Rad2Deg;
        for (int i = 0; i <= safeSegments; i++)
        {
            float angle = facingAngle - viewAngle * 0.5f + viewAngle * i / safeSegments;
            float radians = angle * Mathf.Deg2Rad;
            meshVertices[i + 1] = new Vector3(Mathf.Cos(radians), Mathf.Sin(radians), 0f) * viewDistance;

            if (i >= safeSegments)
                continue;
            int triangle = i * 3;
            meshTriangles[triangle] = 0;
            meshTriangles[triangle + 1] = i + 1;
            meshTriangles[triangle + 2] = i + 2;
        }

        viewMesh.Clear();
        viewMesh.vertices = meshVertices;
        viewMesh.triangles = meshTriangles;
        viewMesh.RecalculateBounds();
        lastMeshDirection = FacingDirection;

        if (viewRenderer != null)
            viewRenderer.material.color = viewColor;
    }

    private void KeepVisualAtWorldScale()
    {
        if (visualTransform == null)
            return;

        Vector3 scale = transform.lossyScale;
        visualTransform.localScale = new Vector3(
            Mathf.Approximately(scale.x, 0f) ? 1f : 1f / scale.x,
            Mathf.Approximately(scale.y, 0f) ? 1f : 1f / scale.y,
            Mathf.Approximately(scale.z, 0f) ? 1f : 1f / scale.z);
    }
}
