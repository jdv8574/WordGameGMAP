using UnityEngine;
using UnityEngine.EventSystems;

public class Bucket : MonoBehaviour
{
    public bool isCorrectBucket;
    private RectTransform rectTransform;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        Debug.Log($"Bucket {name} initialized - isCorrectBucket: {isCorrectBucket}");
    }

    public bool IsMouseOverBucket()
    {
        if (rectTransform == null) return false;

        // Check if mouse position is over this bucket
        Vector2 mousePos = Input.mousePosition;
        return RectTransformUtility.RectangleContainsScreenPoint(rectTransform, mousePos);
    }
}