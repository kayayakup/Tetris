using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Tracks score, lines cleared, and level.
/// Saves/loads high score via PlayerPrefs.
/// </summary>
public class ScoreManager : MonoBehaviour
{
    // ─── Scoring Constants ────────────────────────────────────────────────────
    private const int PointsPerPiece = 5;
    private const int PointsPerLine  = 100;
    private const string HighScoreKey = "TetrisHighScore";

    // ─── State (read-only) ────────────────────────────────────────────────────
    public int Score     { get; private set; }
    public int Lines     { get; private set; }
    public int Level     { get; private set; } = 1;
    public int HighScore { get; private set; }

    // ─── Events (bind UI here) ────────────────────────────────────────────────
    public UnityEvent<int> OnScoreChanged;
    public UnityEvent<int> OnLinesChanged;
    public UnityEvent<int> OnLevelChanged;
    public UnityEvent<int> OnHighScoreChanged;

    // ─── Lifecycle ────────────────────────────────────────────────────────────
    private void Awake()
    {
        HighScore = PlayerPrefs.GetInt(HighScoreKey, 0);
    }

    // ─── Public API ───────────────────────────────────────────────────────────
    public void ResetScore()
    {
        Score = 0;
        Lines = 0;
        Level = 1;

        OnScoreChanged?.Invoke(Score);
        OnLinesChanged?.Invoke(Lines);
        OnLevelChanged?.Invoke(Level);
    }

    /// <summary>Call this when a piece is locked into the grid (+5 points).</summary>
    public void RegisterPiecePlace()
    {
        AddScore(PointsPerPiece);
    }

    /// <summary>Call this after each line-clear event with the number of lines cleared (+100 per line).</summary>
    public void RegisterLineClear(int linesCleared)
    {
        if (linesCleared <= 0) return;
        
        AddScore(linesCleared * PointsPerLine);
        
        Lines += linesCleared;
        // Level up every 10 lines (optional classic touch)
        Level = Lines / 10 + 1;

        OnLinesChanged?.Invoke(Lines);
        OnLevelChanged?.Invoke(Level);
    }

    private void AddScore(int amount)
    {
        Score += amount;
        
        // High score check & save
        if (Score > HighScore)
        {
            HighScore = Score;
            PlayerPrefs.SetInt(HighScoreKey, HighScore);
            PlayerPrefs.Save();
            OnHighScoreChanged?.Invoke(HighScore);
        }

        OnScoreChanged?.Invoke(Score);
    }
}
