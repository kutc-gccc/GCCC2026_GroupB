using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class JikkenCommentStream
{
    [Serializable]
    public class MainStoryLine
    {
        [TextArea] public string text;
        [InspectorName("プレイヤー発言")] public bool player;
        [Min(0.01f), InspectorName("表示秒数")] public float seconds = 3f;
        [InspectorName("この後に配信開始を表示")] public bool showBroadcastTitle;
        [InspectorName("送り主名（空欄なら自動）")] public string senderName;
        [Min(0), InspectorName("表示時に追加するタスクID（0は通常発言）")] public int taskId;
        [InspectorName("追加したタスクの選択を待つ")] public bool waitForTaskSelection;
        [Min(0), InspectorName("発言前に達成を待つタスクID（0は待機なし）")] public int waitForCompletedTaskId;

        public MainStoryLine(string text, bool player, float seconds, bool title = false)
        {
            this.text = text;
            this.player = player;
            this.seconds = seconds;
            showBroadcastTitle = title;
        }
    }

    [Header("本編開始コメント（仮・オフで従来の開始に戻す）")]
    [InspectorName("本編開始コメントを再生")]
    public bool playMainStoryIntro = true;
    [Min(0.01f), InspectorName("配信開始の表示秒数")] public float broadcastTitleSeconds = 2f;
    public MainStoryLine[] mainStoryLines =
    {
        new MainStoryLine("なんとか遊園地に忍ことが出来た", true, 2f),
        new MainStoryLine("まだ登録者は100人だけどこの配信で人生一発逆転人気配信者の仲間入りだ", true, 2f),
        new MainStoryLine("よし配信を始めよう。", true, 3f, true),
        new MainStoryLine("はい！配信始まりました。", true, 2.5f),
        new MainStoryLine("うぽつです", false, 0.5f),
        new MainStoryLine("初見です", false, 0.5f),
        new MainStoryLine("今日はいつもと雰囲気が違うね,ここどこ？", false, 1f),
        new MainStoryLine("本日はなんと閉園中のL遊園地に潜入しています", true, 2.5f),
        new MainStoryLine("e", false, 0.5f),
        new MainStoryLine("え", false, 0.5f),
        new MainStoryLine("やば", false, 0.5f),
        new MainStoryLine("許可取っているの？", false, 0.5f),
        new MainStoryLine("取ってないよ？でもばればきゃ大丈夫", true, 1.5f),
        new MainStoryLine("企画説明するよ題して遊園地でバスってみた！", true, 1.5f),
        new MainStoryLine("視聴者のリクエストに応えて遊園地でバズっていくよ", true, 1.5f),
        new MainStoryLine("ただの犯罪で草", false, 0.5f),
        new MainStoryLine("面白そう！", false, 0.5f),
        new MainStoryLine("すご", false, 0.5f),
        new MainStoryLine("リクエストどしどしくださいな", true, 1.5f),
        new MainStoryLine("警備員とかいないのかな", false, 0.5f),
        new MainStoryLine("それ遊園地に本当にいるの？", false, 0.5f),
        new MainStoryLine("嘘くさいな", false, 0.5f),
        new MainStoryLine("確かに", false, 0.5f),
        new MainStoryLine("そんな皆を騙すこと私はしないよ", true, 1.5f),
        new MainStoryLine("L遊園地には有名な噴水広場があるそこにいって本当にいるのか証明して欲しい", false, 3f)
            { senderName = "リスナーA", taskId = 1, waitForTaskSelection = true },
        // No.26 is the shared task-selection reminder, not a timed story line.
        new MainStoryLine("じゃあその噴水公園に行ってみるよ。", true, 1.5f),
        new MainStoryLine("これで嘘つきかわかるな", false, 0.5f),
        new MainStoryLine("うぽつです", false, 0.5f),
        new MainStoryLine("これどういう状況？", false, 0.5f),
        new MainStoryLine("閉園後の遊園地初めて", false, 0.5f),
        new MainStoryLine("ちょっと怖いな", false, 0.5f),
        new MainStoryLine("到着暗くて見ずらいけど確かにきれいだね", true, 1.5f)
            { waitForCompletedTaskId = 1 },
        new MainStoryLine("すご", false, 0.5f),
        new MainStoryLine("ほんまに来てるんや", false, 0.5f),
        new MainStoryLine("疑ってすみません", false, 0.5f),
        new MainStoryLine("ガチで草", false, 0.5f),
        new MainStoryLine("家近いし行こうかな", false, 0.5f),
        new MainStoryLine("こんな感じでやっていくからリクエスト待ってるよ", true, 1.5f)
    };

    private bool mainStoryIntroRunning;
    private bool mainStoryTaskOffered;
    private readonly HashSet<int> mainStorySelectedTasks = new HashSet<int>();
    private readonly HashSet<int> mainStoryCompletedTasks = new HashSet<int>();
    private int mainStoryWaitingSelection;
    private bool mainStoryWaitingCompletion;
    private int mainStoryDeferredNextTask;
    private int mainStoryLineIndex;
    private float mainStoryTimer;
    private bool mainStoryTitleVisible;
    private TextMeshProUGUI broadcastTitle;
    private Image mainStoryPopupImage;
    private Sprite mainStoryDefaultSprite;
    private Sprite mainStoryPlayerSprite;

    // Optional story progression; task selection, completion and rewards remain shared.
    private bool BeginMainStoryIntro()
    {
        if (IsTutorialScene || !playMainStoryIntro || mainStoryLines == null || mainStoryLines.Length == 0)
            return false;
        mainStoryPopupImage = popupRoot.GetComponent<Image>();
        mainStoryDefaultSprite = mainStoryPopupImage.sprite;
        Sprite[] sprites = Resources.LoadAll<Sprite>("playercomment");
        mainStoryPlayerSprite = sprites.Length > 0 ? sprites[0] : mainStoryDefaultSprite;
        GameObject titleObject = CreateTextObject("Broadcast Start Title", popupRoot.transform.parent,
            "配信開始", 80f, Color.white);
        broadcastTitle = titleObject.GetComponent<TextMeshProUGUI>();
        Stretch(broadcastTitle.rectTransform, 0.1f, 0.35f, 0.9f, 0.65f, 0f);
        broadcastTitle.alignment = TextAlignmentOptions.Center;
        broadcastTitle.raycastTarget = false;
        broadcastTitle.fontStyle = FontStyles.Bold;
        titleObject.SetActive(false);
        mainStoryIntroRunning = true;
        mainStoryTaskOffered = false;
        mainStorySelectedTasks.Clear();
        mainStoryCompletedTasks.Clear();
        mainStoryWaitingSelection = 0;
        mainStoryWaitingCompletion = false;
        mainStoryDeferredNextTask = 0;
        mainStoryLineIndex = -1;
        AdvanceMainStoryIntro();
        return true;
    }

    private bool UpdateMainStoryIntro()
    {
        if (!mainStoryIntroRunning) return false;
        if (mainStoryWaitingSelection > 0)
        {
            if (mainStorySelectedTasks.Contains(mainStoryWaitingSelection))
            {
                mainStoryWaitingSelection = 0;
                popupPersistent = false;
                if (!isOpen) AdvanceMainStoryIntro();
                else mainStoryTimer = 0f;
            }
            return true;
        }
        if (isOpen) return true;
        if (mainStoryWaitingCompletion)
        {
            if (mainStoryCompletedTasks.Contains(mainStoryLines[mainStoryLineIndex].waitForCompletedTaskId))
            {
                mainStoryWaitingCompletion = false;
                PresentMainStoryLine();
            }
            return true;
        }
        if (!mainStoryTitleVisible && !popupRoot.activeSelf && mainStoryTimer > 0f)
            ShowMainStoryLinePopup(mainStoryLines[mainStoryLineIndex]);
        mainStoryTimer -= Time.deltaTime;
        if (mainStoryTimer <= 0f) AdvanceMainStoryIntro();
        return true;
    }

    private void AdvanceMainStoryIntro()
    {
        if (!mainStoryTitleVisible && mainStoryLineIndex >= 0 &&
            mainStoryLines[mainStoryLineIndex] != null && mainStoryLines[mainStoryLineIndex].showBroadcastTitle)
        {
            popupRoot.SetActive(false);
            broadcastTitle.gameObject.SetActive(true);
            mainStoryTitleVisible = true;
            mainStoryTimer = Mathf.Max(0.01f, broadcastTitleSeconds);
            return;
        }
        mainStoryTitleVisible = false;
        broadcastTitle.gameObject.SetActive(false);
        do { mainStoryLineIndex++; }
        while (mainStoryLineIndex < mainStoryLines.Length && mainStoryLines[mainStoryLineIndex] == null);
        if (mainStoryLineIndex >= mainStoryLines.Length)
        {
            mainStoryIntroRunning = false;
            popupPersistent = false;
            mainStoryPopupImage.sprite = mainStoryDefaultSprite;
            popupSenderLabel.text = string.Empty;
            popupRoot.SetActive(false);
            Destroy(broadcastTitle.gameObject);
            if (mainStoryDeferredNextTask > 0) OfferPhaseTask(mainStoryDeferredNextTask);
            else if (!playIntroTutorial && !mainStoryTaskOffered) OfferPhaseTask(1);
            return;
        }
        MainStoryLine nextLine = mainStoryLines[mainStoryLineIndex];
        if (nextLine.waitForCompletedTaskId > 0 && !mainStoryCompletedTasks.Contains(nextLine.waitForCompletedTaskId))
        {
            mainStoryWaitingCompletion = true;
            popupRoot.SetActive(false);
            return;
        }
        PresentMainStoryLine();
    }

    private void PresentMainStoryLine()
    {
        MainStoryLine line = mainStoryLines[mainStoryLineIndex];
        mainStoryTimer = Mathf.Max(0.01f, line.seconds);
        string sender = !string.IsNullOrEmpty(line.senderName) ? line.senderName :
            line.player ? "プレイヤー" : "リスナー";
        bool isTask = line.taskId > 0;
        if (isTask)
        {
            CreateSelectableTaskComment(line.text, rect => AcceptPhaseTask(rect, line.taskId));
            mainStoryTaskOffered = true;
            introPhaseStep = 5;
            mainStoryWaitingSelection = line.waitForTaskSelection ? line.taskId : 0;
        }
        else
            AddTutorialHistoryComment(sender + "：" + line.text, normalTextColor);
        ShowMainStoryLinePopup(line);
    }

    private void ShowMainStoryLinePopup(MainStoryLine line)
    {
        bool isTask = line.taskId > 0;
        ShowPopup(line.text, popupTextColor, mainStoryTimer,
            isTaskComment: isTask,
            frame: isTask ? null : line.player ? mainStoryPlayerSprite : mainStoryDefaultSprite,
            keepVisible: mainStoryWaitingSelection > 0);
        popupSenderLabel.text = !string.IsNullOrEmpty(line.senderName) ? line.senderName :
            line.player ? "プレイヤー" : "リスナー";
    }

    private bool HandleMainStoryTaskCompletion(int taskId)
    {
        if (!mainStoryIntroRunning) return false;
        mainStoryCompletedTasks.Add(taskId);
        if (taskId != 1) return false;
        mainStoryDeferredNextTask = 2;
        return true;
    }

    private bool MainStoryOwnsPopup => mainStoryIntroRunning && !mainStoryWaitingCompletion;
}
