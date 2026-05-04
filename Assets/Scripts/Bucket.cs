using UnityEngine;

public class Bucket : MonoBehaviour
{
    public bool isCorrectBucket; // Check in Inspector

    void Start()
    {
        // Optional: Add a collider for detection
        if (GetComponent<Collider2D>() == null)
        {
            BoxCollider2D col = gameObject.AddComponent<BoxCollider2D>();
            col.isTrigger = true; // Important for detection
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("FallingWord"))
        {
            Word wordScript = other.GetComponent<Word>();
            if (wordScript != null)
            {
                wordScript.OnSortedIntoBucket(isCorrectBucket);
            }
        }
    }
}