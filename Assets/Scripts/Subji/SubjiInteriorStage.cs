using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>屋内シーンで編集する入場地点と移動範囲。</summary>
public sealed class SubjiInteriorStage : MonoBehaviour
{
    public Transform startPoint;
    public SubjiMovementArea2D movementArea;
    public Tilemap walkableTilemap;
    public TileBase walkableTile;
    public Sprite previewPlayerSprite;

    [Header("お化け屋敷内のライト設定")]
    [InspectorName("ライトの照射角度")]
    [Range(1f, 180f)] public float lightViewAngle = 45f;
    [InspectorName("ライトの照射距離")]
    [Min(0.1f)] public float lightViewDistance = 7.5f;
    [InspectorName("ライトの色")]
    [Tooltip("照らした部分に重ねる色。白は従来の見た目。A（透明度）で色の強さを調整できます")]
    public Color lightColor = Color.white;
    [InspectorName("視野外の暗さ")]
    [Range(0f, 1f)] public float outsideDarkness = 1f;
    [InspectorName("暗闇の色")]
    public Color darknessColor = Color.black;

    private Tile boundaryTile;

    public Vector4 GetBlockingWallRect(Vector2 hitPoint, Vector2 direction)
    {
        Vector3Int cell = walkableTilemap.WorldToCell(hitPoint + direction * 0.03f);
        TileBase tile = walkableTilemap.GetTile(cell);
        if (tile == null || tile == walkableTile)
            return new Vector4(1f, 1f, -1f, -1f);
        // 上下に接する壁タイルも同じ壁面として表示する（床の奥は広げない）。
        Vector3Int bottom = cell;
        Vector3Int top = cell + new Vector3Int(1, 1, 0);
        TileBase above = walkableTilemap.GetTile(cell + Vector3Int.up);
        TileBase below = walkableTilemap.GetTile(cell + Vector3Int.down);
        if (above != null && above != walkableTile)
            top += Vector3Int.up;
        if (below != null && below != walkableTile)
            bottom += Vector3Int.down;
        Vector3 a = walkableTilemap.CellToWorld(bottom);
        Vector3 b = walkableTilemap.CellToWorld(top);
        return new Vector4(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y),
            Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (previewPlayerSprite != null)
            return;
        foreach (Object asset in UnityEditor.AssetDatabase.LoadAllAssetsAtPath(
            "Assets/Sprites/Player/Sprite-0001.aseprite"))
            if (asset is Sprite sprite)
            {
                previewPlayerSprite = sprite;
                break;
            }
    }
#endif

    private void Awake()
    {
        if (walkableTilemap == null || walkableTile == null)
            return;
        GameObject boundary = new("Wood Floor Boundary");
        boundary.transform.SetParent(walkableTilemap.transform, false);
        Tilemap boundaryMap = boundary.AddComponent<Tilemap>();
        boundaryTile = ScriptableObject.CreateInstance<Tile>();
        boundaryTile.colliderType = Tile.ColliderType.Grid;
        foreach (Vector3Int cell in walkableTilemap.cellBounds.allPositionsWithin)
            if (walkableTilemap.GetTile(cell) == walkableTile)
                boundaryMap.SetTile(cell, boundaryTile);
        Rigidbody2D body = boundary.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Static;
        CompositeCollider2D outline = boundary.AddComponent<CompositeCollider2D>();
        outline.geometryType = CompositeCollider2D.GeometryType.Outlines;
        TilemapCollider2D tiles = boundary.AddComponent<TilemapCollider2D>();
        tiles.compositeOperation = Collider2D.CompositeOperation.Merge;
        tiles.ProcessTilemapChanges();
        outline.GenerateGeometry();
    }

    private void Start()
    {
        // 通常の入場時は既存プレイヤーを使い、直接再生時だけ確認用を用意する。
        if (FindFirstObjectByType<SubjiPlayerMovement>() != null || startPoint == null)
            return;
        GameObject actor = new("Interior Preview Player");
        actor.SetActive(false);
        actor.transform.SetParent(transform, false);
        actor.transform.position = startPoint.position;
        actor.transform.localScale = Vector3.one * 1.5f;
        SpriteRenderer sprite = actor.AddComponent<SpriteRenderer>();
        sprite.sprite = previewPlayerSprite;
        sprite.sharedMaterial = walkableTilemap.GetComponent<TilemapRenderer>().sharedMaterial;
        sprite.sortingOrder = 100;
        actor.AddComponent<BoxCollider2D>().size = Vector2.one / 3f;
        Rigidbody2D body = actor.AddComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.freezeRotation = true;

        GameObject view = new("Interior Preview Camera");
        view.transform.SetParent(transform, false);
        view.tag = "MainCamera";
        Camera camera = view.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 6f;
        camera.backgroundColor = Color.black;
        camera.clearFlags = CameraClearFlags.SolidColor;
        view.AddComponent<AudioListener>();
        SubjiCameraFollow follow = view.AddComponent<SubjiCameraFollow>();
        follow.player = actor.transform;
        follow.SnapToPlayer();
        SubjiPlayerMovement movement = actor.AddComponent<SubjiPlayerMovement>();
        movement.IsStandalonePreview = true;
        actor.SetActive(true);
    }

    private void OnDestroy()
    {
        if (boundaryTile != null)
            Destroy(boundaryTile);
    }

    public bool CanStandAt(Vector2 position, Vector2 extents)
    {
        if (walkableTilemap == null || walkableTile == null)
            return false;
        extents = Vector2.Max(Vector2.zero, extents - Vector2.one * 0.001f);
        Vector3Int min = walkableTilemap.WorldToCell(position - extents);
        Vector3Int max = walkableTilemap.WorldToCell(position + extents);
        for (int y = min.y; y <= max.y; y++)
            for (int x = min.x; x <= max.x; x++)
                if (walkableTilemap.GetTile(new Vector3Int(x, y, min.z)) != walkableTile)
                    return false;
        return true;
    }

    public Vector2 ConstrainMovement(Vector2 current, Vector2 desired, Vector2 extents)
    {
        Vector2 origin = current;
        // 小さく分けて判定し、速度アップ中も床の隙間を飛び越えない。
        int steps = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(current, desired) / 0.1f));
        Vector2 step = (desired - current) / steps;
        for (int i = 0; i < steps; i++)
        {
            Vector2 next = current + step;
            if (CanStandAt(next, extents))
                current = next;
            else
            {
                next = current + new Vector2(step.x, 0f);
                if (CanStandAt(next, extents))
                    current = next;
                next = current + new Vector2(0f, step.y);
                if (CanStandAt(next, extents))
                    current = next;
            }
        }
        return SubjiRoadMap.ConstrainToArea(origin, current, extents, movementArea);
    }

    private void OnDrawGizmos()
    {
        if (startPoint == null)
            return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(startPoint.position, 0.6f);
        Gizmos.DrawLine(startPoint.position, startPoint.position + Vector3.right * 2f);
    }
}
