using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class PowerUp : MonoBehaviour
{
    private GameManager.PowerUpType type;
    private TextMeshProUGUI displayText;
    private Image backgroundImage;
    private float lifeTime = 8f;

    public void Initialize(GameManager.PowerUpType powerUpType)
    {
        type = powerUpType;

        // Setup visual
        displayText = GetComponentInChildren<TextMeshProUGUI>();
        backgroundImage = GetComponent<Image>();

        if (displayText != null)
        {
            switch (type)
            {
                case GameManager.PowerUpType.ExtraPoints:
                    displayText.text = "+50";
                    if (backgroundImage != null) backgroundImage.color = new Color(1f, 0.84f, 0f); // Gold color
                    break;
                case GameManager.PowerUpType.TimeBonus:
                    displayText.text = "+5s";
                    if (backgroundImage != null) backgroundImage.color = Color.cyan;
                    break;
                case GameManager.PowerUpType.AutoSort:
                    displayText.text = "AUTO";
                    if (backgroundImage != null) backgroundImage.color = Color.magenta;
                    break;
                case GameManager.PowerUpType.SlowTime:
                    displayText.text = "SLOW";
                    if (backgroundImage != null) backgroundImage.color = Color.blue;
                    break;
            }
        }

        Destroy(gameObject, lifeTime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Word word = other.GetComponent<Word>();
        if (word != null)
        {
            if (GameManager.Instance != null)
                GameManager.Instance.ActivatePowerUp(type);

            Destroy(gameObject);
        }
    }
}