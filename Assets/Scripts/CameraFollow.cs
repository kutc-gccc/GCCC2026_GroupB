using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform player;
    private Renderer playerRenderer;

    void Start()
    {
        if (player != null)
            playerRenderer = player.GetComponent<Renderer>();
    }

    void LateUpdate()
    {
        if (player == null)
            return;

        Vector3 playerCenter = playerRenderer != null
            ? playerRenderer.bounds.center
            : player.position;

        transform.position = new Vector3(
            playerCenter.x,
            playerCenter.y,
            -10f
        );
    }
}
