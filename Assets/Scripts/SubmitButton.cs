using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class SubmitButton : MonoBehaviour, IPointerClickHandler
{
    void Start()
    {
        Debug.Log($"SubmitButton started on {name}");
        Button btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.interactable = true;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("Submit button clicked - calling Submit");
        Submit();
    }

    public void OnClick()
    {
        Debug.Log("Submit button OnClick - calling Submit");
        Submit();
    }

    void Submit()
    {
        Debug.Log("Submit method called - checking GameManager.Instance");

        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager.Instance is NULL!");
            return;
        }

        Debug.Log("GameManager.Instance found, calling SubmitSpellingCorrection");
        //GameManager.Instance.SubmitSpellingCorrection();
    }
}