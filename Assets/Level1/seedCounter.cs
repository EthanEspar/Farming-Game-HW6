using UnityEngine;
using TMPro;

// Simple seed counter. Starts with 5 seeds and updates a TextMeshProUGUI field.
public class seedCounter : MonoBehaviour
{
    [Tooltip("TextMeshProUGUI that displays the seed count")]
    public TextMeshProUGUI seedText;

    [Tooltip("Starting number of seeds")]
    public int startingSeeds = 5;

    private int currentSeeds;

    void Start()
    {
        currentSeeds = startingSeeds;
        UpdateSeedUI();
    }

    // Try to use one seed. Returns true if a seed was used and decremented; false if none available.
    public bool TryUseSeed()
    {
        if (currentSeeds <= 0)
            return false;
        currentSeeds = Mathf.Max(0, currentSeeds - 1);
        UpdateSeedUI();
        return true;
    }

    // Add seeds (positive or negative). Updates UI.
    public void AddSeeds(int amount)
    {
        currentSeeds = Mathf.Max(0, currentSeeds + amount);
        UpdateSeedUI();
    }

    private void UpdateSeedUI()
    {
        if (seedText != null)
            seedText.text = currentSeeds.ToString();
        else
            Debug.Log("seedCounter: seedText not assigned. Seeds=" + currentSeeds);
    }
}
