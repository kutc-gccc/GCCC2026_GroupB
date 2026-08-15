using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class SubjiPlayerMovement : MonoBehaviour
{
    [Header("プレイヤーの設定")]
    [Tooltip("プレイヤーが動く速さ")]
    public float moveSpeed = 5f;

    [Header("速度アップの設定")]
    [Tooltip("押している間、移動速度が上がるキー")]
    public Key speedBoostKey = Key.LeftShift;

    [Tooltip("速度アップ中に移動速度へ掛ける倍率。2なら2倍速です")]
    [Min(0f)] public float speedBoostMultiplier = 2f;

    [Header("敵の初期設定")]
    [Tooltip("マップ中央から見た敵の希望出現位置。実際には最寄りの道路上へ補正されます")]
    public Vector2 enemySpawnOffset = new Vector2(10f, 0f);

    [Header("敵との接触判定")]
    [Tooltip("プレイヤーの何割が敵と重なったら接触として数えるか。0.3は30%です")]
    [Range(0.01f, 1f)] public float enemyOverlapThreshold = 0.3f;

    private Rigidbody2D rb;
    private SpriteRenderer playerRenderer;
    private Vector2 movement;
    private InputAction moveAction;
    private GUIStyle coordinateStyle;
    private Vector2 fieldCenter;
    private SubjiRoadMap roadMap;
    private readonly HashSet<int> touchingEnemyIds = new HashSet<int>();
    private readonly HashSet<int> checkedEnemyIds = new HashSet<int>();

    public bool IsMoving => movement.sqrMagnitude > 0.01f;
    public bool IsSpeedBoosting { get; private set; }
    public int EnemyContactCount { get; private set; }
    public event Action<int> EnemyContactCountChanged;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerRenderer = GetComponent<SpriteRenderer>();
        if (rb != null)
        {
            // FixedUpdate間の位置を描画フレームで補間し、見た目の小刻みな揺れを防ぐ。
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.freezeRotation = true;
        }
        roadMap = FindFirstObjectByType<SubjiRoadMap>();
        if (roadMap == null || !roadMap.IsReady)
        {
            Debug.LogError("Subji Road Map がシーンにありません。先にマップを配置してください。", this);
            enabled = false;
            return;
        }

        fieldCenter = roadMap.Center;
        roadMap.RegisterPlayer(transform);
        transform.position = roadMap.GetClosestPointOnRoad(transform.position,
            playerRenderer != null ? playerRenderer.bounds.extents : Vector2.zero);
        CreateEnemy();

        moveAction = new InputAction("Move", InputActionType.Value);
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/upArrow")
            .With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/leftArrow")
            .With("Right", "<Keyboard>/rightArrow");

    }

    void OnEnable()
    {
        if (moveAction != null)
            moveAction.Enable();
    }

    void OnDisable()
    {
        if (moveAction != null)
            moveAction.Disable();
    }

    void Update()
    {
        if (!Application.isPlaying || moveAction == null)
            return;

        movement = moveAction.ReadValue<Vector2>().normalized;
        IsSpeedBoosting = Keyboard.current != null &&
            Keyboard.current[speedBoostKey].isPressed;
    }

    void LateUpdate()
    {
        if (Application.isPlaying)
            UpdateEnemyContacts();
    }

    void UpdateEnemyContacts()
    {
        if (playerRenderer == null)
            return;

        Bounds playerBounds = playerRenderer.bounds;
        float playerArea = playerBounds.size.x * playerBounds.size.y;
        if (playerArea <= Mathf.Epsilon)
            return;

        checkedEnemyIds.Clear();
        SubjiEnemyChase[] enemies = FindObjectsByType<SubjiEnemyChase>(FindObjectsSortMode.None);
        foreach (SubjiEnemyChase enemy in enemies)
        {
            if (enemy == null)
                continue;

            int id = enemy.GetInstanceID();
            checkedEnemyIds.Add(id);
            SpriteRenderer enemySprite = enemy.GetComponent<SpriteRenderer>();
            bool isTouching = enemySprite != null &&
                GetOverlapArea(playerBounds, enemySprite.bounds) / playerArea >= enemyOverlapThreshold;

            if (isTouching)
            {
                if (touchingEnemyIds.Add(id))
                {
                    EnemyContactCount++;
                    EnemyContactCountChanged?.Invoke(EnemyContactCount);
                }
            }
            else
            {
                touchingEnemyIds.Remove(id);
            }
        }

        touchingEnemyIds.RemoveWhere(id => !checkedEnemyIds.Contains(id));
    }

    static float GetOverlapArea(Bounds a, Bounds b)
    {
        float width = Mathf.Max(0f, Mathf.Min(a.max.x, b.max.x) - Mathf.Max(a.min.x, b.min.x));
        float height = Mathf.Max(0f, Mathf.Min(a.max.y, b.max.y) - Mathf.Max(a.min.y, b.min.y));
        return width * height;
    }

    void FixedUpdate()
    {
        if (!Application.isPlaying || rb == null)
            return;

        Vector2 playerExtents = playerRenderer != null
            ? playerRenderer.bounds.extents
            : Vector2.zero;
        float currentMoveSpeed = IsSpeedBoosting
            ? moveSpeed * speedBoostMultiplier
            : moveSpeed;
        Vector2 nextPosition = rb.position
            + movement * currentMoveSpeed * Time.fixedDeltaTime;

        if (roadMap != null)
            nextPosition = roadMap.ConstrainToRoad(rb.position, nextPosition, playerExtents);

        rb.MovePosition(nextPosition);
    }

    void CreateEnemy()
    {
        const string enemyName = "Enemy";
        GameObject enemy = GameObject.Find(enemyName);

        if (enemy == null)
            enemy = new GameObject(enemyName);

        SpriteRenderer enemyRenderer = enemy.GetComponent<SpriteRenderer>();

        if (enemyRenderer == null)
            enemyRenderer = enemy.AddComponent<SpriteRenderer>();

        if (enemyRenderer.sprite == null && playerRenderer != null)
            enemyRenderer.sprite = playerRenderer.sprite;

        enemyRenderer.color = new Color(1f, 0.2f, 0.2f, 1f);
        enemyRenderer.sortingOrder = 1;

        SubjiEnemyChase enemyChase = enemy.GetComponent<SubjiEnemyChase>();

        if (enemyChase == null)
            enemyChase = enemy.AddComponent<SubjiEnemyChase>();

        enemyChase.player = transform;
        enemyChase.roadMap = roadMap;

        Vector2 enemyExtents = enemyRenderer.bounds.extents;
        Vector2 requestedSpawn = fieldCenter + enemySpawnOffset;
        enemy.transform.position = roadMap != null
            ? roadMap.GetClosestPointOnRoad(requestedSpawn, enemyExtents)
            : requestedSpawn;
    }

    void OnGUI()
    {
        if (!Application.isPlaying)
            return;

        if (coordinateStyle == null)
        {
            coordinateStyle = new GUIStyle(GUI.skin.label);
            coordinateStyle.fontSize = 24;
            coordinateStyle.normal.textColor = Color.white;
        }

        string coordinates = $"X: {transform.position.x:F1}  Y: {transform.position.y:F1}";

        GUI.color = new Color(0f, 0f, 0f, 0.7f);
        GUI.Box(new Rect(10f, 70f, 210f, 48f), GUIContent.none);
        GUI.color = Color.white;
        GUI.Label(new Rect(20f, 77f, 190f, 34f), $"HIT: {EnemyContactCount}", coordinateStyle);

        GUI.color = new Color(0f, 0f, 0f, 0.7f);
        GUI.Box(new Rect(10f, 126f, 275f, 48f), GUIContent.none);
        GUI.color = Color.white;
        GUI.Label(new Rect(20f, 133f, 255f, 34f), coordinates, coordinateStyle);
    }
}
