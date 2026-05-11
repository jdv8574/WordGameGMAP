using UnityEngine;
using TMPro;
using System.Collections;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI comboText;
    [SerializeField] private TextMeshProUGUI comboBonusText;
    [SerializeField] private TextMeshProUGUI feedbackText;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TextMeshProUGUI finalScoreText;
    [SerializeField] private GameObject comboMeter;

    private float feedbackTimer = 0f;
    private float bonusTextTimer = 0f;
    private Vector3 feedbackOriginalPosition;
    private Vector3 comboOriginalScale;
    private Vector3 comboBonusOriginalScale;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        if (feedbackText != null)
        {
            feedbackOriginalPosition = feedbackText.transform.position;
            feedbackText.gameObject.SetActive(false);
        }

        if (comboText != null)
            comboOriginalScale = comboText.transform.localScale;
        if (comboBonusText != null)
            comboBonusOriginalScale = comboBonusText.transform.localScale;
    }

    void Update()
    {
        if (feedbackTimer > 0)
        {
            feedbackTimer -= Time.deltaTime;
            if (feedbackTimer <= 0 && feedbackText != null)
                feedbackText.gameObject.SetActive(false);
        }

        if (bonusTextTimer > 0)
        {
            bonusTextTimer -= Time.deltaTime;
            if (bonusTextTimer <= 0 && comboBonusText != null)
                comboBonusText.gameObject.SetActive(false);
        }
    }

    public void UpdateTimer(float time) { }

    public void ShowFeedback(string text, bool isPositive)
    {
        if (feedbackText == null) return;

        StopAllCoroutines();

        feedbackText.text = text;
        feedbackText.color = isPositive ? Color.green : Color.red;

        feedbackText.transform.position = feedbackOriginalPosition;
        feedbackText.transform.localScale = Vector3.one;

        Color color = feedbackText.color;
        color.a = 1f;
        feedbackText.color = color;

        feedbackText.gameObject.SetActive(true);
        feedbackTimer = 1f;

        StartCoroutine(AnimateFeedbackText());
    }

    IEnumerator AnimateFeedbackText()
    {
        float elapsedTime = 0f;
        Vector3 startPosition = feedbackOriginalPosition;
        Vector3 endPosition = startPosition + new Vector3(0, 50, 0);

        while (elapsedTime < 1f)
        {
            float t = elapsedTime / 1f;
            feedbackText.transform.position = Vector3.Lerp(startPosition, endPosition, t);

            float scale = 1f;
            if (t < 0.2f)
                scale = 1f + (t * 2f);
            else
                scale = 1.4f - ((t - 0.2f) / 0.8f) * 0.4f;

            feedbackText.transform.localScale = Vector3.one * Mathf.Clamp(scale, 0.8f, 1.4f);

            Color color = feedbackText.color;
            if (t > 0.6f)
            {
                float alpha = 1f - ((t - 0.6f) / 0.4f);
                color.a = alpha;
                feedbackText.color = color;
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        feedbackText.gameObject.SetActive(false);
    }

    public void ShowComboBonus(int bonus)
    {
        if (comboBonusText != null)
        {
            comboBonusText.text = $"+{bonus} BONUS!";
            comboBonusText.color = Color.yellow;
            comboBonusText.gameObject.SetActive(true);
            comboBonusText.transform.localScale = comboBonusOriginalScale;
            bonusTextTimer = 1f;
            StartCoroutine(AnimateBonusText());
        }
    }

    IEnumerator AnimateBonusText()
    {
        if (comboBonusText == null) yield break;

        Vector3 originalScale = comboBonusOriginalScale;
        comboBonusText.transform.localScale = originalScale * 1.5f;
        yield return new WaitForSeconds(0.15f);
        comboBonusText.transform.localScale = originalScale;
    }

    public void UpdateCombo(int combo)
    {
        if (comboText != null)
        {
            if (combo > 1)
            {
                comboText.text = $"{combo}x COMBO!";
                comboText.gameObject.SetActive(true);
                comboText.transform.localScale = comboOriginalScale;
                StartCoroutine(AnimateComboText());

                if (combo >= 15)
                    comboText.color = Color.magenta;
                else if (combo >= 10)
                    comboText.color = new Color(1f, 0.5f, 0f);
                else if (combo >= 5)
                    comboText.color = Color.red;
                else
                    comboText.color = Color.yellow;
            }
            else
            {
                comboText.gameObject.SetActive(false);
                comboText.transform.localScale = comboOriginalScale;
            }
        }
    }

    IEnumerator AnimateComboText()
    {
        if (comboText == null) yield break;

        Vector3 originalScale = comboOriginalScale;
        comboText.transform.localScale = originalScale * 1.3f;
        yield return new WaitForSeconds(0.1f);
        comboText.transform.localScale = originalScale * 0.9f;
        yield return new WaitForSeconds(0.05f);
        comboText.transform.localScale = originalScale;
    }

    public void UpdateComboTime(float remainingTime) { }

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