using UnityEngine;
using UnityEngine.Events;

public class FarmFund : MonoBehaviour
{
    public static FarmFund Instance { get; private set; }

    [SerializeField] private int startingMoney = 0;

    public int Money { get; private set; }
    public int Seeds { get; private set; }
    public int HarvestedPlants { get; private set; }
    public bool HasSpeedUpgrade { get; private set; }
    // Event invoked when a plant is harvested (HarvestedPlants was incremented)
    public UnityEvent harvestEvent = new UnityEvent();

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
    }

    public bool TrySpendMoney(int amount)
    {
        if (amount < 0 || Money < amount)
            return false;

        Money -= amount;
        return true;
    }

    public void AddSeeds(int amount)
    {
        Seeds += Mathf.Max(0, amount);
    }

    public bool TryUseSeed()
    {
        if (Seeds <= 0)
            return false;

        Seeds--;
        return true;
    }

    public void AddHarvestedPlant()
    {
        HarvestedPlants++;
        harvestEvent.Invoke();
    }

    public void SellHarvestedPlants(int pricePerPlant)
    {
        Money += HarvestedPlants * pricePerPlant;
        HarvestedPlants = 0;
    }

    public bool BuySpeedUpgrade(int price)
    {
        if (HasSpeedUpgrade || !TrySpendMoney(price))
            return false;

        HasSpeedUpgrade = true;
        return true;
    }
}