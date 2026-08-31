using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public sealed class ItemSlotSelector : MonoBehaviour
{
    [Header("Slot Size")]
    [SerializeField] private Vector2 normalSize = new(100f, 100f);
    [SerializeField] private Vector2 selectedSize = new(120f, 120f);

    [Header("Selection")]
    [Min(0)]
    [SerializeField] private int initialSelectedIndex;
    [SerializeField] private bool invertScrollDirection;

    [Header("Comment Item")]
    [Min(0)]
    [SerializeField] private int commentSlotIndex;
    [SerializeField] private JikkenCommentStream commentStream;

    private readonly List<RectTransform> slots = new();
    private int selectedIndex;
    private int knownChildCount = -1;

    public int SelectedIndex => selectedIndex;
    public event Action<int> SelectedSlotClicked;

    private void OnEnable()
    {
        if (commentStream == null)
            commentStream = FindFirstObjectByType<JikkenCommentStream>();

        selectedIndex = initialSelectedIndex;
        RefreshSlots();
    }

    private void Update()
    {
        if (knownChildCount != transform.childCount)
            RefreshSlots();

        if (slots.Count == 0 || Mouse.current == null)
            return;

        float scrollY = Mouse.current.scroll.ReadValue().y;
        if (!Mathf.Approximately(scrollY, 0f))
        {
            int direction = scrollY > 0f ? -1 : 1;
            if (invertScrollDirection)
                direction *= -1;

            Select((selectedIndex + direction + slots.Count) % slots.Count);
        }

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            SelectedSlotClicked?.Invoke(selectedIndex);

            if (selectedIndex == commentSlotIndex)
            {
                if (commentStream == null)
                    commentStream = FindFirstObjectByType<JikkenCommentStream>();
                commentStream?.OpenCommentScreen();
            }
        }
    }

    public void Select(int index)
    {
        if (slots.Count == 0)
            return;

        selectedIndex = Mathf.Clamp(index, 0, slots.Count - 1);
        ApplySizes();
    }

    public void RefreshSlots()
    {
        slots.Clear();
        knownChildCount = transform.childCount;

        for (int i = 0; i < transform.childCount; i++)
        {
            if (transform.GetChild(i) is RectTransform slot)
                slots.Add(slot);
        }

        slots.Sort((left, right) =>
            left.anchoredPosition.x.CompareTo(right.anchoredPosition.x));

        selectedIndex = slots.Count == 0
            ? 0
            : Mathf.Clamp(selectedIndex, 0, slots.Count - 1);
        ApplySizes();
    }

    private void ApplySizes()
    {
        for (int i = 0; i < slots.Count; i++)
            slots[i].sizeDelta = i == selectedIndex ? selectedSize : normalSize;
    }

    private void OnValidate()
    {
        normalSize.x = Mathf.Max(0f, normalSize.x);
        normalSize.y = Mathf.Max(0f, normalSize.y);
        selectedSize.x = Mathf.Max(0f, selectedSize.x);
        selectedSize.y = Mathf.Max(0f, selectedSize.y);

        if (!Application.isPlaying)
        {
            selectedIndex = initialSelectedIndex;
            RefreshSlots();
        }
    }
}
