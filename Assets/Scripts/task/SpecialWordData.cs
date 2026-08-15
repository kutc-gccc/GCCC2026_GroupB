using UnityEngine;

[System.Serializable]
public class SpecialWordData
{
    [Header("コメント欄に出現する文字")]
    public string appearanceWord;

    [Header("保存パネルに表示する文字")]
    public string storedWord;
}