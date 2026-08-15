using UnityEngine;
using TMPro;
using System;

public class CommentManager : MonoBehaviour
{
    // ==================================================
    // 通常コメント
    // ==================================================

    [Header("========== 通常コメント ==========")]

    [Header("コメントPrefab")]
    public GameObject commentPrefab;

    [Header("コメント生成先")]
    public RectTransform commentContent;

    [Header("コメント生成位置")]
    public RectTransform spawnPoint;

    [Header("通常の単語")]
    public string[] words;


    // ==================================================
    // 特殊コメント
    // ==================================================

    [Header("========== 特殊コメント ==========")]

    [Header("特殊コメントPrefab")]
    public GameObject specialWordPrefab;

    [Header("特殊コメント生成先")]
    public RectTransform specialContent;

    [Header("特殊コメント生成位置")]
    public RectTransform specialSpawnPoint;

    [Header("特殊単語")]
    public SpecialWordData[] specialWords;

    [Header("特殊単語の出現確率（％）")]
    [Range(0f, 100f)]
    public float specialSpawnChance = 5f;

    [Header("特殊単語の色")]
    public Color specialWordColor = Color.yellow;


    // ==================================================
    // 特殊コメント保存設定
    // ==================================================

    [Header("========== 特殊コメント保存 ==========")]

    [Header("特殊単語の最大保持数")]
    public int maxSpecialWords = 3;


    // ==================================================
    // 通常コメント設定
    // ==================================================

    [Header("========== 通常コメント設定 ==========")]

    [Header("コメント生成間隔")]
    public float spawnInterval = 1f;

    [Header("通常コメント最大表示数")]
    public int maxComments = 8;

    [Header("通常コメントの縦間隔")]
    public float commentHeight = 60f;


    // ==================================================
    // 特殊コメント設定
    // ==================================================

    [Header("========== 特殊コメント設定 ==========")]

    [Header("特殊コメントの縦間隔")]
    public float specialCommentHeight = 60f;


    // ==================================================
    // 特殊単語フラグ
    // ==================================================

    private bool[] specialFlags;


    // ==================================================
    // 特殊単語出現イベント
    // int = Element番号
    // ==================================================

    public event Action<int> OnSpecialWordAppeared;


    private float timer;


    // ==================================================
    // Start
    // ==================================================

    void Start()
    {
        if (specialWords != null)
        {
            specialFlags = new bool[specialWords.Length];

            for (int i = 0; i < specialFlags.Length; i++)
            {
                specialFlags[i] = false;
            }
        }
    }


    // ==================================================
    // Update
    // ==================================================

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= spawnInterval)
        {
            timer = 0f;

            AddComment();
        }
    }


    // ==================================================
    // コメント追加
    // ==================================================

    void AddComment()
    {
        if (commentPrefab == null)
        {
            Debug.LogWarning("Comment Prefabが設定されていません。");
            return;
        }

        if (commentContent == null)
        {
            Debug.LogWarning("Comment Contentが設定されていません。");
            return;
        }

        if (spawnPoint == null)
        {
            Debug.LogWarning("Spawn Pointが設定されていません。");
            return;
        }

        if (words == null || words.Length == 0)
        {
            Debug.LogWarning("通常の単語が設定されていません。");
            return;
        }


        // ==================================================
        // 特殊単語を出せるか確認
        // ==================================================

        bool canSpawnSpecial =
            specialWords != null &&
            specialWords.Length > 0 &&
            specialContent != null &&
            specialSpawnPoint != null &&
            specialWordPrefab != null &&
            specialContent.childCount < maxSpecialWords;


        bool isSpecial = false;

        int specialIndex = -1;


        // ==================================================
        // 特殊単語抽選
        // ==================================================

        if (canSpawnSpecial)
        {
            float randomValue =
                UnityEngine.Random.Range(0f, 100f);

            if (randomValue < specialSpawnChance)
            {
                specialIndex =
                    GetRandomAvailableSpecialIndex();

                if (specialIndex >= 0)
                {
                    isSpecial = true;
                }
            }
        }


        // ==================================================
        // 出す文字を決定
        // ==================================================

        string selectedWord;

        if (isSpecial)
        {
            selectedWord =
                specialWords[specialIndex].appearanceWord;
        }
        else
        {
            selectedWord =
                words[
                    UnityEngine.Random.Range(
                        0,
                        words.Length
                    )
                ];
        }


        // ==================================================
        // 通常コメントを生成
        // ==================================================

        GameObject newComment =
            Instantiate(
                commentPrefab,
                commentContent
            );


        TMP_Text text =
            newComment.GetComponent<TMP_Text>();


        if (text != null)
        {
            text.text = selectedWord;

            if (isSpecial)
            {
                text.color = specialWordColor;
            }
            else
            {
                text.color = Color.white;
            }
        }


        // 一番上にする

        newComment.transform.SetAsFirstSibling();


        // 通常コメントを並べる

        ArrangeComments();


        // 最大数を超えたら一番古いものを削除

        if (commentContent.childCount > maxComments)
        {
            Destroy(
                commentContent
                    .GetChild(
                        commentContent.childCount - 1
                    )
                    .gameObject
            );
        }


        // ==================================================
        // 特殊単語だった場合
        // ==================================================

        if (isSpecial)
        {
            HandleSpecialWord(specialIndex);
        }
    }


    // ==================================================
    // 特殊単語処理
    // ==================================================

    void HandleSpecialWord(int index)
    {
        if (index < 0)
            return;

        // Element番号のフラグをON

        specialFlags[index] = true;


        // 保存用の文字

        string storedWord =
            specialWords[index].storedWord;


        // 特殊コメント欄に生成

        AddSpecialWord(storedWord);


        // 外部スクリプトへ通知

        OnSpecialWordAppeared?.Invoke(index);


        Debug.Log(
            "特殊単語 Element "
            + index
            + " が出現しました。"
        );
    }


    // ==================================================
    // 特殊コメントを生成
    // ==================================================

    void AddSpecialWord(string word)
    {
        if (specialWordPrefab == null)
        {
            Debug.LogWarning(
                "Special Word Prefabが設定されていません。"
            );

            return;
        }

        if (specialContent == null)
        {
            Debug.LogWarning(
                "Special Contentが設定されていません。"
            );

            return;
        }

        if (specialSpawnPoint == null)
        {
            Debug.LogWarning(
                "Special Spawn Pointが設定されていません。"
            );

            return;
        }


        // 最大数チェック

        if (specialContent.childCount >= maxSpecialWords)
        {
            return;
        }


        // ==================================================
        // 特殊文字を生成
        // ==================================================

        GameObject newSpecial =
            Instantiate(
                specialWordPrefab,
                specialContent
            );


        // ==================================================
        // 文字設定
        // ==================================================

        TMP_Text text =
            newSpecial.GetComponent<TMP_Text>();


        if (text == null)
        {
            text =
                newSpecial.GetComponentInChildren<TMP_Text>();
        }


        if (text != null)
        {
            text.text = word;
            text.color = specialWordColor;
        }


        // ==================================================
        // 生成位置をSpawn Pointに合わせる
        // ==================================================

        RectTransform rect =
            newSpecial.GetComponent<RectTransform>();


        if (rect != null)
        {
            rect.position =
                specialSpawnPoint.position;
        }


        // ==================================================
        // 特殊コメントを並べる
        // ==================================================

        ArrangeSpecialComments();
    }


    // ==================================================
    // 通常コメントを並べる
    // ==================================================

    void ArrangeComments()
    {
        for (int i = 0;
             i < commentContent.childCount;
             i++)
        {
            RectTransform rect =
                commentContent
                    .GetChild(i)
                    .GetComponent<RectTransform>();


            if (rect == null)
                continue;


            rect.position = new Vector3(
                spawnPoint.position.x,

                spawnPoint.position.y
                - (i * commentHeight),

                spawnPoint.position.z
            );
        }
    }


    // ==================================================
    // 特殊コメントを並べる
    // ==================================================

    void ArrangeSpecialComments()
    {
        for (int i = 0;
             i < specialContent.childCount;
             i++)
        {
            RectTransform rect =
                specialContent
                    .GetChild(i)
                    .GetComponent<RectTransform>();


            if (rect == null)
                continue;


            rect.position = new Vector3(
                specialSpawnPoint.position.x,

                specialSpawnPoint.position.y
                - (i * specialCommentHeight),

                specialSpawnPoint.position.z
            );
        }
    }


    // ==================================================
    // まだ出ていない特殊単語をランダム取得
    // ==================================================

    int GetRandomAvailableSpecialIndex()
    {
        if (specialFlags == null)
            return -1;


        int availableCount = 0;


        for (int i = 0;
             i < specialFlags.Length;
             i++)
        {
            if (!specialFlags[i])
            {
                availableCount++;
            }
        }


        if (availableCount == 0)
        {
            return -1;
        }


        int randomIndex =
            UnityEngine.Random.Range(
                0,
                availableCount
            );


        int count = 0;


        for (int i = 0;
             i < specialFlags.Length;
             i++)
        {
            if (!specialFlags[i])
            {
                if (count == randomIndex)
                {
                    return i;
                }

                count++;
            }
        }


        return -1;
    }


    // ==================================================
    // 特殊単語のフラグ取得
    // ==================================================

    public bool GetSpecialFlag(int index)
    {
        if (specialFlags == null)
            return false;

        if (index < 0 ||
            index >= specialFlags.Length)
            return false;

        return specialFlags[index];
    }


    // ==================================================
    // 特殊単語のフラグをリセット
    // ==================================================

    public void ResetSpecialFlag(int index)
    {
        if (specialFlags == null)
            return;

        if (index < 0 ||
            index >= specialFlags.Length)
            return;

        specialFlags[index] = false;
    }
}