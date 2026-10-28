using System;
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
        new MainStoryLine("なんとか遊園地に忍ことが出来た", true, 3f),
        new MainStoryLine("まだ登録者は100人だけどこの配信で人生一発逆転人気配信者の仲間入りだ", true, 3f),
        new MainStoryLine("よし配信を始めよう。", true, 3f, true),
        new MainStoryLine("はい！配信始まりました。", true, 2.5f),
        new MainStoryLine("うぽつです", false, 1.5f),
        new MainStoryLine("初見です", false, 1.5f),
        new MainStoryLine("今日はいつもと雰囲気が違うね,ここどこ？", false, 1.5f),
        new MainStoryLine("本日はなんと閉園中のL遊園地に潜入しています", true, 2.5f),
        new MainStoryLine("e", false, 0.5f),
        new MainStoryLine("え", false, 0.5f),
        new MainStoryLine("やば", false, 0.5f),
        new MainStoryLine("許可取っているの？", false, 0.5f),
        new MainStoryLine("取ってないよ？でもばればきゃ大丈夫", true, 1.5f),
        new MainStoryLine("企画説明するよ題して遊園地でバスってみた！", true, 2f),
        new MainStoryLine("視聴者のリクエストに応えて遊園地でバズっていくよ", true, 2f),
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
            { senderName = "リスナーA", taskId = 1 }
    };

    private bool mainStoryIntroRunning;
    private bool mainStoryTaskOffered;
    private int mainStoryLineIndex;
    private float mainStoryTimer;
    private bool mainStoryTitleVisible;
    private TextMeshProUGUI broadcastTitle;
    private Image mainStoryPopupImage;
    private Sprite mainStoryDefaultSprite;
    private Sprite mainStoryPlayerSprite;

    // Optional intro: only the two hooks in PhaseTasks.cs depend on this file.
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
        mainStoryLineIndex = -1;
        AdvanceMainStoryIntro();
        return true;
    }

    private bool UpdateMainStoryIntro()
    {
        if (!mainStoryIntroRunning) return false;
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
            mainStoryPopupImage.sprite = mainStoryDefaultSprite;
            popupSenderLabel.text = string.Empty;
            popupRoot.SetActive(false);
            Destroy(broadcastTitle.gameObject);
            if (!playIntroTutorial && !mainStoryTaskOffered) OfferPhaseTask(1);
            return;
        }
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
        }
        else
            AddTutorialHistoryComment(sender + "：" + line.text, normalTextColor);
        ShowPopup(line.text, popupTextColor, mainStoryTimer,
            isTaskComment: isTask,
            frame: isTask ? null : line.player ? mainStoryPlayerSprite : mainStoryDefaultSprite);
        popupSenderLabel.text = sender;
    }
}
