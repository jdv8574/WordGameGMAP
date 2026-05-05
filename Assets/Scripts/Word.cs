using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class Word : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Word Data")]
    public string wordText;
    public string definition;
    public bool isCorrectSpelling;
    public string correctSpelling;

    [Header("References")]
    public TextMeshProUGUI wordDisplay;
    public TextMeshProUGUI definitionDisplay;
    public GameObject tooltipPanel;

    private Vector2 originalAnchoredPosition;
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private bool isDragging = false;
    private Color originalColor;
    private float originalFontSize;

    void Awake()
    {
        // Get components
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        rectTransform = GetComponent<RectTransform>();
        if (rectTransform == null)
            Debug.LogError("Word prefab needs a RectTransform!");

        // Store original colors
        Image img = GetComponent<Image>();
        if (img != null)
            originalColor = img.color;

        if (wordDisplay != null)
            originalFontSize = wordDisplay.fontSize;

        // Auto-find displays if not assigned
        if (wordDisplay == null)
            wordDisplay = GetComponentInChildren<TextMeshProUGUI>();

        if (definitionDisplay == null)
        {
            var allTexts = GetComponentsInChildren<TextMeshProUGUI>();
            if (allTexts.Length > 1)
                definitionDisplay = allTexts[1];
        }

        if (tooltipPanel == null && definitionDisplay != null)
            tooltipPanel = definitionDisplay.gameObject;

        // Hide tooltip initially
        if (tooltipPanel != null)
            tooltipPanel.SetActive(false);

        // Add collider for raycast detection
        if (GetComponent<Collider2D>() == null)
        {
            var col = gameObject.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
        }

        // Set collider size
        BoxCollider2D boxCol = GetComponent<BoxCollider2D>();
        if (boxCol != null && rectTransform != null)
        {
            boxCol.size = rectTransform.rect.size;
        }
    }

    public void Initialize(string word, string def, bool isCorrect, string correctWord = "")
    {
        wordText = word;
        definition = def;
        isCorrectSpelling = isCorrect;
        correctSpelling = correctWord;

        if (wordDisplay != null)
        {
            wordDisplay.text = word;
            wordDisplay.fontSize = originalFontSize;
            wordDisplay.color = Color.black;
        }

        if (definitionDisplay != null)
            definitionDisplay.text = def;

        // Reset visual state
        Image img = GetComponent<Image>();
        if (img != null)
            img.color = originalColor;

        gameObject.SetActive(true);
    }

    void Update()
    {
        // Skip if dragging
        if (isDragging || tooltipPanel == null || rectTransform == null)
            return;

        // Simple hover detection for tooltip
        Vector2 mousePos = Input.mousePosition;
        bool isOver = RectTransformUtility.RectangleContainsScreenPoint(rectTransform, mousePos);

        if (isOver && !tooltipPanel.activeSelf)
        {
            tooltipPanel.SetActive(true);
        }
        else if (!isOver && tooltipPanel.activeSelf)
        {
            tooltipPanel.SetActive(false);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        isDragging = true;
        originalAnchoredPosition = rectTransform.anchoredPosition;

        // Make word semi-transparent while dragging
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0.8f;
            canvasGroup.blocksRaycasts = false;
        }

        // Visual feedback
        Image img = GetComponent<Image>();
        if (img != null)
        {
            img.color = Color.yellow;
        }

        if (wordDisplay != null)
        {
            wordDisplay.fontSize = originalFontSize + 8;
            wordDisplay.color = Color.red;
        }

        // Hide tooltip while dragging
        if (tooltipPanel != null)
            tooltipPanel.SetActive(false);

        Debug.Log($"Started dragging: {wordText}");
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (rectTransform == null) return;

        // Move word with mouse
        rectTransform.position = Input.mousePosition;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;

        // Check which bucket the mouse is over
        Bucket targetBucket = null;
        Bucket[] buckets = FindObjectsOfType<Bucket>();

        foreach (Bucket bucket in buckets)
        {
            if (bucket.IsMouseOverBucket())
            {
                targetBucket = bucket;
                Debug.Log($"Word dropped on bucket: {bucket.name}");
                break;
            }
        }

        if (targetBucket != null)
        {
            // Dropped on a bucket - process the sorting
            OnSortedIntoBucket(targetBucket.isCorrectBucket);
        }
        else
        {
            // Not dropped on bucket - return to original position
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = originalAnchoredPosition;
            }
            Debug.Log($"Word {wordText} dropped outside bucket - returning to position");
        }

        // Restore appearance
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
        }

        Image img = GetComponent<Image>();
        if (img != null)
        {
            img.color = originalColor;
        }

        if (wordDisplay != null)
        {
            wordDisplay.fontSize = originalFontSize;
            wordDisplay.color = Color.black;
        }
    }

    public void OnSortedIntoBucket(bool isBucketCorrect)
    {
        int pointsToAdd = 0;
        string message = "";

        // Rule 1: Correct word in correct bucket = +10 points
        if (isCorrectSpelling && isBucketCorrect)
        {
            pointsToAdd = 10;
            message = $"✓ Correct word '{wordText}' in correct bucket! +10 points";
            if (ScoreManager.Instance != null)
                ScoreManager.Instance.AddPoints(pointsToAdd);
            Destroy(gameObject);
        }
        // Rule 2: Word put into incorrect bucket = +5 points (any word, any bucket that's wrong)
        else if (!isBucketCorrect)
        {
            pointsToAdd = 5;
            message = $"✓ Word '{wordText}' put in incorrect bucket! +5 points";
            if (ScoreManager.Instance != null)
                ScoreManager.Instance.AddPoints(pointsToAdd);

            // If it's a misspelled word AND in incorrect bucket, also trigger bonus
            if (!isCorrectSpelling)
            {
                Debug.Log($"Misspelled word in incorrect bucket - triggering spelling bonus!");
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.StartSpellingCorrection(this);
                    return; // Don't destroy yet - bonus system handles it
                }
            }
            Destroy(gameObject);
        }
        // Rule: Correct word in correct bucket already handled above
        // Any other combination? (misspelled in correct bucket)
        else if (!isCorrectSpelling && isBucketCorrect)
        {
            pointsToAdd = 0;
            message = $"Word '{wordText}' is misspelled in correct bucket - no points";
            if (ScoreManager.Instance != null)
                ScoreManager.Instance.AddPoints(pointsToAdd);
            Destroy(gameObject);
        }

        Debug.Log(message);

        // If we haven't destroyed yet and not triggering bonus
        if (!(!isCorrectSpelling && !isBucketCorrect))
        {
            Destroy(gameObject);
        }
    }

    public bool IsBeingDragged()
    {
        return isDragging;
    }
}