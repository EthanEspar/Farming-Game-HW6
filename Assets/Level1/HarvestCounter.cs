using UnityEngine;
using TMPro;

public class HarvestCounter : MonoBehaviour
{
    public TextMeshProUGUI harvestedText;
    public TextMeshProUGUI moneyText;
    public GameObject levelCompleteButton;

    public int harvestGoal = 1;

    private bool levelCompleted;
    private int harvestedAtSceneStart;
    private FarmFund fund;

    private void Start()
    {
        Debug.Log("HarvestCounter running! Goal = " + harvestGoal);

        if (levelCompleteButton != null)
            levelCompleteButton.SetActive(false);

        fund = FarmFund.Instance;
        if (fund == null)
        {
            Debug.LogWarning("HarvestCounter: Farm Fund is missing.");
            return;
        }

        // Persistent crops stay in the Bag, but every level must be won by
        // harvesting its own goal instead of reusing crops from an old level.
        harvestedAtSceneStart = fund.HarvestedPlants;
        fund.InventoryChanged += UpdateProgress;
        UpdateProgress();
    }

    private void UpdateProgress()
    {
        if (fund == null) return;

        int harvestedThisLevel = Mathf.Max(
            0,
            fund.HarvestedPlants - harvestedAtSceneStart
        );

        UpdateUI(harvestedThisLevel);

        if (!levelCompleted && harvestedThisLevel >= harvestGoal)
        {
            levelCompleted = true;

            if (levelCompleteButton != null)
            {
                levelCompleteButton.SetActive(true);
            }

            Debug.Log("Scene 3 completed!");
        }
    }

    private void UpdateUI(int harvestedThisLevel)
    {
        if (harvestedText != null)
        {
            harvestedText.text =
                "Harvested this level: " +
                harvestedThisLevel +
                " / " +
                harvestGoal;
        }

        if (moneyText != null)
        {
            moneyText.text = fund.Money.ToString();
        }
    }

    private void OnDestroy()
    {
        if (fund != null)
            fund.InventoryChanged -= UpdateProgress;
    }
}
