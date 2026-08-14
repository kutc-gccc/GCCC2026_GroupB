using UnityEngine;
using TMPro;

public class WordSpawner : MonoBehaviour
{
    public GameObject wordPrefab;

    public Transform canvas;

    public string[] words;

    public float spawnInterval = 2f;

    [Header("開始地点")]
    public RectTransform startPoint;

    [Header("終了地点")]
    public RectTransform endPoint;

    void Start()
    {
        InvokeRepeating(nameof(SpawnWord), 0, spawnInterval);
    }

    void SpawnWord()
    {
        if (words.Length == 0) return;

        GameObject obj = Instantiate(wordPrefab, canvas);

        RectTransform rect = obj.GetComponent<RectTransform>();

        rect.anchoredPosition = startPoint.anchoredPosition;

        obj.GetComponent<TMP_Text>().text =
            words[Random.Range(0, words.Length)];

        WordMover mover = obj.GetComponent<WordMover>();
        mover.targetPoint = endPoint;
    }
}