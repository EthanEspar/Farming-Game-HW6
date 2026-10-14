using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class SeedsMoneyScene2 : MonoBehaviour
{
    public TextMeshProUGUI seedsText;
    public TextMeshProUGUI moneyText;
    public Button buyButton;
    public GameObject firstPurchaseButton;
    public TMP_InputField quantityInput;
    public TextMeshProUGUI priceText;

    public Button sellButton;
    public TextMeshProUGUI harvestedText;

    public int seedCost = 2;
    public int cropSellPrice = 5;

    private void Start()
    {
        if (FarmFund.Instance == null)
        {
            Debug.LogError(
                "Farm Fund is missing. Start from Scene 1."
            );
            return;
        }

        if (buyButton != null)
        {
            buyButton.onClick.RemoveListener(BuySeed);
            buyButton.onClick.AddListener(BuySeed);
        }

        if (sellButton != null)
        {
            sellButton.onClick.RemoveListener(SellHarvestedPlants);
            sellButton.onClick.AddListener(SellHarvestedPlants);
        }

        if (quantityInput != null &&
            string.IsNullOrWhiteSpace(quantityInput.text))
        {
            quantityInput.text = "1";
        }

        if (firstPurchaseButton != null)
        {
            firstPurchaseButton.SetActive(
                FarmFund.Instance.Seeds > 0
            );
        }

        UpdateUI();
    }

    public void BuySeed()
    {
        if (FarmFund.Instance == null)
            return;

        if (quantityInput == null)
        {
            Debug.LogError("Quantity Input is not assigned.");
            return;
        }

        int quantity;

        if (!int.TryParse(quantityInput.text, out quantity) ||
            quantity < 1)
        {
            Debug.Log("Enter a valid seed quantity.");
            return;
        }

        int totalPrice = quantity * seedCost;

        if (FarmFund.Instance.TrySpendMoney(totalPrice))
        {
            FarmFund.Instance.AddSeeds(quantity);

            if (firstPurchaseButton != null)
            {
                firstPurchaseButton.SetActive(true);
            }

            Debug.Log(
                "Bought " + quantity +
                " seeds for $" + totalPrice
            );
        }
        else
        {
            Debug.Log("Not enough money.");
        }

        UpdateUI();
    }

    public void SellHarvestedPlants()
    {
        if (FarmFund.Instance == null)
            return;

        int amountSold = FarmFund.Instance.HarvestedPlants;

        if (amountSold <= 0)
        {
            Debug.Log("There are no harvested plants to sell.");
            return;
        }

        FarmFund.Instance.SellHarvestedPlants(cropSellPrice);

        Debug.Log(
            "Sold " + amountSold +
            " plants for $" +
            (amountSold * cropSellPrice)
        );

        UpdateUI();
    }

    private void UpdateUI()
    {
        if (FarmFund.Instance == null)
            return;

        if (seedsText != null)
        {
            seedsText.text = FarmFund.Instance.Seeds.ToString();
        }

        if (moneyText != null)
        {
            moneyText.text = FarmFund.Instance.Money.ToString();
        }

        if (priceText != null)
        {
            priceText.text = "Seeds: $" + seedCost + " each";
        }

        if (harvestedText != null)
        {
            harvestedText.text =
                "Harvested: " +
                FarmFund.Instance.HarvestedPlants;
        }

        if (buyButton != null)
        {
            buyButton.interactable =
                FarmFund.Instance.Money >= seedCost;
        }

        if (sellButton != null)
        {
            sellButton.gameObject.SetActive(
                FarmFund.Instance.HarvestedPlants > 0
            );
        }
    }
}