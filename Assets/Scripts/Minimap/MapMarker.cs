using UnityEngine;

/// <summary>
/// Adds a point-of-interest icon on the map (Minimap & Big Map).
/// The icon is rendered on the MinimapIcons layer (layer 11) so it is only visible
/// to the minimap camera and completely invisible to the player's main game camera.
/// Features circular badge framing, color-coded rims, proper world unit sizing,
/// and adaptive scaling for camera zooming.
/// </summary>
public class MapMarker : MonoBehaviour
{
    public enum MarkerType
    {
        Custom,
        Cabin,
        Dumpster,
        TreeZone,
        TrashZone
    }

    [Header("Marker Settings")]
    [Tooltip("Type of point of interest.")]
    public MarkerType markerType = MarkerType.Custom;

    [Tooltip("Custom sprite for this marker. If empty, a default icon is automatically assigned.")]
    public Sprite customSprite;

    [Tooltip("Tint/accent color for the marker badge rim.")]
    public Color markerColor = Color.white;

    [Tooltip("Target diameter of the marker in world units (meters).")]
    public float iconSize = 1.4f;

    [Tooltip("Height above the object where the icon floats.")]
    public float heightOffset = 10f;

    [Tooltip("Optional local offset from parent transform.")]
    public Vector3 localOffset = Vector3.zero;

    [Tooltip("Whether to wrap the icon inside a clean, high-contrast circular badge.")]
    public bool useBadgeBackground = true;

    [Tooltip("Dynamically adjusts marker size when zooming between minimap and big map.")]
    public bool scaleWithCameraZoom = true;

    [Tooltip("If true, marker rotates with the parent GameObject (e.g. for vehicles/characters).")]
    public bool rotateWithParent = false;

    // References
    private GameObject markerRoot;
    private GameObject badgeChild;
    private GameObject iconChild;
    private SpriteRenderer badgeRenderer;
    private SpriteRenderer iconRenderer;
    private Camera cachedMapCamera;

    private static Sprite cachedBadgeSprite;

    private void Start()
    {
        SetupMarker();
    }

    public void SetupMarker()
    {
        if (markerRoot != null) return;

        // 1. Create root container
        markerRoot = new GameObject("MapMarker_Root");
        markerRoot.transform.SetParent(transform, false);
        markerRoot.transform.localPosition = new Vector3(localOffset.x, heightOffset + localOffset.y, localOffset.z);
        markerRoot.layer = 11; // MinimapIcons

        // 2. Resolve sprite and theme color based on markerType
        Sprite resolvedSprite = customSprite;
        Color rimColor = markerColor;

        if (resolvedSprite == null)
        {
            switch (markerType)
            {
                case MarkerType.Cabin:
                    resolvedSprite = Resources.Load<Sprite>("SPRITES/Home") ?? LoadSpriteFromPath("Assets/SPRITES/Home.png");
                    rimColor = new Color(1f, 0.85f, 0.35f, 1f); // Warm Gold
                    break;

                case MarkerType.Dumpster:
                    resolvedSprite = Resources.Load<Sprite>("SPRITES/dumpster") ?? LoadSpriteFromPath("Assets/SPRITES/dumpster.png");
                    // Auto-detect dumpster type for color-coded badge rim
                    Dumpster d = GetComponent<Dumpster>();
                    if (d != null)
                    {
                        switch (d.dumpsterType)
                        {
                            case TrashType.Recyclable:
                                rimColor = new Color(0.2f, 0.88f, 0.35f, 1f); // Vibrant Green
                                break;
                            case TrashType.Glass:
                                rimColor = new Color(0.25f, 0.75f, 1f, 1f); // Sky Blue
                                break;
                            case TrashType.Organic:
                                rimColor = new Color(1f, 0.58f, 0.18f, 1f); // Warm Orange
                                break;
                        }
                    }
                    else
                    {
                        rimColor = new Color(0.3f, 0.85f, 0.5f, 1f);
                    }
                    break;

                case MarkerType.TreeZone:
                    resolvedSprite = Resources.Load<Sprite>("SPRITES/tree") ?? LoadSpriteFromPath("Assets/SPRITES/tree.png");
                    rimColor = new Color(0.25f, 0.9f, 0.4f, 1f); // Leaf Green
                    break;

                default:
                    if (markerColor == Color.white) rimColor = new Color(0.9f, 0.9f, 0.9f, 1f);
                    break;
            }
        }

        // 3. Create Circular Badge Background if enabled
        if (useBadgeBackground)
        {
            badgeChild = new GameObject("Badge");
            badgeChild.transform.SetParent(markerRoot.transform, false);
            badgeChild.layer = 11;

            badgeRenderer = badgeChild.AddComponent<SpriteRenderer>();
            badgeRenderer.sprite = GetOrCreateBadgeSprite();
            badgeRenderer.color = rimColor;
            badgeRenderer.sortingOrder = 90;

            // Unscaled badge sprite is 128px @ 100 PPU = 1.28 world units
            float badgeSpriteWidth = badgeRenderer.sprite.bounds.size.x > 0.001f ? badgeRenderer.sprite.bounds.size.x : 1.28f;
            badgeChild.transform.localScale = Vector3.one * (iconSize / badgeSpriteWidth);
        }

        // 4. Create Icon Sprite
        if (resolvedSprite != null)
        {
            iconChild = new GameObject("Icon");
            iconChild.transform.SetParent(markerRoot.transform, false);
            iconChild.layer = 11;

            iconRenderer = iconChild.AddComponent<SpriteRenderer>();
            iconRenderer.sprite = resolvedSprite;
            // Full color sprite artwork
            iconRenderer.color = Color.white;
            iconRenderer.sortingOrder = 92; // Above badge

            // Calculate exact world size
            float spriteWidth = resolvedSprite.bounds.size.x > 0.001f ? resolvedSprite.bounds.size.x : 1f;
            float targetDiameter = useBadgeBackground ? (iconSize * 0.72f) : iconSize;
            iconChild.transform.localScale = Vector3.one * (targetDiameter / spriteWidth);
        }

        // Initial orientation flat towards top-down camera
        markerRoot.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        FindMapCamera();
        UpdateScale();
    }

    private void LateUpdate()
    {
        if (markerRoot == null) return;

        // Rotation
        if (rotateWithParent)
        {
            float yaw = transform.eulerAngles.y;
            markerRoot.transform.rotation = Quaternion.Euler(90f, yaw, 0f);
        }
        else
        {
            markerRoot.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        }

        // Dynamic camera zoom scaling
        if (scaleWithCameraZoom)
        {
            UpdateScale();
        }
    }

    private void UpdateScale()
    {
        if (markerRoot == null) return;

        if (cachedMapCamera == null)
        {
            FindMapCamera();
        }

        float currentOrtho = 20f;
        if (cachedMapCamera != null && cachedMapCamera.orthographic)
        {
            currentOrtho = cachedMapCamera.orthographicSize;
        }

        // Gentle power curve so icons stay readable on Big Map without growing excessively:
        // Ortho 20 -> multiplier = 1.0
        // Ortho 65 -> multiplier ≈ 1.39
        // Ortho 110 -> multiplier ≈ 1.6
        float orthoRatio = Mathf.Max(0.1f, currentOrtho / 20f);
        float zoomMultiplier = Mathf.Clamp(Mathf.Pow(orthoRatio, 0.28f), 0.9f, 1.6f);

        // Apply scale to root
        markerRoot.transform.localScale = Vector3.one * zoomMultiplier;
    }

    private void FindMapCamera()
    {
        LimitCamera limitCam = FindFirstObjectByType<LimitCamera>();
        if (limitCam != null)
        {
            cachedMapCamera = limitCam.GetComponent<Camera>();
            return;
        }

        Camera[] cams = Camera.allCameras;
        foreach (Camera c in cams)
        {
            if (c.orthographic && (c.cullingMask & (1 << 11)) != 0)
            {
                cachedMapCamera = c;
                break;
            }
        }
    }

    private void OnDestroy()
    {
        if (markerRoot != null)
        {
            Destroy(markerRoot);
        }
    }

    /// <summary>
    /// Creates a sharp, high-contrast antialiased circular badge texture with colored rim and dark fill.
    /// </summary>
    private static Sprite GetOrCreateBadgeSprite()
    {
        if (cachedBadgeSprite != null) return cachedBadgeSprite;

        int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Color[] cols = new Color[size * size];
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        float radius = size * 0.48f;
        float innerRadius = radius - 7f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                if (dist <= innerRadius)
                {
                    // Dark charcoal translucent center backing for 100% contrast on any terrain
                    cols[y * size + x] = new Color(0.08f, 0.10f, 0.14f, 0.92f);
                }
                else if (dist <= radius)
                {
                    // Outer accent ring border (tintable by badgeRenderer.color)
                    float edgeAlpha = Mathf.Clamp01((radius - dist) + 0.5f);
                    cols[y * size + x] = new Color(1f, 1f, 1f, edgeAlpha * 0.98f);
                }
                else
                {
                    cols[y * size + x] = new Color(0f, 0f, 0f, 0f);
                }
            }
        }

        tex.SetPixels(cols);
        tex.Apply();
        cachedBadgeSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        return cachedBadgeSprite;
    }

    private static Sprite LoadSpriteFromPath(string path)
    {
#if UNITY_EDITOR
        return UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
#else
        return null;
#endif
    }
}
