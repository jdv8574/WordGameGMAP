using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
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
    private Vector2 dragOffset;
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

        // Store original colors for debug
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

        // Add collider for bucket detection
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
            wordDisplay.text = word;

        if (definitionDisplay != null)
            definitionDisplay.text = def;

        gameObject.SetActive(true);

        // Reset visual state
        Image img = GetComponent<Image>();
        if (img != null)
            img.color = originalColor;

        if (wordDisplay != null)
        {
            wordDisplay.fontSize = originalFontSize;
            wordDisplay.color = Color.black;
        }
    }

    public bool IsDragging()
    {
        return isDragging;
    }
    void Update()
    {
        // Skip if dragging
        if (isDragging || tooltipPanel == null || rectTransform == null)
            return;

        // Simple hover detection for tooltip
        Rect rect = rectTransform.rect;
        rect.position = rectTransform.position;

        if (rect.Contains(Input.mousePosition))
        {
            if (!tooltipPanel.activeSelf)
                tooltipPanel.SetActive(true);
        }
        else
        {
            if (tooltipPanel.activeSelf)
                tooltipPanel.SetActive(false);
        }
    }



    public void OnBeginDrag(PointerEventData eventData)
    {
        isDragging = true;
        originalAnchoredPosition = rectTransform.anchoredPosition;

        // IMPORTANT: Disable the falling script/component if you have one
        // If the word has a separate falling script, disable it here

        // Make word semi-transparent and highlight while dragging
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

        if (tooltipPanel != null)
            tooltipPanel.SetActive(false);

        Debug.Log($"Started dragging: {wordText}");
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (rectTransform == null) return;

        // Move the word
        rectTransform.position = Input.mousePosition;

        Debug.Log($"Dragging to: {rectTransform.position}");
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;

        // Check which bucket the mouse is over
        Bucket targetBucket = null;

        // Find all buckets and check if mouse is over them
        Bucket[] buckets = FindObjectsOfType<Bucket>();
        foreach (Bucket bucket in buckets)
        {
            if (bucket.IsMouseOverBucket())
            {
                targetBucket = bucket;
                Debug.Log($"Mouse over bucket: {bucket.name}");
                break;
            }
        }

        if (targetBucket != null)
        {
            // Dropped on a bucket
            Debug.Log($"Word {wordText} dropped on bucket {targetBucket.name}");
            OnSortedIntoBucket(targetBucket.isCorrectBucket);
        }
        else
        {
            // Not dropped on bucket - reset position
            rectTransform.anchoredPosition = originalAnchoredPosition;
            Debug.Log($"Word {wordText} dropped outside bucket - resetting position");
        }

        // Restore appearance
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
        }

        // Restore original color
        Image img = GetComponent<Image>();
        if (img != null)
        {
            img.color = originalColor;
        }

        // Restore text size
        if (wordDisplay != null)
        {
            wordDisplay.fontSize = originalFontSize;
            wordDisplay.color = Color.black;
        }
    }
    public void OnSortedIntoBucket(bool isBucketCorrect)
    {
        // Determine if the sorting was correct
        bool sortedCorrectly = false;
        int pointsToAdd = 0;
        string message = "";

        if (isCorrectSpelling && isBucketCorrect)
        {
            // Correct word in CORRECT bucket
            sortedCorrectly = true;
            pointsToAdd = 10;
            message = $"✓ Correct! '{wordText}' in correct bucket! +10 points";
        }
        else if (!isCorrectSpelling && !isBucketCorrect)
        {
            // Misspelled word in INCORRECT bucket - trigger spelling bonus
            sortedCorrectly = true;
            pointsToAdd = 10;
            message = $"✓ Misspelled word in incorrect bucket! +10 points + bonus opportunity!";

            // Trigger the spelling bonus popup
            if (GameManager.Instance != null)
            {
                Debug.Log("Triggering spelling correction bonus...");
                GameManager.Instance.StartSpellingCorrection(this);
                return; // Don't destroy the word yet - the bonus system will handle it
            }
        }
        else if (isCorrectSpelling && !isBucketCorrect)
        {
            // Correct word in INCORRECT bucket
            sortedCorrectly = false;
            pointsToAdd = -5;
            message = $"✗ '{wordText}' is correct but put in wrong bucket! -5 points";
        }
        else if (!isCorrectSpelling && isBucketCorrect)
        {
            // Misspelled word in CORRECT bucket
            sortedCorrectly = false;
            pointsToAdd = -5;
            message = $"✗ '{wordText}' is misspelled but put in correct bucket! -5 points";
        }

        // Add points
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddPoints(pointsToAdd);
        }

        Debug.Log(message);

        // Destroy the word (unless bonus was triggered)
        if (!(!isCorrectSpelling && !isBucketCorrect))
        {
            Destroy(gameObject);
        }
    }
}