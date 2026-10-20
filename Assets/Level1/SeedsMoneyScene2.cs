using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Reusable seed-buying and crop-selling widget. Scene flow is intentionally
/// handled by ShopUpgradeManager so this component can be reused in any shop.
/// </summary>
public class SeedsMoneyScene2 : MonoBehaviour
{
    public TextMeshProUGUI seedsText;
    public TextMeshProUGUI moneyText;
    public Button buyButton;
    public TMP_InputField quantityInput;
    public TextMeshProUGUI priceText;

    public Button sellButton;
    public TextMeshProUGUI harvestedText;

    public int seedCost = 2;
    public int cropSellPrice = 5;

    public event Action ShopChanged;

    private void Start()
    {
        if (FarmFund.Instance == null)
        {
            Debug.LogError("SeedsMoneyScene2: Farm Fund is missing. Start from Scene 1.");
            return;
        }

        FarmFund.Instance.InventoryChanged -= OnPersistentInventoryChanged;
        FarmFund.Instance.InventoryChanged += OnPersistentInventoryChanged;

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

        if (quantityInput != null && string.IsNullOrWhiteSpace(quantityInput.text))
            quantityInput.text = "1";

        RefreshUI();
    }

    public void BuySeed()
    {
        if (FarmFund.Instance == null) return;

        if (!TryGetRequestedQuantity(out int quantity))
            return;

        int totalPrice = quantity * seedCost;
        if (FarmFund.Instance.TrySpendMoney(totalPrice))
        {
            FarmFund.Instance.AddSeeds(quantity);
            Debug.Log("Bought " + quantity + " seeds for $" + totalPrice + ".");
        }
        else
        {
            Debug.Log("SeedsMoneyScene2: not enough money.");
        }

        RefreshUI();
    }

    /// <summary>
    /// Lets every shop offer use the same quantity input and validation.
    /// </summary>
    public bool TryGetRequestedQuantity(out int quantity)
    {
        quantity = 0;

        if (quantityInput == null)
        {
            Debug.LogError("SeedsMoneyScene2: Quantity Input is not assigned.");
            return false;
        }

        if (!int.TryParse(quantityInput.text, out quantity) || quantity < 1)
        {
            Debug.Log("SeedsMoneyScene2: enter a valid seed quantity.");
            return false;
        }

        return true;
    }

    public void SellHarvestedPlants()
    {
        if (FarmFund.Instance == null) return;

        int amountSold = FarmFund.Instance.HarvestedPlants;
        if (amountSold <= 0)
        {
            Debug.Log("SeedsMoneyScene2: there are no harvested plants to sell.");
            return;
        }

        FarmFund.Instance.SellHarvestedPlants(cropSellPrice);
        Debug.Log(
            "Sold " + amountSold + " plants for $" +
            (amountSold * cropSellPrice) + "."
        );

        RefreshUI();
    }

    public void RefreshUI()
    {
        if (FarmFund.Instance == null) return;

        if (seedsText != null)
            seedsText.text = FarmFund.Instance.Seeds.ToString();

        if (moneyText != null)
            moneyText.text = FarmFund.Instance.Money.ToString();

        if (priceText != null)
            priceText.text = "Seeds: $" + seedCost + " each";

        if (harvestedText != null)
            harvestedText.text = "Harvested: " + FarmFund.Instance.HarvestedPlants;

        if (buyButton != null)
            buyButton.interactable = FarmFund.Instance.Money >= seedCost;

        if (sellButton != null)
            sellButton.gameObject.SetActive(FarmFund.Instance.HarvestedPlants > 0);

        ShopChanged?.Invoke();
    }

    private void OnPersistentInventoryChanged()
    {
        RefreshUI();
    }

    private void OnDestroy()
    {
        if (FarmFund.Instance != null)
            FarmFund.Instance.InventoryChanged -= OnPersistentInventoryChanged;
    }
}
