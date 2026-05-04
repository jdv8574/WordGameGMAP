using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI bonusTimerText;
    [SerializeField] private GameObject gameOverPanel;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
    }

    public void UpdateTimer(float time)
    {
        if (timerText != null)
            timerText.text = $"Time: {Mathf.CeilToInt(time)}s";
    }

    public void UpdateBonusTimer(float time)
    {
        if (bonusTimerText != null)
            bonusTimerText.text = $"Bonus Time: {Mathf.CeilToInt(time)}s";
    }

    public void ShowGameOver()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);
    }
}