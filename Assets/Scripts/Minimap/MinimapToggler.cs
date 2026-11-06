using UnityEngine;
using UnityEngine.UI;
using TMPro;
using StarterAssets;

/// <summary>
/// Controls toggling between the compact Minimap HUD and the interactive Fullscreen Big Map.
/// Manages mouse lock/unlock, character control freezing, smooth camera zoom modes,
/// and adds an interactive header with controls and recenter functionality.
/// </summary>
public class MinimapToggler : MonoBehaviour
{
    public static bool IsAnyBigMapOpen { get; private set; } = false;

    [Header("UI Objects")]
    [Tooltip("The compact minimap UI container in the HUD.")]
    public GameObject MinimapObject;

    [Tooltip("The fullscreen big map UI container.")]
    public GameObject FullscreenMapObject;

    [Header("Camera Reference")]
    [Tooltip("The LimitCamera script on the minimap camera. Will auto-find if unassigned.")]
    public LimitCamera mapCamera;

    [Header("Keybindings")]
    public KeyCode toggleKey = KeyCode.M;
    public KeyCode closeKey = KeyCode.Escape;

    [Header("Player Input Management")]
    [Tooltip("If true, disables character movement and camera-look while the Big Map is open.")]
    public bool freezePlayerInputOnBigMap = true;

    [Header("Header Overlay")]
    [Tooltip("Automatically adds a stylish header bar with map title and control hints.")]
    public bool autoAddHeaderOverlay = true;

    private StarterAssetsInputs playerInputs;

    private void Awake()
    {
        IsAnyBigMapOpen = false;
    }

    private void Start()
    {
        // Auto-find mapCamera if unassigned
        if (mapCamera == null)
        {
            mapCamera = FindFirstObjectByType<LimitCamera>();
        }

        FindPlayerInputs();

        // Ensure clean initial state
        if (FullscreenMapObject != null)
        {
            FullscreenMapObject.SetActive(false);
            if (autoAddHeaderOverlay)
            {
                EnsureBigMapHeader();
            }
        }

        if (MinimapObject != null) MinimapObject.SetActive(true);
        if (mapCamera != null) mapCamera.SetFullscreenMode(false);
    }

    private void FindPlayerInputs()
    {
        if (playerInputs == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerInputs = player.GetComponent<StarterAssetsInputs>();
            }
        }
    }

    private void Update()
    {
        // Don't process map keys if the game is currently paused
        if (PauseMenu.isPaused) return;

        // Toggle Big Map with M
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleMap();
            return;
        }

        // Close Big Map with Escape (before PauseMenu consumes it)
        if (IsAnyBigMapOpen && Input.GetKeyDown(closeKey))
        {
            CloseBigMap();
        }
    }

    /// <summary>
    /// Toggles between Minimap and Fullscreen Map.
    /// </summary>
    public void ToggleMap()
    {
        if (IsAnyBigMapOpen)
        {
            CloseBigMap();
        }
        else
        {
            OpenBigMap();
        }
    }

    /// <summary>
    /// Opens the interactive Fullscreen Big Map.
    /// </summary>
    public void OpenBigMap()
    {
        IsAnyBigMapOpen = true;

        if (MinimapObject != null) MinimapObject.SetActive(false);
        if (FullscreenMapObject != null)
        {
            FullscreenMapObject.SetActive(true);
            if (autoAddHeaderOverlay)
            {
                EnsureBigMapHeader();
            }
        }

        if (mapCamera != null)
        {
            mapCamera.SetFullscreenMode(true);
        }

        // Unlock mouse cursor for map interaction
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Freeze player movement/look
        if (freezePlayerInputOnBigMap)
        {
            FindPlayerInputs();
            if (playerInputs != null)
            {
                playerInputs.cursorInputForLook = false;
                playerInputs.cursorLocked = false;
                playerInputs.move = Vector2.zero;
            }
        }
    }

    /// <summary>
    /// Closes the Big Map and returns to the standard compact Minimap.
    /// </summary>
    public void CloseBigMap()
    {
        IsAnyBigMapOpen = false;

        if (FullscreenMapObject != null) FullscreenMapObject.SetActive(false);
        if (MinimapObject != null) MinimapObject.SetActive(true);

        if (mapCamera != null)
        {
            mapCamera.SetFullscreenMode(false);
        }

        // Re-lock mouse cursor for gameplay
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Restore player movement/look
        if (freezePlayerInputOnBigMap)
        {
            FindPlayerInputs();
            if (playerInputs != null)
            {
                playerInputs.cursorInputForLook = true;
                playerInputs.cursorLocked = true;
            }
        }
    }

    private void EnsureBigMapHeader()
    {
        if (FullscreenMapObject == null) return;
        Transform existing = FullscreenMapObject.transform.Find("MapHeaderBar");
        if (existing != null) return;

        // Build header overlay
        GameObject headerObj = new GameObject("MapHeaderBar");
        headerObj.transform.SetParent(FullscreenMapObject.transform, false);

        RectTransform rt = headerObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.05f, 1f);
        rt.anchorMax = new Vector2(0.95f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -20f);
        rt.sizeDelta = new Vector2(0f, 54f);

        // Dark translucent pill banner
        Image bg = headerObj.AddComponent<Image>();
        bg.color = new Color(0.07f, 0.08f, 0.1f, 0.9f);

        // Title Text
        GameObject titleObj = new GameObject("TitleText");
        titleObj.transform.SetParent(headerObj.transform, false);
        RectTransform titleRt = titleObj.AddComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0f, 0f);
        titleRt.anchorMax = new Vector2(0.3f, 1f);
        titleRt.pivot = new Vector2(0f, 0.5f);
        titleRt.anchoredPosition = new Vector2(20f, 0f);
        titleRt.sizeDelta = new Vector2(200f, 0f);

        TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "PARK MAP";
        titleText.fontSize = 22;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = new Color(0.95f, 0.95f, 0.95f, 1f);
        titleText.alignment = TextAlignmentOptions.MidlineLeft;

        // Controls Hint Text
        GameObject hintObj = new GameObject("HintText");
        hintObj.transform.SetParent(headerObj.transform, false);
        RectTransform hintRt = hintObj.AddComponent<RectTransform>();
        hintRt.anchorMin = new Vector2(0.25f, 0f);
        hintRt.anchorMax = new Vector2(0.85f, 1f);
        hintRt.pivot = new Vector2(0.5f, 0.5f);
        hintRt.anchoredPosition = Vector2.zero;
        hintRt.sizeDelta = Vector2.zero;

        TextMeshProUGUI hintText = hintObj.AddComponent<TextMeshProUGUI>();
        hintText.text = "<b>Scroll:</b> Zoom  |  <b>Drag:</b> Pan  |  <b>[R]:</b> Recenter  |  <b>[M / Esc]:</b> Close";
        hintText.fontSize = 14;
        hintText.color = new Color(0.82f, 0.85f, 0.9f, 0.95f);
        hintText.alignment = TextAlignmentOptions.Center;

        // Close Button
        GameObject closeBtnObj = new GameObject("CloseButton");
        closeBtnObj.transform.SetParent(headerObj.transform, false);
        RectTransform closeRt = closeBtnObj.AddComponent<RectTransform>();
        closeRt.anchorMin = new Vector2(1f, 0.5f);
        closeRt.anchorMax = new Vector2(1f, 0.5f);
        closeRt.pivot = new Vector2(1f, 0.5f);
        closeRt.anchoredPosition = new Vector2(-12f, 0f);
        closeRt.sizeDelta = new Vector2(34f, 34f);

        Image btnImg = closeBtnObj.AddComponent<Image>();
        btnImg.color = new Color(0.22f, 0.25f, 0.3f, 0.9f);
        Button btn = closeBtnObj.AddComponent<Button>();
        btn.onClick.AddListener(CloseBigMap);

        GameObject btnTextObj = new GameObject("Text");
        btnTextObj.transform.SetParent(closeBtnObj.transform, false);
        RectTransform btnTextRt = btnTextObj.AddComponent<RectTransform>();
        btnTextRt.anchorMin = Vector2.zero;
        btnTextRt.anchorMax = Vector2.one;
        btnTextRt.offsetMin = Vector2.zero;
        btnTextRt.offsetMax = Vector2.zero;

        TextMeshProUGUI btnText = btnTextObj.AddComponent<TextMeshProUGUI>();
        btnText.text = "X";
        btnText.fontSize = 18;
        btnText.fontStyle = FontStyles.Bold;
        btnText.color = Color.white;
        btnText.alignment = TextAlignmentOptions.Center;
    }

    private void OnDisable()
    {
        IsAnyBigMapOpen = false;
    }
}
