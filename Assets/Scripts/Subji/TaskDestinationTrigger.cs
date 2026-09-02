using UnityEngine;

/// <summary>
/// プレイヤーがタスク目的地へ入った時だけ完了を通知します。
/// 毎フレームの距離計算は行いません。
/// </summary>
public class TaskDestinationTrigger : MonoBehaviour
{
    private JikkenCommentStream owner;
    private bool notified;

    public void Configure(JikkenCommentStream taskOwner)
    {
        owner = taskOwner;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (notified || owner == null ||
            other.GetComponentInParent<SubjiPlayerMovement>() == null)
            return;

        notified = true;
        owner.NotifyTaskDestinationReached();
    }
}
