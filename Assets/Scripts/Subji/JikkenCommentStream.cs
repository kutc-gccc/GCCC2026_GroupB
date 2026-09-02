using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

[ExecuteAlways]
public class JikkenCommentStream : MonoBehaviour
{
    private enum TaskCompletionType
    {
        Destination,
        VendingPurchase,
        BushHide,
        CoffeeCupLight,
        CastleLight
    }

    [Header("自動生成")]
    [Tooltip("オンにすると、プレイ中にタスク付きコメントと通常コメントを自動生成します")]
    public bool autoGenerateComments;

    [Serializable]
    public class TaskComment
    {
        public string commentText;
        [TextArea] public string taskText;
    }

    private class AcceptedTask
    {
        public Vector2 destination;
        public string displayText;
        public bool isTutorialTask;
        public RectTransform taskView;
        public RectTransform sourceComment;
        public TaskCompletionType completionType;
        public int subscriberReward = 100;
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

    [Header("導入チュートリアル")]
    [Tooltip("コメント表のID 1～4をゲーム開始時に順番に再生します")]
    public bool playIntroTutorial = true;
    [Tooltip("ID1を表示してからID2へ進むまでの秒数")]
    [Min(0f)] public float introFirstMessageDuration = 4f;
    [Min(0.5f)] public float tutorialMessageInterval = 4f;

    [Header("ID5～7の表示タイミング")]
    [Tooltip("ID5を表示してからID6を表示するまでの秒数")]
    [Min(0f)] public float id5To6Delay = 2f;
    [Tooltip("ID6を表示してからID7を表示するまでの秒数")]
    [Min(0f)] public float id6To7Delay = 2f;

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
    private int tutorialStep;
    private float tutorialTimer;
    private bool tutorialTaskCommentCreated;
    private bool tutorialStep3CommentCreated;
    private GameObject taskArrivalTriggerObject;
    private static Sprite taskDestinationMarkerSprite;
    private DrinkItemController drinkItemController;
    private CoffeeCupLightInteraction coffeeCupInteraction;
    private CoffeeCupLightInteraction castleInteraction;
    private bool postTutorialTasksStarted;
    private int postTutorialSequenceStep;
    private float postTutorialSequenceTimer;
    private string activeWorldTaskGuidance;
    private float worldTaskGuidanceTimer;
    private bool vendingTutorialCompleted;
    private bool bushTutorialCompleted;
    private bool greenGuardSpawned;
    private bool coffeeCupTaskCreated;
    private bool castleTaskCreated;

    [Header("ID6・7完了後の監視敵")]
    [Tooltip("配下にシーン配置した監視敵を置きます。複製した敵も両タスク完了時にまとめて有効化します")]
    [SerializeField] private GameObject greenGuardContainer;

    private void Awake()
    {
        if (fontAsset != null)
            fontAsset.isMultiAtlasTexturesEnabled = true;

        roadMap = FindFirstObjectByType<SubjiRoadMap>();
        playerMovement = FindFirstObjectByType<SubjiPlayerMovement>();
        if (playerMovement != null)
            player = playerMovement.transform;
        EnsureUiExists(false);
        if (Application.isPlaying)
        {
            uiRoot.SetActive(false);
            CreatePopupUi();
            SubscribeToWorldTasks();
            if (playIntroTutorial)
                BeginIntroTutorial();
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

        UpdateIntroTutorial();
        UpdatePostTutorialSequence();
        UpdateWorldTaskGuidance();

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

        if (autoGenerateComments)
        {
            timer += Time.deltaTime;
            if (timer >= spawnInterval)
            {
                timer = 0f;
                SpawnComment();
            }
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

    private void ShowPopup(string message, Color color, float duration = -1f)
    {
        if (isOpen || popupRoot == null || popupLabel == null)
            return;

        popupLabel.text = message;
        popupLabel.color = color;
        popupTimer = duration >= 0f ? duration : popupDuration;
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

    private void BeginIntroTutorial()
    {
        tutorialStep = 1;
        tutorialTimer = introFirstMessageDuration;
        AddTutorialHistoryComment("wasdでキャラを動かしてみよう", normalTextColor);
        ShowPopup("wasdでキャラを動かしてみよう", normalTextColor);
        popupTimer = tutorialTimer;
    }

    private void UpdateIntroTutorial()
    {
        if (!playIntroTutorial || tutorialStep <= 0 || tutorialStep >= 5)
            return;

        tutorialTimer -= Time.deltaTime;
        if (tutorialStep == 1 && tutorialTimer <= 0f)
        {
            tutorialStep = 2;
            CreateTutorialTaskComment(new Vector2(0f, 20f),
                "◆ (0,20)の地点に行ってみよう", true);
            // タスクはまだ開始せず、先に選択方法を案内する。
            tutorialStep = 3;
            tutorialTimer = 2f;
            return;
        }

        if (tutorialStep == 3)
        {
            if (!tutorialStep3CommentCreated)
            {
                AddTutorialHistoryComment(
                    "マウススクロールしてコメント左クリックでタスクを確認しよう",
                    normalTextColor);
                tutorialStep3CommentCreated = true;
            }

            if (isOpen)
            {
                tutorialStep = 4;
                tutorialTimer = 0f;
                tutorialTaskCommentCreated = false;
            }
            else if (tutorialTimer <= 0f)
            {
                ShowPopup("マウススクロールしてコメント左クリックでタスクを確認しよう",
                    popupTextColor);
                tutorialTimer = tutorialMessageInterval;
            }
            return;
        }

        if (tutorialStep == 4 && tutorialTimer <= 0f)
        {
            ShowPopup("コメントの赤文字をクリックしてみて右クリで閉じれるよ", popupTextColor);
            tutorialTimer = tutorialMessageInterval;
            if (!tutorialTaskCommentCreated)
            {
                GameObject instruction = CreateTextObject("Tutorial Instruction", commentContent,
                    "コメントの赤文字をクリックしてみて右クリで閉じれるよ",
                    fontSize, normalTextColor);
                RectTransform instructionRect = instruction.GetComponent<RectTransform>();
                ConfigureListItem(instructionRect);
                instruction.GetComponent<TextMeshProUGUI>().raycastTarget = false;
                comments.Insert(0, instructionRect);
                tutorialTaskCommentCreated = true;
                ArrangeComments();
            }
        }
    }

    private void CreateTutorialTaskComment(Vector2 destination, string message, bool isTutorialTask)
    {
        if (commentContent == null || taskComments == null || taskComments.Length == 0)
            return;

        CreateSelectableTaskComment(message,
            rect => AcceptTask(rect, 0, destination, isTutorialTask));
    }

    private void CreateSelectableTaskComment(string message, Action<RectTransform> onSelected)
    {
        GameObject comment = CreateTextObject("Tutorial Task Comment", commentContent,
            message, fontSize, taskTextColor);
        RectTransform rect = comment.GetComponent<RectTransform>();
        ConfigureListItem(rect);
        Button button = comment.AddComponent<Button>();
        button.targetGraphic = comment.GetComponent<TextMeshProUGUI>();
        button.onClick.AddListener(() => onSelected(rect));
        comments.Insert(0, rect);
        ArrangeComments();
        ShowPopup(message, taskTextColor);
    }

    private void AddTutorialHistoryComment(string message, Color color)
    {
        if (commentContent == null || string.IsNullOrEmpty(message))
            return;

        GameObject comment = CreateTextObject("Tutorial Comment History", commentContent,
            message, fontSize, color);
        RectTransform rect = comment.GetComponent<RectTransform>();
        ConfigureListItem(rect);
        comment.GetComponent<TextMeshProUGUI>().raycastTarget = false;
        comments.Insert(0, rect);
        ArrangeComments();
    }

    private string GetRandomNormalWord()
    {
        if (words == null || words.Length == 0)
            return string.Empty;
        return words[UnityEngine.Random.Range(0, words.Length)];
    }

    private void AcceptTask(RectTransform sourceComment, int taskIndex, Vector2 destination,
        bool isTutorialTask = false)
    {
        if (taskIndex < 0 || taskIndex >= taskComments.Length || HasActiveTask())
            return;

        MarkTaskCommentSelected(sourceComment);

        string taskDisplayText =
            $"タスク：指定地点へ移動\nX:{destination.x:F1}、Y:{destination.y:F1} のところへ進め";
        AcceptedTask acceptedTask = new AcceptedTask
        {
            destination = destination,
            displayText = taskDisplayText,
            isTutorialTask = isTutorialTask,
            taskView = CreateTaskView(taskDisplayText),
            sourceComment = sourceComment,
            completionType = TaskCompletionType.Destination
        };

        if (roadMap == null)
            roadMap = FindFirstObjectByType<SubjiRoadMap>();
        if (player == null)
        {
            playerMovement = FindFirstObjectByType<SubjiPlayerMovement>();
            if (playerMovement != null)
                player = playerMovement.transform;
        }

        // コメントをクリックした時点で受諾と選択を同時に行う。
        SelectTask(acceptedTask);
        if (tutorialStep == 4 && isTutorialTask)
            tutorialStep = 5;
    }

    private void SelectTask(AcceptedTask task)
    {
        if (task == null)
            return;

        selectedTask = task;

        if (roadMap != null && task.completionType == TaskCompletionType.Destination)
        {
            roadMap.SetTaskDestination(task.destination, task.displayText);
            CreateTaskArrivalTrigger(roadMap.TaskDestination);
        }
        else
        {
            roadMap?.SetActiveTaskText(task.displayText);
        }
    }

    private void CreateTaskArrivalTrigger(Vector2 destination)
    {
        if (taskArrivalTriggerObject != null)
            Destroy(taskArrivalTriggerObject);

        taskArrivalTriggerObject = new GameObject("Task Arrival Trigger");
        taskArrivalTriggerObject.transform.position = destination;
        int ignoreRaycastLayer = LayerMask.NameToLayer("Ignore Raycast");
        if (ignoreRaycastLayer >= 0)
            taskArrivalTriggerObject.layer = ignoreRaycastLayer;

        CircleCollider2D triggerCollider = taskArrivalTriggerObject.AddComponent<CircleCollider2D>();
        triggerCollider.isTrigger = true;
        triggerCollider.radius = taskArrivalDistance;
        TaskDestinationTrigger trigger = taskArrivalTriggerObject.AddComponent<TaskDestinationTrigger>();
        trigger.Configure(this);

        GameObject markerObject = new GameObject("Task Destination Marker");
        markerObject.transform.SetParent(taskArrivalTriggerObject.transform, false);
        markerObject.layer = taskArrivalTriggerObject.layer;
        SpriteRenderer markerRenderer = markerObject.AddComponent<SpriteRenderer>();
        markerRenderer.sprite = GetTaskDestinationMarkerSprite();
        markerRenderer.color = new Color(1f, 0.85f, 0.15f, 0.9f);
        markerRenderer.sortingLayerName = "New Layer 2";
        markerRenderer.sortingOrder = 1100;
        markerObject.transform.localScale = Vector3.one * 0.8f;
    }

    private void MarkTaskCommentSelected(RectTransform sourceComment)
    {
        if (sourceComment != null)
        {
            Button sourceButton = sourceComment.GetComponent<Button>();
            if (sourceButton != null)
                sourceButton.interactable = false;
            TextMeshProUGUI sourceLabel = sourceComment.GetComponent<TextMeshProUGUI>();
            if (sourceLabel != null)
            {
                sourceLabel.raycastTarget = false;
                sourceLabel.color = Color.yellow;
                sourceLabel.fontStyle = FontStyles.Bold;
            }
        }
        ArrangeComments();
    }

    private static void MarkTaskCommentCompleted(RectTransform sourceComment)
    {
        if (sourceComment == null)
            return;

        TextMeshProUGUI sourceLabel = sourceComment.GetComponent<TextMeshProUGUI>();
        if (sourceLabel != null)
        {
            sourceLabel.color = new Color(0.35f, 1f, 0.45f, 1f);
            sourceLabel.fontStyle = FontStyles.Bold;
            sourceLabel.raycastTarget = false;
        }
    }

    private bool HasActiveTask()
    {
        return selectedTask != null;
    }

    private void OnDestroy()
    {
        if (drinkItemController != null)
            drinkItemController.DrinkPurchased -= CompleteVendingTask;
        BushHideSpot2D.PlayerEnteredBush -= CompleteBushTask;
        if (coffeeCupInteraction != null)
            coffeeCupInteraction.LightActivated -= CompleteCoffeeCupTask;
        if (castleInteraction != null)
            castleInteraction.LightActivated -= CompleteCastleTask;
    }

    private static Sprite GetTaskDestinationMarkerSprite()
    {
        if (taskDestinationMarkerSprite == null)
        {
            taskDestinationMarkerSprite = Sprite.Create(Texture2D.whiteTexture,
                new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
            taskDestinationMarkerSprite.name = "Runtime Task Destination Marker";
        }
        return taskDestinationMarkerSprite;
    }

    public void NotifyTaskDestinationReached()
    {
        if (selectedTask == null ||
            selectedTask.completionType != TaskCompletionType.Destination)
            return;

        CompleteActiveTask();
    }

    private RectTransform CreateTaskView(string message)
    {
        if (taskContent == null)
            return null;

        GameObject task = CreateTextObject("Active Task", taskContent, message,
            fontSize, normalTextColor);
        RectTransform rect = task.GetComponent<RectTransform>();
        ConfigureListItem(rect);
        task.GetComponent<TextMeshProUGUI>().raycastTarget = false;
        return rect;
    }

    private void SubscribeToWorldTasks()
    {
        drinkItemController = FindFirstObjectByType<DrinkItemController>();
        if (drinkItemController != null)
            drinkItemController.DrinkPurchased += CompleteVendingTask;
        BushHideSpot2D.PlayerEnteredBush += CompleteBushTask;
        coffeeCupInteraction = CoffeeCupLightInteraction.FindOrCreateInScene(
            "コーヒーカップ", 4f, 6f, 3f, false);
        if (coffeeCupInteraction != null)
            coffeeCupInteraction.LightActivated += CompleteCoffeeCupTask;
        castleInteraction = CoffeeCupLightInteraction.FindOrCreateInScene(
            "城", 4f, 7f, 6f, true);
        if (castleInteraction != null)
            castleInteraction.LightActivated += CompleteCastleTask;
        ApplyEnemyActivationStage(SubjiEnemyChase.SpawnTiming.GameStart);
    }

    private void StartPostTutorialTasks()
    {
        postTutorialTasksStarted = true;
        const string message = "警備員から逃げながらタスクをこなして登録者を増やそう";
        AddTutorialHistoryComment(message, normalTextColor);
        ShowPopup(message, popupTextColor);
        postTutorialSequenceStep = 1;
        postTutorialSequenceTimer = id5To6Delay;
    }

    private void UpdatePostTutorialSequence()
    {
        if (!postTutorialTasksStarted || postTutorialSequenceStep <= 0 ||
            postTutorialSequenceStep >= 3)
            return;

        postTutorialSequenceTimer -= Time.deltaTime;
        if (postTutorialSequenceTimer > 0f)
            return;

        if (postTutorialSequenceStep == 1)
        {
            const string message = "自販機でドリンクを購入しよう";
            CreateSelectableTaskComment(message,
                rect => AcceptWorldTask(rect, true, message));
            postTutorialSequenceStep = 2;
            postTutorialSequenceTimer = id6To7Delay;
            return;
        }

        const string bushMessage = "ブッシュの中に隠れよう";
        CreateSelectableTaskComment(bushMessage,
            rect => AcceptWorldTask(rect, false, bushMessage));
        postTutorialSequenceStep = 3;
    }

    private void AcceptWorldTask(RectTransform sourceComment, bool isVendingTask,
        string message)
    {
        if (HasActiveTask())
            return;

        MarkTaskCommentSelected(sourceComment);
        string taskDisplayText = $"タスク：{message}";
        if (roadMap == null)
            roadMap = FindFirstObjectByType<SubjiRoadMap>();
        TaskCompletionType completionType = isVendingTask
            ? TaskCompletionType.VendingPurchase
            : TaskCompletionType.BushHide;
        SelectTask(new AcceptedTask
        {
            displayText = taskDisplayText,
            taskView = CreateTaskView(taskDisplayText),
            sourceComment = sourceComment,
            completionType = completionType
        });

        activeWorldTaskGuidance = isVendingTask
            ? "自販機の近くでEを押すと購入できるよ"
            : "ブッシュの近くでEを押すと隠れられるよ";
        AddTutorialHistoryComment(activeWorldTaskGuidance, normalTextColor);
        ShowPopup(activeWorldTaskGuidance, popupTextColor);
        worldTaskGuidanceTimer = tutorialMessageInterval;

        if (isVendingTask)
            roadMap?.SetActiveTaskTargets(drinkItemController?.GetVendingMachineTransforms());
        else
            roadMap?.SetActiveTaskTargets(FindObjectsByType<BushHideSpot2D>(
                FindObjectsSortMode.None).Select(bush => bush.transform));
    }

    private void UpdateWorldTaskGuidance()
    {
        if (selectedTask == null || string.IsNullOrEmpty(activeWorldTaskGuidance))
            return;

        worldTaskGuidanceTimer -= Time.deltaTime;
        if (worldTaskGuidanceTimer > 0f)
            return;

        ShowPopup(activeWorldTaskGuidance, popupTextColor);
        worldTaskGuidanceTimer = tutorialMessageInterval;
    }

    private void CompleteVendingTask()
    {
        if (selectedTask == null ||
            selectedTask.completionType != TaskCompletionType.VendingPurchase)
            return;

        CompleteActiveTask();
    }

    private void CompleteBushTask()
    {
        if (selectedTask == null ||
            selectedTask.completionType != TaskCompletionType.BushHide)
            return;

        CompleteActiveTask();
    }

    private void CompleteCoffeeCupTask()
    {
        if (selectedTask == null ||
            selectedTask.completionType != TaskCompletionType.CoffeeCupLight)
            return;

        CompleteActiveTask();
    }

    private void CompleteCastleTask()
    {
        if (selectedTask == null ||
            selectedTask.completionType != TaskCompletionType.CastleLight)
            return;

        CompleteActiveTask();
    }

    private void TryCreateCoffeeCupTask()
    {
        if (coffeeCupTaskCreated || !vendingTutorialCompleted ||
            !bushTutorialCompleted || coffeeCupInteraction == null)
            return;

        coffeeCupTaskCreated = true;
        const string message = "コーヒーカップに電気を灯そう";
        CreateSelectableTaskComment(message,
            rect => AcceptCoffeeCupTask(rect, message));
    }

    private void AcceptCoffeeCupTask(RectTransform sourceComment, string message)
    {
        if (HasActiveTask() || coffeeCupInteraction == null)
            return;

        MarkTaskCommentSelected(sourceComment);
        string taskDisplayText = $"タスク：{message}";
        SelectTask(new AcceptedTask
        {
            displayText = taskDisplayText,
            taskView = CreateTaskView(taskDisplayText),
            sourceComment = sourceComment,
            completionType = TaskCompletionType.CoffeeCupLight,
            subscriberReward = 500
        });

        activeWorldTaskGuidance = "コーヒーカップの近くでEを押すとつくよ";
        AddTutorialHistoryComment(activeWorldTaskGuidance, normalTextColor);
        ShowPopup(activeWorldTaskGuidance, popupTextColor);
        worldTaskGuidanceTimer = tutorialMessageInterval;
        coffeeCupInteraction.SetTaskActive(true);
        roadMap?.SetActiveTaskTargets(new[] { coffeeCupInteraction.transform });
    }

    private void TryCreateCastleTask()
    {
        if (castleTaskCreated || castleInteraction == null)
            return;

        castleTaskCreated = true;
        const string message = "城の電気を灯そう";
        CreateSelectableTaskComment(message,
            rect => AcceptCastleTask(rect, message));
    }

    private void AcceptCastleTask(RectTransform sourceComment, string message)
    {
        if (HasActiveTask() || castleInteraction == null)
            return;

        MarkTaskCommentSelected(sourceComment);
        string taskDisplayText = $"タスク：{message}";
        SelectTask(new AcceptedTask
        {
            displayText = taskDisplayText,
            taskView = CreateTaskView(taskDisplayText),
            sourceComment = sourceComment,
            completionType = TaskCompletionType.CastleLight,
            subscriberReward = 1000
        });

        activeWorldTaskGuidance = "城の近くでEで点灯するよ";
        AddTutorialHistoryComment(activeWorldTaskGuidance, normalTextColor);
        ShowPopup(activeWorldTaskGuidance, popupTextColor);
        worldTaskGuidanceTimer = tutorialMessageInterval;
        castleInteraction.SetTaskActive(true);
        roadMap?.SetActiveTaskTargets(new[] { castleInteraction.transform });
    }

    private void CompleteActiveTask()
    {
        if (selectedTask == null)
            return;

        AcceptedTask completedTask = selectedTask;
        selectedTask = null;
        activeWorldTaskGuidance = null;
        worldTaskGuidanceTimer = 0f;
        playerMovement?.CompleteTask(completedTask.subscriberReward);
        MarkTaskCommentCompleted(completedTask.sourceComment);
        if (completedTask.taskView != null)
            Destroy(completedTask.taskView.gameObject);

        if (completedTask.completionType == TaskCompletionType.Destination)
            roadMap?.ClearTaskDestination();
        else
            roadMap?.ClearActiveTaskText();

        if (completedTask.completionType == TaskCompletionType.VendingPurchase)
        {
            vendingTutorialCompleted = true;
            const string message = "ドリンクを飲むと一定時間Shiftダッシュが強化されるよ";
            AddTutorialHistoryComment(message, normalTextColor);
            ShowPopup(message, popupTextColor, 4f);
        }
        else if (completedTask.completionType == TaskCompletionType.BushHide)
        {
            bushTutorialCompleted = true;
            const string message = "ブッシュの中に隠れると敵の追跡から逃れられるよ";
            AddTutorialHistoryComment(message, normalTextColor);
            ShowPopup(message, popupTextColor, 4f);
        }
        else if (completedTask.completionType == TaskCompletionType.CoffeeCupLight)
        {
            coffeeCupInteraction?.SetTaskActive(false);
            ApplyEnemyActivationStage(
                SubjiEnemyChase.SpawnTiming.AfterTask12);
            TryCreateCastleTask();
        }
        else if (completedTask.completionType == TaskCompletionType.CastleLight)
        {
            castleInteraction?.SetTaskActive(false);
        }

        TrySpawnGreenGuard();
        TryCreateCoffeeCupTask();

        if (taskArrivalTriggerObject != null)
        {
            Destroy(taskArrivalTriggerObject);
            taskArrivalTriggerObject = null;
        }

        if (playIntroTutorial && completedTask.isTutorialTask &&
            !postTutorialTasksStarted)
            StartPostTutorialTasks();
    }

    private void TrySpawnGreenGuard()
    {
        if (greenGuardSpawned || !vendingTutorialCompleted ||
            !bushTutorialCompleted)
            return;

        if (player == null)
        {
            playerMovement = FindFirstObjectByType<SubjiPlayerMovement>();
            if (playerMovement != null)
                player = playerMovement.transform;
        }
        if (player == null)
            return;

        if (greenGuardContainer != null)
        {
            foreach (SubjiEnemyChase sceneEnemy in
                greenGuardContainer.GetComponentsInChildren<SubjiEnemyChase>(true))
            {
                sceneEnemy.player = player;
                sceneEnemy.roadMap = roadMap != null
                    ? roadMap
                    : FindFirstObjectByType<SubjiRoadMap>();
                sceneEnemy.ApplyAppearanceAndCollider();
            }
            ApplyEnemyActivationStage(
                SubjiEnemyChase.SpawnTiming.AfterTasks6And7);
        }
        else
        {
            GameObject guard = new GameObject("Green Guard Post (-10, -3)");
            guard.transform.position = new Vector3(-10f, -3f, 0f);
            SubjiEnemyChase enemy = guard.AddComponent<SubjiEnemyChase>();
            enemy.spriteResourcePath = "guard_green_enemy";
            enemy.movementType = SubjiEnemyChase.MovementType.GuardPost;
            enemy.matchPlayerMoveSpeed = true;
            enemy.returnSpeed = 1f;
            enemy.guardLookInterval = 1.5f;
            enemy.useRadialDetection = false;
            enemy.player = player;
            enemy.roadMap = roadMap != null
                ? roadMap
                : FindFirstObjectByType<SubjiRoadMap>();
            enemy.ApplyAppearanceAndCollider();
        }
        greenGuardSpawned = true;
    }

    private void ApplyEnemyActivationStage(
        SubjiEnemyChase.SpawnTiming reachedStage)
    {
        if (greenGuardContainer == null)
            return;

        greenGuardContainer.SetActive(true);
        if (reachedStage == SubjiEnemyChase.SpawnTiming.AfterTask12)
            EnsureAfterTask12EnemyExists();
        foreach (SubjiEnemyChase enemy in FindObjectsByType<SubjiEnemyChase>(
            FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            enemy.gameObject.SetActive(enemy.spawnTiming <= reachedStage);
        }
    }

    private void EnsureAfterTask12EnemyExists()
    {
        foreach (SubjiEnemyChase existingEnemy in
            greenGuardContainer.GetComponentsInChildren<SubjiEnemyChase>(true))
        {
            if (existingEnemy.spawnTiming == SubjiEnemyChase.SpawnTiming.AfterTask12)
                return;
        }

        SubjiEnemyChase source =
            greenGuardContainer.GetComponentInChildren<SubjiEnemyChase>(true);
        GameObject guard = new GameObject("Green Guard Post (After ID12 Runtime)");
        guard.transform.SetParent(greenGuardContainer.transform, false);
        guard.transform.position = source != null
            ? source.transform.position + Vector3.right * 3f
            : new Vector3(-7f, -3f, 0f);
        SubjiEnemyChase enemy = guard.AddComponent<SubjiEnemyChase>();
        enemy.spriteResourcePath = source != null
            ? source.spriteResourcePath
            : "guard_green_enemy";
        enemy.movementType = SubjiEnemyChase.MovementType.GuardPost;
        enemy.spawnTiming = SubjiEnemyChase.SpawnTiming.AfterTask12;
        enemy.matchPlayerMoveSpeed = true;
        enemy.returnSpeed = source != null ? source.returnSpeed : 1f;
        enemy.guardLookInterval = source != null ? source.guardLookInterval : 1.5f;
        enemy.useRadialDetection = false;
        enemy.player = player;
        enemy.roadMap = roadMap;
        enemy.ApplyAppearanceAndCollider();
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
