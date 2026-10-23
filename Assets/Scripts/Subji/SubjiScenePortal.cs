using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>通常マップを保持したまま屋内へ入り、出口から元の位置へ戻る。</summary>
[DisallowMultipleComponent]
public sealed class SubjiScenePortal : MonoBehaviour
{
    public bool returnToMap;
    public string interiorScenePath = "Assets/Scenes/subji/HauntedHouse.unity";
    [Min(0.1f)] public float interactionRadius = 2.5f;
    [Tooltip("未指定なら、Eを押した場所へ戻ります")]
    public Transform returnPoint;
    public static event System.Action<SubjiScenePortal> GoalReturned;

    private static SubjiScenePortal currentVisit;
    private readonly List<GameObject> suspendedRoots = new();
    private readonly List<Behaviour> suspendedBehaviours = new();
    private SubjiPlayerMovement player;
    private Scene interiorScene;
    private Vector3 outdoorPosition;
    private bool busy;
    private GUIStyle hintStyle;

    public bool IsVisiting => currentVisit == this;
    public bool IsBusy => busy;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession()
    {
        currentVisit = null;
        GoalReturned = null;
    }

    private void Start()
    {
        if (!returnToMap)
            player = FindFirstObjectByType<SubjiPlayerMovement>();
    }

    private bool CanEnter => !returnToMap && !busy && currentVisit == null &&
        player != null && player.isActiveAndEnabled && !player.IsHidden &&
        Time.timeScale > 0f &&
        ((Vector2)(player.transform.position - transform.position)).sqrMagnitude <=
        interactionRadius * interactionRadius;

    private void Update()
    {
        if (CanEnter && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            Enter(player);
    }

    public void Enter(SubjiPlayerMovement visitor)
    {
        if (returnToMap || busy || currentVisit != null || visitor == null)
            return;
        if (!Application.CanStreamedLevelBeLoaded(interiorScenePath))
        {
            Debug.LogError($"屋内シーンがビルドに登録されていません: {interiorScenePath}", this);
            return;
        }
        player = visitor;
        StartCoroutine(EnterRoutine());
    }

    private IEnumerator EnterRoutine()
    {
        busy = true;
        player.GetComponent<TimedWorldInteraction>()?.Cancel();
        player.SetInteractionMovementLocked(true);
        yield return SceneManager.LoadSceneAsync(interiorScenePath, LoadSceneMode.Additive);
        interiorScene = SceneManager.GetSceneByPath(interiorScenePath);
        SubjiInteriorStage stage = null;
        if (interiorScene.IsValid() && interiorScene.isLoaded)
        {
            foreach (GameObject root in interiorScene.GetRootGameObjects())
            {
                stage = root.GetComponentInChildren<SubjiInteriorStage>();
                if (stage != null)
                    break;
            }
        }
        if (stage == null || stage.startPoint == null || stage.movementArea == null)
        {
            Debug.LogError("屋内シーンの入場地点・移動範囲の参照を設定してください。", this);
            if (interiorScene.IsValid() && interiorScene.isLoaded)
                yield return SceneManager.UnloadSceneAsync(interiorScene);
            player.SetInteractionMovementLocked(false);
            busy = false;
            yield break;
        }

        outdoorPosition = returnPoint != null ? returnPoint.position : player.transform.position;
        SuspendOutdoorWorld();
        currentVisit = this;
        player.SetInteriorArea(stage.movementArea);
        player.TeleportTo(stage.startPoint.position);
        SnapCamera();
        // ActiveSceneは通常マップのままにし、既存のゲーム再開処理を維持する。
        player.SetInteractionMovementLocked(false);
        busy = false;
    }

    private void SuspendOutdoorWorld()
    {
        foreach (GameObject root in gameObject.scene.GetRootGameObjects())
        {
            if (!root.activeSelf)
                continue;
            // プレイヤー・カメラ・UI・コメントのデータは同じ実体を使い続ける。
            if (root == transform.root.gameObject || root == player.transform.root.gameObject ||
                root.GetComponentInChildren<Camera>(true) != null ||
                root.GetComponentInChildren<Canvas>(true) != null ||
                root.GetComponentInChildren<EventSystem>(true) != null ||
                root.GetComponentInChildren<JikkenCommentStream>(true) != null ||
                root.name == "Player Vision Darkness")
                continue;
            suspendedRoots.Add(root);
            root.SetActive(false);
        }
        foreach (JikkenCommentStream stream in
            FindObjectsByType<JikkenCommentStream>(FindObjectsSortMode.None))
        {
            if (stream.enabled && stream.gameObject.scene == gameObject.scene)
            {
                suspendedBehaviours.Add(stream);
                stream.enabled = false;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (returnToMap && currentVisit == null && !busy &&
            other.TryGetComponent(out SubjiPlayerMovement preview) && preview.IsStandalonePreview)
        {
            busy = true;
            SceneManager.sceneLoaded += PlacePreviewReturnPlayer;
            SceneManager.LoadSceneAsync("Assets/Scenes/subji/jikkennsupace.unity");
            return;
        }
        if (returnToMap && currentVisit != null &&
            other.GetComponentInParent<SubjiPlayerMovement>() == currentVisit.player)
            currentVisit.ReturnToMap();
    }

    private static void PlacePreviewReturnPlayer(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= PlacePreviewReturnPlayer;
        SubjiPlayerMovement visitor = FindFirstObjectByType<SubjiPlayerMovement>();
        foreach (SubjiScenePortal entrance in FindObjectsByType<SubjiScenePortal>(FindObjectsSortMode.None))
        {
            if (entrance.returnToMap || visitor == null)
                continue;
            visitor.TeleportTo(entrance.returnPoint != null
                ? entrance.returnPoint.position : entrance.transform.position);
            if (Camera.main != null && Camera.main.TryGetComponent(out SubjiCameraFollow follow))
                follow.SnapToPlayer();
            break;
        }
    }

    public void ReturnToMap()
    {
        if (currentVisit == this && !busy)
            StartCoroutine(ReturnRoutine());
    }

    private IEnumerator ReturnRoutine()
    {
        busy = true;
        player.GetComponent<TimedWorldInteraction>()?.Cancel();
        player.SetInteractionMovementLocked(true);
        // 通常マップを有効にする前に、屋内のColliderとライトを停止する。
        foreach (GameObject root in interiorScene.GetRootGameObjects())
            root.SetActive(false);
        player.SetInteriorArea(null);
        player.TeleportTo(outdoorPosition);
        foreach (GameObject root in suspendedRoots)
            if (root != null)
                root.SetActive(true);
        foreach (Behaviour behaviour in suspendedBehaviours)
            if (behaviour != null)
                behaviour.enabled = true;
        suspendedRoots.Clear();
        suspendedBehaviours.Clear();
        SnapCamera();
        yield return SceneManager.UnloadSceneAsync(interiorScene);
        currentVisit = null;
        player.SetInteractionMovementLocked(false);
        busy = false;
        GoalReturned?.Invoke(this);
    }

    private void SnapCamera()
    {
        Camera camera = Camera.main;
        if (camera == null)
            return;
        if (camera.TryGetComponent(out SubjiCameraFollow follow))
            follow.SnapToPlayer();
    }

    private void OnGUI()
    {
        if (!CanEnter)
            return;
        hintStyle ??= new GUIStyle(GUI.skin.box)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 22
        };
        GUI.Box(new Rect((Screen.width - 340f) * 0.5f, Screen.height * 0.8f, 340f, 44f),
            "E：お化け屋敷に入る", hintStyle);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = returnToMap ? Color.green : Color.cyan;
        if (returnToMap && TryGetComponent(out BoxCollider2D box))
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(box.offset, box.size);
        }
        else
            Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
}
