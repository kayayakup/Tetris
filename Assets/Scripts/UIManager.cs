using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages all UI displays (HUD, Game Over, High Score).
/// Subscribes to events from ScoreManager and GameManager.
/// </summary>
public class UIManager : MonoBehaviour
{
    [Header("In-Game HUD")]
    [Tooltip("Text element for the current live score.")]
    public TextMeshProUGUI scoreText;
    [Tooltip("Text element for the highest lifetime score.")]
    public TextMeshProUGUI bestScoreText;

    [Header("Game Over Menu")]
    [Tooltip("Panel displayed when the player loses.")]
    public GameObject gameOverPanel;
    [Tooltip("Text element inside the Game Over panel showing final score.")]
    public TextMeshProUGUI gameOverScoreText;
    [Tooltip("Button used to restart the game.")]
    public Button playAgainButton;

    private void Awake()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.UI = this;

        // Ensure Game Over is hidden at start (Awake happens before most game logic)
        HideGameOverMenu();
    }

    private void Start()
    {
        // 1. Subscribing to Score Changes
        if (GameManager.Instance != null && GameManager.Instance.Score != null)
        {
            GameManager.Instance.Score.OnScoreChanged.AddListener(UpdateScoreUI);
            GameManager.Instance.Score.OnHighScoreChanged.AddListener(UpdateBestScoreUI);

            // Initial UI Sync
            UpdateScoreUI(GameManager.Instance.Score.Score);
            UpdateBestScoreUI(GameManager.Instance.Score.HighScore);
        }

        // 2. Setup Button to Restart Game
        if (playAgainButton != null)
        {
            playAgainButton.onClick.AddListener(() => {
                HideGameOverMenu();
                GameManager.Instance.StartGame();
            });
        }
    }

    private void UpdateScoreUI(int score)
    {
        if (scoreText != null) 
            scoreText.text = "Score:\n" + score;
    }

    private void UpdateBestScoreUI(int best)
    {
        if (bestScoreText != null) 
            bestScoreText.text = "Best:\n" + best;
    }

    public void ShowGameOverMenu()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);

            // Update the summary score in the game over panel
            if (gameOverScoreText != null && GameManager.Instance.Score != null)
            {
                gameOverScoreText.text = GameManager.Instance.Score.Score.ToString();
            }
        }
    }

    public void HideGameOverMenu()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }
}
