using UnityEngine;

[DisallowMultipleComponent]
public sealed class SubjiEnemyDetectionTrigger : MonoBehaviour
{
    private SubjiEnemyChase owner;
    private int playerColliderCount;

    public void Configure(SubjiEnemyChase enemyOwner)
    {
        owner = enemyOwner;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponentInParent<SubjiPlayerMovement>() == null)
            return;

        playerColliderCount++;
        owner?.SetPlayerInsideDetectionTrigger(true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponentInParent<SubjiPlayerMovement>() == null)
            return;

        playerColliderCount = Mathf.Max(0, playerColliderCount - 1);
        if (playerColliderCount == 0)
            owner?.SetPlayerInsideDetectionTrigger(false);
    }

    private void OnDisable()
    {
        playerColliderCount = 0;
        owner?.SetPlayerInsideDetectionTrigger(false);
    }
}
