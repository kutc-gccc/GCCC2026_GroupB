using UnityEngine;

public class KeyItem : MonoBehaviour
{
    public static bool HasKey { get; private set; } = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // ログを出して、判定が来ているか確認する
        Debug.Log("何かがKeyItemに触れました: " + collision.gameObject.name);

        if (collision.CompareTag("Player"))
        {
            Debug.Log("プレイヤーを検知！鍵を取得します。");
            HasKey = true;
            Destroy(gameObject);
        }
    }
}