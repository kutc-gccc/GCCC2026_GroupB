using UnityEngine;

public class EnemyChase : MonoBehaviour
{
    [Header("Detection Settings")]
    [Tooltip("Detection radius while the player is moving")]
    [Min(0.1f)] public float movingDetectionRadius = 5f;

    [Tooltip("Detection radius while the player is stopped")]
    [Min(0.1f)] public float idleDetectionRadius = 1.5f;

    [Tooltip("Enemy chase speed")]
    [Min(0f)] public float chaseSpeed = 3f;

    [Header("Target")]
    public Transform player;

    [Header("Detection Circle Appearance")]
    [Range(0.01f, 0.5f)] public float circleWidth = 0.12f;

    private LineRenderer detectionCircle;
    private PlayerMovement playerMovement;
    private const int CircleSegments = 96;

    void Start()
    {
        if (player != null)
            playerMovement = player.GetComponent<PlayerMovement>();

        CreateDetectionCircle();
    }

    void Update()
    {
        if (player == null || detectionCircle == null)
            return;

        bool playerIsMoving = playerMovement != null && playerMovement.IsMoving;
        float activeRadius = playerIsMoving
            ? movingDetectionRadius
            : idleDetectionRadius;

        UpdateDetectionCircle(activeRadius);

        float distance = Vector2.Distance(transform.position, player.position);

        if (distance <= activeRadius)
        {
            transform.position = Vector2.MoveTowards(
                transform.position,
                player.position,
                chaseSpeed * Time.deltaTime
            );
        }
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
            Vector3 point = player.position + new Vector3(
                Mathf.Cos(angle) * radius,
                Mathf.Sin(angle) * radius,
                0f
            );
            detectionCircle.SetPosition(i, point);
        }
    }
}
