using System.Collections;
using UnityEngine;

/// <summary>
/// Defines all 7 Tetris tetromino shapes, handles movement, rotation,
/// gravity fall, and locking into the grid.
/// </summary>
public class Tetromino : MonoBehaviour
{
    // ─── Piece types ──────────────────────────────────────────────────────────
    public enum PieceType { I = 0, O = 1, T = 2, S = 3, Z = 4, J = 5, L = 6 }

    // ─── Shape data ───────────────────────────────────────────────────────────
    // Each entry is an array of (col, row) offsets relative to the rotation pivot
    // Listed as all 4 rotations: [rotation][blockIndex] = {col, row}
    private static readonly int[][,] Shapes = new int[][,]
    {
        // I
        new int[,] { {-1,0},{0,0},{1,0},{2,0} },
        // O
        new int[,] { {0,0},{1,0},{0,1},{1,1} },
        // T
        new int[,] { {-1,0},{0,0},{1,0},{0,1} },
        // S
        new int[,] { {-1,0},{0,0},{0,1},{1,1} },
        // Z
        new int[,] { {1,0},{0,0},{0,1},{-1,1} },
        // J
        new int[,] { {-1,0},{0,0},{1,0},{-1,1} },
        // L
        new int[,] { {-1,0},{0,0},{1,0},{1,1} },
    };

    private static readonly Vector2Int[][] WallKicks = new Vector2Int[][]
    {
        new Vector2Int[] { new Vector2Int(1,0), new Vector2Int(-1,0), new Vector2Int(0,1), new Vector2Int(1,1), new Vector2Int(-1,1) },
    };

    // ─── Inspector ────────────────────────────────────────────────────────────
    [HideInInspector] public PieceType Type;

    // ─── Settings ─────────────────────────────────────────────────────────────
    public float hardDropDuration = 0.15f; // time in seconds to animate drop

    // ─── Runtime state ────────────────────────────────────────────────────────
    private Vector2Int _gridPos;    // pivot grid coordinate
    private int        _rotation;   // 0–3
    private Coroutine  _fallRoutine;
    private bool       _locked;
    private bool       _isHardDropping;

    private Transform[] _ghostBlocks;

    private GameManager _gm;
    private GridSystem  _grid;

    // ─── Init ─────────────────────────────────────────────────────────────────
    public void Init(PieceType type, Vector2Int startGridPos)
    {
        _gm       = GameManager.Instance;
        _grid     = _gm.GridSystem;
        Type      = type;
        _gridPos  = startGridPos;
        _rotation = 0;
        _locked   = false;

        BuildBlocks();
        UpdateWorldPosition();

        // Check if spawn position is already blocked → game over
        if (!IsValidPosition(_gridPos))
        {
            _gm.TriggerGameOver();
            Destroy(gameObject);
            return;
        }

        _fallRoutine = StartCoroutine(FallLoop());
    }

    // ─── Block construction ───────────────────────────────────────────────────
    private void BuildBlocks()
    {
        int typeIndex = (int)Type;
        int[,] shape = Shapes[typeIndex];
        Sprite sprite = null;
        if (typeIndex < _gm.tetrominoSprites.Length)
        {
            sprite = _gm.tetrominoSprites[typeIndex];
        }

        // Final safety fallback
        if (sprite == null && _gm.tetrominoSprites.Length > 0)
        {
            sprite = _gm.tetrominoSprites[0];
        }

        float cellSize = _gm.CellSize;
        int blockCount = shape.GetLength(0);

        _ghostBlocks = new Transform[blockCount];

        for (int i = 0; i < blockCount; i++)
        {
            // Build main block
            GameObject block = new GameObject($"Block_{i}");
            block.transform.SetParent(transform);

            SpriteRenderer sr = block.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 1;

            float spriteSize = sprite.bounds.size.x;
            block.transform.localScale = Vector3.one * (cellSize / spriteSize);

            // Build ghost block
            GameObject ghost = new GameObject($"Ghost_{i}");
            ghost.transform.SetParent(transform);
            
            SpriteRenderer ghostSr = ghost.AddComponent<SpriteRenderer>();
            ghostSr.sprite = sprite;
            ghostSr.sortingOrder = 0; // behind real piece

            Color ghostColor = sr.color;
            ghostColor.a = 0.3f; // transluscent
            ghostSr.color = ghostColor;

            ghost.transform.localScale = block.transform.localScale;
            _ghostBlocks[i] = ghost.transform;
        }
    }

    // ─── Current squares (grid coords) ───────────────────────────────────────
    private Vector2Int[] GetSquares(Vector2Int pivot, int rotation)
    {
        int[,] baseShape = Shapes[(int)Type];
        int blockCount = baseShape.GetLength(0);
        Vector2Int[] squares = new Vector2Int[blockCount];

        for (int i = 0; i < blockCount; i++)
        {
            int localX = baseShape[i, 0];
            int localY = baseShape[i, 1];

            // Apply rotation (0°, 90°, 180°, 270°)
            int rx, ry;
            switch (rotation % 4)
            {
                case 1: rx = -localY; ry =  localX; break;   // 90° CW
                case 2: rx = -localX; ry = -localY; break;   // 180°
                case 3: rx =  localY; ry = -localX; break;   // 270° CW
                default: rx = localX; ry =  localY; break;   // 0°
            }

            // O-piece doesn't rotate
            if (Type == PieceType.O) { rx = localX; ry = localY; }

            squares[i] = new Vector2Int(pivot.x + rx, pivot.y + ry);
        }

        return squares;
    }

    private bool IsValidPosition(Vector2Int pivot) =>
        _grid.IsValidPiecePosition(GetSquares(pivot, _rotation), Vector2Int.zero);

    // ─── World position sync ──────────────────────────────────────────────────
    private void UpdateWorldPosition()
    {
        int[,] baseShape = Shapes[(int)Type];
        int blockCount = baseShape.GetLength(0);
        float cellSize = _gm.CellSize;

        Vector2Int[] squares = GetSquares(_gridPos, _rotation);
        
        // Calculate ghost drop grid position
        Vector2Int dropGridPos = GetDropPosition();
        Vector2Int[] ghostSquares = GetSquares(dropGridPos, _rotation);

        int bi = 0;
        foreach (Transform child in transform)
        {
            // Skip ghost blocks in this main loop
            if (child.name.StartsWith("Ghost")) continue;

            if (bi >= blockCount) break;
            
            // Real block position
            Vector2 worldPos = _gm.GridToWorld(squares[bi].x, squares[bi].y);
            child.position = new Vector3(worldPos.x, worldPos.y, 0f);

            // Ghost block position
            Vector2 ghostWorldPos = _gm.GridToWorld(ghostSquares[bi].x, ghostSquares[bi].y);
            _ghostBlocks[bi].position = new Vector3(ghostWorldPos.x, ghostWorldPos.y, 0f);

            bi++;
        }
    }

    private Vector2Int GetDropPosition()
    {
        Vector2Int dropPos = _gridPos;
        while (_grid.IsValidPiecePosition(GetSquares(new Vector2Int(dropPos.x, dropPos.y - 1), _rotation), Vector2Int.zero))
        {
            dropPos.y--;
        }
        return dropPos;
    }

    // ─── Fall loop ────────────────────────────────────────────────────────────
    private IEnumerator FallLoop()
    {
        while (!_locked)
        {
            float interval = GetFallInterval();
            yield return new WaitForSeconds(interval);

            if (!_locked) TryMoveDown();
        }
    }

    private float GetFallInterval()
    {
        return _gm.fallInterval;
    }

    // ─── Movement & rotation (called by InputHandler) ─────────────────────────
    public void MoveLeft()
    {
        if (_locked || _isHardDropping || _gm.IsGameOver || _gm.IsPaused) return;
        TryMove(new Vector2Int(-1, 0));
    }

    public void MoveRight()
    {
        if (_locked || _isHardDropping || _gm.IsGameOver || _gm.IsPaused) return;
        TryMove(new Vector2Int(1, 0));
    }

    public void Rotate()
    {
        if (_locked || _isHardDropping || _gm.IsGameOver || _gm.IsPaused) return;
        if (Type == PieceType.O) return; // O doesn't rotate

        int newRot = (_rotation + 1) % 4;

        // Try the rotated position, then wall-kick offsets
        if (_grid.IsValidPiecePosition(GetSquares(_gridPos, newRot), Vector2Int.zero))
        {
            _rotation = newRot;
            UpdateWorldPosition();
            _gm.Audio?.PlayRotate();
            return;
        }

        foreach (var kick in WallKicks[0])
        {
            Vector2Int kickedPivot = _gridPos + kick;
            if (_grid.IsValidPiecePosition(GetSquares(kickedPivot, newRot), Vector2Int.zero))
            {
                _rotation = newRot;
                _gridPos  = kickedPivot;
                UpdateWorldPosition();
                _gm.Audio?.PlayRotate();
                return;
            }
        }
        // Rotation blocked — do nothing
    }

    public void SoftDrop()
    {
        if (_locked || _isHardDropping || _gm.IsGameOver || _gm.IsPaused) return;
        TryMoveDown();
    }

    public void HardDrop()
    {
        if (_locked || _isHardDropping || _gm.IsGameOver || _gm.IsPaused) return;

        _isHardDropping = true;
        if (_fallRoutine != null) StopCoroutine(_fallRoutine);

        Vector2Int dropPos = GetDropPosition();
        StartCoroutine(SmoothDropRoutine(dropPos));
    }

    private IEnumerator SmoothDropRoutine(Vector2Int dropGridPos)
    {
        // Calculate world positions for interpolation
        Vector3 startOffset = transform.position;
        // Current position syncs blocks to GridToWorld, dropping means shifting the parent
        // but we normally move blocks individually. To keep it simple, we animate the block offsets.

        // Actually, since we don't move the parent (it stays at 0,0,0), we must animate the individual blocks.
        // Easiest way: set logical grid pos, calculate final world pos, and lerp them.

        int blockCount = Shapes[(int)Type].GetLength(0);
        Vector2Int[] startSquares = GetSquares(_gridPos, _rotation);
        Vector2Int[] endSquares   = GetSquares(dropGridPos, _rotation);

        Vector3[] startPositions = new Vector3[blockCount];
        Vector3[] endPositions   = new Vector3[blockCount];

        int bi = 0;
        foreach (Transform child in transform)
        {
            if (child.name.StartsWith("Ghost")) {
                child.gameObject.SetActive(false); // hide ghost during hard drop
                continue;
            }
            if (bi >= blockCount) break;

            startPositions[bi] = child.position;
            Vector2 endWPos = _gm.GridToWorld(endSquares[bi].x, endSquares[bi].y);
            endPositions[bi] = new Vector3(endWPos.x, endWPos.y, 0f);
            bi++;
        }

        float elapsed = 0f;
        while (elapsed < hardDropDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / hardDropDuration;

            bi = 0;
            foreach (Transform child in transform)
            {
                if (child.name.StartsWith("Ghost")) continue;
                if (bi >= blockCount) break;

                child.position = Vector3.Lerp(startPositions[bi], endPositions[bi], t);
                bi++;
            }
            yield return null;
        }

        _gridPos = dropGridPos;
        UpdateWorldPosition(); // snap exactly to end
        Lock();
    }

    // ─── Internal helpers ─────────────────────────────────────────────────────
    private void TryMove(Vector2Int delta)
    {
        Vector2Int newPos = _gridPos + delta;
        if (_grid.IsValidPiecePosition(GetSquares(newPos, _rotation), Vector2Int.zero))
        {
            _gridPos = newPos;
            UpdateWorldPosition();
            _gm.Audio?.PlayMove();
        }
    }

    private void TryMoveDown()
    {
        if (_locked || _gm.IsGameOver) return;

        if (CanMoveDown())
        {
            _gridPos.y--;
            UpdateWorldPosition();
        }
        else
        {
            Lock();
        }
    }

    private bool CanMoveDown()
    {
        Vector2Int testPos = new Vector2Int(_gridPos.x, _gridPos.y - 1);
        return _grid.IsValidPiecePosition(GetSquares(testPos, _rotation), Vector2Int.zero);
    }

    public void Lock()
    {
        if (_locked) return;
        _locked = true;

        if (_fallRoutine != null) StopCoroutine(_fallRoutine);

        // Crucial: Clear ghost before locking real blocks into the grid
        DestroyGhost();

        _gm.Audio?.PlayLock();

        // 1. Grid Logic: register blocks into the cell array
        _grid.LockPiece(this);
    }

    private void DestroyGhost()
    {
        if (_ghostBlocks == null) return;
        foreach (var ghost in _ghostBlocks)
        {
            if (ghost != null) Destroy(ghost.gameObject);
        }
        _ghostBlocks = null;
    }
}
