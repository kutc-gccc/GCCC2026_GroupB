using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

/// <summary>Play中の変更は保存せず、入力・タイマー・屋内往復を確認する。</summary>
[InitializeOnLoad]
public static class PhaseTwoPlayModeCheck
{
    private const string RequestKey = "PhaseTwoPlayModeCheck.Request";
    private static IEnumerator check;
    private static Keyboard keyboard;
    private static double deadline;
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    static PhaseTwoPlayModeCheck() => EditorApplication.playModeStateChanged += OnPlayModeChanged;

    [MenuItem("Tools/Tasks/フェーズ2の動作を検証（Play Mode） %#F9")]
    public static void Run()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Play Modeを停止してから実行してください");
        PhaseTwoTaskSetup.Validate();
        SessionState.SetBool(RequestKey, true);
        EditorApplication.EnterPlaymode();
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(RequestKey, false)) return;
        SessionState.SetBool(RequestKey, false);
        deadline = EditorApplication.timeSinceStartup + 90;
        check = Check();
        EditorApplication.update += Tick;
    }

    private static void Tick()
    {
        try
        {
            if (!EditorApplication.isPlaying) { Finish(); return; }
            if (EditorApplication.timeSinceStartup > deadline) throw new TimeoutException("フェーズ検証が時間切れになりました");
            if (!check.MoveNext()) { Debug.Log("PHASE_TWO_PLAYMODE_OK"); Finish(); }
        }
        catch (Exception error) { Debug.LogException(error); Finish(); }
    }

    private static void Finish()
    {
        EditorApplication.update -= Tick;
        (check as IDisposable)?.Dispose();
        check = null;
        if (keyboard != null) { InputSystem.RemoveDevice(keyboard); keyboard = null; }
        if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("PHASE_TWO_CHECK_FAILED: " + message);
    }

    private static object Call(object target, string method, params object[] args) =>
        target.GetType().GetMethod(method, Private).Invoke(target, args);
    private static object Field(object target, string name) => target.GetType().GetField(name, Private).GetValue(target);
    private static void Set(object target, string name, object value) => target.GetType().GetField(name, Private).SetValue(target, value);
    private static bool HasTask(JikkenCommentStream stream) => Field(stream, "selectedTask") != null;
    private static void Accept(JikkenCommentStream stream, int id) => Call(stream, "AcceptPhaseTask", null, id);

    private static IEnumerator Wait(float seconds)
    {
        float until = Time.time + seconds;
        while (Time.time < until) yield return null;
    }

    private static IEnumerator Press(Key key)
    {
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(key));
        yield return Wait(0.1f);
        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        yield return Wait(0.1f);
    }

    // 入れ子のIEnumeratorもEditor更新で進める。
    private static IEnumerator Flatten(IEnumerator routine)
    {
        while (routine.MoveNext())
        {
            if (routine.Current is IEnumerator nested)
            {
                var iterator = Flatten(nested);
                while (iterator.MoveNext()) yield return iterator.Current;
            }
            else yield return routine.Current;
        }
    }

    private static IEnumerator Check() => Flatten(CheckSteps());

    private static IEnumerator CheckSteps()
    {
        yield return Wait(0.3f);
        var stream = UnityEngine.Object.FindFirstObjectByType<JikkenCommentStream>();
        var player = UnityEngine.Object.FindFirstObjectByType<SubjiPlayerMovement>();
        var timer = player.GetComponent<TimedWorldInteraction>();
        Require(stream != null && stream.enablePhaseTwoTasks, "共通進行が無効");
        Require(SubjiEnemyChase.ActiveEnemies.Count == 2, "フェーズ1の敵数");
        foreach (var enemy in SubjiEnemyChase.ActiveEnemies.ToArray()) enemy.enabled = false;
        keyboard = InputSystem.AddDevice<Keyboard>();
        Set(stream, "introPhaseStep", 5);
        player.subscribersPerSecond = 0;
        int initialSubscribers = player.SubscriberCount;

        Accept(stream, 1);
        player.TeleportTo(stream.fountainDestination.position);
        yield return Wait(0.3f);
        Require(!HasTask(stream), "噴水の到達判定");
        Accept(stream, 2);
        var vending = UnityEngine.Object.FindFirstObjectByType<DrinkItemController>();
        var machines = vending.GetVendingMachineTransforms();
        Require(machines.Length > 0, "自販機の対象なし");
        player.TeleportTo(machines[0].position);
        yield return Wait(0.2f);
        yield return Press(Key.E);
        Require(timer.IsRunning, "自販機でEを押して開始できない");
        yield return Wait(stream.vendingSeconds + 0.1f);
        Require(!HasTask(stream), "Eを離した後の自販機購入");

        Accept(stream, 3);
        var bush = UnityEngine.Object.FindFirstObjectByType<BushHideSpot2D>();
        player.TeleportTo(bush.transform.position);
        yield return Wait(0.2f);
        yield return Press(Key.E);
        yield return Wait(stream.bushSeconds + 0.1f);
        Require(player.IsHidden && !HasTask(stream), "ブッシュの完了");
        var order = (int[])Field(stream, "phaseTwoOrder");
        Require(order.Length == 3 && order.Distinct().Count() == 3 && order.Contains(7), "フェーズ2の抽選");
        Require(UnityEngine.Object.FindObjectsByType<SubjiEnemyChase>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Count(e => e.gameObject.activeInHierarchy) == 7, "フェーズ2の敵数");
        foreach (var enemy in SubjiEnemyChase.ActiveEnemies.ToArray()) enemy.enabled = false;
        yield return Press(Key.E);
        Require(!player.IsHidden, "ブッシュから退出できない");
        Require(player.SubscriberCount - initialSubscribers == 3 * stream.tutorialSubscriberReward, "フェーズ1の報酬");

        // 実行時の抽選を検証した後、全4種類の条件を一度ずつ確認する。
        Set(stream, "phaseTwoOrder", new[] { 5, 6, 7, 8 });
        Set(stream, "phaseOrderIndex", 0);
        Accept(stream, 5);
        player.TeleportTo(stream.signLocation.position);
        yield return Wait(0.2f);
        yield return Press(Key.E);
        yield return Press(Key.RightArrow);
        Require(HasTask(stream), "QTE誤入力で完了してしまう");
        foreach (Key key in stream.signSequence) yield return Press(key);
        Require(!HasTask(stream), "QTEの正解入力");

        Accept(stream, 6);
        player.TeleportTo(stream.freeFallBoardingPoint.position);
        yield return Wait(0.2f);
        yield return Press(Key.E);
        Require(timer.IsRunning && player.IsHidden, "フリーフォール乗車");
        yield return Press(Key.W);
        Require(timer.IsRunning, "乗車中に移動で中断してしまう");
        yield return Wait(stream.freeFallSeconds + 0.1f);
        Require(!HasTask(stream) && !player.IsHidden, "フリーフォール降車");

        Accept(stream, 7);
        stream.hauntedHouseEntrance.Enter(player);
        SubjiInteriorStage stage = null;
        while (stage == null || stream.hauntedHouseEntrance.IsBusy)
        {
            stage = UnityEngine.Object.FindFirstObjectByType<SubjiInteriorStage>();
            yield return null;
        }
        var goal = UnityEngine.Object.FindObjectsByType<SubjiScenePortal>(FindObjectsSortMode.None)
            .First(p => p.returnToMap);
        player.TeleportTo(goal.transform.position);
        yield return Wait(0.2f);
        while (stream.hauntedHouseEntrance.IsVisiting || stream.hauntedHouseEntrance.IsBusy) yield return null;
        Require(!HasTask(stream), "お化け屋敷のゴール到達");

        Accept(stream, 8);
        player.TeleportTo(stream.trainWhistlePoint.position);
        yield return Wait(0.2f);
        yield return Press(Key.E);
        Require(((AudioSource)Field(stream, "whistleSource")).isPlaying, "汽笛の再生");
        yield return Press(Key.W);
        Require(!timer.IsRunning && !((AudioSource)Field(stream, "whistleSource")).isPlaying, "移動で汽笛が止まらない");
        yield return Press(Key.E);
        yield return Wait(stream.whistleAlertSeconds + 0.1f);
        Require((bool)Field(stream, "whistleAlerted"), "2秒の警備員反応");
        yield return Wait(stream.whistleSeconds + 0.1f);
        Require(!HasTask(stream) && !((AudioSource)Field(stream, "whistleSource")).isPlaying, "汽笛の完了と停止");
        Require((bool)Field(stream, "phaseTwoCompleted"), "フェーズ2の終了");
        Require(player.SubscriberCount - initialSubscribers == 3 * stream.tutorialSubscriberReward + 4 * stream.phaseTwoSubscriberReward,
            "報酬の重複または不足");
    }
}
