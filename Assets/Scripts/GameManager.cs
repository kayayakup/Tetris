using System.Collections;
using UnityEngine;

/// <summary>
/// Central singleton. Holds all public configuration, computes the grid layout
/// from screen dimensions, and coordinates the other systems.
/// </summary>
public class GameManager : MonoBehaviour
{
    // ─── Singleton ────────────────────────────────────────────────────────────
    public static GameManager Instance { get; private set; }

    // ─── Inspector – Sprites (assign in Editor; auto-generated if null) ────────
    [Header("Tetromino Sprites (optional – auto-generated if empty)")]
    [Tooltip("Order: I, O, T, S, Z, J, L")]
    public Sprite[] tetrominoSprites = new Sprite[6];

    // ─── Inspector – Grid settings ────────────────────────────────────────────
    [Header("Grid Settings")]
    public int gridWidth  = 10;
    public int gridHeight = 20;

    [Header("Safe Area")]
    [Tooltip("Pixels reserved at the bottom for UI / ads.")]
    public int bottomSafeArea = 160;

    // ─── Inspector – Grid Visuals ─────────────────────────────────────────────
    [Header("Grid Visuals")]
    public Color gridLineColor = new Color(1f, 1f, 1f, 0.1f);
    public float gridLineWidth = 0.02f;

    // ─── Runtime layout (read-only after Awake) ───────────────────────────────
    /// <summary>World-space size of one grid cell.</summary>
    public float CellSize      { get; private set; }
    /// <summary>World-space bottom-left corner of the grid.</summary>
    public Vector2 GridOrigin  { get; private set; }

    [Header("Speed Settings")]
    [Tooltip("Time in seconds between each downward step.")]
    public float fallInterval = 0.8f;

    // ─── State ────────────────────────────────────────────────────────────────
    public bool IsGameOver     { get; private set; }
    public bool IsPaused       { get; private set; }

    // ─── Default block colours (one per piece type: I O T S Z J L) ───────────
    private static readonly Color32[] DefaultColors = new Color32[]
    {
        new Color32(0,   240, 240, 255),   // I – Cyan
        new Color32(240, 240,   0, 255),   // O – Yellow
        new Color32(160,   0, 240, 255),   // T – Purple
        new Color32(0,   240,   0, 255),   // S – Green
        new Color32(240,   0,   0, 255),   // Z – Red
        new Color32(0,   0,   240, 255),   // J – Blue
        new Color32(240, 160,   0, 255),   // L – Orange
    };

    // ─── Component references (set by other scripts) ──────────────────────────
    [HideInInspector] public GridSystem       GridSystem;
    [HideInInspector] public TetrominoSpawner Spawner;
    [HideInInspector] public ScoreManager     Score;
    [HideInInspector] public InputHandler     Input;
    [HideInInspector] public UIManager        UI;
    public AudioManager     Audio;

    // ─────────────────────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Ensure we have exactly 7 slots for the 7 Tetromino types
        if (tetrominoSprites == null || tetrominoSprites.Length < 7)
        {
            System.Array.Resize(ref tetrominoSprites, 7);
        }

        // Ensure default visibility if the user hasn't set it (serialized as 0,0,0,0)
        if (gridLineColor.a < 0.01f) gridLineColor = new Color(1f, 1f, 1f, 0.4f);

        // Grab sibling components (all on the same GameObject or children)
        GridSystem = GetComponentInChildren<GridSystem>();
        Spawner    = GetComponentInChildren<TetrominoSpawner>();
        Score      = GetComponentInChildren<ScoreManager>();
        Input      = GetComponentInChildren<InputHandler>();

        if (GridSystem == null) Debug.LogError("[GameManager] No GridSystem found!");
        if (Spawner    == null) Debug.LogError("[GameManager] No TetrominoSpawner found!");

        Audio = GetComponentInChildren<AudioManager>();
        if (Audio == null) Audio = Object.FindAnyObjectByType<AudioManager>();
        if (Audio == null) Debug.LogWarning("[GameManager] No AudioManager found in scene!");

        GeneratePlaceholderSprites();
    }


    private void Start()
    {
        ComputeLayout();
        GenerateGridLines();

        StartGame();
    }

    // ─── Layout computation ───────────────────────────────────────────────────
    //
    //  Strategy (pure ratio math – no ScreenToWorldPoint):
    //
    //  • 1 grid cell  = 1 world unit  (CellSize is always 1)
    //  • Camera must be Orthographic; orthographicSize = half the visible world height
    //  • We compute the minimum orthographicSize that lets BOTH the grid width
    //    AND the usable height (screen minus safe area) fit inside the viewport.
    //  • Camera is centred at X=0; Y is chosen so the grid bottom lands
    //    exactly at the top of the safe-area strip.
    //
    private void ComputeLayout()
    {
        // Each cell is exactly 1 world unit (placeholder sprites use PPU = pixel size,
        // so they naturally render as 1 × 1 world units; no extra scaling needed).
        CellSize = 1f;

        Camera cam  = Camera.main;
        float aspect           = (float)Screen.width / Screen.height;
        float safeAreaFraction = (float)bottomSafeArea / Screen.height;
        float usableFraction   = 1f - safeAreaFraction;   // fraction of visible screen above safe area

        // ── orthographicSize so the GRID WIDTH fills the viewport exactly ─────
        //   Camera half-width  = orthographicSize × aspect
        //   Grid world width   = gridWidth  (1 cell = 1 unit)
        //   → orthographicSize = gridWidth / (2 × aspect)
        float camSizeForWidth = (gridWidth * CellSize) / (2f * aspect);

        // ── orthographicSize so the GRID HEIGHT fills the usable area exactly ─
        //   Usable world height = 2 × orthographicSize × usableFraction
        //   Grid world height   = gridHeight
        //   → orthographicSize = gridHeight / (2 × usableFraction)
        float camSizeForHeight = (gridHeight * CellSize) / (2f * usableFraction);

        // Take the LARGER value so both axes fit without overflow.
        // (On a very wide screen, height dominates; on a very tall screen, width dominates.)
        float camSize = Mathf.Max(camSizeForWidth, camSizeForHeight);
        cam.orthographicSize = camSize;

        // ── Camera & grid positions ───────────────────────────────────────────
        // Camera centred at (0, 0).
        //   Camera bottom  = -camSize  (world units)
        //   Safe-area top  = -camSize + safeAreaFraction × 2 × camSize
        //   Grid bottom    = safe-area top  →  GridOrigin.y
        //   Grid left      = -gridWidth / 2  →  GridOrigin.x  (horizontally centred)
        float safeAreaWorldHeight = safeAreaFraction * 2f * camSize;

        GridOrigin = new Vector2(
            -(gridWidth  * CellSize) * 0.5f,
            -camSize + safeAreaWorldHeight
        );

        cam.transform.position = new Vector3(0f, 0f, cam.transform.position.z);

        Debug.Log($"[GameManager] CamSize={camSize:F3}  GridOrigin={GridOrigin}  " +
                  $"Screen={Screen.width}x{Screen.height}  Aspect={aspect:F3}");
    }

    // ─── Placeholder sprites ──────────────────────────────────────────────────
    private void GeneratePlaceholderSprites()
    {
        for (int i = 0; i < 7; i++)
        {
            if (tetrominoSprites[i] != null) continue;

            int size = 32;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;

            Color32 col = DefaultColors[i % DefaultColors.Length];
            Color32[] pixels = new Color32[size * size];

            for (int p = 0; p < pixels.Length; p++)
            {
                int px = p % size;
                int py = p / size;
                // 1-pixel dark border
                bool border = px == 0 || py == 0 || px == size - 1 || py == size - 1;
                pixels[p] = border ? new Color32(20, 20, 20, 255) : col;
            }

            tex.SetPixels32(pixels);
            tex.Apply();

            tetrominoSprites[i] = Sprite.Create(
                tex,
                new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f),
                size   // pixels-per-unit = size so 1 world unit = 1 cell
            );
        }
    }

    // ─── Grid Visuals (Tiled) ──────────────────────────────────────────────────
    private void GenerateGridLines()
    {
        GameObject gridObj = new GameObject("FullGridTiled");
        gridObj.transform.SetParent(transform);

        // 1. Create a "Cell Tile" texture (e.g., 64x64 with a 1px white border)
        int texSize = 64;
        Texture2D tileTex = new Texture2D(texSize, texSize);
        tileTex.filterMode = FilterMode.Point;
        tileTex.wrapMode = TextureWrapMode.Repeat;
        
        Color32[] pixels = new Color32[texSize * texSize];
        Color32 gridLineCol32 = gridLineColor;
        Color32 clearCol = new Color32(0, 0, 0, 0);

        for (int y = 0; y < texSize; y++)
        {
            for (int x = 0; x < texSize; x++)
            {
                // Draw 1px border on left and bottom to form a grid when tiled
                bool isBorder = (x == 0 || y == 0);
                pixels[y * texSize + x] = isBorder ? gridLineCol32 : clearCol;
            }
        }
        tileTex.SetPixels32(pixels);
        tileTex.Apply();

        // Create Sprite with FullRect mesh so it tiles correctly
        Sprite tileSprite = Sprite.Create(tileTex, new Rect(0, 0, texSize, texSize), Vector2.zero, texSize / CellSize, 0, SpriteMeshType.FullRect);

        Camera cam = Camera.main;
        float camHeight = cam.orthographicSize * 2f;
        float camWidth = camHeight * cam.aspect;

        float screenWorldLeft = cam.transform.position.x - camWidth / 2f;
        float screenWorldTop = cam.transform.position.y + cam.orthographicSize;

        // The grid should ONLY cover the area above the safe-area.
        // Screen bottom in world units = cam.transform.position.y - cam.orthographicSize
        // Grid bottom = GridOrigin.y
        float usableWorldHeight = screenWorldTop - GridOrigin.y;

        SpriteRenderer sr = gridObj.AddComponent<SpriteRenderer>();
        sr.sprite = tileSprite;
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.sortingOrder = -10;
        
        // Width covers the full screen horizontally
        sr.size = new Vector2(camWidth + CellSize * 4f, usableWorldHeight + CellSize);

        // Align the tiled sprite:
        // X: Align to the leftmost position that covers screenWorldLeft and matches GridOrigin.x steps
        float alignedX = GridOrigin.x - Mathf.Ceil((GridOrigin.x - screenWorldLeft + CellSize) / CellSize) * CellSize;
        // Y: Start exactly at GridOrigin.y (the top of the safe area)
        float alignedY = GridOrigin.y;

        gridObj.transform.position = new Vector3(alignedX, alignedY, 0f);

        // 2. Play Area Background (dark tint behind the 10x20 playable area)
        GameObject playAreaBg = new GameObject("PlayAreaBackground");
        playAreaBg.transform.SetParent(gridObj.transform);
        playAreaBg.transform.position = new Vector3(
            GridOrigin.x + (gridWidth * CellSize) / 2f,
            GridOrigin.y + (gridHeight * CellSize) / 2f,
            0.1f // Slightly behind grid lines
        );
        playAreaBg.transform.localScale = new Vector3(gridWidth * CellSize, gridHeight * CellSize, 1f);
        
        // Reuse a simple white texture for the solid background
        Texture2D whiteTex = new Texture2D(1, 1);
        whiteTex.SetPixel(0, 0, Color.white);
        whiteTex.Apply();
        Sprite whiteSprite = Sprite.Create(whiteTex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);

        SpriteRenderer bgSr = playAreaBg.AddComponent<SpriteRenderer>();
        bgSr.sprite = whiteSprite;
        bgSr.color = new Color(0f, 0f, 0f, 0.4f);
        bgSr.sortingOrder = -15;
    }


    // ─── Game flow ────────────────────────────────────────────────────────────
    public void StartGame()
    {
        IsGameOver = false;
        IsPaused   = false;
        GridSystem?.ClearGrid();
        Score?.ResetScore();
        Spawner?.SpawnNext();
    }

    public void TriggerGameOver()
    {
        IsGameOver = true;
        Debug.Log("[GameManager] Game Over!");
        UI?.ShowGameOverMenu();
        Audio?.PlayGameOver();
    }

    public void PauseGame(bool pause)
    {
        IsPaused = pause;
        Time.timeScale = pause ? 0f : 1f;
    }

    // ─── Helper: grid-coord → world position (cell centre) ───────────────────
    public Vector2 GridToWorld(int col, int row)
    {
        return new Vector2(
            GridOrigin.x + col * CellSize + CellSize * 0.5f,
            GridOrigin.y + row * CellSize + CellSize * 0.5f
        );
    }
}
