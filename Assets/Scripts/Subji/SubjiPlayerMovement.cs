using System;
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

    [Tooltip("ライト消灯中の最小視野半径")]
    [Min(0.1f)] public float playerVisionRadius = 1f;

    [Tooltip("視野境界のぼかし幅")]
    [Min(0.1f)] public float visionGradientWidth = 1.3f;

    [Tooltip("視野外の暗さ。1で完全な黒、0で透明です")]
    [Range(0f, 1f)] public float outsideDarkness = 0.95f;

    [Tooltip("視野外に重ねる色。黒以外にすると霧や警戒演出にも使えます")]
    public Color darknessColor = Color.black;

    [Tooltip("暗闇が覆う範囲。通常はカメラ表示範囲より十分大きくしてください")]
    [Min(10f)] public float darknessOverlaySize = 40f;

    [Tooltip("グラデーション画像の解像度。高いほど滑らかですが生成負荷が増えます")]
    [Range(64, 512)] public int darknessTextureResolution = 256;

    [Header("スロット2のライト")]
    [Tooltip("左から数えたライトのスロット番号")]
    [Min(1)] public int lightSlotNumber = 2;
    [Tooltip("敵の正面視野と同じ、ライトの照射角度")]
    [Range(1f, 180f)] public float lightViewAngle = 30f;
    [Tooltip("敵の正面視野と同じ、ライトの照射距離")]
    [Min(0.1f)] public float lightViewDistance = 5.5f;

    [Header("敵の初期設定")]
    [Tooltip("マップ中央から見た敵の希望出現位置。実際には最寄りの道路上へ補正されます")]
    public Vector2 enemySpawnOffset = new Vector2(10f, 0f);

    [Header("敵との接触判定")]
    [Tooltip("プレイヤーの何割が敵と重なったらゲームオーバーにするか。0.3は30%です")]
    [Range(0.01f, 1f)] public float enemyOverlapThreshold = 0.3f;

    [Header("登録者数")]
    [Tooltip("1秒ごとに増える登録者数")]
    [Min(0)] public int subscribersPerSecond = 2;

    [Tooltip("タスクを1つ完了したときに増える登録者数")]
    [Min(0)] public int subscribersPerCompletedTask = 100;

    private Rigidbody2D rb;
    private SpriteRenderer playerRenderer;
    private Vector2 movement;
    private InputAction moveAction;
    private GUIStyle coordinateStyle;
    private GUIStyle taskCompleteStyle;
    private Vector2 fieldCenter;
    private SubjiRoadMap roadMap;
    private float subscriberGrowthTimer;
    private float taskCompleteMessageUntil;
    private int lastTaskSubscriberReward;
    private Transform speedBoostGaugeRoot;
    private Transform speedBoostGaugeFill;
    private float speedBoostAmount = 1f;
    private float lastSpeedBoostUseTime = float.NegativeInfinity;
    private bool speedBoostWasUsed;
    private static Sprite gaugeSprite;
    private GameObject darknessOverlay;
    private Texture2D darknessTexture;
    private Texture2D lightDarknessTexture;
    private Sprite darknessSprite;
    private Sprite lightDarknessSprite;
    private SpriteRenderer darknessRenderer;
    private ItemSlotSelector itemSlotSelector;
    private bool isLightOn;
    private float nextPlacedLightRefreshTime;
    private Color[] darknessPixelBuffer;
    private readonly System.Collections.Generic.List<Vector3> placedLightData = new();
    private int lastPlacedLightCount = -1;
    private Material darknessMaterial;
    private readonly Vector4[] placedLightShaderData = new Vector4[30];
    private Vector2 flashlightDirection = Vector2.right;
    private int lastPlacedLightRevision = -1;
    private float staminaFreezeEndTime;
    private Color staminaFreezeGaugeColor;

    public bool IsMoving => movement.sqrMagnitude > 0.01f;
    public bool IsHidden { get; private set; }
    public bool IsSpeedBoosting { get; private set; }
    public float SpeedBoostAmount => speedBoostAmount;
    public bool IsStaminaFrozen => Time.time < staminaFreezeEndTime;
    public int SubscriberCount { get; private set; }
    public event Action<int> SubscriberCountChanged;

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
        if (GetComponent<SubjiGameClearGoal>() == null)
            gameObject.AddComponent<SubjiGameClearGoal>();
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

        movement = IsHidden ? Vector2.zero : moveAction.ReadValue<Vector2>().normalized;
        if (Keyboard.current != null &&
            Keyboard.current[nightVisionToggleKey].wasPressedThisFrame)
        {
            enableNightVision = !enableNightVision;
            UpdateDarknessVisibility();
        }
        UpdateSlotLight();
        UpdateDarknessMaterial();
        UpdateSpeedBoost();
        UpdateSubscriberGrowth();
    }

    void UpdateSubscriberGrowth()
    {
        subscriberGrowthTimer += Time.deltaTime;
        int elapsedSeconds = Mathf.FloorToInt(subscriberGrowthTimer);
        if (elapsedSeconds <= 0)
            return;

        subscriberGrowthTimer -= elapsedSeconds;
        AddSubscribers(elapsedSeconds * subscribersPerSecond);
    }

    public void CompleteTask()
    {
        AddSubscribers(subscribersPerCompletedTask);
        lastTaskSubscriberReward = subscribersPerCompletedTask;
        taskCompleteMessageUntil = Time.unscaledTime + 1.3f;
    }

    private void AddSubscribers(int amount)
    {
        if (amount <= 0)
            return;

        SubscriberCount += amount;
        SubscriberCountChanged?.Invoke(SubscriberCount);
    }

    void CreateDarknessOverlay()
    {
        darknessOverlay = new GameObject("Player Vision Darkness");
        // ミニマップカメラから除外できるよう、Ignore Raycastレイヤーを使用する。
        darknessOverlay.layer = 2;
        darknessOverlay.transform.position = new Vector3(fieldCenter.x, fieldCenter.y, -0.1f);
        float overlaySize = Mathf.Max(darknessOverlaySize,
            roadMap != null ? roadMap.fieldSize + 20f : 100f);

        darknessTexture = new Texture2D(1, 1);
        darknessTexture.SetPixel(0, 0, Color.white);
        darknessTexture.Apply();
        darknessSprite = Sprite.Create(darknessTexture, new Rect(0f, 0f, 1f, 1f),
            new Vector2(0.5f, 0.5f), 1f);
        darknessOverlay.transform.localScale = new Vector3(overlaySize, overlaySize, 1f);

        darknessRenderer = darknessOverlay.AddComponent<SpriteRenderer>();
        darknessRenderer.sprite = darknessSprite;
        // 建物や敵を含むワールド描画の手前に置き、視界外を確実に隠す。
        darknessRenderer.sortingLayerName = "New Layer 2";
        darknessRenderer.sortingOrder = 1000;
        Shader darknessShader = Shader.Find("Subji/Static Darkness Reveal");
        if (darknessShader != null)
        {
            darknessMaterial = new Material(darknessShader);
            darknessRenderer.material = darknessMaterial;
        }
        UpdateDarknessVisibility();
        UpdateDarknessMaterial();
    }

    void UpdateDarknessMaterial()
    {
        if (darknessMaterial == null)
            return;

        Color appliedDarknessColor = darknessColor;
        appliedDarknessColor.a *= outsideDarkness;
        darknessMaterial.SetColor("_Color", appliedDarknessColor);
        darknessMaterial.SetVector("_PlayerData", new Vector4(transform.position.x,
            transform.position.y, playerVisionRadius, visionGradientWidth));
        darknessMaterial.SetVector("_FlashlightData", new Vector4(isLightOn ? 1f : 0f,
            lightViewDistance, Mathf.Cos(lightViewAngle * 0.5f * Mathf.Deg2Rad), 0f));
        darknessMaterial.SetVector("_FlashlightDirection",
            new Vector4(flashlightDirection.x, flashlightDirection.y, 0f, 0f));

        if (lastPlacedLightRevision == SubjiPlacedLight.Revision)
            return;

        lastPlacedLightRevision = SubjiPlacedLight.Revision;
        int count = 0;
        foreach (SubjiPlacedLight placedLight in SubjiPlacedLight.ActiveLights)
        {
            if (placedLight == null || count >= placedLightShaderData.Length)
                continue;
            Vector3 position = placedLight.transform.position;
            placedLightShaderData[count++] = new Vector4(position.x, position.y,
                placedLight.radius, placedLight.blurWidth);
        }
        darknessMaterial.SetInt("_PlacedLightCount", count);
        darknessMaterial.SetVectorArray("_PlacedLights", placedLightShaderData);
    }

    Texture2D CreateDarknessTexture(int resolution, float overlaySize, float clearRadius,
        float gradientWidth, bool includeLightCone)
    {
        Texture2D texture = new Texture2D(
            resolution, resolution, TextureFormat.RGBA32, false);
        texture.name = includeLightCone
            ? "Runtime Player Light Vision Gradient"
            : "Runtime Player Vision Gradient";
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;

        FillDarknessTexture(texture, resolution, overlaySize, clearRadius,
            gradientWidth, includeLightCone);
        return texture;
    }

    void FillDarknessTexture(Texture2D texture, int resolution, float overlaySize,
        float clearRadius, float gradientWidth, bool includeLightCone)
    {
        if (darknessPixelBuffer == null || darknessPixelBuffer.Length != resolution * resolution)
            darknessPixelBuffer = new Color[resolution * resolution];

        placedLightData.Clear();
        foreach (SubjiPlacedLight placedLight in SubjiPlacedLight.ActiveLights)
        {
            if (placedLight == null)
                continue;
            Vector2 localPosition = darknessOverlay.transform.InverseTransformPoint(
                placedLight.transform.position);
            placedLightData.Add(new Vector3(localPosition.x, localPosition.y,
                Mathf.Max(0.1f, placedLight.radius)));
            placedLightData.Add(new Vector3(placedLight.blurWidth, 0f, 0f));
        }

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float worldX = ((x + 0.5f) / resolution - 0.5f) * overlaySize;
                float worldY = ((y + 0.5f) / resolution - 0.5f) * overlaySize;
                float distance = Mathf.Sqrt(worldX * worldX + worldY * worldY);
                float gradient = Mathf.SmoothStep(0f, 1f,
                    Mathf.InverseLerp(clearRadius, clearRadius + gradientWidth, distance));

                if (includeLightCone && distance <= lightViewDistance)
                {
                    float angle = Mathf.Abs(Mathf.Atan2(worldY, worldX) * Mathf.Rad2Deg);
                    if (angle <= lightViewAngle * 0.5f)
                        gradient = 0f;
                }

                for (int lightIndex = 0; lightIndex < placedLightData.Count; lightIndex += 2)
                {
                    Vector3 light = placedLightData[lightIndex];
                    float blur = Mathf.Max(0.01f, placedLightData[lightIndex + 1].x);
                    float deltaX = worldX - light.x;
                    float deltaY = worldY - light.y;
                    float outerRadius = light.z + blur;
                    float squaredDistance = deltaX * deltaX + deltaY * deltaY;
                    if (squaredDistance >= outerRadius * outerRadius)
                        continue;
                    float lightDistance = Mathf.Sqrt(squaredDistance);
                    float lightGradient = Mathf.SmoothStep(0f, 1f,
                        Mathf.InverseLerp(light.z, outerRadius, lightDistance));
                    gradient = Mathf.Min(gradient, lightGradient);
                }

                darknessPixelBuffer[y * resolution + x] = new Color(
                    darknessColor.r,
                    darknessColor.g,
                    darknessColor.b,
                    gradient * outsideDarkness * darknessColor.a);
            }
        }

        texture.SetPixels(darknessPixelBuffer);
        texture.Apply(false, false);
    }

    void RefreshVisibleDarknessTexture()
    {
        if (darknessTexture == null || lightDarknessTexture == null || darknessOverlay == null)
            return;

        int resolution = darknessTexture.width;
        float overlaySize = Mathf.Max(10f, darknessOverlaySize);
        float clearRadius = Mathf.Max(0.1f, playerVisionRadius);
        float gradientWidth = Mathf.Max(0.1f, visionGradientWidth);
        FillDarknessTexture(isLightOn ? lightDarknessTexture : darknessTexture,
            resolution, overlaySize, clearRadius, gradientWidth, isLightOn);
    }

    static Sprite CreateDarknessSprite(Texture2D texture, int resolution, float overlaySize,
        string spriteName)
    {
        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, resolution, resolution),
            new Vector2(0.5f, 0.5f),
            resolution / overlaySize);
        sprite.name = spriteName;
        return sprite;
    }

    void UpdateSlotLight()
    {
        if (Mouse.current == null)
            return;

        if (itemSlotSelector == null)
            itemSlotSelector = FindFirstObjectByType<ItemSlotSelector>();

        bool lightSlotSelected = itemSlotSelector != null &&
            itemSlotSelector.SelectedIndex == Mathf.Max(1, lightSlotNumber) - 1;
        if (lightSlotSelected && Mouse.current.leftButton.wasPressedThisFrame)
            SetLightOn(true);
        if (lightSlotSelected && Mouse.current.rightButton.wasPressedThisFrame)
            SetLightOn(false);

        if (!isLightOn || darknessOverlay == null || Camera.main == null)
            return;

        Vector2 mouseWorld = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        Vector2 direction = mouseWorld - (Vector2)transform.position;
        if (direction.sqrMagnitude > 0.0001f)
            flashlightDirection = direction.normalized;
    }

    void SetLightOn(bool turnOn)
    {
        isLightOn = turnOn;
        UpdateDarknessMaterial();
    }

    void UpdateDarknessVisibility()
    {
        if (darknessOverlay != null)
            darknessOverlay.SetActive(enableNightVision);
    }

    void OnDestroy()
    {
        if (darknessSprite != null)
            Destroy(darknessSprite);
        if (lightDarknessSprite != null)
            Destroy(lightDarknessSprite);
        if (darknessTexture != null)
            Destroy(darknessTexture);
        if (lightDarknessTexture != null)
            Destroy(lightDarknessTexture);
        if (darknessMaterial != null)
            Destroy(darknessMaterial);
    }

    void UpdateSpeedBoost()
    {
        bool boostKeyPressed = Keyboard.current != null &&
            Keyboard.current[speedBoostKey].isPressed;
        bool wantsToBoost = boostKeyPressed && IsMoving && speedBoostAmount > 0f;

        IsSpeedBoosting = wantsToBoost;

        if (IsSpeedBoosting)
        {
            if (!IsStaminaFrozen)
            {
                speedBoostAmount = Mathf.Max(0f,
                    speedBoostAmount - Time.deltaTime / Mathf.Max(0.1f, speedBoostDuration));
                lastSpeedBoostUseTime = Time.time;
                speedBoostWasUsed = true;
            }

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
        speedBoostGaugeRoot.gameObject.SetActive(
            IsStaminaFrozen || speedBoostWasUsed || speedBoostAmount < 1f);

        SpriteRenderer fillRenderer = speedBoostGaugeFill.GetComponent<SpriteRenderer>();
        if (fillRenderer != null)
            fillRenderer.color = IsStaminaFrozen ? staminaFreezeGaugeColor : speedBoostGaugeColor;

        float width = speedBoostGaugeSize.x * Mathf.Clamp01(speedBoostAmount);
        speedBoostGaugeFill.localScale = new Vector3(width, speedBoostGaugeSize.y, 1f);
        speedBoostGaugeFill.localPosition = new Vector3(
            (width - speedBoostGaugeSize.x) * 0.5f, 0f, -0.01f);
    }

    public void PreventStaminaDrain(float duration, Color gaugeColor)
    {
        speedBoostAmount = 1f;
        speedBoostWasUsed = false;
        lastSpeedBoostUseTime = float.NegativeInfinity;
        staminaFreezeEndTime = Mathf.Max(staminaFreezeEndTime,
            Time.time + Mathf.Max(0f, duration));
        staminaFreezeGaugeColor = gaugeColor;
        UpdateSpeedBoostGauge();
    }

    void LateUpdate()
    {
        if (!Application.isPlaying || playerRenderer == null)
            return;

        if (IsHidden)
            return;

        Collider2D playerCollider = GetComponent<Collider2D>();
        Bounds playerBounds = playerCollider != null ? playerCollider.bounds : playerRenderer.bounds;
        float playerArea = playerBounds.size.x * playerBounds.size.y;
        if (playerArea <= Mathf.Epsilon)
            return;

        foreach (SubjiEnemyChase enemy in SubjiEnemyChase.ActiveEnemies)
        {
            if (enemy == null)
                continue;

            Bounds enemyBounds = enemy.GetContactBounds();
            if (enemyBounds.size.sqrMagnitude <= 0f ||
                GetOverlapArea(playerBounds, enemyBounds) / playerArea < enemyOverlapThreshold)
            {
                continue;
            }

            SubjiGameClearGoal gameEnd = GetComponent<SubjiGameClearGoal>();
            if (gameEnd != null)
                gameEnd.GameOver();
            return;
        }
    }

    static float GetOverlapArea(Bounds a, Bounds b)
    {
        float width = Mathf.Max(0f, Mathf.Min(a.max.x, b.max.x) - Mathf.Max(a.min.x, b.min.x));
        float height = Mathf.Max(0f, Mathf.Min(a.max.y, b.max.y) - Mathf.Max(a.min.y, b.min.y));
        return width * height;
    }

    public void SetHidden(bool hidden)
    {
        IsHidden = hidden;
        movement = Vector2.zero;
        if (rb != null)
            rb.linearVelocity = Vector2.zero;
        if (playerRenderer != null)
            playerRenderer.enabled = !hidden;
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

        enemyRenderer.color = Color.white;
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
        GUI.Label(new Rect(20f, 77f, 190f, 34f), $"登録者: {SubscriberCount}", coordinateStyle);

        GUI.color = new Color(0f, 0f, 0f, 0.7f);
        GUI.Box(new Rect(10f, 126f, 275f, 48f), GUIContent.none);
        GUI.color = Color.white;
        GUI.Label(new Rect(20f, 133f, 255f, 34f), coordinates, coordinateStyle);

        if (Time.unscaledTime < taskCompleteMessageUntil)
        {
            if (taskCompleteStyle == null)
            {
                taskCompleteStyle = new GUIStyle(GUI.skin.label);
                taskCompleteStyle.fontSize = 38;
                taskCompleteStyle.fontStyle = FontStyle.Bold;
                taskCompleteStyle.alignment = TextAnchor.MiddleCenter;
                taskCompleteStyle.normal.textColor = new Color(1f, 0.9f, 0.2f, 1f);
            }

            const float width = 520f;
            const float height = 90f;
            Rect messageRect = new Rect((Screen.width - width) * 0.5f,
                (Screen.height - height) * 0.5f, width, height);
            GUI.color = Color.white;
            GUI.Label(messageRect,
                $"タスク完了  +{lastTaskSubscriberReward}人", taskCompleteStyle);
        }
    }
}
