using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class DrinkItemController : MonoBehaviour
{
    public event Action DrinkPurchased;

    [Header("Vending Machine")]
    [SerializeField] private Key purchaseKey = Key.E;
    [Min(0.1f)]
    [FormerlySerializedAs("purchaseDistance")]
    [SerializeField] private float purchaseRange = 2f;
    [Min(0.1f)]
    [SerializeField] private float purchaseDuration = 2f;

    [Header("Drink Slots (left is 1)")]
    [Min(1)]
    [SerializeField] private int firstDrinkSlotNumber = 3;
    [Min(1)]
    [SerializeField] private int secondDrinkSlotNumber = 4;
    [SerializeField] private Sprite drinkSprite;
    [Range(0.1f, 1f)]
    [SerializeField] private float iconScale = 0.9f;

    [Header("Stamina Effect")]
    [Min(0f)]
    [SerializeField] private float effectDuration = 4f;
    [SerializeField] private Color effectGaugeColor = new(0.25f, 1f, 0.45f, 1f);

    private readonly Dictionary<int, Image> drinkIcons = new();
    private ItemSlotSelector selector;
    private SubjiPlayerMovement player;
    private VendingMachinePurchaseZone[] vendingZones;
    private TimedWorldInteraction timedInteraction;

    public Transform[] GetVendingMachineTransforms()
    {
        if (vendingZones == null || vendingZones.Length == 0)
            SetupVendingZones();

        return vendingZones
            .Where(zone => zone != null)
            .Select(zone => zone.transform)
            .ToArray();
    }

    private void Awake()
    {
        selector = GetComponent<ItemSlotSelector>();
        player = FindFirstObjectByType<SubjiPlayerMovement>();
        if (player != null)
            timedInteraction = player.GetComponent<TimedWorldInteraction>() ??
                player.gameObject.AddComponent<TimedWorldInteraction>();
        SetupVendingZones();
        FindDrinkIcons();
    }

    private void OnEnable()
    {
        if (selector == null)
            selector = GetComponent<ItemSlotSelector>();
        if (selector != null)
            selector.SelectedSlotClicked += UseDrink;
    }

    private void OnDisable()
    {
        if (selector != null)
            selector.SelectedSlotClicked -= UseDrink;
    }

    private void Update()
    {
        if (Keyboard.current == null ||
            !Keyboard.current[purchaseKey].wasPressedThisFrame ||
            player == null)
            return;

        if (vendingZones == null || vendingZones.Length == 0)
            SetupVendingZones();

        // E入力が重なる場所では、ブッシュへの出入りを優先する。
        if (BushHideSpot2D.IsPlayerWithinInteractionRange(player))
            return;

        if (vendingZones.Any(zone => zone != null && zone.IsPlayerInside))
        {
            if (timedInteraction == null)
                timedInteraction = player.GetComponent<TimedWorldInteraction>() ??
                    player.gameObject.AddComponent<TimedWorldInteraction>();
            timedInteraction.Begin(purchaseDuration, PurchaseDrink);
        }
    }

    private void PurchaseDrink()
    {
        int[] slotIndices = { firstDrinkSlotNumber - 1, secondDrinkSlotNumber - 1 };
        foreach (int slotIndex in slotIndices)
        {
            if (!drinkIcons.TryGetValue(slotIndex, out Image icon) || icon.gameObject.activeSelf)
                continue;

            icon.gameObject.SetActive(true);
            DrinkPurchased?.Invoke();
            return;
        }
    }

    private void UseDrink(int selectedSlotIndex)
    {
        if (!drinkIcons.TryGetValue(selectedSlotIndex, out Image icon) ||
            !icon.gameObject.activeSelf)
            return;

        if (player == null)
            player = FindFirstObjectByType<SubjiPlayerMovement>();
        if (player == null)
            return;

        player.PreventStaminaDrain(effectDuration, effectGaugeColor);
        icon.gameObject.SetActive(false);
    }

    private void FindDrinkIcons()
    {
        drinkIcons.Clear();
        RectTransform[] slots = GetComponentsInChildren<RectTransform>(true)
            .Where(rect => rect.parent == transform)
            .OrderBy(rect => rect.anchoredPosition.x)
            .ToArray();

        for (int i = 0; i < slots.Length; i++)
        {
            Transform iconTransform = slots[i].Find("DrinkIcon");
            if (iconTransform != null && iconTransform.TryGetComponent(out Image icon))
                drinkIcons[i] = icon;
        }
    }

    private void SetupVendingZones()
    {
        Transform[] vendingMachines = FindObjectsByType<Transform>(FindObjectsSortMode.None)
            .Where(candidate => candidate.name == "RedObject" ||
                candidate.name.StartsWith("RedObject ("))
            .ToArray();

        vendingZones = new VendingMachinePurchaseZone[vendingMachines.Length];
        for (int i = 0; i < vendingMachines.Length; i++)
        {
            var zone = vendingMachines[i].GetComponent<VendingMachinePurchaseZone>();
            if (zone == null)
                zone = vendingMachines[i].gameObject.AddComponent<VendingMachinePurchaseZone>();
            zone.Configure(purchaseRange);
            vendingZones[i] = zone;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        foreach (Transform candidate in FindObjectsByType<Transform>(FindObjectsSortMode.None))
        {
            if (candidate.name == "RedObject" || candidate.name.StartsWith("RedObject ("))
                Gizmos.DrawWireSphere(candidate.position, purchaseRange);
        }
    }
}
