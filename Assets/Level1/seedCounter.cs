using UnityEngine;
using TMPro;

public class seedCounter : MonoBehaviour
{
    public TextMeshProUGUI seedText;

    private void Start()
    {
        UpdateSeedUI();
    }

    public bool TryUseSeed()
    {
        if (FarmFund.Instance == null)
        {
            Debug.LogError("Farm Fund is missing.");
            return false;
        }

        bool usedSeed = FarmFund.Instance.TryUseSeed();
        UpdateSeedUI();

        return usedSeed;
    }

    public void AddSeeds(int amount)
    {
        if (FarmFund.Instance == null)
            return;

        FarmFund.Instance.AddSeeds(amount);
        UpdateSeedUI();
    }

    private void UpdateSeedUI()
    {
        if (seedText != null && FarmFund.Instance != null)
        {
            seedText.text = FarmFund.Instance.Seeds.ToString();
        }
    }
}