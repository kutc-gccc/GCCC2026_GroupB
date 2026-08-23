using UnityEngine;
using UnityEngine.InputSystem;

[ExecuteAlways]
public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float fieldSize = 60f;
    public float borderWidth = 0.3f;
    [Tooltip("60x60の移動可能範囲にプレイヤーを制限します")]
    public bool constrainToField = true;
    [Tooltip("60x60の外周ボーダーを表示します")]
    public bool showFieldBorder = true;

    private Rigidbody2D rb;
    private SpriteRenderer playerRenderer;
    private Vector2 movement;
    private InputAction moveAction;
    private GUIStyle coordinateStyle;
    private Vector2 fieldCenter;

    public bool IsMoving => movement.sqrMagnitude > 0.01f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerRenderer = GetComponent<SpriteRenderer>();
        fieldCenter = transform.position;

        CreateFieldBorder();
        CreateEnemy();

        if (!Application.isPlaying)
            return;

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
        if (!Application.isPlaying)
            fieldCenter = transform.position;

        CreateFieldBorder();
        CreateEnemy();

        if (Application.isPlaying && moveAction != null)
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
    }

    void FixedUpdate()
    {
        if (!Application.isPlaying || rb == null)
            return;

        float playerHalfWidth = playerRenderer != null
            ? playerRenderer.bounds.extents.x
            : 0f;
        float playerHalfHeight = playerRenderer != null
            ? playerRenderer.bounds.extents.y
            : 0f;
        float halfField = fieldSize * 0.5f;
        Vector2 nextPosition = rb.position
            + movement * moveSpeed * Time.fixedDeltaTime;

        if (constrainToField)
        {
            nextPosition.x = Mathf.Clamp(
                nextPosition.x,
                fieldCenter.x - halfField + playerHalfWidth,
                fieldCenter.x + halfField - playerHalfWidth
            );
            nextPosition.y = Mathf.Clamp(
                nextPosition.y,
                fieldCenter.y - halfField + playerHalfHeight,
                fieldCenter.y + halfField - playerHalfHeight
            );
        }

        rb.MovePosition(nextPosition);
        rb.linearVelocity = Vector2.zero;
    }

    void CreateFieldBorder()
    {
        const string borderName = "60x60 Field Border";
        GameObject borderObject = GameObject.Find(borderName);

        if (borderObject == null && !showFieldBorder)
        {
            Transform[] sceneTransforms = FindObjectsByType<Transform>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (Transform sceneTransform in sceneTransforms)
            {
                if (sceneTransform.name == borderName)
                {
                    borderObject = sceneTransform.gameObject;
                    break;
                }
            }
        }

        if (!showFieldBorder)
        {
            if (borderObject != null)
            {
                if (Application.isPlaying)
                    Destroy(borderObject);
                else
                    DestroyImmediate(borderObject);
            }
            return;
        }

        if (borderObject == null)
            borderObject = new GameObject(borderName);

        LineRenderer border = borderObject.GetComponent<LineRenderer>();

        if (border == null)
            border = borderObject.AddComponent<LineRenderer>();

        float halfField = fieldSize * 0.5f;
        border.loop = true;
        border.useWorldSpace = true;
        border.positionCount = 4;
        border.startWidth = borderWidth;
        border.endWidth = borderWidth;
        border.startColor = Color.black;
        border.endColor = Color.black;
        border.sortingOrder = -1;
        if (border.sharedMaterial == null)
            border.sharedMaterial = new Material(Shader.Find("Sprites/Default"));

        border.SetPosition(0, new Vector3(fieldCenter.x - halfField, fieldCenter.y - halfField, 0f));
        border.SetPosition(1, new Vector3(fieldCenter.x - halfField, fieldCenter.y + halfField, 0f));
        border.SetPosition(2, new Vector3(fieldCenter.x + halfField, fieldCenter.y + halfField, 0f));
        border.SetPosition(3, new Vector3(fieldCenter.x + halfField, fieldCenter.y - halfField, 0f));
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

        EnemyChase enemyChase = enemy.GetComponent<EnemyChase>();

        if (enemyChase == null)
            enemyChase = enemy.AddComponent<EnemyChase>();

        enemyChase.player = transform;
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
        GUI.Box(new Rect(10f, 10f, 275f, 48f), GUIContent.none);
        GUI.color = Color.white;
        GUI.Label(new Rect(20f, 17f, 255f, 34f), coordinates, coordinateStyle);
    }
}
