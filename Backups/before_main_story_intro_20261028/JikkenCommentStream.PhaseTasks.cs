using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public partial class JikkenCommentStream
{
    [Header("フェーズ1・2 共通設定")]
    public bool enablePhaseTwoTasks;
    [Header("導入コメントの時間")]
    [InspectorName("最初の移動説明まで（秒）")]
    [Min(0f)] public float phaseIntroDelay = 1f;
    [InspectorName("移動を続ける時間（秒）")]
    [Min(0.01f)] public float phaseIntroMovementSeconds = 3f;
    [InspectorName("ダッシュ後の説明間隔（秒）")]
    [Min(0f)] public float phaseIntroAfterDashDelay = 1f;
    [InspectorName("うぽつコメントまで（秒）")]
    [Min(0f)] public float phaseIntroViewerDelay = 0.5f;
    public Transform fountainDestination;
    public Transform signLocation;
    public Transform freeFallBoardingPoint;
    public Transform freeFallExitPoint;
    [InspectorName("フリーフォールのアニメーション")]
    public Animator freeFallAnimator;
    public Transform freeFallSeat;
    public Transform trainWhistlePoint;
    [InspectorName("汽笛のアニメーション")]
    public Animator trainWhistleAnimator;
    public SubjiScenePortal hauntedHouseEntrance;
    [Min(0.01f)] public float vendingSeconds = 1f;
    [Min(0.01f)] public float bushSeconds = 0.5f;
    [Min(0.01f)] public float freeFallSeconds = 3f;
    [Min(0.01f)] public float whistleSeconds = 4f;
    [Min(0f)] public float whistleAlertSeconds = 2f;
    [Min(0.1f)] public float phaseInteractionRange = 3f;
    [InspectorName("汽車の操作範囲倍率")]
    [Min(0.1f)] public float trainInteractionRangeMultiplier = 3f;
    [Min(0.1f)] public float whistleAlertRadius = 30f;
    public AudioClip whistleClip;
    [Range(0f, 1f)] public float whistleVolume = 1f;
    public Key[] signSequence = { Key.UpArrow, Key.DownArrow, Key.LeftArrow,
        Key.DownArrow, Key.DownArrow, Key.RightArrow };
    [Min(0)] public int tutorialSubscriberReward = 100;
    [Min(0)] public int phaseTwoSubscriberReward = 5000;
    [Tooltip("出現順は開始時にシャッフル。7は必ず含まれます")]
    [SerializeField] private int[] phaseTwoOrder = Array.Empty<int>();
    [SerializeField] private int currentPhase = 1;
    [SerializeField] private bool phaseTwoCompleted;
    private static readonly Key[] SignArrowKeys = { Key.UpArrow, Key.DownArrow, Key.LeftArrow, Key.RightArrow };

    private TimedWorldInteraction phaseInteraction;
    private AudioSource whistleSource;
    private SubjiEnemyChase[] phaseEnemies;
    private SubjiEnemyChase[] normalPhaseEnemies;
    private SubjiEnemyChase[] guardPhaseEnemies;
    private readonly List<SubjiEnemyChase> ridingPausedEnemies = new();
    private int phaseOrderIndex;
    private int introPhaseStep;
    private float introPhaseTimer;
    private float movingTutorialSeconds;
    private int qteIndex = -1;
    private bool whistleRunning;
    private bool whistleAlerted;
    private bool ridingFreeFall;
    private Vector3 freeFallSeatRestPosition;
    private SubjiCameraFollow freeFallCamera;
    private GUIStyle phaseHintStyle;

    private void InitializePhaseTasks()
    {
        if (playerMovement == null)
        {
            Debug.LogError("フェーズタスクにプレイヤーが設定されていません", this);
            return;
        }
        phaseInteraction = playerMovement.GetComponent<TimedWorldInteraction>() ??
            playerMovement.gameObject.AddComponent<TimedWorldInteraction>();
        phaseInteraction.Ended += OnPhaseInteractionEnded;
        AlignFreeFallInteractionPoints();
        if (freeFallSeat != null) freeFallSeatRestPosition = freeFallSeat.localPosition;
        if (drinkItemController != null)
            drinkItemController.SetPurchaseDuration(vendingSeconds);
        foreach (BushHideSpot2D bush in FindObjectsByType<BushHideSpot2D>(FindObjectsSortMode.None))
            bush.interactionDuration = bushSeconds;
        whistleSource = gameObject.AddComponent<AudioSource>();
        whistleSource.playOnAwake = false;
        whistleSource.loop = true;
        whistleSource.clip = whistleClip != null ? whistleClip : Resources.Load<AudioClip>("Audio/train_whistle");
        whistleSource.volume = whistleVolume;
        SubjiScenePortal.GoalReturned += OnHauntedGoalReturned;
        CachePhaseEnemies();
        ApplyPhaseEnemyCounts(2, 0);
        currentPhase = 1;
        phaseTwoCompleted = false;
        introPhaseStep = playIntroTutorial ? 0 : 5;
        introPhaseTimer = playIntroTutorial ? phaseIntroDelay : 0f;
        if (!playIntroTutorial)
            OfferPhaseTask(1);
    }

    private void AlignFreeFallInteractionPoints()
    {
        if (freeFallBoardingPoint == null) return;
        foreach (var attraction in FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
        {
            if (attraction.gameObject.scene != gameObject.scene ||
                !attraction.name.StartsWith("フリーフォール")) continue;
            Vector3 exitOffset = freeFallExitPoint != null
                ? freeFallExitPoint.position - freeFallBoardingPoint.position : Vector3.right * 2f;
            freeFallBoardingPoint.position = new Vector3(attraction.bounds.center.x,
                attraction.bounds.min.y - 1f, attraction.transform.position.z);
            freeFallBoardingPoint.SetParent(attraction.transform, true);
            if (freeFallExitPoint != null)
            {
                freeFallExitPoint.position = freeFallBoardingPoint.position + exitOffset;
                freeFallExitPoint.SetParent(attraction.transform, true);
            }
            break;
        }
    }

    private void CachePhaseEnemies()
    {
        phaseEnemies = FindObjectsByType<SubjiEnemyChase>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Array.Sort(phaseEnemies, (a, b) => string.CompareOrdinal(a.name, b.name));
        var normal = new List<SubjiEnemyChase>();
        var guards = new List<SubjiEnemyChase>();
        foreach (var enemy in phaseEnemies)
        {
            if (enemy.gameObject.scene != gameObject.scene) continue;
            (enemy.movementType == SubjiEnemyChase.MovementType.GuardPost ? guards : normal).Add(enemy);
        }
        normalPhaseEnemies = normal.ToArray();
        guardPhaseEnemies = guards.ToArray();
        foreach (var spawner in FindObjectsByType<SubjiEnemySpawner>(FindObjectsSortMode.None))
            if (spawner.gameObject.scene == gameObject.scene) spawner.enabled = false;
    }

    private void ApplyPhaseEnemyCounts(int normalCount, int guardCount)
    {
        if (greenGuardContainer != null) greenGuardContainer.SetActive(true);
        for (int i = 0; i < normalPhaseEnemies.Length; i++)
            normalPhaseEnemies[i].gameObject.SetActive(i < normalCount);
        for (int i = 0; i < guardPhaseEnemies.Length; i++)
            guardPhaseEnemies[i].gameObject.SetActive(i < guardCount);
        if (normalPhaseEnemies.Length < normalCount || guardPhaseEnemies.Length < guardCount)
            Debug.LogWarning("フェーズの敵配置数が不足しています。シーンの配置を確認してください", this);
    }

    private void UpdatePhaseTasks()
    {
        if (phaseInteraction == null || playerMovement == null) return;
        if (Time.timeScale <= 0f || !playerMovement.isActiveAndEnabled)
        {
            if (whistleRunning || ridingFreeFall) phaseInteraction.Cancel();
            return;
        }
        UpdatePhaseIntro();
        UpdateWhistleAnimation();
        if (whistleRunning && phaseInteraction.IsRunning && !whistleAlerted &&
            phaseInteraction.Elapsed >= whistleAlertSeconds)
        {
            whistleAlerted = true;
            foreach (var enemy in phaseEnemies)
                if (enemy != null && enemy.isActiveAndEnabled &&
                    Vector2.Distance(enemy.transform.position, trainWhistlePoint.position) <= whistleAlertRadius)
                    enemy.InvestigateNoise(trainWhistlePoint.position);
        }
        if (selectedTask == null || isOpen || playerMovement.IsHidden || Keyboard.current == null) return;
        int id = selectedTask.designTaskId;
        if (id != 5 && id != 6 && id != 8) return;
        Transform target = GetPhaseTarget(id);
        if (target == null) return;
        float interactionRange = phaseInteractionRange * (id == 8 ? trainInteractionRangeMultiplier : 1f);
        bool inRange = Vector2.Distance(player.position, target.position) <= interactionRange;
        if (!inRange)
        {
            if (qteIndex >= 0) playerMovement.SetInteractionMovementLocked(false);
            qteIndex = -1;
            if (whistleRunning) phaseInteraction.Cancel();
            return;
        }
        if (id == 5)
        {
            UpdateSignQte();
            return;
        }
        if (phaseInteraction.IsRunning || !Keyboard.current.eKey.wasPressedThisFrame ||
            BushHideSpot2D.IsPlayerWithinInteractionRange(playerMovement)) return;
        if (id == 6)
        {
            if (!phaseInteraction.Begin(freeFallSeconds, CompleteFreeFall, false)) return;
            ridingFreeFall = true;
            if (freeFallAnimator != null)
            {
                var clips = freeFallAnimator.runtimeAnimatorController != null
                    ? freeFallAnimator.runtimeAnimatorController.animationClips : Array.Empty<AnimationClip>();
                freeFallAnimator.speed = clips.Length > 0 ? clips[0].length / Mathf.Max(0.01f, freeFallSeconds) : 1f;
                freeFallAnimator.enabled = true;
                freeFallAnimator.Play(0, 0, 0f);
                freeFallAnimator.Update(0f);
                var attractionRenderer = freeFallAnimator.GetComponent<SpriteRenderer>();
                var mainCamera = Camera.main;
                if (attractionRenderer != null && mainCamera != null)
                {
                    freeFallCamera = mainCamera.GetComponent<SubjiCameraFollow>();
                    Bounds bounds = attractionRenderer.bounds;
                    if (freeFallSeat != null && freeFallSeat.TryGetComponent<SpriteRenderer>(out var seatRenderer))
                        bounds.Encapsulate(seatRenderer.bounds);
                    freeFallCamera?.FocusAttraction(bounds);
                }
            }
            // 共通の隠れ状態により接触判定も無効化。周囲の敵は乗車中だけ停止する。
            playerMovement.SetHidden(true);
            foreach (var enemy in phaseEnemies)
                if (enemy != null && enemy.isActiveAndEnabled)
                {
                    ridingPausedEnemies.Add(enemy);
                    enemy.enabled = false;
                }
        }
        else if (phaseInteraction.Begin(whistleSeconds, CompleteSelectedPhaseTask))
        {
            whistleAlerted = false;
            whistleRunning = true;
            whistleSource.Play();
            UpdateWhistleAnimation();
        }
    }

    private void UpdateWhistleAnimation()
    {
        if (trainWhistleAnimator == null) return;
        bool playing = whistleRunning && phaseInteraction.IsRunning && !isOpen &&
            !playerMovement.IsHidden && Keyboard.current != null && Keyboard.current.eKey.isPressed;
        if (trainWhistleAnimator.enabled == playing) return;
        if (playing)
        {
            trainWhistleAnimator.enabled = true;
            trainWhistleAnimator.Rebind();
            trainWhistleAnimator.Play(0, 0, 0f);
            trainWhistleAnimator.Update(0f);
        }
        else
        {
            trainWhistleAnimator.enabled = false;
            // クリップは子の煙だけを操作する。停止時は残っている煙も消す。
            foreach (Transform smoke in trainWhistleAnimator.transform)
                if (smoke.name.StartsWith("Gemini_Generated_Image_d8v20dd8v20dd8v2_0", StringComparison.Ordinal))
                    smoke.gameObject.SetActive(false);
        }
    }

    private void UpdatePhaseIntro()
    {
        if (introPhaseStep >= 5) return;
        introPhaseTimer -= Time.deltaTime;
        if (introPhaseStep == 0 && introPhaseTimer <= 0f)
        {
            PhaseMessage("WASDで移動することができるよ");
            introPhaseStep = 1;
        }
        else if (introPhaseStep == 1)
        {
            movingTutorialSeconds = playerMovement.HasMovementInput() ? movingTutorialSeconds + Time.deltaTime : 0f;
            if (movingTutorialSeconds >= phaseIntroMovementSeconds)
            {
                PhaseMessage("Shiftでダッシュできるよ");
                introPhaseStep = 2;
            }
        }
        else if (introPhaseStep == 2 && Keyboard.current != null &&
            (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed) &&
            playerMovement.HasMovementInput())
        {
            PhaseMessage("Shiftはゲージ制。限界まで使うと息切れするから要注意");
            introPhaseStep = 3;
            introPhaseTimer = phaseIntroAfterDashDelay;
        }
        else if (introPhaseStep == 3 && introPhaseTimer <= 0f)
        {
            PhaseMessage("その他の説明は配信開始後に説明します。");
            introPhaseStep = 4;
            introPhaseTimer = phaseIntroViewerDelay;
        }
        else if (introPhaseStep == 4 && introPhaseTimer <= 0f)
        {
            PhaseMessage("うぽつです。");
            PhaseMessage("スクロールでメッセージを開き、左のタスクをクリックして開始しよう。右上の×ボタンで閉じられるよ");
            introPhaseStep = 5;
            OfferPhaseTask(1);
        }
    }

    private void PhaseMessage(string message)
    {
        AddTutorialHistoryComment(message, normalTextColor);
        ShowPopup(message, popupTextColor);
    }

    private string PhaseTaskName(int id) => id switch
    {
        1 => "噴水広場に行こう", 2 => "自販機でジュースを買おう",
        3 => "ブッシュの中に隠れよう", 5 => "壁にサインして",
        6 => "フリーーーーフォール!!", 7 => "お化け屋敷の潜入調査",
        8 => "車掌さんごっこ", _ => "タスク"
    };

    private Transform GetPhaseTarget(int id) => id switch
    {
        1 => fountainDestination, 5 => signLocation, 6 => freeFallBoardingPoint,
        7 => hauntedHouseEntrance != null ? hauntedHouseEntrance.transform : null,
        8 => trainWhistlePoint, _ => null
    };

    private void OfferPhaseTask(int id)
    {
        string message = "◆ " + PhaseTaskName(id);
        CreateSelectableTaskComment(message, rect => AcceptPhaseTask(rect, id));
        ShowPopup(message + "（左のタスク欄から開始）", popupTextColor);
    }

    private void AcceptPhaseTask(RectTransform source, int id)
    {
        if (HasActiveTask() || phaseTwoCompleted) return;
        Transform target = GetPhaseTarget(id);
        if ((id == 1 || id >= 5) && target == null)
        {
            ShowPopup("タスク対象が未設定です。フェーズ1・2 共通設定を確認してください", popupTextColor);
            return;
        }
        MarkTaskCommentSelected(source);
        string text = "タスク：" + PhaseTaskName(id);
        SelectTask(new AcceptedTask
        {
            designTaskId = id, displayText = text, sourceComment = source,
            taskView = CreateTaskView(text), destination = target != null ? (Vector2)target.position : Vector2.zero,
            completionType = id == 1 ? TaskCompletionType.Destination :
                id == 2 ? TaskCompletionType.VendingPurchase : id == 3 ? TaskCompletionType.BushHide : TaskCompletionType.PhaseTask,
            subscriberReward = id <= 3 ? tutorialSubscriberReward : phaseTwoSubscriberReward
        });
        if (id == 2)
        {
            drinkItemController?.SetPurchaseDuration(vendingSeconds);
            roadMap?.SetActiveTaskTargets(drinkItemController?.GetVendingMachineTransforms());
        }
        else if (id == 3)
        {
            var bushes = FindObjectsByType<BushHideSpot2D>(FindObjectsSortMode.None);
            var targets = new List<Transform>();
            foreach (var bush in bushes) { bush.interactionDuration = bushSeconds; targets.Add(bush.transform); }
            roadMap?.SetActiveTaskTargets(targets);
        }
        else if (id >= 5) roadMap?.SetActiveTaskTargets(new[] { target });
        activeWorldTaskGuidance = id == 1 ? "噴水の目的地へ移動しよう" :
            id == 7 ? "入口でEを押して入場し、お化け屋敷のゴールへ" :
            "目的地の近くでEを1回押すと操作開始。移動するとキャンセル";
        if (id == 6) activeWorldTaskGuidance = "乗り場でE。乗車中は途中下車できません";
        PhaseMessage(activeWorldTaskGuidance);
        worldTaskGuidanceTimer = tutorialMessageInterval;
    }

    private void AdvancePhaseTasks(AcceptedTask task)
    {
        qteIndex = -1;
        switch (task.designTaskId)
        {
            case 1: OfferPhaseTask(2); break;
            case 2: OfferPhaseTask(3); break;
            case 3:
                currentPhase = 2;
                ApplyPhaseEnemyCounts(4, 3);
                var candidates = new List<int> { 5, 6, 8 };
                candidates.RemoveAt(UnityEngine.Random.Range(0, candidates.Count));
                candidates.Add(7);
                for (int i = candidates.Count - 1; i > 0; i--)
                {
                    int j = UnityEngine.Random.Range(0, i + 1);
                    (candidates[i], candidates[j]) = (candidates[j], candidates[i]);
                }
                phaseTwoOrder = candidates.ToArray();
                phaseOrderIndex = 0;
                PhaseMessage("フェーズ2：遊園地のタスクを3つ達成しよう");
                OfferPhaseTask(phaseTwoOrder[0]);
                break;
            default:
                if (currentPhase != 2 || task.designTaskId < 5) break;
                phaseOrderIndex++;
                if (phaseOrderIndex < phaseTwoOrder.Length) OfferPhaseTask(phaseTwoOrder[phaseOrderIndex]);
                else { phaseTwoCompleted = true; PhaseMessage("フェーズ2のタスクをすべて達成しました！"); }
                break;
        }
    }

    private void UpdateSignQte()
    {
        if (phaseInteraction.IsRunning) return;
        if (qteIndex < 0)
        {
            if (Keyboard.current.eKey.wasPressedThisFrame)
            {
                if (signSequence == null || signSequence.Length == 0) return;
                qteIndex = 0;
                playerMovement.SetInteractionMovementLocked(true);
            }
            return;
        }
        if (Keyboard.current.wKey.isPressed || Keyboard.current.aKey.isPressed ||
            Keyboard.current.sKey.isPressed || Keyboard.current.dKey.isPressed)
        {
            qteIndex = -1;
            playerMovement.SetInteractionMovementLocked(false);
            return;
        }
        // 同一フレームの複数キーで複数段階を進めない。
        Key? pressed = null;
        foreach (var key in SignArrowKeys)
            if (Keyboard.current[key].wasPressedThisFrame)
            {
                if (pressed.HasValue) { qteIndex = 0; return; }
                pressed = key;
            }
        if (!pressed.HasValue) return;
        qteIndex = pressed.Value == signSequence[qteIndex] ? qteIndex + 1 : 0;
        if (qteIndex == signSequence.Length)
        {
            qteIndex = -1;
            playerMovement.SetInteractionMovementLocked(false);
            CompleteSelectedPhaseTask();
        }
    }

    private void CompleteSelectedPhaseTask()
    {
        if (selectedTask?.completionType == TaskCompletionType.PhaseTask) CompleteActiveTask();
    }

    private void CompleteFreeFall()
    {
        if (freeFallExitPoint != null) playerMovement.TeleportTo(freeFallExitPoint.position);
        CompleteSelectedPhaseTask();
    }

    private void OnPhaseInteractionEnded()
    {
        whistleRunning = false;
        UpdateWhistleAnimation();
        if (whistleSource != null) whistleSource.Stop();
        if (ridingFreeFall)
        {
            ridingFreeFall = false;
            if (freeFallCamera != null) freeFallCamera.ResumePlayerFollow();
            freeFallCamera = null;
            if (freeFallAnimator != null) freeFallAnimator.enabled = false;
            if (freeFallSeat != null) freeFallSeat.localPosition = freeFallSeatRestPosition;
            playerMovement.SetHidden(false);
            foreach (var enemy in ridingPausedEnemies) if (enemy != null) enemy.enabled = true;
            ridingPausedEnemies.Clear();
        }
    }

    private void OnHauntedGoalReturned(SubjiScenePortal portal)
    {
        if (portal == hauntedHouseEntrance && selectedTask?.designTaskId == 7)
        {
            CompleteSelectedPhaseTask();
        }
    }

    private void DisposePhaseTasks()
    {
        SubjiScenePortal.GoalReturned -= OnHauntedGoalReturned;
        if (phaseInteraction != null) phaseInteraction.Ended -= OnPhaseInteractionEnded;
        OnPhaseInteractionEnded();
    }

    private void OnDisable()
    {
        if (roadMap != null)
            roadMap.MessageScreenOpen = false;
        if (!Application.isPlaying || !enablePhaseTwoTasks) return;
        if (whistleRunning || ridingFreeFall) phaseInteraction?.Cancel();
        if (qteIndex >= 0)
        {
            qteIndex = -1;
            if (playerMovement != null) playerMovement.SetInteractionMovementLocked(false);
        }
    }

    private void OnGUI()
    {
        using var referenceGui = new SubjiPlayerMovement.ReferenceGuiScope(Screen.width, Screen.height);
        if (!Application.isPlaying || !enablePhaseTwoTasks || selectedTask == null || isOpen || qteIndex < 0) return;
        var arrows = new List<string>();
        foreach (Key key in signSequence)
            arrows.Add(key == Key.UpArrow ? "↑" : key == Key.DownArrow ? "↓" : key == Key.LeftArrow ? "←" : key == Key.RightArrow ? "→" : key.ToString());
        phaseHintStyle ??= new GUIStyle(GUI.skin.box) { fontSize = 24, alignment = TextAnchor.MiddleCenter };
        GUI.Box(new Rect((referenceGui.Width - 600f) * 0.5f, referenceGui.Height * 0.65f, 600f, 70f),
            "サイン：" + string.Join("  ", arrows) + $"\n{qteIndex}/{signSequence.Length}  誤入力で最初から", phaseHintStyle);
    }
}
