using UnityEngine;

/// <summary>
/// Minimal stamina implementation so PlayerMovementScript can compile and run.
/// Replace with your real stamina system when available.
/// </summary>
public class PlayerStamina : MonoBehaviour
{
    public float maxStamina = 100f;
    public float currentStamina = 100f;
    public float sprintCostPerSecond = 15f;
    public float regenPerSecond = 10f;

    public bool CanSprint()
    {
        return currentStamina > 0.1f;
    }

    void Update()
    {
        // Very simple drain when holding LeftShift; replace with game logic later
        if (Input.GetKey(KeyCode.LeftShift) && CanSprint())
        {
            currentStamina = Mathf.Max(0f, currentStamina - sprintCostPerSecond * Time.deltaTime);
        }
        else
        {
            currentStamina = Mathf.Min(maxStamina, currentStamina + regenPerSecond * Time.deltaTime);
        }
    }
}
