using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class SubjiClearScreen : MonoBehaviour
{
    [Header("花火（シーン上の位置が開く場所・サイズになります）")]
    public RawImage[] fireworks;
    public Sprite launchSprite;
    public Sprite[] burstSprites;
    [Min(0.1f)] public float launchSeconds = 1.25f;
    [Min(0.1f)] public float burstSeconds = 1.55f;
    [Min(0f)] public float pauseSeconds = 1.6f;
    public float launchDistance = 550f;
    public Vector2 launchSize = new Vector2(18f, 72f);
    public string restartScene = "Title";

    private Vector2[] positions;
    private Vector2[] sizes;
    private Color[] colors;
    private float startedAt;

    private void Awake()
    {
        Time.timeScale = 1f;
        startedAt = Time.unscaledTime;
        positions = new Vector2[fireworks.Length];
        sizes = new Vector2[fireworks.Length];
        colors = new Color[fireworks.Length];
        for (int i = 0; i < fireworks.Length; i++)
        {
            if (fireworks[i] == null) continue;
            positions[i] = fireworks[i].rectTransform.anchoredPosition;
            sizes[i] = fireworks[i].rectTransform.sizeDelta;
            colors[i] = fireworks[i].color;
        }
    }

    private void Update()
    {
        float cycle = Mathf.Max(0.1f, launchSeconds + burstSeconds + pauseSeconds);
        for (int i = 0; i < fireworks.Length; i++)
        {
            RawImage image = fireworks[i];
            if (image == null) continue;
            float age = Mathf.Repeat(Time.unscaledTime - startedAt + i * 0.63f, cycle);
            bool launching = age < launchSeconds;
            float progress = Mathf.Clamp01((age - launchSeconds) / Mathf.Max(0.1f, burstSeconds));
            Sprite sprite = launching ? launchSprite :
                burstSprites != null && burstSprites.Length > 0 ? burstSprites[i % burstSprites.Length] : null;
            image.enabled = sprite != null && age < launchSeconds + burstSeconds;
            if (!image.enabled) continue;
            SetSprite(image, sprite);
            image.rectTransform.anchoredPosition = positions[i] + (launching
                ? Vector2.down * launchDistance * (1f - age / Mathf.Max(0.1f, launchSeconds)) : Vector2.zero);
            image.rectTransform.sizeDelta = launching ? launchSize :
                sizes[i] * Mathf.Lerp(0.35f, 1.15f, Mathf.Clamp01(progress * 3f));
            Color color = colors[i];
            color.a *= launching ? 1f : 1f - progress * progress;
            image.color = color;
        }
    }

    public static void SetSprite(RawImage image, Sprite sprite)
    {
        Rect rect = sprite.textureRect;
        image.texture = sprite.texture;
        image.uvRect = new Rect(rect.x / sprite.texture.width, rect.y / sprite.texture.height,
            rect.width / sprite.texture.width, rect.height / sprite.texture.height);
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(restartScene);
    }
}
