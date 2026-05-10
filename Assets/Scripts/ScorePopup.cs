using UnityEngine;
using TMPro;

public class ScorePopup : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private AnimationCurve movementCurve;

    private float lifeTime = 0.8f;
    private float startTime;
    private Vector3 startPosition;
    private Vector3 endPosition;

    public void Initialize(int points)
    {
        startPosition = transform.position;
        endPosition = startPosition + new Vector3(0, 50, 0);
        startTime = Time.time;

        if (scoreText != null)
        {
            scoreText.text = points > 0 ? $"+{points}" : $"{points}";
            scoreText.color = points > 0 ? Color.green : Color.red;
        }

        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        float t = (Time.time - startTime) / lifeTime;
        transform.position = Vector3.Lerp(startPosition, endPosition, movementCurve.Evaluate(t));

        if (scoreText != null)
        {
            Color c = scoreText.color;
            c.a = 1 - t;
            scoreText.color = c;
        }
    }
}