using System;
using UnityEngine;
using UnityEngine.Events;

public class FarmFund : MonoBehaviour
{
    public static FarmFund Instance { get; private set; }

    [SerializeField] private int startingMoney = 0;

    public int Money { get; private set; }
    public int Seeds { get; private set; }
    public int SecondPlantSeeds { get; private set; }
    public int HarvestedPlants { get; private set; }
    public bool HasSpeedUpgrade { get; private set; }
    public bool HasBag { get; private set; }

    /// <summary>
    /// Raised after money, inventory, or an owned upgrade changes. Runtime UI
    /// subscribes to this so every scene shows the same persistent values.
    /// </summary>
    public event Action InventoryChanged;

    // Event invoked when a plant is harvested (HarvestedPlants was incremented)
    public UnityEvent harvestEvent = new UnityEvent();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Instance = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureInstanceExists()
    {
        if (Instance != null) return;

        GameObject fundObject = new GameObject("Farm Fund (Persistent)");
        fundObject.AddComponent<FarmFund>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Money = startingMoney;

        DontDestroyOnLoad(gameObject);
    }

    public void AddMoney(int amount)
    {
        Money += Mathf.Max(0, amount);
        NotifyInventoryChanged();
    }

    public bool TrySpendMoney(int amount)
    {
        if (amount < 0 || Money < amount)
            return false;

        Money -= amount;
        NotifyInventoryChanged();
        return true;
    }

    // Uses one available seed from the global seed pool.
    // Prefers wheat seeds first, then corn seeds.
    public bool TryUseAnySeed()
    {
        if (Seeds > 0)
            return TryUseSeed();

        if (SecondPlantSeeds > 0)
            return TryUseSecondPlantSeed();

        return false;
    }

    public void AddSeeds(int amount)
    {
        Seeds += Mathf.Max(0, amount);
        NotifyInventoryChanged();
    }

    public bool TryUseSeed()
    {
        if (Seeds <= 0)
            return false;

        Seeds--;
        NotifyInventoryChanged();
        return true;
    }

    public void AddSecondPlantSeeds(int amount)
    {
        SecondPlantSeeds += Mathf.Max(0, amount);
        NotifyInventoryChanged();
    }

    public bool TryUseSecondPlantSeed()
    {
        if (SecondPlantSeeds <= 0)
            return false;

        SecondPlantSeeds--;
        NotifyInventoryChanged();
        return true;
    }

    public void AddHarvestedPlant()
    {
        HarvestedPlants++;
        harvestEvent.Invoke();
        NotifyInventoryChanged();
    }

    public void SellHarvestedPlants(int pricePerPlant)
    {
        Money += HarvestedPlants * pricePerPlant;
        HarvestedPlants = 0;
        NotifyInventoryChanged();
    }

    public bool BuySpeedUpgrade(int price)
    {
        if (HasSpeedUpgrade || !TrySpendMoney(price))
            return false;

        HasSpeedUpgrade = true;
        NotifyInventoryChanged();
        return true;
    }

    public bool BuyBag(int price)
    {
        if (HasBag || !TrySpendMoney(price))
            return false;

        HasBag = true;
        NotifyInventoryChanged();
        return true;
    }

    public int GetItemCount(FarmInventoryItemId itemId)
    {
        switch (itemId)
        {
            case FarmInventoryItemId.FirstPlantSeed:
                return Seeds;
            case FarmInventoryItemId.FirstPlantHarvest:
                return HarvestedPlants;
            case FarmInventoryItemId.SecondPlantSeed:
                return SecondPlantSeeds;
            default:
                return 0;
        }
    }

    /// <summary>
    /// Shared inventory API used by the testing canvas and future plant types.
    /// Game code may continue using the named methods above.
    /// </summary>
    public void AddItem(FarmInventoryItemId itemId, int amount = 1)
    {
        if (amount <= 0) return;

        switch (itemId)
        {
            case FarmInventoryItemId.FirstPlantSeed:
                AddSeeds(amount);
                break;
            case FarmInventoryItemId.FirstPlantHarvest:
                for (int i = 0; i < amount; i++)
                    AddHarvestedPlant();
                break;
            case FarmInventoryItemId.SecondPlantSeed:
                AddSecondPlantSeeds(amount);
                break;
        }
    }

    public bool TryRemoveItem(FarmInventoryItemId itemId)
    {
        switch (itemId)
        {
            case FarmInventoryItemId.FirstPlantSeed:
                return TryUseSeed();
            case FarmInventoryItemId.SecondPlantSeed:
                return TryUseSecondPlantSeed();
            default:
                return false;
        }
    }

    private void NotifyInventoryChanged()
    {
        InventoryChanged?.Invoke();
    }
}
