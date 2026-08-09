using UnityEngine;
using TMPro;

public class CoordinateDisplay : MonoBehaviour
{
    public Transform player;
    public TMP_Text coordinateText;

    void Update()
    {
        if (player == null || coordinateText == null)
            return;

        coordinateText.text =
            $"X: {player.position.x:F1}  Y: {player.position.y:F1}";
    }
}
