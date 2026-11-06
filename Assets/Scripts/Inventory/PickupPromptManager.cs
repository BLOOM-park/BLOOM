using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages the floating world-space interaction UI prompt for picking up items.
/// Automatically bill-boards towards the active camera and displays when the player is in range
/// and able to pick up the item (e.g. holding the Grabber tool for trash).
/// </summary>
public class PickupPromptManager : MonoBehaviour
{
    private static PickupPromptManager instance;

    public static PickupPromptManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<PickupPromptManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("PickupPromptManager");
                    instance = go.AddComponent<PickupPromptManager>();
                }
            }
            return instance;
        }
    }

    [Header("Prompt Settings")]
    [Tooltip("Vertical height offset above the item's top bounds.")]
    [SerializeField] private float heightOffset = 0.25f;

    [Tooltip("Key display text on the badge.")]
    [SerializeField] private string keyString = "E";

    [Tooltip("Action label text.")]
    [SerializeField] private string actionText = "Pick Up";

    [Tooltip("If true, appends the item name to the action label (e.g., 'Pick Up Plastic Bottle').")]
    [SerializeField] private bool showItemName = false;

    [Tooltip("World scale of the floating UI.")]
    [SerializeField] private float promptScale = 0.005f;

    [Tooltip("Speed at which the prompt follows the target position.")]
    [SerializeField] private float followSpeed = 16f;

    [Tooltip("Fade speed when appearing and disappearing.")]
    [SerializeField] private float fadeSpeed = 10f;

    [Header("Visual Styling")]
    [SerializeField] private Color backgroundColor = new Color(0.08f, 0.08f, 0.08f, 0.85f);
    [SerializeField] private Color keyBadgeColor = new Color(0.18f, 0.18f, 0.18f, 0.95f);
    [SerializeField] private Color keyOutlineColor = new Color(0.9f, 0.9f, 0.9f, 0.95f);
    [SerializeField] private Color textColor = Color.white;
    [SerializeField] private TMP_FontAsset fontAsset;

    // Runtime tracking
    private readonly List<Pickup> candidatePickups = new List<Pickup>();
    private Pickup currentTargetPickup;
    private Camera cachedCamera;
    private Transform playerTransform;

    // UI references
    private Canvas worldCanvas;
    private CanvasGroup canvasGroup;
    private RectTransform rootPanel;
    private TextMeshProUGUI keyTextComponent;
    private TextMeshProUGUI actionTextComponent;

    private float currentScaleMultiplier = 0.8f;
    private Vector3 currentVelocity;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        BuildPromptUI();
        SetVisible(false, true);
    }

    private void Start()
    {
        FindPlayer();
    }

    private void FindPlayer()
    {
        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerTransform = playerObj.transform;
            }
        }
    }

    /// <summary>
    /// Register a pickup that is in range and valid to be picked up.
    /// </summary>
    public void RegisterPickup(Pickup pickup)
    {
        if (pickup == null) return;
        if (!candidatePickups.Contains(pickup))
        {
            candidatePickups.Add(pickup);
        }
    }

    /// <summary>
    /// Unregister a pickup when out of range, picked up, or disabled.
    /// </summary>
    public void UnregisterPickup(Pickup pickup)
    {
        if (pickup == null) return;
        candidatePickups.Remove(pickup);

        if (currentTargetPickup == pickup)
        {
            currentTargetPickup = null;
        }
    }

    private void Update()
    {
        CleanCandidateList();
        SelectClosestPickup();

        bool shouldShow = currentTargetPickup != null && currentTargetPickup.CanBePickedUp();
        SetVisible(shouldShow, false);

        if (shouldShow && currentTargetPickup != null)
        {
            UpdatePromptContent();
        }
    }

    private void LateUpdate()
    {
        if (canvasGroup == null || canvasGroup.alpha <= 0.001f) return;

        UpdateCameraReference();
        if (cachedCamera == null) return;

        // Face the camera directly (billboard)
        transform.rotation = cachedCamera.transform.rotation;

        // Position above the item
        if (currentTargetPickup != null)
        {
            Vector3 targetPosition = CalculatePromptPosition(currentTargetPickup);
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * followSpeed);
        }

        // Smooth scale punch
        float targetScaleMultiplier = 1f;
        currentScaleMultiplier = Mathf.Lerp(currentScaleMultiplier, targetScaleMultiplier, Time.deltaTime * 12f);
        transform.localScale = Vector3.one * (promptScale * currentScaleMultiplier);
    }

    private void CleanCandidateList()
    {
        for (int i = candidatePickups.Count - 1; i >= 0; i--)
        {
            if (candidatePickups[i] == null || !candidatePickups[i].gameObject.activeInHierarchy)
            {
                candidatePickups.RemoveAt(i);
            }
        }
    }

    private void SelectClosestPickup()
    {
        if (candidatePickups.Count == 0)
        {
            currentTargetPickup = null;
            return;
        }

        FindPlayer();
        Vector3 referencePos = playerTransform != null ? playerTransform.position : (cachedCamera != null ? cachedCamera.transform.position : transform.position);

        Pickup closest = null;
        float minDistanceSqr = float.MaxValue;

        for (int i = 0; i < candidatePickups.Count; i++)
        {
            Pickup p = candidatePickups[i];
            if (p == null || !p.CanBePickedUp()) continue;

            float distSqr = (p.transform.position - referencePos).sqrMagnitude;
            if (distSqr < minDistanceSqr)
            {
                minDistanceSqr = distSqr;
                closest = p;
            }
        }

        if (currentTargetPickup != closest)
        {
            currentTargetPickup = closest;
            if (currentTargetPickup != null)
            {
                // Snap position when switching to a new target to avoid long slide
                transform.position = CalculatePromptPosition(currentTargetPickup);
                currentScaleMultiplier = 0.82f;
            }
        }
    }

    private Vector3 CalculatePromptPosition(Pickup pickup)
    {
        if (pickup == null) return transform.position;

        Collider col = pickup.GetComponent<Collider>();
        if (col != null)
        {
            return col.bounds.center + Vector3.up * (col.bounds.extents.y + heightOffset);
        }

        return pickup.transform.position + Vector3.up * heightOffset;
    }

    private void UpdatePromptContent()
    {
        if (actionTextComponent == null) return;

        if (showItemName && currentTargetPickup != null)
        {
            actionTextComponent.text = $"{actionText} {currentTargetPickup.GetItemDisplayName()}";
        }
        else
        {
            actionTextComponent.text = actionText;
        }

        if (keyTextComponent != null)
        {
            keyTextComponent.text = keyString;
        }
    }

    private void SetVisible(bool visible, bool immediate)
    {
        if (canvasGroup == null) return;

        float targetAlpha = visible ? 1f : 0f;
        if (immediate)
        {
            canvasGroup.alpha = targetAlpha;
        }
        else
        {
            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, targetAlpha, Time.deltaTime * fadeSpeed);
        }
    }

    private void UpdateCameraReference()
    {
        if (cachedCamera == null || !cachedCamera.gameObject.activeInHierarchy)
        {
            cachedCamera = Camera.main;
        }
    }

    #region Procedural UI Building

    private void BuildPromptUI()
    {
        // Setup Canvas
        worldCanvas = gameObject.GetComponent<Canvas>();
        if (worldCanvas == null) worldCanvas = gameObject.AddComponent<Canvas>();
        worldCanvas.renderMode = RenderMode.WorldSpace;

        CanvasScaler scaler = gameObject.GetComponent<CanvasScaler>();
        if (scaler == null) scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 20f;

        canvasGroup = gameObject.GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        RectTransform canvasRect = GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(200f, 50f);
        transform.localScale = Vector3.one * promptScale;

        // Try load font if not assigned
        if (fontAsset == null)
        {
            fontAsset = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        }

        // 1. Root Container Panel (Dark Rounded Pill)
        GameObject panelObj = new GameObject("PromptPanel");
        panelObj.transform.SetParent(transform, false);
        rootPanel = panelObj.AddComponent<RectTransform>();

        Image panelImage = panelObj.AddComponent<Image>();
        panelImage.sprite = CreateRoundedRectSprite(64, 64, 16, backgroundColor);
        panelImage.type = Image.Type.Sliced;
        panelImage.color = Color.white;

        HorizontalLayoutGroup layout = panelObj.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(12, 16, 8, 8);
        layout.spacing = 10;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = panelObj.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // 2. Key Badge Container (Rounded Box outline with 'E')
        GameObject badgeObj = new GameObject("KeyBadge");
        badgeObj.transform.SetParent(panelObj.transform, false);
        RectTransform badgeRect = badgeObj.AddComponent<RectTransform>();
        badgeRect.sizeDelta = new Vector2(34f, 34f);

        Image badgeImage = badgeObj.AddComponent<Image>();
        badgeImage.sprite = CreateRoundedRectSprite(64, 64, 14, keyBadgeColor, keyOutlineColor, 4);
        badgeImage.type = Image.Type.Sliced;

        // Key Text (inside badge)
        GameObject keyTextObj = new GameObject("KeyText");
        keyTextObj.transform.SetParent(badgeObj.transform, false);
        RectTransform keyTextRect = keyTextObj.AddComponent<RectTransform>();
        keyTextRect.anchorMin = Vector2.zero;
        keyTextRect.anchorMax = Vector2.one;
        keyTextRect.offsetMin = Vector2.zero;
        keyTextRect.offsetMax = Vector2.zero;

        keyTextComponent = keyTextObj.AddComponent<TextMeshProUGUI>();
        if (fontAsset != null) keyTextComponent.font = fontAsset;
        keyTextComponent.text = keyString;
        keyTextComponent.fontSize = 20;
        keyTextComponent.fontStyle = FontStyles.Bold;
        keyTextComponent.color = textColor;
        keyTextComponent.alignment = TextAlignmentOptions.Center;

        // 3. Action Text Label ("Pick Up")
        GameObject actionTextObj = new GameObject("ActionText");
        actionTextObj.transform.SetParent(panelObj.transform, false);
        RectTransform actionRect = actionTextObj.AddComponent<RectTransform>();
        actionRect.sizeDelta = new Vector2(90f, 34f);

        actionTextComponent = actionTextObj.AddComponent<TextMeshProUGUI>();
        if (fontAsset != null) actionTextComponent.font = fontAsset;
        actionTextComponent.text = actionText;
        actionTextComponent.fontSize = 19;
        actionTextComponent.fontStyle = FontStyles.Bold;
        actionTextComponent.color = textColor;
        actionTextComponent.alignment = TextAlignmentOptions.MidlineLeft;

        ContentSizeFitter textFitter = actionTextObj.AddComponent<ContentSizeFitter>();
        textFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        textFitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
    }

    /// <summary>
    /// Generates a smooth, antialiased 9-sliced rounded rectangle sprite with optional border stroke.
    /// </summary>
    private static Sprite CreateRoundedRectSprite(int width, int height, int radius, Color fillColor, Color? strokeColor = null, int strokeWidth = 0)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Color[] colors = new Color[width * height];
        Color clear = new Color(0, 0, 0, 0);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int cx = (x < radius) ? radius : (x >= width - radius ? width - radius - 1 : x);
                int cy = (y < radius) ? radius : (y >= height - radius ? height - radius - 1 : y);
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));

                if (dist > radius)
                {
                    colors[y * width + x] = clear;
                }
                else
                {
                    float alphaEdge = Mathf.Clamp01(radius - dist + 0.5f);

                    if (strokeColor.HasValue && strokeWidth > 0 && dist >= radius - strokeWidth)
                    {
                        Color c = strokeColor.Value;
                        c.a *= alphaEdge;
                        colors[y * width + x] = c;
                    }
                    else
                    {
                        Color c = fillColor;
                        c.a *= alphaEdge;
                        colors[y * width + x] = c;
                    }
                }
            }
        }

        tex.SetPixels(colors);
        tex.Apply();

        Vector4 border = new Vector4(radius, radius, radius, radius);
        return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
    }

    #endregion
}
