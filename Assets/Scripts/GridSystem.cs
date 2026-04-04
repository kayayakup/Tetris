using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages the logical grid: cell occupancy, bounds checking, locking pieces,
/// detecting full lines, and shifting rows downward after a clear.
/// </summary>
public class GridSystem : MonoBehaviour
{
    // 2-D array of locked-in cell transforms (null = empty)
    private Transform[,] _cells;

    private int Width  => GameManager.Instance.gridWidth;
    private int Height => GameManager.Instance.gridHeight;

    // ─── Init ─────────────────────────────────────────────────────────────────
    private void Awake()
    {
        // Awake order: GameManager first (Script Execution Order), then this.
        // We lazily init to be safe.
    }

    private void EnsureGrid()
    {
        if (_cells == null)
            _cells = new Transform[Width, Height];
    }

    public void ClearGrid()
    {
        EnsureGrid();
        for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++)
            {
                if (_cells[x, y] != null)
                    Destroy(_cells[x, y].gameObject);
                _cells[x, y] = null;
            }
    }

    // ─── Bounds & occupancy ───────────────────────────────────────────────────
    public bool IsInBounds(int x, int y)
    {
        EnsureGrid();
        return x >= 0 && x < Width && y >= 0 && y < Height;
    }

    public bool IsOccupied(int x, int y)
    {
        EnsureGrid();
        return IsInBounds(x, y) && _cells[x, y] != null;
    }

    /// <summary>Returns true if the position is inside the grid AND not occupied.</summary>
    public bool IsValidPosition(int x, int y)
    {
        EnsureGrid();
        if (x < 0 || x >= Width) return false; // wall collision
        if (y < 0)               return false; // floor collision
        if (y >= Height)         return true;  // above grid is fine
        return _cells[x, y] == null;
    }

    /// <summary>
    /// Validates an entire piece shape at an offset.
    /// Returns true when every occupied square is inside a valid position.
    /// </summary>
    public bool IsValidPiecePosition(Vector2Int[] squares, Vector2Int offset)
    {
        foreach (var sq in squares)
        {
            int x = sq.x + offset.x;
            int y = sq.y + offset.y;
            if (!IsValidPosition(x, y)) return false;
        }
        return true;
    }

    // ─── Locking ──────────────────────────────────────────────────────────────
    /// <summary>
    /// Locks each child block of the given tetromino into the grid array,
    /// then checks for completed lines.
    /// </summary>
    public void LockPiece(Tetromino piece)
    {
        EnsureGrid();

        foreach (Transform block in piece.transform)
        {
            // Skip ghost pieces or other non-block children just in case
            if (block.name.StartsWith("Ghost")) continue;

            // Compute grid coords from world position
            Vector2Int coord = WorldToGrid(block.position);

            if (!IsInBounds(coord.x, coord.y))
            {
                // Block above the grid top → game over
                GameManager.Instance.TriggerGameOver();
                Destroy(piece.gameObject);
                return;
            }

            _cells[coord.x, coord.y] = block;
        }

        // Detach blocks from the piece parent so the piece GO can be destroyed
        piece.transform.DetachChildren();
        Destroy(piece.gameObject);

        // Report piece placement score (+5 points)
        GameManager.Instance.Score?.RegisterPiecePlace();

        StartCoroutine(CheckAndClearLines());
    }

    // ─── Line clearing ────────────────────────────────────────────────────────
    private IEnumerator CheckAndClearLines()
    {
        EnsureGrid();

        List<int> fullRows = new List<int>();

        for (int y = 0; y < Height; y++)
        {
            if (IsRowFull(y))
                fullRows.Add(y);
        }

        if (fullRows.Count == 0)
        {
            GameManager.Instance.Spawner.SpawnNext();
            yield break;
        }

        GameManager.Instance.Audio?.PlayClear();

        // Collect all cell transforms in the full rows for animation
        List<Transform> toAnimate = new List<Transform>();
        foreach (int row in fullRows)
            for (int x = 0; x < Width; x++)
                if (_cells[x, row] != null)
                    toAnimate.Add(_cells[x, row]);

        // Play line-clear animation and wait for it to finish
        yield return StartCoroutine(LineClearAnimation.PlayAll(toAnimate));

        // Destroy animated blocks
        foreach (var t in toAnimate)
            if (t != null) Destroy(t.gameObject);

        // Remove full rows from the grid array (highest row first to preserve indices)
        fullRows.Sort((a, b) => b.CompareTo(a));
        foreach (int row in fullRows)
            ClearRow(row);

        // Report score
        GameManager.Instance.Score?.RegisterLineClear(fullRows.Count);

        // Spawn next piece
        GameManager.Instance.Spawner.SpawnNext();
    }

    private bool IsRowFull(int y)
    {
        for (int x = 0; x < Width; x++)
            if (_cells[x, y] == null) return false;
        return true;
    }

    private void ClearRow(int row)
    {
        // Null out the row (blocks already destroyed)
        for (int x = 0; x < Width; x++)
            _cells[x, row] = null;

        // Shift everything above this row down by one
        for (int y = row + 1; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                if (_cells[x, y] != null)
                {
                    Transform block = _cells[x, y];
                    // Move world position down one cell
                    block.position += Vector3.down * GameManager.Instance.CellSize;
                    _cells[x, y - 1] = block;
                    _cells[x, y]     = null;
                }
            }
        }
    }

    // ─── Coordinate helpers ───────────────────────────────────────────────────
    public Vector2Int WorldToGrid(Vector3 worldPos)
    {
        Vector2 origin   = GameManager.Instance.GridOrigin;
        float   cellSize = GameManager.Instance.CellSize;

        int gx = Mathf.FloorToInt((worldPos.x - origin.x) / cellSize);
        int gy = Mathf.FloorToInt((worldPos.y - origin.y) / cellSize);
        return new Vector2Int(gx, gy);
    }
}
