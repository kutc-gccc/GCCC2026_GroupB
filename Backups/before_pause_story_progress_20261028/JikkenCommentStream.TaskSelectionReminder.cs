using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class JikkenCommentStream
{
    [Header("タスク未選択時の共通案内")]
    [Min(0f), InspectorName("案内までの秒数")] public float taskSelectionReminderDelay = 3f;
    [TextArea, InspectorName("案内文")] public string taskSelectionReminderText = "メッセージ機能のタスク欄確認してる？";
    [InspectorName("案内の送り主")] public string taskSelectionReminderSender = "ナビゲーター";

    private readonly HashSet<RectTransform> pendingTaskComments = new HashSet<RectTransform>();
    private float taskSelectionReminderTimer;
    private GameObject taskSelectionReminderRoot;
    private bool taskSelectionReminderShown;

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
            taskSelectionReminderRoot = Instantiate(popupRoot, popupRoot.transform.parent);
            taskSelectionReminderRoot.name = "Task Selection Reminder";
            var rect = taskSelectionReminderRoot.GetComponent<RectTransform>();
            rect.anchoredPosition = popupRoot.GetComponent<RectTransform>().anchoredPosition +
                new Vector2(0f, -popupSize.y - 8f);
            var image = taskSelectionReminderRoot.GetComponent<Image>();
            image.sprite = normalCommentFrame;
            image.raycastTarget = false;
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
