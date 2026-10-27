using UnityEngine;

public class SubjiCameraFollow : MonoBehaviour
{
    public Transform player;
    [Header("カメラ追従設定")]
    [Tooltip("小さいほど素早く、大きいほどゆっくり追従します")]
    [Range(0.01f, 1f)] public float smoothTime = 0.15f;
    private Vector3 followVelocity;
    private bool focusingAttraction;
    private Bounds attractionBounds;
    private Camera viewCamera;
    private float previousOrthographicSize;

    public void FocusAttraction(Bounds bounds)
    {
        if (viewCamera == null) viewCamera = GetComponent<Camera>();
        if (viewCamera == null || !viewCamera.orthographic) return;
        if (!focusingAttraction) previousOrthographicSize = viewCamera.orthographicSize;
        attractionBounds = bounds;
        focusingAttraction = true;
        followVelocity = Vector3.zero;
        UpdateAttractionView();
    }

    public void ResumePlayerFollow()
    {
        if (!focusingAttraction) return;
        focusingAttraction = false;
        if (viewCamera != null) viewCamera.orthographicSize = previousOrthographicSize;
        followVelocity = Vector3.zero;
    }

    private void UpdateAttractionView()
    {
        transform.position = new Vector3(attractionBounds.center.x, attractionBounds.center.y, -10f);
        viewCamera.orthographicSize = Mathf.Max(previousOrthographicSize,
            Mathf.Max(attractionBounds.extents.y,
                attractionBounds.extents.x / Mathf.Max(0.01f, viewCamera.aspect)) * 1.15f);
    }

    private void OnDisable()
    {
        ResumePlayerFollow();
    }

    public void SnapToPlayer()
    {
        if (player == null)
            return;
        followVelocity = Vector3.zero;
        transform.position = new Vector3(player.position.x, player.position.y, -10f);
    }

    void LateUpdate()
    {
        if (focusingAttraction)
        {
            UpdateAttractionView();
            return;
        }
        if (player == null)
            return;

        // Rigidbody2Dの補間後のTransformを参照することで、物理フレーム間も滑らかに追従する。
        Vector3 playerCenter = player.position;

        Vector3 targetPosition = new Vector3(
            playerCenter.x,
            playerCenter.y,
            -10f
        );

        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPosition,
            ref followVelocity,
            smoothTime
        );
    }
}
