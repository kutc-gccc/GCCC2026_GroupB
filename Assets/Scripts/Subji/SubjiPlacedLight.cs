using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class SubjiPlacedLight : MonoBehaviour
{
    public static readonly HashSet<SubjiPlacedLight> ActiveLights = new();
    public static int Revision { get; private set; }

    [Min(0.1f)] public float radius = 1.5f;
    [Min(0.01f)] public float blurWidth = 0.5f;
    public Color editorColor = new(0.2f, 0.85f, 1f, 0.35f);

    private void OnEnable()
    {
        ActiveLights.Add(this);
        Revision++;
    }

    private void OnDisable()
    {
        ActiveLights.Remove(this);
        Revision++;
    }

    private void OnValidate() => Revision++;

    private void OnDrawGizmos()
    {
        if (Application.isPlaying)
            return;

        Gizmos.color = editorColor;
        Gizmos.DrawSphere(transform.position, radius);
        Gizmos.color = new Color(editorColor.r, editorColor.g, editorColor.b, 1f);
        Gizmos.DrawWireSphere(transform.position, radius + blurWidth);
    }
}
