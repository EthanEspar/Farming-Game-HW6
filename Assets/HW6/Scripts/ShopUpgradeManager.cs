using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DefaultExecutionOrder(100)]
public class ShopUpgradeManager : MonoBehaviour
{
    public enum ShopState { SeedShopOnly, Market }

    [Header("Reusable shop configuration")]
    [SerializeField] private ShopSceneConfig config;

    [Header("State (read only)")]
    public ShopState shopState = ShopState.SeedShopOnly;

    [Header("Upgrade")]
    public int speedBootsPrice = 15;
    [Tooltip("Keep this value in sync with SpeedBootsUpgrade on the Player.")]
    public int speedBootsBonusPercent = 50;
    public int bagPrice = 20;
    public int secondPlantSeedPrice = 3;

    [Header("Optional market panel")]
    public GameObject marketPanel;
    public TMP_Text upgradeInfoText;
    public Button buySpeedBootsButton;
    public TMP_Text buySpeedBootsLabel;
    public TMP_Text marketMessageText;

    [Header("Shared shop UI")]
    public TMP_Text instructionsText;
    public Button nextLevelButton;
    public TMP_Text nextLevelButtonLabel;

    [Header("Testing only - set back to 0 before recording")]
    public int testAddMoney = 0;
    public bool showInventoryTestingCanvas = false;

    private FarmFund fund;
    private SeedsMoneyScene2 seedShop;
    private ShopSceneConfig.Visit activeVisit;
    private Button bagButton;
    private TMP_Text bagButtonLabel;
    private Button secondSeedButton;
    private TMP_Text secondSeedButtonLabel;
    private InventoryManager inventoryUI;

    private void Start()
    {
        fund = FarmFund.Instance;
        if (fund == null)
        {
            Debug.LogError("ShopUpgradeManager: Farm Fund is missing. Start from Scene 1 or the intro scene.");
            if (marketPanel != null) marketPanel.SetActive(false);
            return;
        }

        if (config == null)
        {
            Debug.LogError("ShopUpgradeManager: assign a ShopSceneConfig asset.");
            if (marketPanel != null) marketPanel.SetActive(false);
            return;
        }

        seedShop = FindAnyObjectByType<SeedsMoneyScene2>();
        if (seedShop != null) seedShop.ShopChanged += OnShopChanged;

        int debugMoneyGrant = testAddMoney;
        if (showInventoryTestingCanvas && debugMoneyGrant <= 0)
            debugMoneyGrant = 100;

        if (debugMoneyGrant > 0)
        {
            fund.AddMoney(debugMoneyGrant);
            Debug.Log(
                "ShopUpgradeManager TEST: added $" + debugMoneyGrant +
                ". Runtime total is $" + fund.Money + "."
            );
        }

        string activeScene = SceneManager.GetActiveScene().name;
        string cameFrom = SceneHistory.GetSceneBefore(activeScene);
        activeVisit = config.ResolveVisit(cameFrom);

        if (activeVisit == null)
        {
            Debug.LogError("ShopUpgradeManager: the shop configuration has no matching or fallback visit.");
            return;
        }

        shopState = activeVisit.showMarketPanel
            ? ShopState.Market
            : ShopState.SeedShopOnly;

        Debug.Log(
            "ShopUpgradeManager: entered from '" + cameFrom +
            "' using route '" + activeVisit.displayName + "'."
        );

        BindButtons();
        ApplyVisit();

        // Refresh the original shop labels after the optional test grant.
        // RefreshUI also invokes ShopChanged, which refreshes route controls.
        if (seedShop != null)
            seedShop.RefreshUI();
        else
            RefreshAllUI();
    }

    private void BindButtons()
    {
        CreateSpecialOfferButtons();

        if (buySpeedBootsButton != null)
        {
            buySpeedBootsButton.onClick.RemoveListener(BuySpeedBoots);
            buySpeedBootsButton.onClick.AddListener(BuySpeedBoots);
        }

        if (nextLevelButton == null) return;

        // The original scene has a persistent destination. Disable it so every
        // visit can use the destination declared in ShopSceneConfig instead.
        for (int i = 0; i < nextLevelButton.onClick.GetPersistentEventCount(); i++)
            nextLevelButton.onClick.SetPersistentListenerState(i, UnityEventCallState.Off);

        nextLevelButton.onClick.RemoveListener(GoToNextLevel);
        nextLevelButton.onClick.AddListener(GoToNextLevel);
    }

    private void CreateSpecialOfferButtons()
    {
        if (buySpeedBootsButton == null)
            return;

        if (activeVisit.showBagOffer)
            bagButton = CreateOfferButton(
                "Buy Bag Button",
                BuyBag,
                out bagButtonLabel
            );

        if (activeVisit.showSecondSeedOffer)
            secondSeedButton = CreateOfferButton(
                "Buy Corn Seeds Button",
                BuySecondPlantSeeds,
                out secondSeedButtonLabel
            );
    }

    private Button CreateOfferButton(
        string objectName,
        UnityAction action,
        out TMP_Text label
    )
    {
        GameObject buttonObject = Instantiate(
            buySpeedBootsButton.gameObject,
            buySpeedBootsButton.transform.parent
        );
        buttonObject.name = objectName;

        Button button = buttonObject.GetComponent<Button>();
        label = buttonObject.GetComponentInChildren<TMP_Text>(true);
        if (button == null) return null;

        for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
            button.onClick.SetPersistentListenerState(i, UnityEventCallState.Off);

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
        return button;
    }

    private void ApplyVisit()
    {
        if (marketPanel != null)
            marketPanel.SetActive(activeVisit.showMarketPanel);

        if (buySpeedBootsButton != null)
            buySpeedBootsButton.gameObject.SetActive(activeVisit.showSpeedBootsOffer);

        if (bagButton != null)
            bagButton.gameObject.SetActive(activeVisit.showBagOffer);

        if (secondSeedButton != null)
            secondSeedButton.gameObject.SetActive(activeVisit.showSecondSeedOffer);

        if (instructionsText != null && !string.IsNullOrWhiteSpace(activeVisit.instructions))
            instructionsText.text = activeVisit.instructions;

        if (marketMessageText != null)
            marketMessageText.text = activeVisit.announcement;

        if (nextLevelButton != null)
            nextLevelButton.gameObject.SetActive(true);

        if (nextLevelButtonLabel != null)
            nextLevelButtonLabel.text = activeVisit.continueButtonLabel;
    }

    public void BuySpeedBoots()
    {
        if (fund == null) return;

        if (fund.HasSpeedUpgrade)
        {
            ShowMarketMessage("You already own the Speed Boots.");
            return;
        }

        if (fund.BuySpeedUpgrade(speedBootsPrice))
        {
            ShowMarketMessage(
                "Speed Boots bought! You'll walk " +
                speedBootsBonusPercent + "% faster in the next level."
            );
        }
        else
        {
            ShowMarketMessage(
                "Not enough money - you need $" +
                (speedBootsPrice - fund.Money) + " more."
            );
        }

        if (seedShop != null)
            seedShop.RefreshUI();
        else
            RefreshAllUI();
    }

    public void BuyBag()
    {
        if (fund == null) return;

        if (fund.HasBag)
        {
            ShowMarketMessage("You already own the Bag.");
            return;
        }

        if (fund.BuyBag(bagPrice))
            ShowMarketMessage("Bag purchased! The inventory level is now unlocked.");
        else
            ShowMarketMessage("Not enough money - the Bag costs $" + bagPrice + ".");

        if (seedShop != null)
            seedShop.RefreshUI();
        else
            RefreshAllUI();
    }

    public void BuySecondPlantSeeds()
    {
        if (fund == null || seedShop == null) return;

        if (!seedShop.TryGetRequestedQuantity(out int quantity))
        {
            ShowMarketMessage("Enter a valid corn seed quantity first.");
            return;
        }

        int totalPrice = quantity * secondPlantSeedPrice;
        if (fund.TrySpendMoney(totalPrice))
        {
            fund.AddSecondPlantSeeds(quantity);
            ShowMarketMessage(
                "Bought " + quantity + " corn seeds for $" + totalPrice + "."
            );
        }
        else
        {
            ShowMarketMessage(
                "Not enough money - " + quantity + " corn seeds cost $" +
                totalPrice + "."
            );
        }

        seedShop.RefreshUI();
    }

    public void GoToNextLevel()
    {
        if (activeVisit == null || fund == null) return;

        if (activeVisit.requireSeedsToContinue && fund.Seeds <= 0)
        {
            ShowMarketMessage("Buy at least one seed before continuing.");
            RefreshContinueButton();
            return;
        }

        if (activeVisit.requireBagToContinue && !fund.HasBag)
        {
            ShowMarketMessage("Buy the Bag before continuing to the inventory level.");
            RefreshContinueButton();
            return;
        }

        if (fund.SecondPlantSeeds < activeVisit.minimumSecondSeedsToContinue)
        {
            ShowMarketMessage(
                "Buy at least " + activeVisit.minimumSecondSeedsToContinue +
                " corn seeds so you can finish the next level."
            );
            RefreshContinueButton();
            return;
        }

        string destination = activeVisit.nextSceneName;
        if (string.IsNullOrWhiteSpace(destination))
        {
            ShowMarketMessage("This shop route does not have a next scene yet.");
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(destination))
        {
            ShowMarketMessage(
                "Scene '" + destination + "' must be enabled in Build Profiles."
            );
            return;
        }

        SceneManager.LoadScene(destination);
    }

    private void OnShopChanged()
    {
        RefreshAllUI();
    }

    private void RefreshAllUI()
    {
        RefreshUpgradeUI();
        RefreshBagUI();
        RefreshSecondSeedUI();
        EnsureInventoryUI();
        RefreshContinueButton();
    }

    private void RefreshUpgradeUI()
    {
        if (fund == null) return;

        if (upgradeInfoText != null)
        {
            if (activeVisit != null && activeVisit.showBagOffer)
                upgradeInfoText.text = "Bag - unlocks the inventory level";
            else if (activeVisit != null && activeVisit.showSecondSeedOffer)
                upgradeInfoText.text =
                    "Corn Seeds - buy at least " +
                    activeVisit.minimumSecondSeedsToContinue +
                    " for the next level";
            else if (activeVisit != null && activeVisit.showSpeedBootsOffer)
                upgradeInfoText.text =
                    "Speed Boots - walk " + speedBootsBonusPercent + "% faster";
            else
                upgradeInfoText.text = "";
        }

        if (buySpeedBootsButton != null)
            buySpeedBootsButton.interactable = !fund.HasSpeedUpgrade;

        if (buySpeedBootsLabel != null)
        {
            buySpeedBootsLabel.text = fund.HasSpeedUpgrade
                ? "Speed Boots: OWNED"
                : "Buy Speed Boots ($" + speedBootsPrice + ")";
        }
    }

    private void RefreshBagUI()
    {
        if (fund == null || bagButton == null) return;

        bagButton.interactable = !fund.HasBag;

        if (bagButtonLabel != null)
        {
            bagButtonLabel.text = fund.HasBag
                ? "Bag: OWNED"
                : "Buy Bag ($" + bagPrice + ")";
        }
    }

    private void RefreshSecondSeedUI()
    {
        if (fund == null || secondSeedButton == null) return;

        secondSeedButton.interactable = fund.Money >= secondPlantSeedPrice;

        if (secondSeedButtonLabel != null)
        {
            secondSeedButtonLabel.text =
                "Buy Corn Seeds ($" + secondPlantSeedPrice + " each)";
        }
    }

    private void EnsureInventoryUI()
    {
        bool canShowBag = fund != null &&
            (fund.HasBag || showInventoryTestingCanvas);

        if (!canShowBag || inventoryUI != null)
            return;

        Canvas canvas = null;
        if (nextLevelButton != null)
            canvas = nextLevelButton.GetComponentInParent<Canvas>();
        if (canvas == null && instructionsText != null)
            canvas = instructionsText.GetComponentInParent<Canvas>();

        if (canvas == null)
        {
            Debug.LogWarning("ShopUpgradeManager: no Canvas found for the Bag UI.");
            return;
        }

        GameObject inventoryObject = new GameObject(
            "Persistent Bag Inventory",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image)
        );
        inventoryObject.layer = canvas.gameObject.layer;
        inventoryObject.transform.SetParent(canvas.transform, false);

        inventoryUI = inventoryObject.AddComponent<InventoryManager>();
        TMP_FontAsset font = instructionsText != null
            ? instructionsText.font
            : TMP_Settings.defaultFontAsset;
        inventoryUI.Initialize(fund, font, showInventoryTestingCanvas);
    }

    private void RefreshContinueButton()
    {
        if (nextLevelButton == null || activeVisit == null || fund == null)
            return;

        bool hasRequiredSeeds =
            !activeVisit.requireSeedsToContinue || fund.Seeds > 0;
        bool hasRequiredBag =
            !activeVisit.requireBagToContinue || fund.HasBag;
        bool hasRequiredSecondSeeds =
            fund.SecondPlantSeeds >= activeVisit.minimumSecondSeedsToContinue;

        nextLevelButton.interactable =
            hasRequiredSeeds && hasRequiredBag && hasRequiredSecondSeeds;
    }

    private void ShowMarketMessage(string message)
    {
        Debug.Log("Shop: " + message);
        if (marketMessageText != null) marketMessageText.text = message;
    }

    private void OnDestroy()
    {
        if (seedShop != null) seedShop.ShopChanged -= OnShopChanged;
    }
}

public static class SceneHistory
{
    private static string previousScene = "";
    private static string currentScene = "";

    public static string GetSceneBefore(string thisScene)
    {
        return currentScene == thisScene ? previousScene : currentScene;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Init()
    {
        previousScene = "";
        currentScene = SceneManager.GetActiveScene().name;
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
        SceneManager.activeSceneChanged += OnActiveSceneChanged;
    }

    private static void OnActiveSceneChanged(Scene oldScene, Scene newScene)
    {
        if (newScene.name == currentScene) return;
        previousScene = currentScene;
        currentScene = newScene.name;
    }
}
