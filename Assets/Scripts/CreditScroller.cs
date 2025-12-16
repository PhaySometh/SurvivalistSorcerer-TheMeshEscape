using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class CreditScroller : MonoBehaviour
{
    public float scrollSpeed = 50f; // Adjust this speed
    public RectTransform contentTransform;
    public float waitAtEnd = 3f; // Seconds to wait at end
    
    private float contentHeight;
    private float startPosition;
    
    void Start()
    {
        if (contentTransform == null)
            contentTransform = GetComponent<RectTransform>();
            
        contentHeight = contentTransform.rect.height;
        startPosition = contentTransform.anchoredPosition.y;
    }
    
    void Update()
    {
        // Move content upward
        float newY = contentTransform.anchoredPosition.y + 
                    (scrollSpeed * Time.deltaTime);
        contentTransform.anchoredPosition = new Vector2(0, newY);
        
        // Check if we've scrolled past all content
        if (contentTransform.anchoredPosition.y > contentHeight + 500)
        {
            // Wait a bit then return to menu
            Invoke("ReturnToMenu", waitAtEnd);
        }
        
        // Allow skipping with Escape or Space
        if (Input.GetKeyDown(KeyCode.Escape) || 
            Input.GetKeyDown(KeyCode.Space) ||
            Input.GetMouseButtonDown(0))
        {
            ReturnToMenu();
        }
    }
    
    void ReturnToMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
}