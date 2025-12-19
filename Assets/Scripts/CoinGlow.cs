using UnityEngine;

/// <summary>
/// Makes coin glow/pulse to be more visible
/// </summary>
public class CoinGlow : MonoBehaviour
{
    [Header("Glow Settings")]
    [Tooltip("Pulsing speed")]
    public float pulseSpeed = 2f;
    
    [Tooltip("Min scale")]
    public float minScale = 0.9f;
    
    [Tooltip("Max scale")]
    public float maxScale = 1.1f;
    
    private Vector3 originalScale;
    private float pulseTimer = 0f;
    
    void Start()
    {
        originalScale = transform.localScale;
        pulseTimer = Random.Range(0f, Mathf.PI * 2f); // Random start
    }
    
    void Update()
    {
        // Pulse effect
        pulseTimer += Time.deltaTime * pulseSpeed;
        float scale = Mathf.Lerp(minScale, maxScale, (Mathf.Sin(pulseTimer) + 1f) / 2f);
        transform.localScale = originalScale * scale;
    }
}
