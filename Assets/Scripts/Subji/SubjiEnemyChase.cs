using System.Collections.Generic;
using UnityEngine;

public class SubjiEnemyChase : MonoBehaviour
{
    public static readonly HashSet<SubjiEnemyChase> ActiveEnemies = new();
    public enum MovementType
    {
        PatrolAndChase,
        WaitUntilPlayerFound,
        CompletelyStationary
    }

    [Header("個体の行動タイプ")]
    [Tooltip("徘徊型、発見まで停止する型、完全停止型から選びます")]
    public MovementType movementType = MovementType.PatrolAndChase;

    [Header("見た目・当たり判定")]
    [Tooltip("Resourcesから読み込む敵スプライトの名前")]
    public string spriteResourcePath = "guard";
    [Tooltip("敵イラストの表示倍率")]
    public Vector2 visualScale = new Vector2(1.567f, 1.5f);
    [Tooltip("接触判定の大きさ。敵の足元寄りに細く設定できます")]
    public Vector2 collisionSize = new Vector2(0.33333334f, 0.33333334f);
    [Tooltip("接触判定の中心位置")]
    public Vector2 collisionOffset = Vector2.zero;

    [Header("徘徊設定")]
    [Tooltip("プレイヤーを発見していない時の移動速度")]
    [Min(0f)] public float patrolSpeed = 1.5f;
    [Tooltip("目的地へ到着してから次に動き出すまでの最小時間")]
    [Min(0f)] public float minimumPatrolWait = 0.5f;
    [Tooltip("目的地へ到着してから次に動き出すまでの最大時間")]
    [Min(0f)] public float maximumPatrolWait = 2f;

    [Header("発見・追跡設定")]
    [Tooltip("プレイヤーが移動中の発見半径")]
    [Min(0.1f)] public float movingDetectionRadius = 5f;

    [Tooltip("プレイヤーが停止中の発見半径")]
    [Min(0.1f)] public float idleDetectionRadius = 1.5f;

    [Tooltip("プレイヤー追跡中の移動速度")]
    [Min(0f)] public float chaseSpeed = 3f;
    [Tooltip("発見範囲から外れた後も追跡を続ける時間")]
    [Min(0f)] public float chaseMemorySeconds = 1.5f;

    [Header("Target")]
    public Transform player;
    [HideInInspector] public SubjiRoadMap roadMap;

    [Header("Detection Circle Appearance")]
    [Range(0.01f, 0.5f)] public float circleWidth = 0.12f;

    [Header("仮の正面視野（実験機能）")]
    [Tooltip("オフにすると扇形の表示と発見判定をまとめて無効にします")]
    public bool useTemporaryVisionCone = true;
    [Tooltip("扇形の端から端までの角度")]
    [Range(1f, 180f)] public float temporaryVisionAngle = 30f;
    [Tooltip("扇形の長さ。移動中の索敵半径より少し大きい値を推奨します")]
    [Min(0.1f)] public float temporaryVisionDistance = 5.5f;

    private LineRenderer detectionCircle;
    private SubjiPlayerMovement playerMovement;
    private SpriteRenderer enemyRenderer;
    private BoxCollider2D contactCollider;
    private SubjiEnemyVisionCone visionCone;
    private Vector2 patrolDestination;
    private float patrolWaitTimer;
    private float chaseMemoryTimer;
    private bool hasPatrolDestination;
    private Vector2 cachedObstacleWaypoint;
    private Vector2 cachedPathGoal;
    private float nextPathRefreshTime;
    private static InvisibleWall2D[] cachedWalls;
    private static float nextWallCacheRefreshTime;
    private const int CircleSegments = 32;
    private Vector3 lastCirclePosition = new(float.PositiveInfinity, 0f, 0f);
    private float lastCircleRadius = -1f;

    public float CurrentDetectionRadius
    {
        get
        {
            bool playerIsMoving = playerMovement != null && playerMovement.IsMoving;
            return playerIsMoving ? movingDetectionRadius : idleDetectionRadius;
        }
    }

    void Awake()
    {
        ApplyAppearanceAndCollider();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        ApplyAppearanceAndCollider();
    }
#endif

    public void ApplyAppearanceAndCollider()
    {
        enemyRenderer = GetComponent<SpriteRenderer>();
        if (enemyRenderer == null)
            enemyRenderer = gameObject.AddComponent<SpriteRenderer>();

        if (!string.IsNullOrWhiteSpace(spriteResourcePath))
        {
            Sprite[] sprites = Resources.LoadAll<Sprite>(spriteResourcePath);
            if (sprites.Length > 0)
                enemyRenderer.sprite = sprites[0];
        }

        enemyRenderer.color = Color.white;
        enemyRenderer.sortingLayerName = "Player";
        enemyRenderer.sortingOrder = 90;
        transform.localScale = new Vector3(
            Mathf.Max(0.01f, visualScale.x),
            Mathf.Max(0.01f, visualScale.y), 1f);

        contactCollider = GetComponent<BoxCollider2D>();
        if (contactCollider == null)
            contactCollider = gameObject.AddComponent<BoxCollider2D>();
        contactCollider.isTrigger = true;
        contactCollider.size = new Vector2(
            Mathf.Max(0.01f, collisionSize.x),
            Mathf.Max(0.01f, collisionSize.y));
        contactCollider.offset = collisionOffset;

    }

    public Bounds GetContactBounds()
    {
        if (contactCollider == null)
            contactCollider = GetComponent<BoxCollider2D>();
        return contactCollider != null
            ? contactCollider.bounds
            : (enemyRenderer != null ? enemyRenderer.bounds : new Bounds(transform.position, Vector3.zero));
    }

    void Start()
    {
        if (player != null)
            playerMovement = player.GetComponent<SubjiPlayerMovement>();

        ApplyAppearanceAndCollider();

        if (roadMap == null)
            roadMap = FindFirstObjectByType<SubjiRoadMap>();

        if (roadMap != null)
        {
            Vector2 extents = enemyRenderer != null ? enemyRenderer.bounds.extents : Vector2.zero;
            transform.position = roadMap.GetClosestPointOnRoad(transform.position, extents);
        }

        CreateDetectionCircle();
        CreateTemporaryVisionCone();
        ChooseNextPatrolDestination();
    }

    void Update()
    {
        if (player == null || detectionCircle == null)
            return;

        float activeRadius = CurrentDetectionRadius;

        UpdateDetectionCircle(activeRadius);

        if (playerMovement != null && playerMovement.IsHidden)
        {
            chaseMemoryTimer = 0f;
            if (movementType == MovementType.PatrolAndChase)
                UpdatePatrol();
            return;
        }

        float distance = Vector2.Distance(transform.position, player.position);
        bool isInsideVisionCone = visionCone != null &&
            visionCone.isActiveAndEnabled && visionCone.Contains(player.position);
        if (distance <= activeRadius || isInsideVisionCone)
            chaseMemoryTimer = chaseMemorySeconds;
        else
            chaseMemoryTimer = Mathf.Max(0f, chaseMemoryTimer - Time.deltaTime);

        bool isChasing = distance <= activeRadius || isInsideVisionCone || chaseMemoryTimer > 0f;
        if (isChasing && movementType != MovementType.CompletelyStationary)
        {
            MoveAlongRoad(player.position, chaseSpeed);
            return;
        }

        if (movementType == MovementType.PatrolAndChase)
            UpdatePatrol();
    }

    void UpdatePatrol()
    {
        if (roadMap == null || patrolSpeed <= 0f)
            return;

        if (patrolWaitTimer > 0f)
        {
            patrolWaitTimer -= Time.deltaTime;
            return;
        }

        if (!hasPatrolDestination)
            ChooseNextPatrolDestination();

        if (Vector2.Distance(transform.position, patrolDestination) <= 0.2f)
        {
            hasPatrolDestination = false;
            patrolWaitTimer = Random.Range(minimumPatrolWait,
                Mathf.Max(minimumPatrolWait, maximumPatrolWait));
            return;
        }

        MoveAlongRoad(patrolDestination, patrolSpeed);
    }

    void ChooseNextPatrolDestination()
    {
        if (roadMap == null)
            return;

        patrolDestination = roadMap.GetRandomPointOnRoad();
        hasPatrolDestination = true;
    }

    void MoveAlongRoad(Vector2 destination, float speed)
    {
        Vector2 currentPosition = transform.position;
        Vector2 extents = enemyRenderer != null ? enemyRenderer.bounds.extents : Vector2.zero;
        // 中心同士を無理に一致させず、敵が道路内に収まれる領域の中から
        // プレイヤーとの重なり面積が最大になる（中心距離が最小の）位置を選ぶ。
        Vector2 reachableDestination = roadMap != null
            ? roadMap.GetClosestPointOnRoad(destination, extents)
            : destination;
        Vector2 pathPoint = roadMap != null
            ? roadMap.GetShortestPathPoint(currentPosition, reachableDestination, extents)
            : reachableDestination;
        if (Time.time >= nextPathRefreshTime || Vector2.Distance(cachedPathGoal, pathPoint) > 0.5f)
        {
            cachedPathGoal = pathPoint;
            cachedObstacleWaypoint = GetObstacleWaypoint(currentPosition, pathPoint, extents);
            nextPathRefreshTime = Time.time + 0.25f;
        }
        pathPoint = cachedObstacleWaypoint;
        Vector2 desiredPosition = Vector2.MoveTowards(currentPosition, pathPoint,
            speed * Time.deltaTime);

        if (roadMap != null)
            desiredPosition = roadMap.ConstrainToRoad(currentPosition, desiredPosition, extents);

        transform.position = desiredPosition;
    }

    void OnEnable() => ActiveEnemies.Add(this);
    void OnDisable() => ActiveEnemies.Remove(this);

    static Vector2 GetObstacleWaypoint(Vector2 start, Vector2 goal, Vector2 extents)
    {
        if (cachedWalls == null || Time.time >= nextWallCacheRefreshTime ||
            System.Array.Exists(cachedWalls, wall => wall == null))
        {
            cachedWalls = FindObjectsByType<InvisibleWall2D>(FindObjectsSortMode.None);
            nextWallCacheRefreshTime = Time.time + 1f;
        }
        if (cachedWalls.Length == 0)
            return goal;

        List<Bounds> obstacles = new();
        List<Vector2> nodes = new() { start, goal };
        foreach (InvisibleWall2D wall in cachedWalls)
        {
            if (wall == null)
                continue;

            BoxCollider2D box = wall.GetComponent<BoxCollider2D>();
            if (box == null || !box.enabled)
                continue;
            Bounds bounds = box.bounds;
            bounds.Expand(new Vector3(extents.x * 2f + 0.12f, extents.y * 2f + 0.12f));
            obstacles.Add(bounds);
            const float cornerGap = 0.03f;
            nodes.Add(new Vector2(bounds.min.x - cornerGap, bounds.min.y - cornerGap));
            nodes.Add(new Vector2(bounds.min.x - cornerGap, bounds.max.y + cornerGap));
            nodes.Add(new Vector2(bounds.max.x + cornerGap, bounds.min.y - cornerGap));
            nodes.Add(new Vector2(bounds.max.x + cornerGap, bounds.max.y + cornerGap));
        }

        if (SegmentIsClear(start, goal, obstacles))
            return goal;

        int count = nodes.Count;
        float[] distance = new float[count];
        int[] previous = new int[count];
        bool[] visited = new bool[count];
        for (int i = 0; i < count; i++) { distance[i] = float.PositiveInfinity; previous[i] = -1; }
        distance[0] = 0f;

        for (int step = 0; step < count; step++)
        {
            int current = -1;
            for (int i = 0; i < count; i++)
                if (!visited[i] && (current < 0 || distance[i] < distance[current])) current = i;
            if (current < 0 || float.IsInfinity(distance[current]) || current == 1) break;
            visited[current] = true;
            for (int next = 0; next < count; next++)
            {
                if (next == current || visited[next] || !SegmentIsClear(nodes[current], nodes[next], obstacles))
                    continue;
                float candidate = distance[current] + Vector2.Distance(nodes[current], nodes[next]);
                if (candidate < distance[next]) { distance[next] = candidate; previous[next] = current; }
            }
        }

        if (previous[1] < 0)
            return start;
        int waypoint = 1;
        while (previous[waypoint] > 0) waypoint = previous[waypoint];
        return nodes[waypoint];
    }

    static bool SegmentIsClear(Vector2 from, Vector2 to, List<Bounds> obstacles)
    {
        foreach (Bounds bounds in obstacles)
        {
            if (bounds.Contains(from) || bounds.Contains(to))
                continue;
            Vector2 direction = to - from;
            float length = direction.magnitude;
            if (length <= 0.001f) continue;
            Ray ray = new(from, direction / length);
            if (bounds.IntersectRay(ray, out float hitDistance) && hitDistance < length)
                return false;
        }
        return true;
    }

    void CreateDetectionCircle()
    {
        GameObject circleObject = new GameObject("Player Detection Radius");
        detectionCircle = circleObject.AddComponent<LineRenderer>();
        detectionCircle.loop = true;
        detectionCircle.useWorldSpace = true;
        detectionCircle.positionCount = CircleSegments;
        detectionCircle.startColor = new Color(0.3f, 0.85f, 1f, 0.45f);
        detectionCircle.endColor = new Color(0.3f, 0.85f, 1f, 0.45f);
        detectionCircle.sortingOrder = 5;
        detectionCircle.material = new Material(Shader.Find("Sprites/Default"));

        UpdateDetectionCircle(idleDetectionRadius);
    }

    void CreateTemporaryVisionCone()
    {
        visionCone = GetComponent<SubjiEnemyVisionCone>();
        if (!useTemporaryVisionCone)
        {
            if (visionCone != null)
                visionCone.enabled = false;
            return;
        }

        if (visionCone == null)
            visionCone = gameObject.AddComponent<SubjiEnemyVisionCone>();
        visionCone.viewAngle = temporaryVisionAngle;
        visionCone.viewDistance = temporaryVisionDistance;
        visionCone.enabled = true;
    }

    void UpdateDetectionCircle(float radius)
    {
        if ((transform.position - lastCirclePosition).sqrMagnitude < 0.0025f &&
            Mathf.Abs(radius - lastCircleRadius) < 0.01f)
            return;
        lastCirclePosition = transform.position;
        lastCircleRadius = radius;

        detectionCircle.startWidth = circleWidth;
        detectionCircle.endWidth = circleWidth;

        for (int i = 0; i < CircleSegments; i++)
        {
            float angle = i * Mathf.PI * 2f / CircleSegments;
            Vector3 point = transform.position + new Vector3(
                Mathf.Cos(angle) * radius,
                Mathf.Sin(angle) * radius,
                0f
            );
            detectionCircle.SetPosition(i, point);
        }
    }
}
