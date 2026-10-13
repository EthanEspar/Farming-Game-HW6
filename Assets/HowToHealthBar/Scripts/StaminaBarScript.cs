using UnityEngine;

// Controls a stamina value in range [0,1] and updates this GameObject's X scale to reflect it.
public class StaminaBarScript : MonoBehaviour
{
    [Tooltip("Current stamina (0..1)")]
    [Range(0f, 1f)]
    public float currentStamina = 1f;

    [Tooltip("Drain per second while moving (0..1)")]
    public float moveDrainPerSecond = 0.01f;

    [Tooltip("Amount drained when watering a crop (instant)")]
    public float waterDrainAmount = 0.5f;

    [Tooltip("Regen per second when idle (0..1)")]
    public float regenPerSecond = 0.02f;

    // small epsilon to consider as moving
    private const float moveEpsilon = 0.001f;

    void Start()
    {
        currentStamina = Mathf.Clamp01(currentStamina);
        UpdateBarScale();
    }

    void Update()
    {
        // nothing here; external callers should call DrainWhileMoving/Regenerate as appropriate
        UpdateBarScale();
    }

    // Called each frame while the player is moving
    public void DrainWhileMoving()
    {
        if (currentStamina <= 0f) return;
        currentStamina -= moveDrainPerSecond * Time.deltaTime;
        currentStamina = Mathf.Clamp01(currentStamina);
    }

    // Called each frame while the player is idle
    public void RegenerateWhileIdle()
    {
        if (currentStamina >= 1f) return;
        currentStamina += regenPerSecond * Time.deltaTime;
        currentStamina = Mathf.Clamp01(currentStamina);
    }

    // Try to consume a fixed amount (for watering). Returns true if consumption succeeded.
    // Try to consume a fixed amount (for watering). Returns true if full amount was consumed.
    public bool TryConsume(float amount)
    {
        if (amount <= 0f) return true;
        // require full amount available
        if (currentStamina + 1e-6f >= amount)
        {
            currentStamina -= amount;
            currentStamina = Mathf.Clamp01(currentStamina);
            return true;
        }
        return false;
    }

    private void UpdateBarScale()
    {
        Vector3 s = transform.localScale;
        s.x = Mathf.Clamp01(currentStamina);
        transform.localScale = s;
    }
}
