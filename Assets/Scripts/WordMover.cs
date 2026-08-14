using UnityEngine;

public class WordMover : MonoBehaviour
{
    [HideInInspector] public RectTransform targetPoint;

    public float speed = 300f;

    private RectTransform rect;

    void Awake()
    {
        rect = GetComponent<RectTransform>();
    }

    void Update()
    {
        if (targetPoint == null) return;

        // I“_‚ÖŒü‚©‚Á‚ÄˆÚ“®
        rect.anchoredPosition = Vector2.MoveTowards(
            rect.anchoredPosition,
            targetPoint.anchoredPosition,
            speed * Time.deltaTime
        );

        // “’…‚µ‚½‚çíœ
        if (Vector2.Distance(rect.anchoredPosition, targetPoint.anchoredPosition) < 1f)
        {
            Destroy(gameObject);
        }
    }
}