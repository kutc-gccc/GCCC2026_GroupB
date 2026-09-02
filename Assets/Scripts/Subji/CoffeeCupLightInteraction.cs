using System;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public sealed class CoffeeCupLightInteraction : MonoBehaviour
{
    [Min(0.1f)] public float interactionRange = 3f;
    [Min(0.1f)] public float activationDuration = 4f;
    [Min(0.1f)] public float lightRadius = 6f;
    [Min(0.01f)] public float lightBlurWidth = 0.75f;
    [Tooltip("対象画像全体が収まるよう、画像サイズからライト半径を拡張します")]
    public bool fitLightToRendererBounds;
    public Key interactionKey = Key.E;

    public event Action LightActivated;

    private CircleCollider2D interactionTrigger;
    private SubjiPlayerMovement player;
    private TimedWorldInteraction timedInteraction;
    private int playerColliderCount;
    private bool taskActive;
    private bool lightIsOn;

    public static CoffeeCupLightInteraction FindOrCreateInScene(
        string objectNamePrefix, float duration, float radius, float range,
        bool fitToRenderer)
    {
        foreach (CoffeeCupLightInteraction existing in
            FindObjectsByType<CoffeeCupLightInteraction>(FindObjectsSortMode.None))
        {
            if (existing.name.StartsWith(objectNamePrefix))
            {
                existing.ApplyConfiguration(duration, radius, range, fitToRenderer);
                return existing;
            }
        }

        foreach (Transform candidate in
            FindObjectsByType<Transform>(FindObjectsSortMode.None))
        {
            if (!candidate.name.StartsWith(objectNamePrefix))
                continue;

            CoffeeCupLightInteraction interaction =
                candidate.gameObject.AddComponent<CoffeeCupLightInteraction>();
            interaction.ApplyConfiguration(duration, radius, range, fitToRenderer);
            return interaction;
        }
        return null;
    }

    private void ApplyConfiguration(float duration, float radius, float range,
        bool fitToRenderer)
    {
        activationDuration = duration;
        lightRadius = radius;
        interactionRange = range;
        fitLightToRendererBounds = fitToRenderer;
        ConfigureTrigger();
    }

    private void Awake()
    {
        player = FindFirstObjectByType<SubjiPlayerMovement>();
        if (player != null)
            timedInteraction = player.GetComponent<TimedWorldInteraction>() ??
                player.gameObject.AddComponent<TimedWorldInteraction>();
        ConfigureTrigger();
    }

    public void SetTaskActive(bool active)
    {
        taskActive = active && !lightIsOn;
    }

    private void ConfigureTrigger()
    {
        interactionTrigger = GetComponent<CircleCollider2D>();
        if (interactionTrigger == null)
            interactionTrigger = gameObject.AddComponent<CircleCollider2D>();
        interactionTrigger.isTrigger = true;
        interactionTrigger.radius = interactionRange;
    }

    private void Update()
    {
        if (!taskActive || lightIsOn || playerColliderCount <= 0 ||
            Keyboard.current == null ||
            !Keyboard.current[interactionKey].wasPressedThisFrame)
            return;

        if (BushHideSpot2D.IsPlayerWithinInteractionRange(player))
            return;

        if (timedInteraction == null && player != null)
            timedInteraction = player.GetComponent<TimedWorldInteraction>() ??
                player.gameObject.AddComponent<TimedWorldInteraction>();
        timedInteraction?.Begin(activationDuration, TurnOnLight);
    }

    private void TurnOnLight()
    {
        if (lightIsOn)
            return;

        lightIsOn = true;
        taskActive = false;
        SubjiPlacedLight placedLight = GetComponent<SubjiPlacedLight>();
        if (placedLight == null)
            placedLight = gameObject.AddComponent<SubjiPlacedLight>();
        float effectiveRadius = lightRadius;
        if (fitLightToRendererBounds &&
            TryGetComponent(out SpriteRenderer targetRenderer))
            effectiveRadius = Mathf.Max(effectiveRadius,
                ((Vector2)targetRenderer.bounds.extents).magnitude + 1f);
        placedLight.radius = effectiveRadius;
        placedLight.blurWidth = lightBlurWidth;
        LightActivated?.Invoke();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponentInParent<SubjiPlayerMovement>() != null)
            playerColliderCount++;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponentInParent<SubjiPlayerMovement>() != null)
            playerColliderCount = Mathf.Max(0, playerColliderCount - 1);
    }

    private void OnDisable()
    {
        playerColliderCount = 0;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (interactionTrigger != null)
            interactionTrigger.radius = Mathf.Max(0.1f, interactionRange);
    }
#endif
}
