using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class TimedWorldInteraction : MonoBehaviour
{
    private const float MovementCancelThreshold = 0.01f;

    private SubjiPlayerMovement player;
    private Action completedAction;
    private float duration;
    private float elapsed;
    private GUIStyle labelStyle;

    public bool IsRunning { get; private set; }

    private void Awake()
    {
        player = GetComponent<SubjiPlayerMovement>();
        enabled = false;
    }

    public bool Begin(float seconds, Action onCompleted)
    {
        if (IsRunning || player == null || onCompleted == null)
            return false;

        duration = Mathf.Max(0.01f, seconds);
        elapsed = 0f;
        completedAction = onCompleted;
        IsRunning = true;
        player.SetInteractionMovementLocked(true);
        enabled = true;
        return true;
    }

    private void Update()
    {
        if (player.HasMovementInput(MovementCancelThreshold))
        {
            Cancel();
            return;
        }

        elapsed += Time.deltaTime;
        if (elapsed < duration)
            return;

        Action action = completedAction;
        Finish();
        action.Invoke();
    }

    public void Cancel()
    {
        if (!IsRunning)
            return;

        Finish();
    }

    private void Finish()
    {
        IsRunning = false;
        completedAction = null;
        elapsed = 0f;
        player.SetInteractionMovementLocked(false);
        enabled = false;
    }

    private void OnGUI()
    {
        if (!IsRunning)
            return;

        const float width = 300f;
        const float height = 24f;
        Rect background = new Rect((Screen.width - width) * 0.5f,
            Screen.height * 0.72f, width, height);
        GUI.color = new Color(0f, 0f, 0f, 0.75f);
        GUI.DrawTexture(background, Texture2D.whiteTexture);
        GUI.color = new Color(0.25f, 0.85f, 1f, 1f);
        GUI.DrawTexture(new Rect(background.x + 3f, background.y + 3f,
            (background.width - 6f) * Mathf.Clamp01(elapsed / duration),
            background.height - 6f), Texture2D.whiteTexture);

        if (labelStyle == null)
        {
            labelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold
            };
            labelStyle.normal.textColor = Color.white;
        }

        GUI.color = Color.white;
        GUI.Label(background, "操作中（移動でキャンセル）", labelStyle);
    }

    private void OnDestroy()
    {
        if (IsRunning && player != null)
            player.SetInteractionMovementLocked(false);
    }
}
