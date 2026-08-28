using UnityEngine;

public class SubjiEnemySpawner : MonoBehaviour
{
    public enum SpawnMode
    {
        RandomOnRoad,
        FixedPoints,
        RandomOnStage
    }

    [Header("出現タイマー")]
    [Tooltip("敵が追加される間隔（秒）")]
    [Min(0.1f)] public float spawnInterval = 10f;

    [Header("出現場所")]
    [Tooltip("ランダム道路上、または指定地点から選びます")]
    public SpawnMode spawnMode = SpawnMode.RandomOnRoad;
    [Tooltip("Fixed Pointsを選んだ時に順番に使用する座標です")]
    public Vector2[] fixedSpawnPoints =
    {
        new Vector2(-20f, -20f),
        new Vector2(20f, 20f)
    };
    [Tooltip("指定地点が道路外なら最寄りの道路へ補正します")]
    public bool snapFixedPointsToRoad = true;

    [Header("敵の設定")]
    [Tooltip("空欄ならシーン内のEnemyをひな形にします")]
    public SubjiEnemyChase enemyTemplate;
    [Tooltip("同時に存在できる敵の最大数。0なら無制限です")]
    [Min(0)] public int maximumEnemies = 0;

    [Header("追加される敵の個体差")]
    [Tooltip("オンにすると追加される敵ごとに速度と行動タイプを変えます")]
    public bool randomizeEnemyVariation = false;
    [Tooltip("発見するまで動かない個体になる確率")]
    [Range(0f, 1f)] public float waitingEnemyChance = 0.2f;
    [Tooltip("速度へ掛ける倍率の最小値")]
    [Min(0f)] public float minimumSpeedMultiplier = 0.75f;
    [Tooltip("速度へ掛ける倍率の最大値")]
    [Min(0f)] public float maximumSpeedMultiplier = 1.5f;

    private SubjiRoadMap roadMap;
    private Transform player;
    private float remainingTime;
    private int fixedPointIndex;
    private int spawnedCount;
    private GUIStyle timerStyle;

    private void Start()
    {
        roadMap = GetComponent<SubjiRoadMap>();
        player = FindFirstObjectByType<SubjiPlayerMovement>()?.transform;
        if (enemyTemplate == null)
            enemyTemplate = FindFirstObjectByType<SubjiEnemyChase>();
        remainingTime = spawnInterval;
    }

    private void Update()
    {
        if (roadMap == null || player == null || enemyTemplate == null)
            return;

        remainingTime -= Time.deltaTime;
        if (remainingTime > 0f)
            return;

        remainingTime += spawnInterval;
        if (remainingTime <= 0f)
            remainingTime = spawnInterval;

        if (maximumEnemies <= 0 || FindObjectsByType<SubjiEnemyChase>(FindObjectsSortMode.None).Length < maximumEnemies)
            SpawnEnemy();
    }

    private void SpawnEnemy()
    {
        Vector2 spawnPosition = GetSpawnPosition();
        GameObject enemy = new GameObject($"Enemy {++spawnedCount}");
        SpriteRenderer sourceRenderer = enemyTemplate.GetComponent<SpriteRenderer>();
        SpriteRenderer renderer = enemy.AddComponent<SpriteRenderer>();
        if (sourceRenderer != null)
        {
            renderer.sprite = sourceRenderer.sprite;
            renderer.sharedMaterial = sourceRenderer.sharedMaterial;
            renderer.color = sourceRenderer.color;
            renderer.sortingOrder = sourceRenderer.sortingOrder;
            enemy.transform.localScale = enemyTemplate.transform.localScale;
        }

        SubjiEnemyChase chase = enemy.AddComponent<SubjiEnemyChase>();
        chase.movementType = enemyTemplate.movementType;
        chase.spriteResourcePath = enemyTemplate.spriteResourcePath;
        chase.visualScale = enemyTemplate.visualScale;
        chase.collisionSize = enemyTemplate.collisionSize;
        chase.collisionOffset = enemyTemplate.collisionOffset;
        chase.patrolSpeed = enemyTemplate.patrolSpeed;
        chase.minimumPatrolWait = enemyTemplate.minimumPatrolWait;
        chase.maximumPatrolWait = enemyTemplate.maximumPatrolWait;
        chase.movingDetectionRadius = enemyTemplate.movingDetectionRadius;
        chase.idleDetectionRadius = enemyTemplate.idleDetectionRadius;
        chase.chaseSpeed = enemyTemplate.chaseSpeed;
        chase.chaseMemorySeconds = enemyTemplate.chaseMemorySeconds;
        chase.circleWidth = enemyTemplate.circleWidth;
        chase.useTemporaryVisionCone = enemyTemplate.useTemporaryVisionCone;
        chase.temporaryVisionAngle = enemyTemplate.temporaryVisionAngle;
        chase.temporaryVisionDistance = enemyTemplate.temporaryVisionDistance;
        chase.player = player;
        chase.roadMap = roadMap;
        chase.ApplyAppearanceAndCollider();

        if (randomizeEnemyVariation)
        {
            chase.movementType = Random.value < waitingEnemyChance
                ? SubjiEnemyChase.MovementType.WaitUntilPlayerFound
                : SubjiEnemyChase.MovementType.PatrolAndChase;
            float multiplier = Random.Range(minimumSpeedMultiplier,
                Mathf.Max(minimumSpeedMultiplier, maximumSpeedMultiplier));
            chase.patrolSpeed *= multiplier;
            chase.chaseSpeed *= multiplier;
        }

        Vector2 extents = renderer.bounds.extents;
        enemy.transform.position = roadMap.GetClosestPointOnRoad(spawnPosition, extents);
    }

    private Vector2 GetSpawnPosition()
    {
        if (spawnMode == SpawnMode.FixedPoints && fixedSpawnPoints != null && fixedSpawnPoints.Length > 0)
        {
            Vector2 point = fixedSpawnPoints[fixedPointIndex];
            fixedPointIndex = (fixedPointIndex + 1) % fixedSpawnPoints.Length;
            return snapFixedPointsToRoad && roadMap.restrictMovementToRoads
                ? roadMap.GetClosestPointOnRoad(point, Vector2.zero)
                : point;
        }

        if (spawnMode == SpawnMode.RandomOnStage || !roadMap.restrictMovementToRoads)
            return roadMap.GetRandomPointOnRoad();

        return roadMap.GetRandomPointOnRoad();
    }

    private void OnGUI()
    {
        if (!Application.isPlaying)
            return;

        if (timerStyle == null)
        {
            timerStyle = new GUIStyle(GUI.skin.label);
            timerStyle.fontSize = 28;
            timerStyle.fontStyle = FontStyle.Bold;
            timerStyle.alignment = TextAnchor.MiddleCenter;
            timerStyle.normal.textColor = Color.white;
        }

        GUI.color = new Color(0f, 0f, 0f, 0.75f);
        GUI.Box(new Rect(10f, 10f, 210f, 52f), GUIContent.none);
        GUI.color = Color.white;
        GUI.Label(new Rect(15f, 14f, 200f, 42f), $"NEXT: {Mathf.CeilToInt(Mathf.Max(0f, remainingTime))}", timerStyle);
    }
}
