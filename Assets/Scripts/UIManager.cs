using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

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

    [Header("UI Animations")]
    public float punchScaleFactor = 1.2f;
    public float punchDuration = 0.15f;
    
    private Coroutine _scorePunch;
    private Coroutine _bestPunch;

    [Header("Game Over Menu")]
    [Tooltip("Panel displayed when the player loses.")]
    public GameObject gameOverPanel;
    [Tooltip("Text element inside the Game Over panel showing final score.")]
    public TextMeshProUGUI gameOverScoreText;
    [Tooltip("Button used to restart the game.")]
    public Button playAgainButton;

    [Header("Instructions Menu")]
    [Tooltip("Panel displayed when the player first opens the game.")]
    public GameObject instructionPanel;
    [Tooltip("Button used to start the game after reading instructions.")]
    public Button gotItButton;

    private void Awake()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.UI = this;

        // Ensure Game Over is hidden at start (Awake happens before most game logic)
        HideGameOverMenu();
        if (instructionPanel != null) instructionPanel.SetActive(false);
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

        // 3. Setup Instruction Confirm Button
        if (gotItButton != null)
        {
            gotItButton.onClick.AddListener(HideInstructions);
        }
    }

    private void UpdateScoreUI(int score)
    {
        if (scoreText != null) 
        {
            // Rich text: Small "SCORE" label above large vibrant number
            scoreText.text = "<size=60%>SCORE</size>\n<color=#00FFCC>" + score + "</color>";
            
            if (_scorePunch != null) StopCoroutine(_scorePunch);
            _scorePunch = StartCoroutine(PunchScale(scoreText.rectTransform, punchScaleFactor, punchDuration));
        }
    }

    private void UpdateBestScoreUI(int best)
    {
        if (bestScoreText != null) 
        {
            // Rich text: Small "BEST" label above large gold number
            bestScoreText.text = "<size=60%>BEST</size>\n<color=#FFD700>" + best + "</color>";
            
            if (_bestPunch != null) StopCoroutine(_bestPunch);
            _bestPunch = StartCoroutine(PunchScale(bestScoreText.rectTransform, punchScaleFactor * 1.1f, punchDuration * 1.2f));
        }
    }

    private IEnumerator PunchScale(RectTransform rt, float scale, float duration)
    {
        if (rt == null) yield break;

        Vector3 startScale = Vector3.one;
        Vector3 targetScale = Vector3.one * scale;

        // Punch Up
        float elapsed = 0;
        float halfDuration = duration * 0.5f;

        while (elapsed < halfDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            rt.localScale = Vector3.Lerp(startScale, targetScale, elapsed / halfDuration);
            yield return null;
        }

        // Return to Normal
        elapsed = 0;
        while (elapsed < halfDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            rt.localScale = Vector3.Lerp(targetScale, startScale, elapsed / halfDuration);
            yield return null;
        }

        rt.localScale = startScale;
    }

    public void ShowInstructions()
    {
        if (instructionPanel != null)
            instructionPanel.SetActive(true);
    }

    public void HideInstructions()
    {
        if (instructionPanel != null)
            instructionPanel.SetActive(false);
        
        PlayerPrefs.SetInt("HasSeenInstructions", 1);
        PlayerPrefs.Save();

        if (GameManager.Instance != null && !GameManager.Instance.IsGameOver)
            GameManager.Instance.PauseGame(false);
    }

    public void ShowGameOverMenu()
    {
        if (gameOverPanel != null)
        {
            GoogleAdMobController.Instance.ShowInterstitialAd();
            gameOverPanel.SetActive(true);

            // Update the summary score in the game over panel
            if (gameOverScoreText != null && GameManager.Instance.Score != null)
            {
                gameOverScoreText.text = "<color=#00FFCC>" + GameManager.Instance.Score.Score.ToString() + "</color>";
                StartCoroutine(PunchScale(gameOverScoreText.rectTransform, 1.5f, 0.4f));
            }
        }
    }

    public void HideGameOverMenu()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }
}
