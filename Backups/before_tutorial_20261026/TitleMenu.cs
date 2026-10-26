using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class TitleMenu : MonoBehaviour
{
    [SerializeField] private Texture2D background;
    [SerializeField] private Canvas titleCanvas;
    [SerializeField] private RawImage backgroundImage;

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
            button.interactable = false;
            SceneManager.LoadScene("jikkennsupace");
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
}
