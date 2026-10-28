using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public sealed class GameQuitConfirmation : MonoBehaviour
{
    private bool confirming;
    private float previousTimeScale;
    private EventSystem pausedEventSystem;
    private GUIStyle hintStyle;
    private GUIStyle questionStyle;
    private GUIStyle buttonStyle;
    private Font japaneseFont;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        var root = new GameObject("Game Quit Confirmation");
        DontDestroyOnLoad(root);
        root.AddComponent<GameQuitConfirmation>();
    }

    private void Update()
    {
        if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame) return;
        if (confirming)
        {
            CancelQuit();
            return;
        }
        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        pausedEventSystem = EventSystem.current;
        if (pausedEventSystem != null && pausedEventSystem.enabled)
            pausedEventSystem.enabled = false;
        else
            pausedEventSystem = null;
        confirming = true;
    }

    private void CancelQuit()
    {
        confirming = false;
        Time.timeScale = previousTimeScale;
        if (pausedEventSystem != null) pausedEventSystem.enabled = true;
        pausedEventSystem = null;
    }

    private void OnGUI()
    {
        using var referenceGui = new SubjiPlayerMovement.ReferenceGuiScope(Screen.width, Screen.height);
        GUI.depth = -10000;
        if (hintStyle == null)
        {
            japaneseFont = Font.CreateDynamicFontFromOSFont(new[] { "Yu Gothic", "Meiryo", "Arial" }, 24);
            hintStyle = new GUIStyle(GUI.skin.label) { font = japaneseFont, fontSize = 24 };
            hintStyle.normal.textColor = Color.white;
            questionStyle = new GUIStyle(hintStyle) { alignment = TextAnchor.MiddleCenter, fontSize = 32 };
            buttonStyle = new GUIStyle(GUI.skin.button) { font = japaneseFont, fontSize = 28 };
        }
        GUI.color = new Color(0f, 0f, 0f, 0.75f);
        GUI.DrawTexture(new Rect(10f, 10f, 310f, 44f), Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUI.Label(new Rect(20f, 14f, 300f, 36f), "Escでゲームを閉じる", hintStyle);
        if (!confirming) return;
        GUI.color = new Color(0f, 0f, 0f, 0.7f);
        GUI.DrawTexture(new Rect(0f, 0f, referenceGui.Width, referenceGui.Height), Texture2D.whiteTexture);
        GUI.color = Color.white;
        Rect dialog = new Rect((referenceGui.Width - 560f) * 0.5f,
            (referenceGui.Height - 220f) * 0.5f, 560f, 220f);
        GUI.ModalWindow(GetInstanceID(), dialog, DrawConfirmation, "");
    }

    private void DrawConfirmation(int windowId)
    {
        GUI.Label(new Rect(20f, 30f, 520f, 70f), "ゲームを終了しますか？", questionStyle);
        if (GUI.Button(new Rect(65f, 125f, 190f, 60f), "はい", buttonStyle))
        {
            CancelQuit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
        if (GUI.Button(new Rect(305f, 125f, 190f, 60f), "いいえ", buttonStyle))
            CancelQuit();
    }

    private void OnDestroy()
    {
        if (confirming) CancelQuit();
        if (japaneseFont != null) Destroy(japaneseFont);
    }
}
