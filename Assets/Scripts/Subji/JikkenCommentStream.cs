using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

[ExecuteAlways]
public class JikkenCommentStream : MonoBehaviour
{
    [Serializable]
    public class TaskComment
    {
        public string commentText;
        [TextArea] public string taskText;
    }

    private class AcceptedTask
    {
        public RectTransform rect;
        public TextMeshProUGUI label;
        public Vector2 destination;
    }

    [Header("通常コメント")]
    public string[] words = { "ああああ", "いいいいい", "ううううう" };

    [Header("タスク付きコメント")]
    public TaskComment[] taskComments =
    {
        new TaskComment
        {
            commentText = "◆ 座標のX:2、Y:2を調査して！",
            taskText = "タスク：指定地点へ移動\nX:2、Y:2のところへ進め"
        }
    };
    [Range(0f, 100f)] public float taskCommentChance = 25f;

    [Header("タスク目的地")]
    [Tooltip("目的地へこの距離まで近づくとタスク完了になります")]
    [Min(0.1f)] public float taskArrivalDistance = 1f;

    [Header("表示設定")]
    public TMP_FontAsset fontAsset;
    [Min(0.05f)] public float spawnInterval = 1f;
    [Min(1)] public int maximumComments = 8;
    [Min(1f)] public float fontSize = 32f;
    [Min(1f)] public float commentHeight = 58f;
    public Color normalTextColor = Color.white;
    public Color taskTextColor = new Color(1f, 0.25f, 0.25f, 1f);
    public Color panelColor = new Color(0.03f, 0.32f, 0.45f, 0.97f);

    [Header("発生コメント表示")]
    [Min(0.1f)] public float popupDuration = 3f;
    public Vector2 popupSize = new Vector2(650f, 130f);
    public Vector2 popupPosition = new Vector2(0f, -8f);
    public Color popupTextColor = Color.black;

    [Header("枠のレイアウト")]
    [Tooltip("画面全体の端からの余白（px）")]
    [Min(0f)] public float screenMargin = 8f;
    [Tooltip("左のコメント枠。X/Y=左下位置、W/H=幅と高さ（0〜1）")]
    public Rect commentPanelRect = new Rect(0.015f, 0.02f, 0.475f, 0.96f);
    [Tooltip("右のタスク枠。X/Y=左下位置、W/H=幅と高さ（0〜1）")]
    public Rect taskPanelRect = new Rect(0.51f, 0.02f, 0.475f, 0.96f);
    [Tooltip("枠内側の文字余白（px）")]
    [Min(0f)] public float panelContentPadding = 28f;

    [Header("編集画面のプレビュ位置")]
    [Tooltip("再生していない時のUI配置座標。ゲーム本体から離して置けます")]
    public Vector3 editorPreviewPosition = new Vector3(75f, 0f, 0f);
    [Tooltip("編集時プレビュの縮小率")]
    [Min(0.0001f)] public float editorPreviewScale = 0.01f;

    private readonly List<RectTransform> comments = new List<RectTransform>();
    private readonly List<AcceptedTask> acceptedTasks = new List<AcceptedTask>();
    private GameObject uiRoot;
    private GameObject popupRoot;
    private TextMeshProUGUI popupLabel;
    private float popupTimer;
    private RectTransform commentContent;
    private RectTransform taskContent;
    private float timer;
    private bool isOpen;
    private SubjiRoadMap roadMap;
    private Transform player;
    private SubjiPlayerMovement playerMovement;
    private AcceptedTask selectedTask;

    private void Awake()
    {
        roadMap = FindFirstObjectByType<SubjiRoadMap>();
        playerMovement = FindFirstObjectByType<SubjiPlayerMovement>();
        if (playerMovement != null)
            player = playerMovement.transform;
        EnsureUiExists(false);
        if (Application.isPlaying)
        {
            uiRoot.SetActive(false);
            CreatePopupUi();
        }
    }

    private void OnEnable()
    {
        if (!Application.isPlaying)
            EnsureUiExists(false);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying || !isActiveAndEnabled)
            return;

        UnityEditor.EditorApplication.delayCall += RebuildEditorPreview;
    }

    private void RebuildEditorPreview()
    {
        if (this == null || Application.isPlaying || !isActiveAndEnabled)
            return;
        // 既存のRectTransformは上書きしない。
        // Play Modeへの切り替え直前にOnValidateが呼ばれても、
        // Sceneビューで調整した枠の位置とサイズを保持する。
        EnsureUiExists(false);
    }
#endif

    private void Update()
    {
        if (!Application.isPlaying)
            return;

        UpdateActiveTask();

        if (isOpen && Mouse.current != null &&
            Mouse.current.rightButton.wasPressedThisFrame)
        {
            CloseCommentScreen();
        }

        if (popupRoot != null && popupRoot.activeSelf)
        {
            if (isOpen)
            {
                popupRoot.SetActive(false);
                popupTimer = 0f;
            }

            popupTimer -= Time.deltaTime;
            if (popupTimer <= 0f)
                popupRoot.SetActive(false);
        }

        timer += Time.deltaTime;
        if (timer >= spawnInterval)
        {
            timer = 0f;
            SpawnComment();
        }
    }

    public void ToggleCommentScreen()
    {
        if (uiRoot == null)
            EnsureUiExists(false);

        isOpen = !isOpen;
        uiRoot.SetActive(isOpen);

        if (isOpen && popupRoot != null)
        {
            popupRoot.SetActive(false);
            popupTimer = 0f;
        }
    }

    public void OpenCommentScreen()
    {
        if (uiRoot == null)
            EnsureUiExists(false);

        isOpen = true;
        uiRoot.SetActive(true);

        if (popupRoot != null)
        {
            popupRoot.SetActive(false);
            popupTimer = 0f;
        }
    }

    public void CloseCommentScreen()
    {
        isOpen = false;
        if (uiRoot != null)
            uiRoot.SetActive(false);
    }

    private void CreatePopupUi()
    {
        GameObject canvasObject = new GameObject("Comment Popup Canvas", typeof(RectTransform),
            typeof(Canvas), typeof(CanvasScaler));
        canvasObject.transform.SetParent(transform, false);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 110;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        popupRoot = new GameObject("Generated Comment Popup", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(Image));
        popupRoot.transform.SetParent(canvasObject.transform, false);
        RectTransform popupRect = popupRoot.GetComponent<RectTransform>();
        popupRect.anchorMin = new Vector2(0.5f, 1f);
        popupRect.anchorMax = new Vector2(0.5f, 1f);
        popupRect.pivot = new Vector2(0.5f, 1f);
        popupRect.sizeDelta = popupSize;
        popupRect.anchoredPosition = popupPosition;

        Sprite[] popupSprites = Resources.LoadAll<Sprite>("comment");
        Image image = popupRoot.GetComponent<Image>();
        if (popupSprites.Length > 0)
        {
            image.sprite = popupSprites[0];
            image.type = Image.Type.Sliced;
            image.preserveAspect = false;
        }
        else
        {
            image.color = new Color(0f, 0f, 0f, 0.75f);
        }

        GameObject textObject = CreateTextObject("Popup Comment Text", popupRoot.transform,
            string.Empty, fontSize, popupTextColor);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        Stretch(textRect, 0f, 0f, 1f, 1f, 24f);
        popupLabel = textObject.GetComponent<TextMeshProUGUI>();
        popupLabel.textWrappingMode = TextWrappingModes.Normal;
        popupLabel.raycastTarget = false;
        popupRoot.SetActive(false);
    }

    private void ShowPopup(string message, Color color)
    {
        if (isOpen || popupRoot == null || popupLabel == null)
            return;

        popupLabel.text = message;
        popupLabel.color = color;
        popupTimer = popupDuration;
        popupRoot.SetActive(true);
    }

    private void CreateRuntimeUi()
    {
        EnsureEventSystem();
        uiRoot = new GameObject("Comment Task Canvas", typeof(RectTransform),
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        uiRoot.transform.SetParent(transform, false);

        Canvas canvas = uiRoot.GetComponent<Canvas>();
        canvas.sortingOrder = 100;

        CanvasScaler scaler = uiRoot.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        CreateBackdrop(uiRoot.transform);
        RectTransform commentPanel = CreatePanel(
            uiRoot.transform, "コメント", commentPanelRect);
        RectTransform taskPanel = CreatePanel(
            uiRoot.transform, "タスク", taskPanelRect);
        commentContent = CreateContent(commentPanel, "Comment Content");
        taskContent = CreateContent(taskPanel, "Task Content");
        ConfigureCanvasForCurrentMode();
    }

    private void EnsureUiExists(bool applyInspectorLayout)
    {
        Transform existingCanvas = transform.Find("Comment Task Canvas");
        if (existingCanvas == null)
        {
            CreateRuntimeUi();
        }
        else
        {
            uiRoot = existingCanvas.gameObject;
            Transform commentPanel = existingCanvas.Find("コメント Panel");
            Transform taskPanel = existingCanvas.Find("タスク Panel");
            commentContent = commentPanel != null
                ? commentPanel.Find("Comment Content") as RectTransform
                : null;
            taskContent = taskPanel != null
                ? taskPanel.Find("Task Content") as RectTransform
                : null;

            if (commentContent == null || taskContent == null)
            {
                DestroyGeneratedObject(uiRoot);
                CreateRuntimeUi();
            }
            else
            {
                SetHideFlagsRecursively(existingCanvas, HideFlags.None);
                if (applyInspectorLayout)
                    ApplySavedLayout(existingCanvas);
            }
        }

        uiRoot.SetActive(true);
        ConfigureCanvasForCurrentMode();

#if UNITY_EDITOR
        if (!Application.isPlaying && gameObject.scene.IsValid())
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif
    }

    private void ConfigureCanvasForCurrentMode()
    {
        if (uiRoot == null)
            return;

        Canvas canvas = uiRoot.GetComponent<Canvas>();
        RectTransform canvasRect = uiRoot.GetComponent<RectTransform>();
        if (canvas == null || canvasRect == null)
            return;

        if (Application.isPlaying)
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasRect.localPosition = Vector3.zero;
            canvasRect.localRotation = Quaternion.identity;
            canvasRect.localScale = Vector3.one;
        }
        else
        {
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = null;
            canvasRect.sizeDelta = new Vector2(1920f, 1080f);
            canvasRect.localPosition = editorPreviewPosition;
            canvasRect.localRotation = Quaternion.identity;
            canvasRect.localScale = Vector3.one * editorPreviewScale;
        }
    }

    private static void DestroyGeneratedObject(GameObject target)
    {
        if (target == null)
            return;
        if (Application.isPlaying)
            Destroy(target);
        else
            DestroyImmediate(target);
    }

    private void ApplySavedLayout(Transform canvasTransform)
    {
        RectTransform backdrop = canvasTransform.Find("Backdrop") as RectTransform;
        RectTransform commentPanel = canvasTransform.Find("コメント Panel") as RectTransform;
        RectTransform taskPanel = canvasTransform.Find("タスク Panel") as RectTransform;

        if (backdrop != null)
            Stretch(backdrop, 0f, 0f, 1f, 1f, screenMargin);
        ApplyPanelRect(commentPanel, commentPanelRect);
        ApplyPanelRect(taskPanel, taskPanelRect);

        ApplyContentPadding(commentContent);
        ApplyContentPadding(taskContent);
    }

    private static void ApplyPanelRect(RectTransform panel, Rect layout)
    {
        if (panel == null)
            return;
        Stretch(panel, Mathf.Clamp01(layout.xMin), Mathf.Clamp01(layout.yMin),
            Mathf.Clamp01(layout.xMax), Mathf.Clamp01(layout.yMax), 0f);
    }

    private void ApplyContentPadding(RectTransform content)
    {
        if (content == null)
            return;
        content.offsetMin = new Vector2(panelContentPadding, panelContentPadding);
        content.offsetMax = new Vector2(-panelContentPadding, -(panelContentPadding + 77f));
    }

    private static void SetHideFlagsRecursively(Transform root, HideFlags flags)
    {
        root.gameObject.hideFlags = flags;
        for (int i = 0; i < root.childCount; i++)
            SetHideFlagsRecursively(root.GetChild(i), flags);
    }

    private void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
            return;
        GameObject eventSystem = new GameObject("Jikken EventSystem",
            typeof(EventSystem), typeof(InputSystemUIInputModule));
        eventSystem.transform.SetParent(transform, false);
    }

    private void CreateBackdrop(Transform parent)
    {
        GameObject backdrop = new GameObject("Backdrop", typeof(RectTransform), typeof(Image));
        backdrop.transform.SetParent(parent, false);
        RectTransform rect = backdrop.GetComponent<RectTransform>();
        Stretch(rect, 0f, 0f, 1f, 1f, screenMargin);
        backdrop.GetComponent<Image>().color = new Color(0.8f, 0.8f, 0.8f, 0.96f);
    }

    private RectTransform CreatePanel(Transform parent, string title, Rect layout)
    {
        GameObject panel = new GameObject(title + " Panel", typeof(RectTransform),
            typeof(Image), typeof(Outline), typeof(RectMask2D));
        panel.transform.SetParent(parent, false);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        float minX = Mathf.Clamp01(layout.xMin);
        float minY = Mathf.Clamp01(layout.yMin);
        float maxX = Mathf.Clamp01(layout.xMax);
        float maxY = Mathf.Clamp01(layout.yMax);
        Stretch(panelRect, minX, minY, maxX, maxY, 0f);
        panel.GetComponent<Image>().color = panelColor;
        Outline outline = panel.GetComponent<Outline>();
        outline.effectColor = new Color(0.01f, 0.07f, 0.09f, 1f);
        outline.effectDistance = new Vector2(3f, -3f);

        GameObject titleObject = CreateTextObject(title + " Title", panel.transform,
            title, 42f, Color.white);
        RectTransform titleRect = titleObject.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -24f);
        titleRect.sizeDelta = new Vector2(0f, 70f);
        return panelRect;
    }

    private RectTransform CreateContent(RectTransform panel, string name)
    {
        GameObject content = new GameObject(name, typeof(RectTransform));
        content.transform.SetParent(panel, false);
        RectTransform rect = content.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(panelContentPadding, panelContentPadding);
        rect.offsetMax = new Vector2(-panelContentPadding, -(panelContentPadding + 77f));
        return rect;
    }

    private void SpawnComment()
    {
        bool canSpawnTask = taskComments != null && taskComments.Length > 0;
        bool isTask = canSpawnTask && UnityEngine.Random.Range(0f, 100f) < taskCommentChance;
        int taskIndex = isTask ? UnityEngine.Random.Range(0, taskComments.Length) : -1;
        string message = isTask ? taskComments[taskIndex].commentText : GetRandomNormalWord();
        if (string.IsNullOrEmpty(message))
            return;

        GameObject comment = CreateTextObject(isTask ? "Task Comment" : "Comment",
            commentContent, message, fontSize, isTask ? taskTextColor : normalTextColor);
        RectTransform rect = comment.GetComponent<RectTransform>();
        ConfigureListItem(rect);

        if (isTask)
        {
            Vector2 destination = roadMap != null
                ? roadMap.GetRandomPointOnRoad()
                : Vector2.zero;
            TextMeshProUGUI taskLabel = comment.GetComponent<TextMeshProUGUI>();
            taskLabel.text = $"◆ 座標 X:{destination.x:F1}、Y:{destination.y:F1} を調査して！";
            Button button = comment.AddComponent<Button>();
            button.targetGraphic = comment.GetComponent<TextMeshProUGUI>();
            int capturedIndex = taskIndex;
            button.onClick.AddListener(() => AcceptTask(rect, capturedIndex, destination));
        }
        else
        {
            comment.GetComponent<TextMeshProUGUI>().raycastTarget = false;
        }

        ShowPopup(comment.GetComponent<TextMeshProUGUI>().text,
            isTask ? taskTextColor : popupTextColor);

        comments.Insert(0, rect);
        ArrangeComments();
        while (comments.Count > maximumComments)
            RemoveCommentAt(comments.Count - 1);
    }

    private string GetRandomNormalWord()
    {
        if (words == null || words.Length == 0)
            return string.Empty;
        return words[UnityEngine.Random.Range(0, words.Length)];
    }

    private void AcceptTask(RectTransform sourceComment, int taskIndex, Vector2 destination)
    {
        if (taskIndex < 0 || taskIndex >= taskComments.Length)
            return;

        int sourceIndex = comments.IndexOf(sourceComment);
        if (sourceIndex >= 0)
            comments.RemoveAt(sourceIndex);
        if (sourceComment != null)
            Destroy(sourceComment.gameObject);
        ArrangeComments();

        GameObject task = CreateTextObject("Accepted Task", taskContent,
            $"タスク：指定地点へ移動\nX:{destination.x:F1}、Y:{destination.y:F1} のところへ進め",
            fontSize, Color.white);
        RectTransform taskRect = task.GetComponent<RectTransform>();
        taskRect.anchorMin = new Vector2(0f, 1f);
        taskRect.anchorMax = new Vector2(1f, 1f);
        taskRect.pivot = new Vector2(0.5f, 1f);
        taskRect.sizeDelta = new Vector2(0f, 110f);
        TextMeshProUGUI label = task.GetComponent<TextMeshProUGUI>();
        label.alignment = TextAlignmentOptions.TopLeft;
        label.textWrappingMode = TextWrappingModes.Normal;

        AcceptedTask acceptedTask = new AcceptedTask
        {
            rect = taskRect,
            label = label,
            destination = destination
        };
        Button taskButton = task.AddComponent<Button>();
        taskButton.targetGraphic = label;
        taskButton.onClick.AddListener(() => SelectTask(acceptedTask));
        acceptedTasks.Add(acceptedTask);
        ArrangeAcceptedTasks();

        if (roadMap == null)
            roadMap = FindFirstObjectByType<SubjiRoadMap>();
        if (player == null)
        {
            SubjiPlayerMovement playerMovement = FindFirstObjectByType<SubjiPlayerMovement>();
            if (playerMovement != null)
                player = playerMovement.transform;
        }

    }

    private void SelectTask(AcceptedTask task)
    {
        if (task == null || task.rect == null)
            return;

        selectedTask = task;
        for (int i = 0; i < acceptedTasks.Count; i++)
        {
            bool isSelected = acceptedTasks[i] == selectedTask;
            acceptedTasks[i].label.color = isSelected ? Color.yellow : Color.white;
            acceptedTasks[i].label.fontStyle = isSelected ? FontStyles.Bold : FontStyles.Normal;
        }

        if (roadMap != null)
            roadMap.SetTaskDestination(task.destination);
    }

    private void ArrangeAcceptedTasks()
    {
        const float acceptedTaskHeight = 110f;
        for (int i = acceptedTasks.Count - 1; i >= 0; i--)
        {
            if (acceptedTasks[i].rect == null)
            {
                acceptedTasks.RemoveAt(i);
                continue;
            }
            acceptedTasks[i].rect.anchoredPosition = new Vector2(0f, -(i * acceptedTaskHeight));
        }
    }

    private void UpdateActiveTask()
    {
        if (selectedTask == null || roadMap == null || player == null)
            return;

        if (Vector2.Distance(player.position, selectedTask.destination) > taskArrivalDistance)
            return;

        playerMovement.CompleteTask();
        roadMap.ClearTaskDestination();
        acceptedTasks.Remove(selectedTask);
        if (selectedTask.rect != null)
            Destroy(selectedTask.rect.gameObject);
        selectedTask = null;
        ArrangeAcceptedTasks();
    }

    private GameObject CreateTextObject(string objectName, Transform parent,
        string text, float size, Color color)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform),
            typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        TextMeshProUGUI label = textObject.GetComponent<TextMeshProUGUI>();
        label.text = text;
        if (fontAsset != null)
            label.font = fontAsset;
        label.fontSize = size;
        label.color = color;
        label.alignment = TextAlignmentOptions.Center;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        return textObject;
    }

    private void ConfigureListItem(RectTransform rect)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(0f, commentHeight);
    }

    private void ArrangeComments()
    {
        for (int i = comments.Count - 1; i >= 0; i--)
        {
            if (comments[i] == null)
            {
                comments.RemoveAt(i);
                continue;
            }
            comments[i].anchoredPosition = new Vector2(0f, -(i * commentHeight));
        }
    }

    private void RemoveCommentAt(int index)
    {
        RectTransform rect = comments[index];
        comments.RemoveAt(index);
        if (rect != null)
            Destroy(rect.gameObject);
    }

    private static void Stretch(RectTransform rect, float minX, float minY,
        float maxX, float maxY, float margin)
    {
        rect.anchorMin = new Vector2(minX, minY);
        rect.anchorMax = new Vector2(maxX, maxY);
        rect.offsetMin = new Vector2(margin, margin);
        rect.offsetMax = new Vector2(-margin, -margin);
    }
}
