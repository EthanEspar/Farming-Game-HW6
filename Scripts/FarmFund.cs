using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class FarmFund : MonoBehaviour
{
    public static FarmFund Instance { get; private set; }

    [Header("Player's farm data (survives scene changes)")]
    public int money = 0;
    public int seeds = 0;
    public int harvestedPlants = 0;

    [Header("Upgrades bought in the shop")]
    public bool hasSpeedBoots = false;

    [Header("Scene tracking (read only)")]
    public string currentSceneName = "";
    public string previousSceneName = "";

    // fired whenever money / seeds / harvest / upgrades change, so every GUI can refresh itself
    public UnityEvent fundChangedEvent = new UnityEvent();

    // reset static data when entering Play mode (safe even if domain reload is turned off)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Instance = null;
    }

    // Use this from other scripts: FarmFund.Get().AddMoney(10);
    public static FarmFund Get()
    {
        if (Instance == null)
        {
            Debug.Log("FarmFund: none found - creating a temporary one (normal when you press Play in a later scene to test).");
            GameObject fundObject = new GameObject("Farm Fund (auto-created)");
            fundObject.AddComponent<FarmFund>(); // Awake runs right here and sets Instance
        }
        return Instance;
    }

    public static bool Exists()
    {
        return Instance != null;
    }

    void Awake()
    {
        // only one Farm Fund may exist
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        transform.SetParent(null);       // DontDestroyOnLoad only works on root objects
        DontDestroyOnLoad(gameObject);   // keep this object alive across scene transitions

        currentSceneName = SceneManager.GetActiveScene().name;
        SceneManager.activeSceneChanged += OnActiveSceneChanged;
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.activeSceneChanged -= OnActiveSceneChanged;
            Instance = null;
        }
    }

    private void OnActiveSceneChanged(Scene oldScene, Scene newScene)
    {
        if (newScene.name == currentSceneName) return;
        previousSceneName = currentSceneName;
        currentSceneName = newScene.name;
    }

    // Which scene did the player come from before 'thisScene'?
    // (works no matter whether Unity has already fired activeSceneChanged or not)
    public string GetSceneBefore(string thisScene)
    {
        return currentSceneName == thisScene ? previousSceneName : currentSceneName;
    }

    // True when the Farm Fund was created in the scene that is open right now,
    // i.e. you pressed Play directly in this scene to test it.
    public bool StartedInThisScene()
    {
        return string.IsNullOrEmpty(previousSceneName)
               && currentSceneName == SceneManager.GetActiveScene().name;
    }

    // ---------------- money ----------------
    public void AddMoney(int amount)
    {
        money = Mathf.Max(0, money + amount);
        fundChangedEvent.Invoke();
    }

    public bool TrySpendMoney(int amount)
    {
        if (amount < 0 || money < amount) return false;
        money -= amount;
        fundChangedEvent.Invoke();
        return true;
    }

    // ---------------- seeds ----------------
    public void AddSeeds(int amount)
    {
        seeds = Mathf.Max(0, seeds + amount);
        fundChangedEvent.Invoke();
    }

    public bool TryUseSeed()
    {
        if (seeds <= 0) return false;
        seeds--;
        fundChangedEvent.Invoke();
        return true;
    }

    // ---------------- harvest ----------------
    // Whoever writes the harvesting code (step 23) calls: FarmFund.Get().AddHarvestedPlants(1);
    public void AddHarvestedPlants(int amount)
    {
        harvestedPlants = Mathf.Max(0, harvestedPlants + amount);
        fundChangedEvent.Invoke();
    }

    // Sells up to 'count' harvested plants. Returns how many were actually sold.
    public int SellHarvestedPlants(int count, int pricePerPlant)
    {
        int sold = Mathf.Clamp(count, 0, harvestedPlants);
        if (sold == 0) return 0;
        harvestedPlants -= sold;
        money += sold * pricePerPlant;
        fundChangedEvent.Invoke();
        return sold;
    }

    // ---------------- upgrades ----------------
    public bool TryBuySpeedBoots(int price)
    {
        if (hasSpeedBoots) return false;
        if (!TrySpendMoney(price)) return false;
        hasSpeedBoots = true;
        fundChangedEvent.Invoke();
        return true;
    }
}
