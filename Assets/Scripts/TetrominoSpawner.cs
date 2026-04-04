using UnityEngine;

/// <summary>
/// Manages piece spawning using the 7-bag randomiser.
/// Guarantees every piece type appears before any repeats.
/// </summary>
public class TetrominoSpawner : MonoBehaviour
{
    // ─── Prefab—set in Editor or left null to create at runtime ───────────────
    [Tooltip("Leave null: spawner will create pieces from code.")]
    public GameObject tetrominoPrefab;

    // ─── Bag randomiser ───────────────────────────────────────────────────────
    private int[] _bag     = new int[7];
    private int   _bagIndex = 7; // start past end to force a refill

    // ─────────────────────────────────────────────────────────────────────────
    private void Awake()
    {
        ShuffleBag();
    }

    private void ShuffleBag()
    {
        for (int i = 0; i < 7; i++) _bag[i] = i;
        // Fisher-Yates shuffle
        for (int i = 6; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (_bag[i], _bag[j]) = (_bag[j], _bag[i]);
        }
        _bagIndex = 0;
    }

    private Tetromino.PieceType NextType()
    {
        if (_bagIndex >= 7) ShuffleBag();
        return (Tetromino.PieceType)_bag[_bagIndex++];
    }

    // ─── Spawn ───────────────────────────────────────────────────────────────
    public void SpawnNext()
    {
        if (GameManager.Instance.IsGameOver) return;

        Tetromino.PieceType type = NextType();

        // Spawn position: horizontally centred at top of grid
        int spawnCol = GameManager.Instance.gridWidth / 2;
        int spawnRow = GameManager.Instance.gridHeight; // one row above visible top

        Vector2Int spawnGrid = new Vector2Int(spawnCol, spawnRow);

        // Create the piece GameObject
        GameObject pieceGO;
        if (tetrominoPrefab != null)
        {
            pieceGO = Instantiate(tetrominoPrefab);
        }
        else
        {
            pieceGO = new GameObject($"Tetromino_{type}");
        }

        Tetromino piece = pieceGO.AddComponent<Tetromino>();

        // Register active piece so InputHandler can reference it
        GameManager.Instance.Input?.SetActivePiece(piece);

        piece.Init(type, spawnGrid);
    }
}
