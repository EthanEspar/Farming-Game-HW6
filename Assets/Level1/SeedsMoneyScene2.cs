using UnityEngine;
using TMPro;
using UnityEngine.UI;

// Simple manager for seeds and money display in Scene2.
// - seeds start at 0, money starts at 10
// - calling BuySeed() increments seeds and decrements money
// - on the first successful purchase an extra button is shown
// - when money is 0 the buy button becomes non-interactable and purchases are blocked
public class SeedsMoneyScene2 : MonoBehaviour
{
    [Tooltip("TextMeshProUGUI that displays the current seeds count")]
    public TextMeshProUGUI seedsText;

    [Tooltip("TextMeshProUGUI that displays the current money count")]
    public TextMeshProUGUI moneyText;

    [Tooltip("The UI Button the player presses to buy a seed")]
    public Button buyButton;

    [Tooltip("Optional: another button (or UI GameObject) that appears after the first purchase")]
    public GameObject firstPurchaseButton;

    [Tooltip("Starting number of seeds")]
    public int startingSeeds = 0;
    [Tooltip("Starting money")]
    public int startingMoney = 10;
    [Tooltip("Cost in money for one seed purchase")]
    public int seedCost = 2;

    private int seeds;
    private int money;
    private bool firstPurchaseDone = false;

    void Start()
    {
        seeds = Mathf.Max(0, startingSeeds);
        money = Mathf.Max(0, startingMoney);

        if (firstPurchaseButton != null)
            firstPurchaseButton.SetActive(false);

        UpdateUI();
        UpdateBuyButtonState();

        // auto-hook the buy button if it's assigned but not wired in the Inspector
        if (buyButton != null)
        {
            // ensure our method is subscribed (avoid duplicate subscriptions)
            buyButton.onClick.RemoveListener(BuySeed);
            buyButton.onClick.AddListener(BuySeed);
        }
    }

    // Called by the buy button OnClick
    public void BuySeed()
    {
        Debug.Log("SeedsMoneyScene2: BuySeed called. Money=" + money + " Seeds=" + seeds);
        // ensure there is enough money to buy
        if (money < seedCost)
        {
            Debug.Log("SeedsMoneyScene2: Not enough money to buy a seed.");
            UpdateBuyButtonState();
            return;
        }

        // perform transaction
        seeds += 1;
        money -= seedCost;

        // on first purchase, reveal the other button
        if (!firstPurchaseDone && firstPurchaseButton != null)
        {
            firstPurchaseButton.SetActive(true);
            firstPurchaseDone = true;
        }

        UpdateUI();
        UpdateBuyButtonState();
    }

    private void UpdateUI()
    {
        if (seedsText != null)
            seedsText.text = seeds.ToString();
        if (moneyText != null)
            moneyText.text = money.ToString();
    }

    private void UpdateBuyButtonState()
    {
        if (buyButton != null)
            buyButton.interactable = (money >= seedCost);
    }

    // Optional: allow other systems to add money or seeds
    public void AddMoney(int amount)
    {
        money = Mathf.Max(0, money + amount);
        UpdateUI();
        UpdateBuyButtonState();
    }

    public void AddSeeds(int amount)
    {
        seeds = Mathf.Max(0, seeds + amount);
        UpdateUI();
    }
}
