using UnityEngine;

public class SwordMovement : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 触れたオブジェクトが "Player" タグを持っているか確認する
        if (collision.CompareTag("Player"))
        {
            // KeyItemをすでに取得しているか確認する
            if (KeyItem.HasKey)
            {
                CollectItem();
            }
            else
            {
                Debug.Log("まだ鍵を持っていません！");
            }
        }
    }

    /// <summary>
    /// アイテムを取得した時の処理をまとめた関数
    /// </summary>
    private void CollectItem()
    {
        // ここにスコア加算や効果音の再生などの処理を追加できます

        // アイテムオブジェクトを消去する
        Destroy(gameObject);
    }
}