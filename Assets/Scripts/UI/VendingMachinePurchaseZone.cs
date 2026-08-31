using UnityEngine;

[DisallowMultipleComponent]
public sealed class VendingMachinePurchaseZone : MonoBehaviour
{
    private CircleCollider2D purchaseTrigger;
    private int playerColliderCount;

    public bool IsPlayerInside => playerColliderCount > 0;

    public void Configure(float range)
    {
        if (purchaseTrigger == null)
        {
            purchaseTrigger = GetComponent<CircleCollider2D>();
            if (purchaseTrigger == null)
                purchaseTrigger = gameObject.AddComponent<CircleCollider2D>();
            purchaseTrigger.isTrigger = true;
        }

        purchaseTrigger.radius = Mathf.Max(0.1f, range);
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
}
