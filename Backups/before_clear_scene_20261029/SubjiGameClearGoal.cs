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
    private Sprite[] clearSprites;
    private float clearStartedAt;

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
        clearStartedAt = Time.unscaledTime;
        GameObject artwork = Resources.Load<GameObject>("ClearScreenArtwork");
        if (artwork != null)
        {
            SpriteRenderer[] renderers = artwork.GetComponentsInChildren<SpriteRenderer>(true);
            clearSprites = new Sprite[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
                clearSprites[i] = renderers[i].sprite;
        }
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
        using var referenceGui = new SubjiPlayerMovement.ReferenceGuiScope(Screen.width, Screen.height);
        if (!isClear && !isGameOver)
            return;

        int previousDepth = GUI.depth;
        GUI.depth = -10000;

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

        Color previousColor = GUI.color;
        GUI.color = isClear ? Color.black : new Color(0f, 0f, 0f, 0.82f);
        GUI.DrawTexture(new Rect(0f, 0f, referenceGui.Width, referenceGui.Height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        float centerX = referenceGui.Width * 0.5f;
        float centerY = referenceGui.Height * 0.5f;
        if (isClear)
        {
            DrawClearArtwork(referenceGui.Width, referenceGui.Height);
            centerY = referenceGui.Height * 0.22f;
        }
        GUI.Label(new Rect(centerX - 240f, centerY - 130f, 480f, 100f),
            isGameOver ? "GAME OVER" : "CLEAR", clearStyle);

        if (GUI.Button(new Rect(centerX - 110f, centerY + 10f, 220f, 64f),
            "Restart", restartStyle))
        {
            RestartGame();
        }
        GUI.color = previousColor;
        GUI.depth = previousDepth;
    }

    private void DrawClearArtwork(float width, float height)
    {
        if (clearSprites == null || clearSprites.Length < 3)
            return;

        // Repeating fixed lanes keep the centre title readable. Unscaled time also runs while paused.
        float elapsed = Time.unscaledTime - clearStartedAt;
        for (int i = 0; i < 6; i++)
        {
            float age = Mathf.Repeat(elapsed + i * 0.63f, 4.4f);
            float x = width * (i < 3 ? 0.10f + i * 0.13f : 0.64f + (i - 3) * 0.13f);
            float burstY = height * (0.22f + (i % 3) * 0.12f);
            if (age < 1.25f)
            {
                float y = Mathf.Lerp(height * 0.94f, burstY, age / 1.25f);
                DrawArtworkSprite(clearSprites[1], new Rect(x - 9f, y - 36f, 18f, 72f));
            }
            else if (age < 2.8f)
            {
                float progress = (age - 1.25f) / 1.55f;
                float size = height * 0.23f * Mathf.Lerp(0.35f, 1.15f, Mathf.Clamp01(progress * 3f));
                GUI.color = new Color(1f, 1f, 1f, 1f - progress * progress);
                DrawArtworkSprite(clearSprites[2 + i % (clearSprites.Length - 2)],
                    new Rect(x - size * 0.5f, burstY - size * 0.5f, size, size));
                GUI.color = Color.white;
            }
        }

        // Crop the lower half of the original sprite, preserving the castle's proportions.
        Sprite castle = clearSprites[0];
        if (castle != null)
        {
            float castleWidth = Mathf.Min(width * 0.68f, height * 1.45f);
            float halfHeight = castleWidth * castle.rect.height / castle.rect.width * 0.5f;
            DrawArtworkSprite(castle, new Rect((width - castleWidth) * 0.5f,
                height - halfHeight, castleWidth, halfHeight), true);
        }
    }

    private static void DrawArtworkSprite(Sprite sprite, Rect destination, bool upperHalf = false)
    {
        if (sprite == null) return;
        Rect source = sprite.textureRect;
        if (upperHalf)
        {
            source.y += source.height * 0.5f;
            source.height *= 0.5f;
        }
        Texture2D texture = sprite.texture;
        GUI.DrawTextureWithTexCoords(destination, texture, new Rect(source.x / texture.width,
            source.y / texture.height, source.width / texture.width, source.height / texture.height));
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
