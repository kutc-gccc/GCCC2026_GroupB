using System;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class BushHideSpot2D : MonoBehaviour
{
    public static event Action PlayerEnteredBush;

    [Header("隠れる操作")]
    public Key interactKey = Key.E;
    [Min(0.1f)] public float interactionDistance = 1.5f;
    public Vector2 hidingPositionOffset = new(0f, 0.5f);
    [Min(0.1f)] public float interactionDuration = 2f;

    private static BushHideSpot2D activeHideSpot;
    private SubjiPlayerMovement player;
    private CircleCollider2D interactionTrigger;
    private Vector3 positionBeforeHiding;
    private TimedWorldInteraction timedInteraction;

    public static bool IsPlayerWithinInteractionRange(SubjiPlayerMovement targetPlayer)
    {
        if (targetPlayer == null)
            return false;

        if (activeHideSpot != null)
            return true;

        foreach (BushHideSpot2D hideSpot in
                 FindObjectsByType<BushHideSpot2D>(FindObjectsSortMode.None))
        {
            if (hideSpot != null && hideSpot.isActiveAndEnabled &&
                hideSpot.ContainsPlayer(targetPlayer))
                return true;
        }

        return false;
    }

    private void Awake()
    {
        SetupInteractionTrigger();
    }

    private void Start()
    {
        player = FindFirstObjectByType<SubjiPlayerMovement>();
        if (player != null)
            timedInteraction = player.GetComponent<TimedWorldInteraction>() ??
                player.gameObject.AddComponent<TimedWorldInteraction>();
    }

    private void Update()
    {
        if (Keyboard.current == null)
            return;

        if (player == null)
            player = FindFirstObjectByType<SubjiPlayerMovement>();
        if (player == null)
            return;

        if (activeHideSpot == this)
        {
            if (Keyboard.current[interactKey].wasPressedThisFrame)
                ExitBush();
            return;
        }

        if (activeHideSpot != null)
            return;

        if (ContainsPlayer(player) && Keyboard.current[interactKey].wasPressedThisFrame)
        {
            if (timedInteraction == null)
                timedInteraction = player.GetComponent<TimedWorldInteraction>() ??
                    player.gameObject.AddComponent<TimedWorldInteraction>();
            timedInteraction.Begin(interactionDuration, EnterBush);
        }
    }

    private void SetupInteractionTrigger()
    {
        CircleCollider2D[] circles = GetComponents<CircleCollider2D>();
        foreach (CircleCollider2D circle in circles)
        {
            if (circle.isTrigger)
            {
                interactionTrigger = circle;
                break;
            }
        }

        if (interactionTrigger == null)
            interactionTrigger = gameObject.AddComponent<CircleCollider2D>();

        interactionTrigger.isTrigger = true;
        interactionTrigger.radius = Mathf.Max(0.1f, interactionDistance);
    }

    private bool ContainsPlayer(SubjiPlayerMovement targetPlayer)
    {
        if (targetPlayer == null)
            return false;

        if (interactionTrigger == null)
            SetupInteractionTrigger();

        return interactionTrigger != null && interactionTrigger.enabled &&
            interactionTrigger.OverlapPoint(targetPlayer.transform.position);
    }

    private void EnterBush()
    {
        positionBeforeHiding = player.transform.position;
        player.transform.position = transform.position + (Vector3)hidingPositionOffset;
        player.SetHidden(true);
        activeHideSpot = this;
        PlayerEnteredBush?.Invoke();
    }

    private void ExitBush()
    {
        player.transform.position = positionBeforeHiding;
        player.SetHidden(false);
        activeHideSpot = null;
    }

    private void OnDisable()
    {
        if (activeHideSpot == this)
        {
            if (player != null)
                ExitBush();
            else
                activeHideSpot = null;
        }
    }
}
