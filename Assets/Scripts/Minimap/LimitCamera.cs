using UnityEngine;

/// <summary>
/// Controls the Minimap and Big Map camera.
/// Provides smooth following, dual zoom modes (close-up minimap vs. expansive big map),
/// smooth mouse panning and scroll-wheel zooming in Big Map mode, and directional player indicators.
/// </summary>
[RequireComponent(typeof(Camera))]
public class LimitCamera : MonoBehaviour
{
    [Header("Target & Follow")]
    [Tooltip("The player GameObject to follow. Will auto-find if unassigned.")]
    public GameObject Player;

    [Tooltip("Height of the camera above the terrain.")]
    public float cameraHeight = 40f;

    [Tooltip("Smoothing speed for following the target.")]
    public float followSpeed = 12f;

    [Tooltip("If true, camera rotates to match the player's forward direction. If false, North is always UP.")]
    public bool rotateWithPlayer = false;

    [Header("Zoom Settings")]
    [Tooltip("Camera orthographic size in Minimap mode (close-up).")]
    public float minimapOrthoSize = 20f;

    [Tooltip("Default camera orthographic size in Fullscreen Big Map mode (wide overview).")]
    public float fullscreenDefaultOrthoSize = 65f;

    [Tooltip("Minimum zoom size (closest) in Big Map mode.")]
    public float minFullscreenOrthoSize = 20f;

    [Tooltip("Maximum zoom size (farthest) in Big Map mode.")]
    public float maxFullscreenOrthoSize = 110f;

    [Tooltip("Speed of smooth zoom transitions.")]
    public float zoomTransitionSpeed = 8f;

    [Tooltip("Mouse scroll-wheel zoom sensitivity in Big Map mode.")]
    public float scrollZoomSensitivity = 18f;

    [Header("Panning (Big Map Mode)")]
    [Tooltip("Mouse drag panning speed.")]
    public float panSpeed = 1.2f;

    [Tooltip("Damping speed for smooth panning.")]
    public float panDamping = 14f;

    [Header("Player Map Marker")]
    [Tooltip("Whether to automatically generate a player directional arrow on the MinimapIcons layer.")]
    public bool autoCreatePlayerMarker = true;

    [Tooltip("Color of the player marker arrow on the map.")]
    public Color playerMarkerColor = new Color(0.2f, 0.85f, 1f, 1f); // Vibrant Cyan/Blue

    // State
    private Camera cam;
    private bool isFullscreen = false;
    private float targetOrthoSize;
    private Vector3 panOffset = Vector3.zero;
    private Vector3 currentPanVelocity;
    private Vector3 lastMousePos;
    private bool isDragging = false;
    private GameObject playerMarkerObj;

    public bool IsFullscreen => isFullscreen;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        if (cam != null)
        {
            cam.orthographic = true;
            cam.orthographicSize = minimapOrthoSize;
        }
        targetOrthoSize = minimapOrthoSize;
    }

    private void Start()
    {
        EnsurePlayerReference();
        if (autoCreatePlayerMarker && Player != null)
        {
            SetupPlayerMarker();
        }
    }

    private void EnsurePlayerReference()
    {
        if (Player == null)
        {
            Player = GameObject.FindGameObjectWithTag("Player");
        }
    }

    private void LateUpdate()
    {
        EnsurePlayerReference();
        if (Player == null) return;

        // 1. Handle Big Map Inputs (Zooming, Panning, Recenter)
        if (isFullscreen)
        {
            HandleBigMapInput();
        }

        // 2. Smooth Zoom Interpolation
        if (cam != null)
        {
            cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetOrthoSize, Time.unscaledDeltaTime * zoomTransitionSpeed);
        }

        // 3. Smooth Camera Position (Player Position + Pan Offset)
        Vector3 playerPos = Player.transform.position;
        Vector3 targetPosition = new Vector3(playerPos.x + panOffset.x, playerPos.y + cameraHeight, playerPos.z + panOffset.z);
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.unscaledDeltaTime * followSpeed);

        // 4. Camera Rotation (North-Up vs. Player-Facing)
        if (rotateWithPlayer)
        {
            float playerYaw = Player.transform.eulerAngles.y;
            transform.rotation = Quaternion.Euler(90f, playerYaw, 0f);
        }
        else
        {
            transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        }

        // 5. Update Player Marker Position & Rotation
        UpdatePlayerMarker();
    }

    private void HandleBigMapInput()
    {
        // Scroll Wheel Zoom
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.005f)
        {
            targetOrthoSize = Mathf.Clamp(targetOrthoSize - scroll * scrollZoomSensitivity, minFullscreenOrthoSize, maxFullscreenOrthoSize);
        }

        // Mouse Drag to Pan
        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(2))
        {
            isDragging = true;
            lastMousePos = Input.mousePosition;
        }
        else if (Input.GetMouseButtonUp(0) || Input.GetMouseButtonUp(2))
        {
            isDragging = false;
        }

        if (isDragging && (Input.GetMouseButton(0) || Input.GetMouseButton(2)))
        {
            Vector3 delta = Input.mousePosition - lastMousePos;
            lastMousePos = Input.mousePosition;

            // Scale pan delta by camera size and screen height for consistent feel
            float screenScale = (cam != null ? cam.orthographicSize * 2f : 60f) / Screen.height;
            float panX = -delta.x * screenScale * panSpeed;
            float panZ = -delta.y * screenScale * panSpeed;

            panOffset += new Vector3(panX, 0f, panZ);
        }

        // Recenter on Player with R key
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetPanAndZoom();
        }
    }

    /// <summary>
    /// Switches camera mode between compact minimap and fullscreen big map.
    /// </summary>
    public void SetFullscreenMode(bool fullscreen)
    {
        isFullscreen = fullscreen;
        if (isFullscreen)
        {
            targetOrthoSize = fullscreenDefaultOrthoSize;
            panOffset = Vector3.zero;
        }
        else
        {
            targetOrthoSize = minimapOrthoSize;
            panOffset = Vector3.zero;
            isDragging = false;
        }
    }

    /// <summary>
    /// Resets panning and zooms back to the default big map view.
    /// </summary>
    public void ResetPanAndZoom()
    {
        panOffset = Vector3.zero;
        if (isFullscreen)
        {
            targetOrthoSize = fullscreenDefaultOrthoSize;
        }
        else
        {
            targetOrthoSize = minimapOrthoSize;
        }
    }

    #region Player Map Marker (Directional Arrow)

    private void SetupPlayerMarker()
    {
        if (playerMarkerObj != null || Player == null) return;

        // Check if a marker already exists
        Transform existing = Player.transform.Find("Minimap_PlayerMarker");
        if (existing != null)
        {
            playerMarkerObj = existing.gameObject;
            return;
        }

        playerMarkerObj = new GameObject("Minimap_PlayerMarker");
        playerMarkerObj.transform.SetParent(Player.transform, false);
        playerMarkerObj.transform.localPosition = new Vector3(0f, 4f, 0f);

        // Layer 11 = MinimapIcons
        playerMarkerObj.layer = 11;

        SpriteRenderer sr = playerMarkerObj.AddComponent<SpriteRenderer>();
        sr.sprite = CreatePlayerArrowSprite();
        sr.color = playerMarkerColor;
        sr.sortingOrder = 100; // Always render on top of terrain

        // Adjust scale so arrow is clearly visible and well-proportioned
        playerMarkerObj.transform.localScale = Vector3.one * 2.2f;
    }

    private void UpdatePlayerMarker()
    {
        if (playerMarkerObj == null)
        {
            if (autoCreatePlayerMarker && Player != null) SetupPlayerMarker();
            return;
        }

        // Ensure marker stays flat pointing up towards the minimap camera
        float playerYaw = Player.transform.eulerAngles.y;
        playerMarkerObj.transform.rotation = Quaternion.Euler(90f, playerYaw, 0f);

        // Adjust marker size slightly in Big Map mode so it remains readable
        float markerScale = isFullscreen ? 3.6f : 2.2f;
        playerMarkerObj.transform.localScale = Vector3.Lerp(playerMarkerObj.transform.localScale, Vector3.one * markerScale, Time.unscaledDeltaTime * 10f);
    }

    /// <summary>
    /// Generates a sharp, antialiased directional navigation arrow sprite.
    /// </summary>
    private static Sprite CreatePlayerArrowSprite()
    {
        int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Color[] cols = new Color[size * size];
        Color clear = new Color(0, 0, 0, 0);

        for (int i = 0; i < cols.Length; i++) cols[i] = clear;

        Vector2 tip = new Vector2(size * 0.5f, size * 0.94f);
        Vector2 leftWing = new Vector2(size * 0.14f, size * 0.12f);
        Vector2 rightWing = new Vector2(size * 0.86f, size * 0.12f);
        Vector2 indent = new Vector2(size * 0.5f, size * 0.32f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                if (IsPointInArrow(p, tip, leftWing, rightWing, indent))
                {
                    // Draw white arrow with subtle darker border
                    bool isBorder = IsNearArrowEdge(p, tip, leftWing, rightWing, indent, 4f);
                    cols[y * size + x] = isBorder ? new Color(0.1f, 0.2f, 0.3f, 0.95f) : Color.white;
                }
            }
        }

        tex.SetPixels(cols);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private static bool IsPointInArrow(Vector2 p, Vector2 tip, Vector2 left, Vector2 right, Vector2 indent)
    {
        // Arrow composed of two triangles: (tip, left, indent) and (tip, indent, right)
        return IsPointInTriangle(p, tip, left, indent) || IsPointInTriangle(p, tip, indent, right);
    }

    private static bool IsNearArrowEdge(Vector2 p, Vector2 tip, Vector2 left, Vector2 right, Vector2 indent, float threshold)
    {
        float d1 = DistanceToSegment(p, tip, left);
        float d2 = DistanceToSegment(p, tip, right);
        float d3 = DistanceToSegment(p, left, indent);
        float d4 = DistanceToSegment(p, right, indent);
        return Mathf.Min(Mathf.Min(d1, d2), Mathf.Min(d3, d4)) <= threshold;
    }

    private static bool IsPointInTriangle(Vector2 pt, Vector2 v1, Vector2 v2, Vector2 v3)
    {
        float d1 = Sign(pt, v1, v2);
        float d2 = Sign(pt, v2, v3);
        float d3 = Sign(pt, v3, v1);
        bool hasNeg = (d1 < 0) || (d2 < 0) || (d3 < 0);
        bool hasPos = (d1 > 0) || (d2 > 0) || (d3 > 0);
        return !(hasNeg && hasPos);
    }

    private static float Sign(Vector2 p1, Vector2 p2, Vector2 p3)
    {
        return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
    }

    private static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 pa = p - a, ba = b - a;
        float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Vector2.Dot(ba, ba));
        return (pa - ba * h).magnitude;
    }

    #endregion
}
