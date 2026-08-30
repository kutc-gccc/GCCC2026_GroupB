using UnityEngine;
using UnityEngine.InputSystem;

public sealed class BushHideSpot2D : MonoBehaviour
{
    [Header("隠れる操作")]
    public Key interactKey = Key.E;
    [Min(0.1f)] public float interactionDistance = 1.5f;
    public Vector2 hidingPositionOffset = new(0f, 0.5f);

    private static BushHideSpot2D activeHideSpot;
    private SubjiPlayerMovement player;
    private Vector3 positionBeforeHiding;

    private void Start()
    {
        player = FindFirstObjectByType<SubjiPlayerMovement>();
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

        float distance = Vector2.Distance(transform.position, player.transform.position);
        if (distance <= interactionDistance &&
            Keyboard.current[interactKey].wasPressedThisFrame)
        {
            EnterBush();
        }
    }

    private void EnterBush()
    {
        positionBeforeHiding = player.transform.position;
        player.transform.position = transform.position + (Vector3)hidingPositionOffset;
        player.SetHidden(true);
        activeHideSpot = this;
    }

    private void ExitBush()
    {
        player.transform.position = positionBeforeHiding;
        player.SetHidden(false);
        activeHideSpot = null;
    }

    private void OnDisable()
    {
        if (activeHideSpot == this && player != null)
            ExitBush();
    }
}
