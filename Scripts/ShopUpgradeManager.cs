using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using TMPro;

[DefaultExecutionOrder(100)]
public class ShopUpgradeManager : MonoBehaviour
{
    public enum ShopState { SeedShopOnly, Market }

    [Header("State (read only)")]
    public ShopState shopState = ShopState.SeedShopOnly;

    [Header("When does the Market open?")]
    [Tooltip("The Market opens when the player arrives from one of these scenes.")]
    public string[] openMarketWhenComingFrom = { "Scene3-Crop Sim" };
    [Tooltip("Testing only: always open the Market, even on the first visit.")]
    public bool alwaysOpenMarket = false;

    [Header("Prices")]
    public int sellPricePerPlant = 5;
    public int speedBootsPrice = 15;
    [Tooltip("Shown to the player - keep it the same as SpeedBootsUpgrade's multiplier on the Player")]
    public int speedBootsBonusPercent = 50;

    [Header("Market panel")]
    public GameObject marketPanel;
    public TMP_Text harvestedText;
    public TMP_Text sellPriceText;
    public Button sellOneButton;
    public Button sellAllButton;
    public TMP_Text upgradeInfoText;
    public Button buySpeedBootsButton;
    public TMP_Text buySpeedBootsLabel;
    public TMP_Text marketMessageText;

    [Header("Existing shop objects")]
    [Tooltip("The existing 'Instructions' text at the top of the shop")]
    public TMP_Text instructionsText;
    [Tooltip("The existing 'NextLvl' button (goes to Scene3 on the first visit)")]
    public Button nextLevelButton;
    public TMP_Text nextLevelButtonLabel;

    [Header("Level flow on the Market visit")]
    [Tooltip("Name of the 3rd farming level (Task 4). Must be added to Build Profiles.")]
    public string nextLevelScene = "Scene4-Farm Level 3";
    [Tooltip("Used if the scene above isn't in Build Profiles yet (so you can still test).")]
    public string fallbackScene = "Scene3-Crop Sim";

    [Header("Testing only")]
    public int testAddHarvestedPlants = 0;
    public int testAddMoney = 0;

    private FarmFund fund;

    void Start()
    {
        fund = FarmFund.Get();

        if (testAddHarvestedPlants > 0) fund.AddHarvestedPlants(testAddHarvestedPlants);
        if (testAddMoney > 0) fund.AddMoney(testAddMoney);

        // ---- decide the shop state ----
        string thisScene = SceneManager.GetActiveScene().name;
        string cameFrom = fund.GetSceneBefore(thisScene);
        shopState = ShopState.SeedShopOnly;
        if (alwaysOpenMarket) shopState = ShopState.Market;
        foreach (string sceneName in openMarketWhenComingFrom)
        {
            if (cameFrom == sceneName) shopState = ShopState.Market;
        }
        Debug.Log("ShopUpgradeManager: came from '" + cameFrom + "' -> shop state = " + shopState);

        // ---- wire market buttons ----
        if (sellOneButton != null) sellOneButton.onClick.AddListener(SellOne);
        if (sellAllButton != null) sellAllButton.onClick.AddListener(SellAll);
        if (buySpeedBootsButton != null) buySpeedBootsButton.onClick.AddListener(BuySpeedBoots);

        fund.fundChangedEvent.AddListener(RefreshMarketUI);

        EnterState(shopState);
    }

    private void EnterState(ShopState state)
    {
        switch (state)
        {
            case ShopState.SeedShopOnly:
                if (marketPanel != null) marketPanel.SetActive(false);
                // nothing else changes - the original shop works exactly like before
                break;

            case ShopState.Market:
                if (marketPanel != null) marketPanel.SetActive(true);
                if (instructionsText != null)
                    instructionsText.text = "Market open! Sell crops & buy upgrades";
                if (marketMessageText != null)
                    marketMessageText.text = "You harvested " + fund.harvestedPlants + " plants. Sell them for cash!";

                // reuse the existing NextLvl button, but send the player to the 3rd level instead
                if (nextLevelButton != null)
                {
                    nextLevelButton.gameObject.SetActive(true);
                    for (int i = 0; i < nextLevelButton.onClick.GetPersistentEventCount(); i++)
                        nextLevelButton.onClick.SetPersistentListenerState(i, UnityEventCallState.Off);
                    nextLevelButton.onClick.AddListener(GoToNextLevel);
                }
                if (nextLevelButtonLabel != null) nextLevelButtonLabel.text = "Go to Level 3";

                // SeedsMoneyScene2 shows NextLvl after the first seed purchase - not needed any more
                SeedsMoneyScene2 seedShop = FindAnyObjectByType<SeedsMoneyScene2>();
                if (seedShop != null) seedShop.firstPurchaseButton = null;

                RefreshMarketUI();
                break;
        }
    }

    // ---------------- button actions ----------------
    public void SellOne()
    {
        int sold = fund.SellHarvestedPlants(1, sellPricePerPlant);
        ShowMarketMessage(sold > 0 ? "Sold 1 plant for $" + sellPricePerPlant + "!" : "You have no harvested plants to sell.");
    }

    public void SellAll()
    {
        int sold = fund.SellHarvestedPlants(fund.harvestedPlants, sellPricePerPlant);
        ShowMarketMessage(sold > 0 ? "Sold " + sold + " plants for $" + (sold * sellPricePerPlant) + "!" : "You have no harvested plants to sell.");
    }

    public void BuySpeedBoots()
    {
        if (fund.hasSpeedBoots)
        {
            ShowMarketMessage("You already own the Speed Boots.");
            return;
        }
        if (fund.TryBuySpeedBoots(speedBootsPrice))
            ShowMarketMessage("Speed Boots bought! You'll walk " + speedBootsBonusPercent + "% faster in the next level.");
        else
            ShowMarketMessage("Not enough money - you need $" + (speedBootsPrice - fund.money) + " more. Sell some crops first!");
    }

    public void GoToNextLevel()
    {
        if (Application.CanStreamedLevelBeLoaded(nextLevelScene))
        {
            SceneManager.LoadScene(nextLevelScene);
        }
        else
        {
            Debug.LogWarning("ShopUpgradeManager: scene '" + nextLevelScene + "' is not in Build Profiles yet - loading '" + fallbackScene + "' instead.");
            SceneManager.LoadScene(fallbackScene);
        }
    }

    // ---------------- GUI ----------------
    private void RefreshMarketUI()
    {
        if (shopState != ShopState.Market || fund == null) return;

        if (harvestedText != null) harvestedText.text = "Harvested plants: " + fund.harvestedPlants;
        if (sellPriceText != null) sellPriceText.text = "Sell price: $" + sellPricePerPlant + " each";
        if (sellOneButton != null) sellOneButton.interactable = fund.harvestedPlants > 0;
        if (sellAllButton != null) sellAllButton.interactable = fund.harvestedPlants > 0;

        if (upgradeInfoText != null)
            upgradeInfoText.text = "Speed Boots - walk " + speedBootsBonusPercent + "% faster in Level 3";
        if (buySpeedBootsButton != null)
            buySpeedBootsButton.interactable = !fund.hasSpeedBoots;
        if (buySpeedBootsLabel != null)
            buySpeedBootsLabel.text = fund.hasSpeedBoots ? "Speed Boots: OWNED" : "Buy Speed Boots ($" + speedBootsPrice + ")";
    }

    private void ShowMarketMessage(string message)
    {
        Debug.Log("Market: " + message);
        if (marketMessageText != null) marketMessageText.text = message;
    }

    private void OnDestroy()
    {
        if (fund != null) fund.fundChangedEvent.RemoveListener(RefreshMarketUI);
    }
}
