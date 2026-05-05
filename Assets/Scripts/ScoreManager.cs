using UnityEngine;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;
    private int currentScore = 0;

    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI feedbackText; // Optional: Add feedback text
    [SerializeField] private float feedbackDuration = 1f;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void AddPoints(int points)
    {
        currentScore += points;
        UpdateUI();

        // Show feedback
        string feedback = points > 0 ? $"+{points}" : $"{points}";
        ShowFeedback(feedback, points > 0 ? Color.green : Color.red);

        Debug.Log($"Score changed by {points}. New score: {currentScore}");
    }

    void UpdateUI()
    {
        if (scoreText != null)
            scoreText.text = $"Score: {currentScore}";
    }

    void ShowFeedback(string text, Color color)
    {
        if (feedbackText != null)
        {
            feedbackText.text = text;
            feedbackText.color = color;
            feedbackText.gameObject.SetActive(true);
            Invoke(nameof(HideFeedback), feedbackDuration);
        }
    }

    void HideFeedback()
    {
        if (feedbackText != null)
            feedbackText.gameObject.SetActive(false);
    }

    public int GetScore()
    {
        return currentScore;
    }

    public void ResetScore()
    {
        currentScore = 0;
        UpdateUI();
    }
}