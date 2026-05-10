using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI comboText;
    [SerializeField] private TextMeshProUGUI feedbackText;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TextMeshProUGUI finalScoreText;

    private float feedbackTimer = 0f;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Update()
    {
        if (feedbackTimer > 0)
        {
            feedbackTimer -= Time.deltaTime;
            if (feedbackTimer <= 0 && feedbackText != null)
                feedbackText.gameObject.SetActive(false);
        }
    }

    public void UpdateTimer(float time)
    {
        // Timer is handled by visual fill, but you can keep this for text
    }

    public void ShowFeedback(string text, bool isPositive)
    {
        if (feedbackText != null)
        {
            feedbackText.text = text;
            feedbackText.color = isPositive ? Color.green : Color.red;
            feedbackText.gameObject.SetActive(true);
            feedbackTimer = 1f;
        }
    }

    public void UpdateCombo(int combo)
    {
        if (comboText != null)
        {
            if (combo > 1)
            {
                comboText.text = $"x{combo} COMBO!";
                comboText.gameObject.SetActive(true);
            }
            else
            {
                comboText.gameObject.SetActive(false);
            }
        }
    }

    public void ShowGameOver(int finalScore)
    {
        if (gameOverPanel != null)
        {
            if (finalScoreText != null)
                finalScoreText.text = $"Final Score: {finalScore}";
            gameOverPanel.SetActive(true);
        }
    }
}