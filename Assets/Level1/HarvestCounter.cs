using UnityEngine;
using TMPro;

public class HarvestCounter : MonoBehaviour
{
    public TextMeshProUGUI harvestedText;
    public TextMeshProUGUI moneyText;
    public GameObject levelCompleteButton;

    public int harvestGoal = 1;

    private bool levelCompleted;

    private void Start()
    {
        Debug.Log("HarvestCounter running! Goal = " + harvestGoal);

        if (levelCompleteButton != null)
        {
            levelCompleteButton.SetActive(false);
        }

        UpdateUI();
    }

    private void Update()
    {
        UpdateUI();

        if (FarmFund.Instance == null || levelCompleted)
        {
            Debug.LogWarning("FarmFund.Instance is NULL!");
            return;
        }

        if (FarmFund.Instance.HarvestedPlants >= harvestGoal)
        {
            levelCompleted = true;

            if (levelCompleteButton != null)
            {
                levelCompleteButton.SetActive(true);
            }

            Debug.Log("Scene 3 completed!");
        }
    }

    private void UpdateUI()
    {
        if (FarmFund.Instance == null)
            return;

        if (harvestedText != null)
        {
            harvestedText.text =
                "Harvested: " +
                FarmFund.Instance.HarvestedPlants +
                " / " +
                harvestGoal;
        }

        if (moneyText != null)
        {
            moneyText.text = FarmFund.Instance.Money.ToString();
        }
    }
}