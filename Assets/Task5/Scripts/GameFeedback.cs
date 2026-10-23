using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameSfx
{
    Buy,
    Sell,
    Plant,
    Harvest,
    LevelComplete,
    NewSeason,
    Error
}

[DefaultExecutionOrder(-50)]
public sealed class GameFeedback : MonoBehaviour
{
    public static GameFeedback Instance { get; private set; }

    /// <summary>Raised after FarmFund changes: (money change, seed change, harvest change).</summary>
    public static event Action<int, int, int> InventoryDelta;

    private const string ConfigPath = "Task5/GameFeedbackConfig";
    private const string OwnLogPrefix = "[Feedback]";
    private const string PlayerWarningPrefix = "[Player] ";
    private const string PlayerInfoPrefix = "[PlayerInfo] ";

    private struct LogRule
    {
        public string match;          // text the log message starts with
        public string message;        // shown to the player (null = no message)
        public GameMessageKind kind;
        public bool errorSound;
    }

    // Debug.Log lines already in the game -> what the player should read instead.
    private static readonly LogRule[] LogRules =
    {
        new LogRule { match = "Scene5 rule: select slot 1", message = "Grab the Hoe (press 1) to till the grass.", kind = GameMessageKind.Warning, errorSound = true },
        new LogRule { match = "Scene5 rule: select slot 2", message = "Grab the Watering Can (press 2) to water tilled soil.", kind = GameMessageKind.Warning, errorSound = true },
        new LogRule { match = "Scene5 rule: select slot 3", message = "Grab the Scythe (press 3) to harvest.", kind = GameMessageKind.Warning, errorSound = true },
        new LogRule { match = "Scene5 rule: select slot 4", message = "Select your seeds (press 4) to plant.", kind = GameMessageKind.Warning, errorSound = true },
        new LogRule { match = "The plant is not ready to harvest", message = "Not ready yet - let it finish growing first.", kind = GameMessageKind.Warning, errorSound = true },
        new LogRule { match = "Cannot plant here. Tile is not watered", message = "Till and water this soil before planting.", kind = GameMessageKind.Warning, errorSound = true },
        new LogRule { match = "No seeds left to plant", message = "You're out of seeds! Buy more in the shop.", kind = GameMessageKind.Warning, errorSound = true },
        new LogRule { match = "Not enough stamina", message = "Too tired! Stand still for a moment to get your stamina back.", kind = GameMessageKind.Warning, errorSound = true },
        new LogRule { match = "Cannot plant during Winter", message = null, kind = GameMessageKind.Warning, errorSound = true }, // the Time Display banner already explains it
        new LogRule { match = "SeedsMoneyScene2: not enough money", message = "Not enough money for that many seeds.", kind = GameMessageKind.Warning, errorSound = true },
        new LogRule { match = "SeedsMoneyScene2: enter a valid seed quantity", message = "Type how many seeds you want first (1 or more).", kind = GameMessageKind.Warning, errorSound = true },
        new LogRule { match = "SeedsMoneyScene2: there are no harvested plants to sell", message = "Nothing to sell yet - harvest some crops first.", kind = GameMessageKind.Warning, errorSound = true },
        // shop panel already shows these sentences on screen - just add the error sound
        new LogRule { match = "Shop: Not enough money", message = null, kind = GameMessageKind.Warning, errorSound = true },
        new LogRule { match = "Shop: Buy ", message = null, kind = GameMessageKind.Warning, errorSound = true },
        new LogRule { match = "Shop: Enter a valid", message = null, kind = GameMessageKind.Warning, errorSound = true },
    };

    private GameFeedbackConfig config;
    private AudioSource effectsSource;
    private GameMessageUI messages;

    private FarmFund fund;
    private int lastMoney, lastSeeds, lastHarvest;
    private FarmInventoryItemId[] seedIds;
    private FarmInventoryItemId[] harvestIds;

    private DayNightControl dayNight;
    private readonly Dictionary<GameSfx, float> lastPlayTime = new Dictionary<GameSfx, float>();
    private readonly Dictionary<string, float> lastLogMessageTime = new Dictionary<string, float>();

    // ---------------------------------------------------------------- setup
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Instance = null;
        InventoryDelta = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null) return;
        GameObject host = new GameObject("Game Feedback (Persistent)");
        DontDestroyOnLoad(host);
        host.AddComponent<GameFeedback>();
        host.AddComponent<LevelDirector>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        config = Resources.Load<GameFeedbackConfig>(ConfigPath);
        if (config == null)
        {
            Debug.LogWarning(OwnLogPrefix + " No GameFeedbackConfig at Resources/" + ConfigPath + " - sounds are off.");
            config = ScriptableObject.CreateInstance<GameFeedbackConfig>();
        }

        effectsSource = gameObject.AddComponent<AudioSource>();
        effectsSource.playOnAwake = false;
        effectsSource.spatialBlend = 0f; // 2D: same volume wherever the camera is

        messages = gameObject.AddComponent<GameMessageUI>();
        messages.Build(config.messageSeconds, config.maxMessagesOnScreen, config.messageTopOffset);

        seedIds = FindItemIds("Seed");
        harvestIds = FindItemIds("Harvest");

        SceneManager.sceneLoaded += OnSceneLoaded;
        Application.logMessageReceived += OnLogMessage;
    }

    private void OnDestroy()
    {
        if (Instance != this) return;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        Application.logMessageReceived -= OnLogMessage;
        UnbindFund();
        UnbindDayNight();
        Instance = null;
    }

    private void Update()
    {
        // FarmFund can be created after us (it makes itself AfterSceneLoad), so bind lazily
        if (FarmFund.Instance != fund)
            BindFund(FarmFund.Instance);
    }

    // ---------------------------------------------------------------- public API
    public static void Show(string message, GameMessageKind kind = GameMessageKind.Info)
    {
        if (Instance == null || Instance.messages == null) return;
        Instance.messages.Show(message, kind);
        if (kind == GameMessageKind.Warning || kind == GameMessageKind.Error)
            Play(GameSfx.Error);
    }

    public static void Play(GameSfx sfx)
    {
        if (Instance == null) return;
        Instance.PlayInternal(sfx);
    }

    // ---------------------------------------------------------------- sounds
    private void PlayInternal(GameSfx sfx)
    {
        AudioClip clip = ClipFor(sfx);
        if (clip == null || effectsSource == null) return;

        // the same sound twice in one moment (e.g. two events for one purchase) plays once
        if (lastPlayTime.TryGetValue(sfx, out float last) && Time.unscaledTime - last < 0.12f) return;
        lastPlayTime[sfx] = Time.unscaledTime;

        effectsSource.PlayOneShot(clip, config.effectsVolume);
    }

    private AudioClip ClipFor(GameSfx sfx)
    {
        switch (sfx)
        {
            case GameSfx.Buy: return config.buySound;
            case GameSfx.Sell: return config.sellSound;
            case GameSfx.Plant: return config.plantSound;
            case GameSfx.Harvest: return config.harvestSound;
            case GameSfx.LevelComplete: return config.levelCompleteSound;
            case GameSfx.NewSeason: return config.newSeasonSound;
            case GameSfx.Error: return config.errorSound;
            default: return null;
        }
    }

    // ---------------------------------------------------------------- FarmFund events
    private void BindFund(FarmFund newFund)
    {
        UnbindFund();
        fund = newFund;
        if (fund == null) return;
        TakeSnapshot();
        fund.InventoryChanged += OnInventoryChanged;
    }

    private void UnbindFund()
    {
        if (fund != null) fund.InventoryChanged -= OnInventoryChanged;
        fund = null;
    }

    private void TakeSnapshot()
    {
        if (fund == null) return;
        lastMoney = fund.Money;
        lastSeeds = SumItems(seedIds);
        lastHarvest = SumItems(harvestIds);
    }

    private void OnInventoryChanged()
    {
        int money = fund.Money;
        int seeds = SumItems(seedIds);
        int harvest = SumItems(harvestIds);

        int moneyDelta = money - lastMoney;
        int seedDelta = seeds - lastSeeds;
        int harvestDelta = harvest - lastHarvest;
        lastMoney = money;
        lastSeeds = seeds;
        lastHarvest = harvest;

        if (moneyDelta == 0 && seedDelta == 0 && harvestDelta == 0) return;

        if (LevelDirector.IsShopScene())
        {
            if (moneyDelta > 0 && harvestDelta < 0) Play(GameSfx.Sell);
            else if (moneyDelta < 0) Play(GameSfx.Buy);
        }
        else
        {
            if (harvestDelta > 0) Play(GameSfx.Harvest);
            else if (seedDelta < 0) Play(GameSfx.Plant);
            if (moneyDelta > 0) Play(GameSfx.Buy); // coin sound for money earned on the farm
        }

        InventoryDelta?.Invoke(moneyDelta, seedDelta, harvestDelta);
    }

    /// <summary>Totals every FarmInventoryItemId whose name contains the word, so new
    /// item types (e.g. SecondPlantHarvest) are counted automatically.</summary>
    private static FarmInventoryItemId[] FindItemIds(string word)
    {
        List<FarmInventoryItemId> ids = new List<FarmInventoryItemId>();
        foreach (FarmInventoryItemId id in Enum.GetValues(typeof(FarmInventoryItemId)))
        {
            if (id.ToString().Contains(word)) ids.Add(id);
        }
        return ids.ToArray();
    }

    private int SumItems(FarmInventoryItemId[] ids)
    {
        int total = 0;
        foreach (FarmInventoryItemId id in ids) total += fund.GetItemCount(id);
        return total;
    }

    // ---------------------------------------------------------------- scene hooks
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TakeSnapshot();

        UnbindDayNight();
        dayNight = FindAnyObjectByType<DayNightControl>();
        if (dayNight != null)
            dayNight.newSeasonEvent.AddListener(OnNewSeason);
    }

    private void UnbindDayNight()
    {
        if (dayNight != null) dayNight.newSeasonEvent.RemoveListener(OnNewSeason);
        dayNight = null;
    }

    private void OnNewSeason(DayNightControl.Season season)
    {
        Play(GameSfx.NewSeason);
    }

    // ---------------------------------------------------------------- Debug.Log -> player messages
    private void OnLogMessage(string text, string stackTrace, LogType type)
    {
        if (string.IsNullOrEmpty(text) || text.StartsWith(OwnLogPrefix)) return;

        if (text.StartsWith(PlayerWarningPrefix))
        {
            ShowThrottled(text.Substring(PlayerWarningPrefix.Length), GameMessageKind.Warning, true);
            return;
        }
        if (text.StartsWith(PlayerInfoPrefix))
        {
            ShowThrottled(text.Substring(PlayerInfoPrefix.Length), GameMessageKind.Info, false);
            return;
        }

        // seed shop success lines (the seed shop has no on-screen message of its own)
        if (text.StartsWith("Bought ") && text.Contains(" seeds for $") && LevelDirector.IsShopScene() && !text.Contains("corn"))
        {
            ShowThrottled(text, GameMessageKind.Success, false);
            return;
        }
        if (text.StartsWith("Sold ") && text.Contains(" for $"))
        {
            ShowThrottled(text.Replace(" plants ", " crops "), GameMessageKind.Success, false);
            return;
        }

        foreach (LogRule rule in LogRules)
        {
            if (!text.StartsWith(rule.match)) continue;
            if (rule.message != null)
                ShowThrottled(rule.message, rule.kind, rule.errorSound);
            else if (rule.errorSound)
                Play(GameSfx.Error);
            return;
        }
    }

    private void ShowThrottled(string message, GameMessageKind kind, bool errorSound)
    {
        // holding a key can log the same mistake every frame - show it once per second
        if (lastLogMessageTime.TryGetValue(message, out float last) && Time.unscaledTime - last < 1f) return;
        lastLogMessageTime[message] = Time.unscaledTime;

        messages.Show(message, kind);
        if (errorSound) Play(GameSfx.Error);
    }
}
