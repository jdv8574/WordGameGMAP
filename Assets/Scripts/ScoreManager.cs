using UnityEngine;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;
    private int currentScore = 0;

    [SerializeField] private TextMeshProUGUI scoreText;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
    }

    public void AddPoints(int points)
    {
        currentScore += points;
        UpdateUI();
    }

    void UpdateUI()
    {
        if (scoreText != null)
            scoreText.text = $"Score: {currentScore}";
    }
}