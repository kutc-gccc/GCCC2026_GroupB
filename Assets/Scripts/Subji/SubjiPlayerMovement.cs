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

    [Tooltip("満タンの状態から連続で速度アップできる秒数")]
    [Min(0.1f)] public float speedBoostDuration = 3f;

    [Tooltip("速度アップを使い終えてから回復が始まるまでの秒数")]
    [Min(0f)] public float speedBoostRechargeDelay = 1f;

    [Tooltip("空の状態から満タンまで回復する秒数")]
    [Min(0.1f)] public float speedBoostRechargeDuration = 5f;

    [Header("速度アップゲージの見た目")]
    [Tooltip("プレイヤーを基準にしたゲージの表示位置")]
    public Vector2 speedBoostGaugeOffset = new Vector2(0f, 1f);

    [Tooltip("ゲージの横幅と縦幅")]
    public Vector2 speedBoostGaugeSize = new Vector2(1.4f, 0.16f);

    public Color speedBoostGaugeColor = new Color(0.15f, 0.85f, 1f, 1f);
    public Color speedBoostGaugeBackgroundColor = new Color(0f, 0f, 0f, 0.75f);

    [Header("夜の視野（実験機能）")]
    [Tooltip("ゲーム開始時から暗くするか。プレイ中は切り替えキーで変更できます")]
    public bool enableNightVision = true;

    [Tooltip("夜の暗さをON/OFFするキー")]
    public Key nightVisionToggleKey = Key.P;

    [Tooltip("この半径までは暗くなりません。敵の移動中索敵半径5より少し小さい4.5が初期値です")]
    [Min(0.1f)] public float playerVisionRadius = 4.5f;

    [Tooltip("視野の端が暗くなっていくグラデーションの幅")]
    [Min(0.1f)] public float visionGradientWidth = 3f;

    [Tooltip("視野外の暗さ。1で完全な黒、0で透明です")]
    [Range(0f, 1f)] public float outsideDarkness = 0.95f;

    [Tooltip("視野外に重ねる色。黒以外にすると霧や警戒演出にも使えます")]
    public Color darknessColor = Color.black;

    [Tooltip("暗闇が覆う範囲。通常はカメラ表示範囲より十分大きくしてください")]
    [Min(10f)] public float darknessOverlaySize = 40f;

    [Tooltip("グラデーション画像の解像度。高いほど滑らかですが生成負荷が増えます")]
    [Range(64, 512)] public int darknessTextureResolution = 256;

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
    private Transform speedBoostGaugeRoot;
    private Transform speedBoostGaugeFill;
    private float speedBoostAmount = 1f;
    private float lastSpeedBoostUseTime = float.NegativeInfinity;
    private bool speedBoostWasUsed;
    private static Sprite gaugeSprite;
    private GameObject darknessOverlay;
    private Texture2D darknessTexture;

    public bool IsMoving => movement.sqrMagnitude > 0.01f;
    public bool IsSpeedBoosting { get; private set; }
    public float SpeedBoostAmount => speedBoostAmount;
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
        CreateSpeedBoostGauge();
        CreateDarknessOverlay();

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
        if (Keyboard.current != null &&
            Keyboard.current[nightVisionToggleKey].wasPressedThisFrame)
        {
            enableNightVision = !enableNightVision;
            UpdateDarknessVisibility();
        }
        UpdateSpeedBoost();
    }

    void CreateDarknessOverlay()
    {
        darknessOverlay = new GameObject("Player Vision Darkness");
        darknessOverlay.transform.SetParent(transform, false);
        darknessOverlay.transform.localPosition = new Vector3(0f, 0f, -0.1f);

        int resolution = Mathf.Clamp(darknessTextureResolution, 64, 512);
        float overlaySize = Mathf.Max(10f, darknessOverlaySize);
        float clearRadius = Mathf.Max(0.1f, playerVisionRadius);
        float gradientWidth = Mathf.Max(0.1f, visionGradientWidth);

        darknessTexture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
        darknessTexture.name = "Runtime Player Vision Gradient";
        darknessTexture.filterMode = FilterMode.Bilinear;
        darknessTexture.wrapMode = TextureWrapMode.Clamp;

        Color[] pixels = new Color[resolution * resolution];
        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float worldX = ((x + 0.5f) / resolution - 0.5f) * overlaySize;
                float worldY = ((y + 0.5f) / resolution - 0.5f) * overlaySize;
                float distance = Mathf.Sqrt(worldX * worldX + worldY * worldY);
                float gradient = Mathf.SmoothStep(0f, 1f,
                    Mathf.InverseLerp(clearRadius, clearRadius + gradientWidth, distance));
                pixels[y * resolution + x] = new Color(
                    darknessColor.r,
                    darknessColor.g,
                    darknessColor.b,
                    gradient * outsideDarkness * darknessColor.a);
            }
        }

        darknessTexture.SetPixels(pixels);
        darknessTexture.Apply(false, false);

        Sprite darknessSprite = Sprite.Create(
            darknessTexture,
            new Rect(0f, 0f, resolution, resolution),
            new Vector2(0.5f, 0.5f),
            resolution / overlaySize);
        darknessSprite.name = "Runtime Player Vision Sprite";

        SpriteRenderer darknessRenderer = darknessOverlay.AddComponent<SpriteRenderer>();
        darknessRenderer.sprite = darknessSprite;
        darknessRenderer.sortingOrder = 20;
        UpdateDarknessVisibility();
    }

    void UpdateDarknessVisibility()
    {
        if (darknessOverlay != null)
            darknessOverlay.SetActive(enableNightVision);
    }

    void OnDestroy()
    {
        if (darknessTexture != null)
            Destroy(darknessTexture);
    }

    void UpdateSpeedBoost()
    {
        bool boostKeyPressed = Keyboard.current != null &&
            Keyboard.current[speedBoostKey].isPressed;
        bool wantsToBoost = boostKeyPressed && IsMoving && speedBoostAmount > 0f;

        IsSpeedBoosting = wantsToBoost;

        if (IsSpeedBoosting)
        {
            speedBoostAmount = Mathf.Max(0f,
                speedBoostAmount - Time.deltaTime / Mathf.Max(0.1f, speedBoostDuration));
            lastSpeedBoostUseTime = Time.time;
            speedBoostWasUsed = true;

            if (speedBoostAmount <= 0f)
                IsSpeedBoosting = false;
        }
        else if (speedBoostWasUsed && !boostKeyPressed &&
            Time.time >= lastSpeedBoostUseTime + speedBoostRechargeDelay)
        {
            speedBoostAmount = Mathf.Min(1f,
                speedBoostAmount + Time.deltaTime / Mathf.Max(0.1f, speedBoostRechargeDuration));

            if (speedBoostAmount >= 1f)
                speedBoostWasUsed = false;
        }

        UpdateSpeedBoostGauge();
    }

    void CreateSpeedBoostGauge()
    {
        GameObject root = new GameObject("Speed Boost Gauge");
        root.transform.SetParent(transform, false);
        root.transform.localPosition = speedBoostGaugeOffset;
        speedBoostGaugeRoot = root.transform;

        if (gaugeSprite == null)
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.name = "Runtime Gauge Texture";
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            gaugeSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f),
                new Vector2(0.5f, 0.5f), 1f);
            gaugeSprite.name = "Runtime Gauge Sprite";
        }

        GameObject background = new GameObject("Background");
        background.transform.SetParent(speedBoostGaugeRoot, false);
        background.transform.localScale = new Vector3(
            speedBoostGaugeSize.x + 0.08f, speedBoostGaugeSize.y + 0.08f, 1f);
        SpriteRenderer backgroundRenderer = background.AddComponent<SpriteRenderer>();
        backgroundRenderer.sprite = gaugeSprite;
        backgroundRenderer.color = speedBoostGaugeBackgroundColor;
        backgroundRenderer.sortingOrder = playerRenderer != null
            ? playerRenderer.sortingOrder + 10
            : 10;

        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(speedBoostGaugeRoot, false);
        speedBoostGaugeFill = fill.transform;
        SpriteRenderer fillRenderer = fill.AddComponent<SpriteRenderer>();
        fillRenderer.sprite = gaugeSprite;
        fillRenderer.color = speedBoostGaugeColor;
        fillRenderer.sortingOrder = backgroundRenderer.sortingOrder + 1;

        UpdateSpeedBoostGauge();
    }

    void UpdateSpeedBoostGauge()
    {
        if (speedBoostGaugeRoot == null || speedBoostGaugeFill == null)
            return;

        speedBoostGaugeRoot.localPosition = speedBoostGaugeOffset;
        speedBoostGaugeRoot.gameObject.SetActive(speedBoostWasUsed || speedBoostAmount < 1f);

        float width = speedBoostGaugeSize.x * Mathf.Clamp01(speedBoostAmount);
        speedBoostGaugeFill.localScale = new Vector3(width, speedBoostGaugeSize.y, 1f);
        speedBoostGaugeFill.localPosition = new Vector3(
            (width - speedBoostGaugeSize.x) * 0.5f, 0f, -0.01f);
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
