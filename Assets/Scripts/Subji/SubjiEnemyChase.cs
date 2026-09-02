using System.Collections.Generic;
using UnityEngine;

public class SubjiEnemyChase : MonoBehaviour
{
    public static readonly HashSet<SubjiEnemyChase> ActiveEnemies = new();
    public enum MovementType
    {
        PatrolAndChase,
        WaitUntilPlayerFound,
        GuardPost,
        CompletelyStationary
    }

    public enum SpawnTiming
    {
        [InspectorName("ゲーム開始時")]
        GameStart,
        [InspectorName("ID6・7完了後")]
        AfterTasks6And7,
        [InspectorName("ID12完了後")]
        AfterTask12
    }

    [Header("出現タイミング")]
    [Tooltip("この敵をゲーム開始時、ID6・7完了後、ID12完了後のどこで出現させるか選びます")]
    public SpawnTiming spawnTiming = SpawnTiming.GameStart;

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
    [Tooltip("追跡速度をプレイヤーの通常移動速度に合わせます")]
    public bool matchPlayerMoveSpeed;
    [Tooltip("警備型が見失った後、配置地点へ戻る速度")]
    [Min(0f)] public float returnSpeed = 1f;
    [Tooltip("警備型が視野を上下左右へ切り替える間隔")]
    [Min(0.1f)] public float guardLookInterval = 1.5f;
    [Tooltip("警備型の索敵判定間隔。待機中は毎フレーム判定しません")]
    [Min(0.05f)] public float guardDetectionCheckInterval = 0.2f;
    [Tooltip("警備型が追跡・帰還経路を再計算する間隔")]
    [Min(0.1f)] public float guardPathRefreshInterval = 0.5f;
    [Tooltip("円形の索敵も使うか。警備型の正面監視ではオフを推奨します")]
    public bool useRadialDetection = true;
    [Tooltip("発見範囲から外れた後も追跡を続ける時間")]
    [Min(0f)] public float chaseMemorySeconds = 1.5f;
    [Tooltip("通常敵の索敵判定間隔。毎フレーム索敵しません")]
    [Min(0.05f)] public float detectionCheckInterval = 0.2f;

    [Header("Target")]
    public Transform player;
    [HideInInspector] public SubjiRoadMap roadMap;

    [Header("Detection Circle Appearance")]
    [Tooltip("索敵円を表示します。敵が多い場合はオフを推奨します")]
    public bool showDetectionRadius;
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
    private float nextDetectionCheckTime;
    private float nextDetectionVisualUpdateTime;
    private bool cachedPlayerDetected;
    private bool hasPatrolDestination;
    private Vector2 homePosition;
    private float nextGuardLookTime;
    private float nextGuardDetectionCheckTime;
    private int guardLookIndex;
    private static readonly Vector2[] GuardLookDirections =
    {
        Vector2.up, Vector2.right, Vector2.down, Vector2.left
    };
    private Vector2 movementExtents;
    private Vector2 avoidanceDirection;
    private float avoidanceUntil;
    private Vector2 avoidanceGoal;
    private bool hasAvoidanceGoal;
    private int avoidanceDirectionChanges;
    private readonly RaycastHit2D[] wallCastHits = new RaycastHit2D[8];
    private const int CircleSegments = 32;
    private Vector3 lastCirclePosition = new(float.PositiveInfinity, 0f, 0f);
    private float lastCircleRadius = -1f;
    private bool playerInsideDetectionTrigger;

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
        movementExtents = enemyRenderer != null
            ? (Vector2)enemyRenderer.bounds.extents
            : Vector2.zero;
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

        if (roadMap != null && movementType != MovementType.GuardPost)
        {
            Vector2 extents = enemyRenderer != null ? enemyRenderer.bounds.extents : Vector2.zero;
            transform.position = roadMap.GetClosestPointOnRoad(transform.position, extents);
        }

        CreateDetectionCircle();
        CreateDetectionTrigger();
        CreateTemporaryVisionCone();
        homePosition = transform.position;
        if (movementType == MovementType.GuardPost)
        {
            nextGuardLookTime = Time.time + guardLookInterval;
            nextGuardDetectionCheckTime = Time.time;
            visionCone?.SetFacingDirection(GuardLookDirections[guardLookIndex]);
            if (detectionCircle != null)
                detectionCircle.enabled = false;
        }
        ChooseNextPatrolDestination();
    }

    void Update()
    {
        if (player == null)
            return;

        if (movementType == MovementType.GuardPost)
        {
            UpdateGuardBehaviour();
            return;
        }

        float activeRadius = CurrentDetectionRadius;

        if (detectionCircle != null && Time.time >= nextDetectionVisualUpdateTime)
        {
            UpdateDetectionCircle(activeRadius);
            nextDetectionVisualUpdateTime = Time.time + detectionCheckInterval;
        }

        if (playerMovement != null && playerMovement.IsHidden)
        {
            chaseMemoryTimer = 0f;
            cachedPlayerDetected = false;
            if (movementType == MovementType.PatrolAndChase)
                UpdatePatrol();
            return;
        }

        if (Time.time >= nextDetectionCheckTime)
        {
            nextDetectionCheckTime = Time.time + detectionCheckInterval;
            cachedPlayerDetected = playerInsideDetectionTrigger &&
                (useRadialDetection ||
                 (visionCone != null && visionCone.isActiveAndEnabled &&
                  visionCone.ContainsDirection(player.position)));

            if (cachedPlayerDetected)
                chaseMemoryTimer = chaseMemorySeconds;
        }

        if (!cachedPlayerDetected)
            chaseMemoryTimer = Mathf.Max(0f, chaseMemoryTimer - Time.deltaTime);

        bool isChasing = cachedPlayerDetected || chaseMemoryTimer > 0f;
        if (isChasing && movementType != MovementType.CompletelyStationary)
        {
            MoveAlongRoad(player.position, GetChaseSpeed(), true);
            return;
        }

        if (movementType == MovementType.PatrolAndChase)
            UpdatePatrol();
    }

    void UpdateGuardBehaviour()
    {
        bool playerHidden = playerMovement != null && playerMovement.IsHidden;
        if (playerHidden)
        {
            chaseMemoryTimer = 0f;
        }
        else if (Time.time >= nextGuardDetectionCheckTime)
        {
            nextGuardDetectionCheckTime = Time.time + guardDetectionCheckInterval;
            if (playerInsideDetectionTrigger && visionCone != null &&
                visionCone.isActiveAndEnabled &&
                visionCone.ContainsDirection(player.position))
                chaseMemoryTimer = chaseMemorySeconds;
        }

        if (chaseMemoryTimer > 0f && !playerHidden)
        {
            chaseMemoryTimer = Mathf.Max(0f, chaseMemoryTimer - Time.deltaTime);
            MoveAlongRoad(player.position, GetChaseSpeed(), true);
            return;
        }

        UpdateGuardPost();
    }

    void UpdateGuardPost()
    {
        if (((Vector2)transform.position - homePosition).sqrMagnitude > 0.04f)
        {
            MoveAlongRoad(homePosition, returnSpeed);
            return;
        }

        transform.position = homePosition;
        if (Time.time < nextGuardLookTime)
            return;

        guardLookIndex = (guardLookIndex + 1) % GuardLookDirections.Length;
        visionCone?.SetFacingDirection(GuardLookDirections[guardLookIndex]);
        nextGuardLookTime = Time.time + guardLookInterval;
    }

    float GetChaseSpeed()
    {
        return matchPlayerMoveSpeed && playerMovement != null
            ? playerMovement.moveSpeed
            : chaseSpeed;
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

    void MoveAlongRoad(Vector2 destination, float speed, bool isChasing = false)
    {
        Vector2 currentPosition = transform.position;
        Vector2 toDestination = destination - currentPosition;
        if (!hasAvoidanceGoal ||
            (!isChasing && (avoidanceGoal - destination).sqrMagnitude > 0.25f))
        {
            avoidanceGoal = destination;
            hasAvoidanceGoal = true;
            avoidanceDirectionChanges = 0;
        }
        float step = speed * Time.deltaTime;
        if (toDestination.sqrMagnitude <= step * step)
        {
            transform.position = destination;
            return;
        }

        Vector2 moveDirection = Time.time < avoidanceUntil
            ? avoidanceDirection
            : toDestination.normalized;
        if (WallImmediatelyAhead(moveDirection, step + 0.05f))
        {
            avoidanceDirectionChanges++;
            if (avoidanceDirectionChanges >= 4)
            {
                avoidanceDirectionChanges = 0;
                avoidanceUntil = 0f;
                if (isChasing)
                {
                    chaseMemoryTimer = 0f;
                    cachedPlayerDetected = false;
                    if (movementType == MovementType.PatrolAndChase)
                        ChooseNextPatrolDestination();
                }
                else if (movementType == MovementType.PatrolAndChase)
                {
                    ChooseNextPatrolDestination();
                }
                return;
            }

            Vector2 perpendicular = new(-moveDirection.y, moveDirection.x);
            if (Time.time < avoidanceUntil || (GetInstanceID() & 1) == 0)
                perpendicular = -perpendicular;
            avoidanceDirection = perpendicular;
            avoidanceUntil = Time.time + 0.6f;
            moveDirection = avoidanceDirection;
        }

        Vector2 desiredPosition = currentPosition + moveDirection * step;

        transform.position = desiredPosition;
    }

    private bool WallImmediatelyAhead(Vector2 direction, float distance)
    {
        if (contactCollider == null || direction.sqrMagnitude <= Mathf.Epsilon)
            return false;

        ContactFilter2D filter = ContactFilter2D.noFilter;
        filter.useTriggers = false;
        int hitCount = contactCollider.Cast(direction, filter, wallCastHits, distance);
        for (int i = 0; i < hitCount; i++)
        {
            if (wallCastHits[i].collider != null &&
                wallCastHits[i].collider.GetComponent<InvisibleWall2D>() != null)
                return true;
        }
        return false;
    }

    void OnEnable() => ActiveEnemies.Add(this);
    void OnDisable()
    {
        ActiveEnemies.Remove(this);
        playerInsideDetectionTrigger = false;
    }

    public void SetPlayerInsideDetectionTrigger(bool inside)
    {
        playerInsideDetectionTrigger = inside;
        if (!inside)
            cachedPlayerDetected = false;
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        SubjiPlayerMovement contactedPlayer =
            other.GetComponentInParent<SubjiPlayerMovement>();
        if (contactedPlayer == null || contactedPlayer.IsHidden)
            return;

        Bounds playerBounds = other.bounds;
        float playerArea = playerBounds.size.x * playerBounds.size.y;
        if (playerArea <= Mathf.Epsilon)
            return;

        Bounds enemyBounds = GetContactBounds();
        float width = Mathf.Max(0f, Mathf.Min(playerBounds.max.x, enemyBounds.max.x) -
            Mathf.Max(playerBounds.min.x, enemyBounds.min.x));
        float height = Mathf.Max(0f, Mathf.Min(playerBounds.max.y, enemyBounds.max.y) -
            Mathf.Max(playerBounds.min.y, enemyBounds.min.y));
        if (width * height / playerArea < contactedPlayer.enemyOverlapThreshold)
            return;

        contactedPlayer.GetComponent<SubjiGameClearGoal>()?.GameOver();
    }

    void CreateDetectionCircle()
    {
        if (!showDetectionRadius)
            return;

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

    void CreateDetectionTrigger()
    {
        GameObject triggerObject = new GameObject("Enemy Detection Trigger");
        triggerObject.transform.SetParent(transform, false);
        Vector3 scale = transform.lossyScale;
        triggerObject.transform.localScale = new Vector3(
            Mathf.Approximately(scale.x, 0f) ? 1f : 1f / scale.x,
            Mathf.Approximately(scale.y, 0f) ? 1f : 1f / scale.y, 1f);
        CircleCollider2D trigger = triggerObject.AddComponent<CircleCollider2D>();
        trigger.isTrigger = true;
        trigger.radius = Mathf.Max(movingDetectionRadius, idleDetectionRadius,
            useTemporaryVisionCone ? temporaryVisionDistance : 0f);
        SubjiEnemyDetectionTrigger relay =
            triggerObject.AddComponent<SubjiEnemyDetectionTrigger>();
        relay.Configure(this);
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
