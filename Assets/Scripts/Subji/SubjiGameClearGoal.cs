using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 仮のゲームクリア地点とCLEAR画面を管理します。
/// このコンポーネントを外せば、ゴールとクリア処理をまとめて撤去できます。
/// </summary>
public class SubjiGameClearGoal : MonoBehaviour
{
    [Header("仮のゲームクリア地点")]
    [Tooltip("赤いゴールマークを置くワールド座標")]
    public Vector2 goalPosition = new Vector2(0f, -13f);
    [Tooltip("ゴールマークの一辺の大きさ")]
    [Min(0.1f)] public float goalSize = 1.2f;
    public Color goalColor = new Color(1f, 0.08f, 0.08f, 1f);

    private GameObject goalMarker;
    private SpriteRenderer goalRenderer;
    private Sprite markerSprite;
    private Texture2D markerTexture;
    private GUIStyle clearStyle;
    private GUIStyle restartStyle;
    private bool isClear;
    private bool isGameOver;

    public bool IsClear => isClear;
    public bool IsGameOver => isGameOver;
    public Transform GoalTransform => goalMarker != null ? goalMarker.transform : null;
    public event Action GoalReached;

    private void Awake()
    {
        CreateGoalMarker();
        SetGoalActive(false);
    }

    private void CreateGoalMarker()
    {
        markerTexture = new Texture2D(1, 1);
        markerTexture.name = "Runtime Clear Goal Texture";
        markerTexture.SetPixel(0, 0, Color.white);
        markerTexture.Apply();

        markerSprite = Sprite.Create(markerTexture, new Rect(0f, 0f, 1f, 1f),
            new Vector2(0.5f, 0.5f), 1f);
        markerSprite.name = "Runtime Clear Goal Sprite";

        goalMarker = new GameObject("Temporary Clear Goal (0, -13)");
        goalMarker.transform.position = new Vector3(goalPosition.x, goalPosition.y, 0f);
        goalMarker.transform.localScale = new Vector3(goalSize, goalSize, 1f);
        goalMarker.transform.rotation = Quaternion.Euler(0f, 0f, 45f);

        goalRenderer = goalMarker.AddComponent<SpriteRenderer>();
        goalRenderer.sprite = markerSprite;
        goalRenderer.color = goalColor;
        goalRenderer.sortingOrder = 8;

        BoxCollider2D triggerCollider = goalMarker.AddComponent<BoxCollider2D>();
        triggerCollider.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (goalMarker != null && other.gameObject == goalMarker)
            ReachGoal();
    }

    public void SetGoalActive(bool active)
    {
        if (goalMarker != null && !isClear && !isGameOver)
            goalMarker.SetActive(active);
    }

    public void ReachGoal()
    {
        if (isClear || isGameOver || goalMarker == null || !goalMarker.activeSelf)
            return;

        isClear = true;
        GoalReached?.Invoke();
        StopGame();
    }

    public void GameOver()
    {
        if (isClear || isGameOver)
            return;

        isGameOver = true;
        StopGame();
    }

    private void StopGame()
    {

        SubjiPlayerMovement movement = GetComponent<SubjiPlayerMovement>();
        if (movement != null)
            movement.enabled = false;

        Time.timeScale = 0f;
    }

    private void RestartGame()
    {
        Time.timeScale = 1f;
        Scene activeScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(activeScene.buildIndex);
    }

    private void OnGUI()
    {
        if (!isClear && !isGameOver)
            return;

        if (clearStyle == null)
        {
            clearStyle = new GUIStyle(GUI.skin.label);
            clearStyle.fontSize = 64;
            clearStyle.fontStyle = FontStyle.Bold;
            clearStyle.alignment = TextAnchor.MiddleCenter;
            clearStyle.normal.textColor = Color.white;

            restartStyle = new GUIStyle(GUI.skin.button);
            restartStyle.fontSize = 28;
            restartStyle.fontStyle = FontStyle.Bold;
        }

        GUI.color = new Color(0f, 0f, 0f, 0.82f);
        GUI.Box(new Rect(0f, 0f, Screen.width, Screen.height), GUIContent.none);
        GUI.color = Color.white;

        float centerX = Screen.width * 0.5f;
        float centerY = Screen.height * 0.5f;
        GUI.Label(new Rect(centerX - 240f, centerY - 130f, 480f, 100f),
            isGameOver ? "GAME OVER" : "CLEAR", clearStyle);

        if (GUI.Button(new Rect(centerX - 110f, centerY + 10f, 220f, 64f),
            "Restart", restartStyle))
        {
            RestartGame();
        }
    }

    private void OnDestroy()
    {
        if (goalMarker != null)
            Destroy(goalMarker);
        if (markerSprite != null)
            Destroy(markerSprite);
        if (markerTexture != null)
            Destroy(markerTexture);
    }
}
