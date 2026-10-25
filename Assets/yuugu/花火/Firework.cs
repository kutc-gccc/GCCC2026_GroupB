using UnityEngine;

public class Firework : MonoBehaviour
{
    [Header("--- スプライト設定 ---")]
    [SerializeField] private Sprite 打ち上げ画像;
    [SerializeField] private Sprite 爆発画像;

    [Header("--- サイズ設定 ---")]
    [SerializeField] private Vector2 打ち上げサイズ = new Vector2(1f, 1f);
    [SerializeField] private Vector2 爆発サイズ = new Vector2(2f, 2f);

    [Header("--- 動作・移動設定 ---")]
    [SerializeField] private float 上昇スピード = 8f;
    [SerializeField] private float 上昇する高さ = 5f;
    [SerializeField] private float 消えるまでの時間 = 1.0f; // 花火が咲いてから消えるまでの時間（秒）

    private SpriteRenderer spriteRenderer;
    private bool isBurst = false;
    private float startY;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        startY = transform.position.y;

        // 初期状態（打ち上げ中）のスプライトとサイズをセット
        if (打ち上げ画像 != null)
        {
            spriteRenderer.sprite = 打ち上げ画像;
        }
        transform.localScale = new Vector3(打ち上げサイズ.x, 打ち上げサイズ.y, 1f);
    }

    void Update()
    {
        if (!isBurst)
        {
            // 上に向かって移動する
            transform.Translate(Vector3.up * 上昇スピード * Time.deltaTime);

            // 指定した高さに達したら花火を開く
            if (transform.position.y >= startY + 上昇する高さ)
            {
                Burst();
            }
        }
    }

    void Burst()
    {
        isBurst = true;

        // スプライトを「空中で開いた画像」に切り替える
        if (爆発画像 != null)
        {
            spriteRenderer.sprite = 爆発画像;
        }

        // 開いた瞬間にサイズを爆発用に変更する
        transform.localScale = new Vector3(爆発サイズ.x, 爆発サイズ.y, 1f);

        // 指定した時間（秒）が経過したあとにオブジェクトを消去する
        Destroy(gameObject, 消えるまでの時間);
    }
}