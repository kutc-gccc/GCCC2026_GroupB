using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class JikkenCommentStream
{
    [Header("タスク未選択時の共通案内")]
    [InspectorName("ナビゲーターの案内を表示")] public bool showTaskSelectionReminder;
    [Min(0f), InspectorName("案内までの秒数")] public float taskSelectionReminderDelay = 3f;
    [TextArea, InspectorName("案内文")] public string taskSelectionReminderText = "メッセージ機能のタスク欄確認してる？";
    [InspectorName("案内の送り主")] public string taskSelectionReminderSender = "ナビゲーター";

    private readonly HashSet<RectTransform> pendingTaskComments = new HashSet<RectTransform>();
    private float taskSelectionReminderTimer;
    private GameObject taskSelectionReminderRoot;
    private bool taskSelectionReminderShown;
    private float selectedTaskIdleSeconds;
    private bool selectedTaskActivitySeen;
    private bool selectedTaskHintShown;
    private float selectedTaskHintUntil;
    private GameObject selectedTaskHintRoot;

    private void UpdateSelectedTaskIdleReminder()
    {
        if (selectedTaskHintRoot != null)
            selectedTaskHintRoot.SetActive(selectedTask != null && !isOpen && Time.time < selectedTaskHintUntil);
        if (IsTutorialScene || selectedTask == null || Time.timeScale <= 0f || isOpen ||
            playerMovement == null || !playerMovement.isActiveAndEnabled) return;
        if ((phaseInteraction != null && phaseInteraction.IsRunning) || qteIndex >= 0 || playerMovement.IsHidden)
            selectedTaskActivitySeen = true;
        if (selectedTaskActivitySeen || selectedTaskHintShown) return;
        selectedTaskIdleSeconds += Time.deltaTime;
        if (selectedTaskIdleSeconds < 8f || popupRoot == null) return;
        if (selectedTaskHintRoot == null)
            selectedTaskHintRoot = CreateTaskReminderPopup("Selected Task Hint");
        const string message = "目的地はミニマップに書いてあるよ。近くでEを押すと反応するよ";
        var body = selectedTaskHintRoot.transform.Find("Popup Comment Text").GetComponent<TextMeshProUGUI>();
        var sender = selectedTaskHintRoot.transform.Find("Popup Sender").GetComponent<TextMeshProUGUI>();
        body.text = message;
        body.color = popupTextColor;
        sender.text = "ナビゲーター";
        selectedTaskHintUntil = Time.time + 4f;
        selectedTaskHintRoot.SetActive(true);
        selectedTaskHintShown = true;
        AddTutorialHistoryComment("ナビゲーター：" + message, normalTextColor);
    }

    private GameObject CreateTaskReminderPopup(string objectName)
    {
        GameObject root = Instantiate(popupRoot, popupRoot.transform.parent);
        root.name = objectName;
        var rect = root.GetComponent<RectTransform>();
        rect.anchoredPosition = popupRoot.GetComponent<RectTransform>().anchoredPosition +
            new Vector2(0f, -popupSize.y - 8f);
        var image = root.GetComponent<Image>();
        image.sprite = normalCommentFrame;
        image.raycastTarget = false;
        return root;
    }

    private void RegisterPendingTask(RectTransform comment)
    {
        if (IsTutorialScene) return;
        if (pendingTaskComments.Count == 0) taskSelectionReminderTimer = 0f;
        pendingTaskComments.Add(comment);
    }

    private void PendingTaskSelected(RectTransform comment)
    {
        pendingTaskComments.Remove(comment);
        taskSelectionReminderTimer = 0f;
        taskSelectionReminderShown = false;
        if (taskSelectionReminderRoot != null) taskSelectionReminderRoot.SetActive(false);
        if (popupPersistent)
        {
            popupPersistent = false;
            popupRoot.SetActive(false);
        }
    }

    private void UpdateTaskSelectionReminder()
    {
        if (!showTaskSelectionReminder)
        {
            if (taskSelectionReminderRoot != null) taskSelectionReminderRoot.SetActive(false);
            taskSelectionReminderTimer = 0f;
            taskSelectionReminderShown = false;
            return;
        }
        if (IsTutorialScene || Time.timeScale <= 0f || playerMovement == null ||
            !playerMovement.isActiveAndEnabled) return;
        if (selectedTask != null || pendingTaskComments.Count == 0)
        {
            taskSelectionReminderTimer = 0f;
            if (taskSelectionReminderRoot != null) taskSelectionReminderRoot.SetActive(false);
            return;
        }
        if (taskSelectionReminderShown) return;
        taskSelectionReminderTimer += Time.deltaTime;
        if (taskSelectionReminderTimer < taskSelectionReminderDelay || popupRoot == null) return;
        if (taskSelectionReminderRoot == null)
        {
            taskSelectionReminderRoot = CreateTaskReminderPopup("Task Selection Reminder");
        }
        var body = taskSelectionReminderRoot.transform.Find("Popup Comment Text").GetComponent<TextMeshProUGUI>();
        var sender = taskSelectionReminderRoot.transform.Find("Popup Sender").GetComponent<TextMeshProUGUI>();
        body.text = taskSelectionReminderText;
        body.color = popupTextColor;
        sender.text = taskSelectionReminderSender;
        taskSelectionReminderRoot.SetActive(true);
        taskSelectionReminderShown = true;
        AddTutorialHistoryComment(taskSelectionReminderSender + "：" + taskSelectionReminderText, normalTextColor);
    }
}
