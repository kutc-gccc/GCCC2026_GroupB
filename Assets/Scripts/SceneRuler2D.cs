using System.Collections.Generic;
using UnityEngine;

public sealed class SceneRuler2D : MonoBehaviour
{
    public Vector2 pointA;
    public Vector2 pointB = new(4f, 2f);
    public List<Vector2> additionalPoints = new();
    [HideInInspector] public bool isLoopConnected;
}
