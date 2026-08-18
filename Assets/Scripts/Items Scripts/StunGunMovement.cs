using UnityEngine;

public class StunGunMovement : MonoBehaviour // ファイル名やスペースに合わせて調整
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 触れたオブジェクトが "Player" タグを持っているか確認する
        if (collision.CompareTag("Player"))
        {
            CollectItem();
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