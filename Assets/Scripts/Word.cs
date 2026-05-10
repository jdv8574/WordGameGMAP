using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class Word : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Word Data")]
    public string wordText;
    public bool isCorrectSpelling;
    public string correctSpelling;

    [Header("References")]
    public TextMeshProUGUI wordDisplay;

    private Vector2 originalAnchoredPosition;
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private bool isDragging = false;
    private Color originalColor;
    private float originalFontSize;
    private Image backgroundImage;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        rectTransform = GetComponent<RectTransform>();
        backgroundImage = GetComponent<Image>();

        if (backgroundImage != null)
            originalColor = backgroundImage.color;

        if (wordDisplay != null)
            originalFontSize = wordDisplay.fontSize;

        if (wordDisplay == null)
            wordDisplay = GetComponentInChildren<TextMeshProUGUI>();

        // Add collider for bucket detection
        if (GetComponent<Collider2D>() == null)
        {
            var col = gameObject.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
        }

        BoxCollider2D boxCol = GetComponent<BoxCollider2D>();
        if (boxCol != null && rectTransform != null)
        {
            boxCol.size = rectTransform.rect.size;
        }
    }

    public void Initialize(string word, bool isCorrect, string correctWord = "")
    {
        wordText = word;
        isCorrectSpelling = isCorrect;
        correctSpelling = correctWord;

        if (wordDisplay != null)
        {
            wordDisplay.text = word;
            wordDisplay.fontSize = originalFontSize;
            wordDisplay.color = Color.black;
        }

        if (backgroundImage != null)
            backgroundImage.color = originalColor;

        gameObject.SetActive(true);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        isDragging = true;
        originalAnchoredPosition = rectTransform.anchoredPosition;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0.8f;
            canvasGroup.blocksRaycasts = false;
        }

        if (backgroundImage != null)
            backgroundImage.color = Color.yellow;

        if (wordDisplay != null)
        {
            wordDisplay.fontSize = originalFontSize + 8;
            wordDisplay.color = Color.red;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (rectTransform == null) return;
        rectTransform.position = Input.mousePosition;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;

        Bucket targetBucket = null;
        Bucket[] buckets = FindObjectsOfType<Bucket>();

        foreach (Bucket bucket in buckets)
        {
            if (bucket.IsMouseOverBucket())
            {
                targetBucket = bucket;
                break;
            }
        }

        if (targetBucket != null)
        {
            OnSortedIntoBucket(targetBucket.isCorrectBucket);
        }
        else
        {
            if (rectTransform != null)
                rectTransform.anchoredPosition = originalAnchoredPosition;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
        }

        if (backgroundImage != null)
            backgroundImage.color = originalColor;

        if (wordDisplay != null)
        {
            wordDisplay.fontSize = originalFontSize;
            wordDisplay.color = Color.black;
        }
    }

    public void OnSortedIntoBucket(bool isBucketCorrect)
    {
        int pointsToAdd = 0;
        bool isCorrect = false;
        string feedback = "";

        if (isCorrectSpelling && isBucketCorrect)
        {
            // Correct word in CORRECT bucket
            pointsToAdd = 10;
            isCorrect = true;
            feedback = "+10";
            FlashColor(Color.green);
        }
        else if (!isCorrectSpelling && !isBucketCorrect)
        {
            // Misspelled word in INCORRECT bucket
            pointsToAdd = 15;
            isCorrect = true;
            feedback = "+15";
            FlashColor(Color.green);
        }
        else if (isCorrectSpelling && !isBucketCorrect)
        {
            // Correct word in WRONG bucket
            pointsToAdd = -5;
            isCorrect = false;
            feedback = "-5";
            FlashColor(Color.red);
        }
        else if (!isCorrectSpelling && isBucketCorrect)
        {
            // Misspelled word in CORRECT bucket
            pointsToAdd = -5;
            isCorrect = false;
            feedback = "-5";
            FlashColor(Color.red);
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddPoints(pointsToAdd);
            GameManager.Instance.ShowScorePopup(pointsToAdd, transform.position);
        }

        if (UIManager.Instance != null)
            UIManager.Instance.ShowFeedback(feedback, isCorrect);

        // Play sound effect (you'll need to set this up)
        PlaySortingSound(isCorrect);

        Destroy(gameObject);
    }

    void FlashColor(Color flashColor)
    {
        if (backgroundImage != null)
        {
            StartCoroutine(FlashRoutine(flashColor));
        }
    }

    System.Collections.IEnumerator FlashRoutine(Color flashColor)
    {
        backgroundImage.color = flashColor;
        yield return new WaitForSeconds(0.15f);
        backgroundImage.color = originalColor;
    }

    void PlaySortingSound(bool isCorrect)
    {
        // You can implement sound effects here
        // AudioManager.PlaySound(isCorrect ? "correct" : "wrong");
    }

    public bool IsBeingDragged()
    {
        return isDragging;
    }
}