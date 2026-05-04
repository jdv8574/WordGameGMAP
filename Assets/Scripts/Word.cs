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

        // DON'T reset position immediately - let the bucket detection handle it
        // rectTransform.anchoredPosition = originalAnchoredPosition; // COMMENT THIS OUT

        Debug.Log($"Stopped dragging: {wordText}");
    }
    public void OnSortedIntoBucket(bool isBucketCorrect)
    {
        bool sortedCorrectly = (isBucketCorrect == isCorrectSpelling);

        Debug.Log($"Word '{wordText}' - Sorted correctly? {sortedCorrectly}");

        if (sortedCorrectly)
        {
            // Correct word in correct bucket OR incorrect word in incorrect bucket
            if (ScoreManager.Instance != null)
                ScoreManager.Instance.AddPoints(10);
            Debug.Log($"✓ Correct! +10 points. Word: {wordText}");
            Destroy(gameObject);
        }
        else if (!isCorrectSpelling && !isBucketCorrect)
        {
            // Misspelled word in incorrect bucket - trigger spelling bonus
            Debug.Log($"✗ Misspelled word in incorrect bucket! Triggering spelling bonus for: {wordText}");
            if (GameManager.Instance != null)
                GameManager.Instance.StartSpellingCorrection(this);
            else
                Destroy(gameObject);
        }
        else
        {
            // Wrong bucket
            if (ScoreManager.Instance != null)
                ScoreManager.Instance.AddPoints(-5);
            Debug.Log($"✗ Wrong bucket! -5 points. Word: {wordText}");
            Destroy(gameObject);
        }
    }
}