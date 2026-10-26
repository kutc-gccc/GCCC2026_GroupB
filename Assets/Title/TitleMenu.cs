using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public sealed class TitleMenu : MonoBehaviour
{
    [SerializeField] private Texture2D background;
    [SerializeField] private Canvas titleCanvas;
    [SerializeField] private RawImage backgroundImage;
    [SerializeField] private TMP_FontAsset menuFont;

    private void Awake()
    {
        var canvasObject = titleCanvas != null ? titleCanvas.gameObject
            : new GameObject("Title Canvas", typeof(Canvas));
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObject.AddComponent<GraphicRaycaster>();
        var scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 0.5f;

        var imageObject = backgroundImage != null ? backgroundImage.gameObject
            : new GameObject("Background", typeof(RectTransform), typeof(RawImage));
        imageObject.transform.SetParent(canvas.transform, false);
        var image = imageObject.GetComponent<RawImage>();
        image.texture = background;
        image.raycastTarget = false;
        var fit = imageObject.AddComponent<AspectRatioFitter>();
        fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fit.aspectRatio = 16f / 9f;

        var buttonObject = new GameObject("Start Button", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(canvas.transform, false);
        var rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0, -290);
        rect.sizeDelta = new Vector2(300, 70);
        buttonObject.GetComponent<Image>().color = new Color(0.12f, 0.08f, 0.25f, 0.95f);
        var button = buttonObject.GetComponent<Button>();
        button.onClick.AddListener(() =>
        {
            buttonObject.SetActive(false);
            ShowTutorialChoice(canvas.transform);
        });

        var labelObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
        labelObject.transform.SetParent(buttonObject.transform, false);
        var labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
        var label = labelObject.GetComponent<Text>();
        label.text = "START";
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = 36;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;
        label.raycastTarget = false;

        var cameraObject = new GameObject("Title Camera", typeof(Camera));
        var titleCamera = cameraObject.GetComponent<Camera>();
        titleCamera.clearFlags = CameraClearFlags.SolidColor;
        titleCamera.backgroundColor = Color.black;
        titleCamera.cullingMask = 0;

        var events = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        events.GetComponent<EventSystem>().SetSelectedGameObject(buttonObject);
    }

    private void ShowTutorialChoice(Transform parent)
    {
        var panel = new GameObject("Tutorial Choice", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(parent, false);
        panel.GetComponent<RectTransform>().sizeDelta = new Vector2(720, 320);
        panel.GetComponent<Image>().color = new Color(0.08f, 0.05f, 0.16f, 0.98f);
        var prompt = new GameObject("Prompt", typeof(RectTransform), typeof(TextMeshProUGUI));
        prompt.transform.SetParent(panel.transform, false);
        var text = prompt.GetComponent<TextMeshProUGUI>();
        text.font = menuFont;
        text.text = "チュートリアルをプレイしますか？";
        text.fontSize = 32;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        text.rectTransform.sizeDelta = new Vector2(680, 80);
        text.rectTransform.anchoredPosition = new Vector2(0, 100);
        bool loading = false;
        var play = CreateChoiceButton(panel.transform, "チュートリアルをプレイ", 10);
        var skip = CreateChoiceButton(panel.transform, "スキップして本編へ", -90);
        void StartGame(bool tutorial)
        {
            if (loading) return;
            loading = true;
            play.interactable = skip.interactable = false;
            JikkenCommentStream.ContinueTutorialFromTitle = tutorial;
            SceneManager.LoadScene(tutorial ? "Tutorial" : "jikkennsupace");
        }
        play.onClick.AddListener(() => StartGame(true));
        skip.onClick.AddListener(() => StartGame(false));
        EventSystem.current.SetSelectedGameObject(play.gameObject);
    }

    private Button CreateChoiceButton(Transform parent, string caption, float y)
    {
        var go = new GameObject(caption, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchoredPosition = new Vector2(0, y);
        rect.sizeDelta = new Vector2(580, 75);
        go.GetComponent<Image>().color = new Color(0.22f, 0.14f, 0.38f);
        var label = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        label.transform.SetParent(go.transform, false);
        var text = label.GetComponent<TextMeshProUGUI>();
        text.font = menuFont;
        text.text = caption;
        text.fontSize = 30;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        text.rectTransform.anchorMin = Vector2.zero;
        text.rectTransform.anchorMax = Vector2.one;
        text.rectTransform.offsetMin = text.rectTransform.offsetMax = Vector2.zero;
        return go.GetComponent<Button>();
    }
}
