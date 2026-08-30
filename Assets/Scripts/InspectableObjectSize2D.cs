using UnityEngine;

[ExecuteAlways]
public sealed class InspectableObjectSize2D : MonoBehaviour
{
    [Header("表示サイズ")]
    [Min(0.01f)] public float width = 1f;
    [Min(0.01f)] public float height = 1f;

    [Header("当たり判定の割合")]
    [Range(0.01f, 1f)] public float colliderWidthRatio = 0.85f;
    [Range(0.01f, 1f)] public float colliderHeightRatio = 0.5f;

    [SerializeField] private SpriteRenderer targetRenderer;
    [SerializeField] private BoxCollider2D targetCollider;

    private void OnValidate()
    {
        ApplySize();
    }

    [ContextMenu("サイズを反映")]
    public void ApplySize()
    {
        width = Mathf.Max(0.01f, width);
        height = Mathf.Max(0.01f, height);

        if (targetRenderer != null && targetRenderer.sprite != null)
        {
            Vector2 originalSize = targetRenderer.sprite.bounds.size;
            targetRenderer.transform.localScale = new Vector3(
                width / Mathf.Max(0.01f, originalSize.x),
                height / Mathf.Max(0.01f, originalSize.y),
                1f);
        }

        if (targetCollider != null)
        {
            Vector2 colliderSize = new(
                width * colliderWidthRatio,
                height * colliderHeightRatio);
            targetCollider.size = colliderSize;
            targetCollider.offset = new Vector2(0f, colliderSize.y * 0.5f);
        }
    }
}
