using UnityEngine;

public class SubjiCameraFollow : MonoBehaviour
{
    public Transform player;
    [Header("カメラ追従設定")]
    [Tooltip("小さいほど素早く、大きいほどゆっくり追従します")]
    [Range(0.01f, 1f)] public float smoothTime = 0.15f;
    private Vector3 followVelocity;

    void LateUpdate()
    {
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
