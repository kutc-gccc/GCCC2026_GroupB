using UnityEngine;

public class SubjiEnemyChase : MonoBehaviour
{
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
    public Vector2 collisionSize = new Vector2(0.52f, 0.9f);
    [Tooltip("接触判定の中心位置")]
    public Vector2 collisionOffset = new Vector2(0f, 0.45f);

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

    private LineRenderer detectionCircle;
    private SubjiPlayerMovement playerMovement;
    private SpriteRenderer enemyRenderer;
    private BoxCollider2D contactCollider;
    private Vector2 patrolDestination;
    private float patrolWaitTimer;
    private float chaseMemoryTimer;
    private bool hasPatrolDestination;
    private const int CircleSegments = 96;

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
        ChooseNextPatrolDestination();
    }

    void Update()
    {
        if (player == null || detectionCircle == null)
            return;

        float activeRadius = CurrentDetectionRadius;

        UpdateDetectionCircle(activeRadius);

        float distance = Vector2.Distance(transform.position, player.position);
        if (distance <= activeRadius)
            chaseMemoryTimer = chaseMemorySeconds;
        else
            chaseMemoryTimer = Mathf.Max(0f, chaseMemoryTimer - Time.deltaTime);

        bool isChasing = distance <= activeRadius || chaseMemoryTimer > 0f;
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
        Vector2 desiredPosition = Vector2.MoveTowards(currentPosition, pathPoint,
            speed * Time.deltaTime);

        if (roadMap != null)
            desiredPosition = roadMap.ConstrainToRoad(currentPosition, desiredPosition, extents);

        transform.position = desiredPosition;
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

    void UpdateDetectionCircle(float radius)
    {
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
