using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public partial class JikkenCommentStream
{
    public static bool IsTutorialScene => SceneManager.GetActiveScene().name == "Tutorial";
    // タイトルからの今回の起動だけ、本編まで続ける。
    public static bool ContinueTutorialFromTitle { get; set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetTutorialLaunch() => ContinueTutorialFromTitle = false;

    [Serializable]
    public class TutorialComment
    {
        [TextArea] public string text;
        public string sender = "ナビゲーター";
        [Min(0f)] public float seconds = 2f;
        public TutorialComment(string text, float seconds = 2f)
        {
            this.text = text;
            this.seconds = seconds;
        }
    }

    [Header("専用チュートリアル")]
    [InspectorName("完了後に本編へ進む")]
    public bool continueAfterTutorial;
    public TutorialComment[] tutorialComments =
    {
        new("配信の予行練習として操作方法について説明します。", 2.5f),
        new("まず基本の操作について"),
        new("WASDで移動することができます。"),
        new("Q1. 4秒間移動してみよう", 4f),
        new("次にダッシュについて"),
        new("Shiftを押しながら移動すると速く移動できます。"),
        new("Q2. 2秒間ダッシュしてみよう", 2f),
        new("ダッシュはゲージ制なので使いすぎには注意"),
        new("次はメッセージ画面を確認しよう"),
        new("Q3. マウスホイールでメッセージを選択し、左クリックで開こう"),
        new("右側がコメント欄、左側が視聴者からの要望に応えるタスク欄となっています。"),
        new("左のタスクを左クリックすると受注できるよ"),
        new("Q4. タスクを受注してみよう"),
        new("右上の×で画面を閉じ、マウスホイールでライトを選んで左クリックしよう"),
        new("これで操作説明は終わり。配信の準備をしておこう")
    };
    [Min(0f)] public float tutorialOkSeconds = 1.5f;
    [Min(0f)] public float tutorialBlackoutSeconds = 5f;
    [Min(0f)] public float tutorialFadeSeconds = 0.5f;
    public Color tutorialQuestionColor = new(1f, 0.88f, 0.25f);
    public Color tutorialOkColor = new(0.35f, 1f, 0.45f);
    private int dedicatedStep;
    private float dedicatedElapsed;
    private bool dedicatedOk;
    private bool dedicatedFading;
    private bool dedicatedFinished;
    private Vector2 tutorialPreviousPosition;
    private RectTransform tutorialLightEntry;
    private TextMeshProUGUI tutorialBanner;
    private TextMeshProUGUI tutorialOkLabel;
    private TextMeshProUGUI tutorialSkipLabel;
    private Image tutorialFade;

    private void BeginDedicatedTutorial()
    {
        continueAfterTutorial |= ContinueTutorialFromTitle;
        ContinueTutorialFromTitle = false;
        autoGenerateComments = false;
        foreach (var enemy in FindObjectsByType<SubjiEnemyChase>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            enemy.gameObject.SetActive(false);
        foreach (var spawner in FindObjectsByType<SubjiEnemySpawner>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            spawner.enabled = false;
        foreach (var portal in FindObjectsByType<SubjiScenePortal>(FindObjectsSortMode.None))
            portal.enabled = false;
        foreach (var bush in FindObjectsByType<BushHideSpot2D>(FindObjectsSortMode.None))
            bush.enabled = false;
        foreach (var drink in FindObjectsByType<DrinkItemController>(FindObjectsSortMode.None))
            drink.enabled = false;
        if (playerMovement == null)
        {
            Debug.LogError("チュートリアルのプレイヤーがありません", this);
            return;
        }
        playerMovement.SetLightOn(false);
        tutorialPreviousPosition = playerMovement.transform.position;
        var root = new GameObject("Tutorial Guide", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        tutorialBanner = CreateTutorialLabel(root.transform, "Instruction", new Vector2(0, 420), new Vector2(1500, 140), 30);
        tutorialSkipLabel = CreateTutorialLabel(root.transform, "Skip", new Vector2(0, -320), new Vector2(1000, 60), 24);
        tutorialSkipLabel.text = "Space：コメントを1つ飛ばす";
        tutorialOkLabel = CreateTutorialLabel(root.transform, "OK", Vector2.zero, new Vector2(500, 150), 90);
        tutorialOkLabel.color = tutorialOkColor;
        tutorialOkLabel.gameObject.SetActive(false);
        var fadeObject = new GameObject("Fade", typeof(RectTransform), typeof(Image));
        fadeObject.transform.SetParent(root.transform, false);
        var fadeRect = fadeObject.GetComponent<RectTransform>();
        fadeRect.anchorMin = Vector2.zero;
        fadeRect.anchorMax = Vector2.one;
        fadeRect.offsetMin = fadeRect.offsetMax = Vector2.zero;
        tutorialFade = fadeObject.GetComponent<Image>();
        tutorialFade.color = Color.clear;
        tutorialFade.raycastTarget = false;
        EnterDedicatedStep(0);
    }

    private TextMeshProUGUI CreateTutorialLabel(Transform parent, string name, Vector2 position, Vector2 size, float textSize)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var label = go.GetComponent<TextMeshProUGUI>();
        label.font = fontAsset;
        label.fontSize = textSize;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        label.outlineWidth = 0.15f;
        label.rectTransform.anchoredPosition = position;
        label.rectTransform.sizeDelta = size;
        return label;
    }

    private void EnterDedicatedStep(int step)
    {
        dedicatedStep = step;
        dedicatedElapsed = 0f;
        dedicatedOk = false;
        tutorialOkLabel.gameObject.SetActive(false);
        var comment = tutorialComments[step];
        bool question = step == 3 || step == 6 || step == 9 || step == 12;
        var color = question ? tutorialQuestionColor : normalTextColor;
        tutorialBanner.color = color;
        tutorialBanner.text = comment.sender + "\n" + comment.text;
        AddTutorialHistoryComment(comment.text, color);
        if (step == 10 && tutorialLightEntry == null)
        {
            CreateSelectableTaskComment("◆ 懐中電灯を点灯", AcceptTutorialLight);
            tutorialLightEntry = taskEntries[0];
            tutorialLightEntry.GetComponent<Button>().interactable = false;
        }
        if (step == 12)
            tutorialLightEntry.GetComponent<Button>().interactable = true;
    }

    private void AcceptTutorialLight(RectTransform source)
    {
        if (dedicatedStep < 12 || HasActiveTask()) return;
        playerMovement.SetLightOn(false);
        MarkTaskCommentSelected(source);
        SelectTask(new AcceptedTask
        {
            displayText = "懐中電灯を点灯", sourceComment = source,
            completionType = TaskCompletionType.TutorialLight, subscriberReward = 0
        });
    }

    private void AdvanceDedicatedStep(bool skipped)
    {
        if (dedicatedStep == 12 && selectedTask == null)
            AcceptTutorialLight(tutorialLightEntry);
        if (dedicatedStep == 13 && skipped)
        {
            selectedTask = null;
            if (tutorialLightEntry != null)
            {
                taskEntries.Remove(tutorialLightEntry);
                Destroy(tutorialLightEntry.gameObject);
            }
            roadMap?.ClearActiveTaskText();
        }
        if (dedicatedStep < 14)
        {
            EnterDedicatedStep(dedicatedStep + 1);
            return;
        }
        CloseCommentScreen();
        tutorialSkipLabel.gameObject.SetActive(false);
        if (!continueAfterTutorial)
        {
            dedicatedFinished = true;
            tutorialBanner.text = "チュートリアル完了";
            return;
        }
        dedicatedFading = true;
        dedicatedElapsed = 0f;
        tutorialBanner.gameObject.SetActive(false);
        playerMovement.SetInteractionMovementLocked(true);
        foreach (var selector in FindObjectsByType<ItemSlotSelector>(FindObjectsSortMode.None))
            selector.enabled = false;
    }

    private void UpdateDedicatedTutorial()
    {
        if (playerMovement == null || tutorialBanner == null || dedicatedFinished || Time.timeScale <= 0f) return;
        Vector2 position = playerMovement.transform.position;
        bool moved = (position - tutorialPreviousPosition).sqrMagnitude > 0.000001f;
        tutorialPreviousPosition = position;
        if (dedicatedFading)
        {
            dedicatedElapsed += Time.deltaTime;
            tutorialFade.color = new Color(0, 0, 0, tutorialFadeSeconds <= 0 ? 1 : Mathf.Clamp01(dedicatedElapsed / tutorialFadeSeconds));
            if (dedicatedElapsed >= tutorialBlackoutSeconds)
            {
                dedicatedFinished = true;
                SceneManager.LoadScene("jikkennsupace");
            }
            return;
        }
        // Updateの一度だけで扱い、次の工程へ同じキー入力を渡さない。
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            AdvanceDedicatedStep(true);
            return;
        }
        if (dedicatedOk)
        {
            dedicatedElapsed += Time.deltaTime;
            if (dedicatedElapsed >= tutorialOkSeconds) AdvanceDedicatedStep(false);
            return;
        }
        bool reached = false;
        if (dedicatedStep == 3 || dedicatedStep == 6)
        {
            if (moved && playerMovement.HasMovementInput() && (dedicatedStep == 3 || playerMovement.IsSpeedBoosting))
                dedicatedElapsed += Time.deltaTime;
            reached = dedicatedElapsed >= tutorialComments[dedicatedStep].seconds;
        }
        else if (dedicatedStep == 9) reached = isOpen;
        else if (dedicatedStep == 12) reached = selectedTask?.completionType == TaskCompletionType.TutorialLight;
        else if (dedicatedStep == 13)
        {
            if (!isOpen && selectedTask?.completionType == TaskCompletionType.TutorialLight && playerMovement.IsLightOn)
            {
                CompleteActiveTask();
                AdvanceDedicatedStep(false);
            }
            return;
        }
        else
        {
            dedicatedElapsed += Time.deltaTime;
            if (dedicatedElapsed >= tutorialComments[dedicatedStep].seconds) AdvanceDedicatedStep(false);
            return;
        }
        if (reached)
        {
            dedicatedOk = true;
            dedicatedElapsed = 0f;
            tutorialOkLabel.text = "OK";
            tutorialOkLabel.gameObject.SetActive(true);
        }
    }
}
