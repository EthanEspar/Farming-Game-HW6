using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class LevelDirector : MonoBehaviour
{
    public enum LevelPhase
    {
        Menu,
        Shop,
        Playing,
        Halfway,
        GoalReached
    }

    private const string ConfigPath = "Task5/GameBalanceConfig";
    private const string LogPrefix = "[Feedback] ";

    public static LevelDirector Instance { get; private set; }

    [Tooltip("Current state - visible in the Inspector on 'Game Feedback (Persistent)' while playing.")]
    [SerializeField] private LevelPhase phase = LevelPhase.Menu;
    [SerializeField] private int harvestGoal;
    [SerializeField] private int harvestedThisLevel;

    public LevelPhase Phase => phase;

    private GameBalanceConfig config;
    private GameBalanceConfig.LevelSettings level;
    private string currentScene = "";
    private string previousScene = "";
    private int waterReward;
    private int rescuesGiven;
    private float nextRescueCheck;
    private bool shopWasShortOnSeeds;
    private readonly Dictionary<FarmInventoryItemId, string> itemNames = new Dictionary<FarmInventoryItemId, string>();

    // ---------------------------------------------------------------- setup
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Instance = null;
    }

    private void Awake()
    {
        Instance = this;
        config = Resources.Load<GameBalanceConfig>(ConfigPath);
        if (config == null)
            Debug.LogWarning(LogPrefix + "No GameBalanceConfig at Resources/" + ConfigPath + " - scene values are used as-is.");

        foreach (InventoryItemData item in Resources.LoadAll<InventoryItemData>("InventoryItems"))
        {
            if (item != null && !string.IsNullOrEmpty(item.itemName))
                itemNames[item.itemId] = item.itemName;
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
        GameFeedback.InventoryDelta += OnInventoryDelta;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        GameFeedback.InventoryDelta -= OnInventoryDelta;
        if (Instance == this) Instance = null;
    }

    public static bool IsShopScene()
    {
        Scene active = SceneManager.GetActiveScene();
        if (Instance != null && Instance.config != null && active.name == Instance.config.shopSceneName)
            return true;
        return FindAnyObjectByType<SeedsMoneyScene2>() != null;
    }

    // ---------------------------------------------------------------- state: scene loaded
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode != LoadSceneMode.Single) return;

        StopAllCoroutines();
        previousScene = currentScene;
        currentScene = scene.name;
        level = config != null ? config.Find(scene.name) : null;
        harvestedThisLevel = 0;
        harvestGoal = 0;
        rescuesGiven = 0;
        shopWasShortOnSeeds = false;

        if (IsShopScene())
        {
            phase = LevelPhase.Shop;
            StartCoroutine(ShopHintAfterDelay());
        }
        else if (level != null)
        {
            ApplyBalance();
            phase = LevelPhase.Playing;
            StartCoroutine(LevelIntroAfterDelay());
        }
        else
        {
            phase = LevelPhase.Menu;
        }
    }

    // Runs before the scene's Start() methods, so every script starts with the tuned values.
    private void ApplyBalance()
    {
        DayNightControl dayNight = FindAnyObjectByType<DayNightControl>();
        if (dayNight != null)
        {
            if (level.dayLengthSeconds > 0f) dayNight.cycleLengthInSeconds = level.dayLengthSeconds;
            if (level.weeksPerSeason > 0) dayNight.weeksPerSeason = level.weeksPerSeason;
        }

        HarvestCounter counter = FindAnyObjectByType<HarvestCounter>();
        if (counter != null)
        {
            if (level.harvestGoal > 0) counter.harvestGoal = level.harvestGoal;
            harvestGoal = counter.harvestGoal;

            // Watering every tile pays a bonus, but only the harvest goal should open the
            // next-level button (fundCounter and HarvestCounter share the same button).
            if (config.harvestGoalIsOnlyWinCondition)
            {
                foreach (fundCounter waterBonus in FindObjectsByType<fundCounter>(FindObjectsSortMode.None))
                {
                    if (waterBonus.levelCompleteButton == counter.levelCompleteButton)
                        waterBonus.levelCompleteButton = null;
                }
            }
        }

        fundCounter reward = FindAnyObjectByType<fundCounter>();
        waterReward = reward != null ? reward.fundsWhenAllWatered : 0;

        bool hasBoots = FarmFund.Instance != null && FarmFund.Instance.HasSpeedUpgrade;
        PlayerMovement player = FindAnyObjectByType<PlayerMovement>();
        if (player != null)
        {
            float speed = level.playerSpeed > 0f ? level.playerSpeed : player.moveSpeed;
            if (hasBoots) speed *= config.speedBootsMultiplier;
            player.moveSpeed = speed;
        }

        // The animated player also carries the older PlayerMove script, and both move it,
        // so the Speed Boots bonus has to scale both to really be +50%.
        PlayerMove legacyMove = FindAnyObjectByType<PlayerMove>();
        if (legacyMove != null && hasBoots)
        {
            legacyMove.forwardSpeed *= config.speedBootsMultiplier;
            legacyMove.sidewaySpeed *= config.speedBootsMultiplier;
        }

        Debug.Log(LogPrefix + "Balance for " + currentScene +
                  ": day " + (dayNight != null ? dayNight.cycleLengthInSeconds + "s" : "n/a") +
                  ", goal " + harvestGoal +
                  ", speed " + (player != null ? player.moveSpeed.ToString("0.0") : "n/a"));
    }

    private IEnumerator LevelIntroAfterDelay()
    {
        yield return new WaitForSeconds(1f);

        if (!string.IsNullOrWhiteSpace(level.objective))
        {
            string text = level.objective
                .Replace("{goal}", harvestGoal.ToString())
                .Replace("{reward}", waterReward.ToString());
            GameFeedback.Show(text, GameMessageKind.Info);
        }

        FarmFund fund = FarmFund.Instance;
        if (fund == null) yield break;

        if (fund.HasSpeedUpgrade && config.speedBootsMultiplier > 1f)
        {
            int bonus = Mathf.RoundToInt((config.speedBootsMultiplier - 1f) * 100f);
            GameFeedback.Show("Speed Boots on: you walk " + bonus + "% faster!", GameMessageKind.Success);
        }

        if (harvestGoal > 0)
        {
            int seeds = CountAllSeeds(fund);
            if (seeds < harvestGoal)
            {
                GameFeedback.Show(
                    "You have " + seeds + (seeds == 1 ? " seed" : " seeds") +
                    " for a goal of " + harvestGoal + ". Plant carefully - every seed counts!",
                    GameMessageKind.Info);
            }
        }
    }

    // ---------------------------------------------------------------- state: playing
    private void OnInventoryDelta(int moneyDelta, int seedDelta, int harvestDelta)
    {
        if (phase == LevelPhase.Shop)
        {
            if (seedDelta != 0) EvaluateShopHint(false);
            return;
        }

        if (phase != LevelPhase.Playing && phase != LevelPhase.Halfway) return;

        if (harvestDelta > 0 && harvestGoal > 0)
        {
            harvestedThisLevel += harvestDelta;

            if (harvestedThisLevel >= harvestGoal)
            {
                EnterGoalReached("Goal reached: " + harvestedThisLevel + "/" + harvestGoal +
                                 " harvested! Click the next-level button to visit the shop.");
            }
            else if (phase == LevelPhase.Playing && harvestedThisLevel * 2 >= harvestGoal)
            {
                phase = LevelPhase.Halfway;
                GameFeedback.Show("Halfway there! " + harvestedThisLevel + "/" + harvestGoal + " harvested.",
                                  GameMessageKind.Success);
            }
            else
            {
                GameFeedback.Show("+" + harvestDelta + " harvested (" + harvestedThisLevel + "/" + harvestGoal + ")",
                                  GameMessageKind.Success);
            }
        }

        if (moneyDelta > 0)
        {
            if (harvestGoal == 0)
                EnterGoalReached("All tiles watered! You earned $" + moneyDelta + " - go buy seeds in the shop.");
            else
                GameFeedback.Show("Bonus! +$" + moneyDelta + " earned.", GameMessageKind.Success);
        }
    }

    private void EnterGoalReached(string message)
    {
        phase = LevelPhase.GoalReached;
        GameFeedback.Play(GameSfx.LevelComplete);
        GameFeedback.Show(message, GameMessageKind.Success);
    }

    // ---------------------------------------------------------------- never get stuck
    private void Update()
    {
        if (phase != LevelPhase.Playing && phase != LevelPhase.Halfway) return;
        if (harvestGoal <= 0 || config == null || !config.giveRescueSeeds) return;
        if (Time.time < nextRescueCheck) return;
        nextRescueCheck = Time.time + 1.5f;

        FarmFund fund = FarmFund.Instance;
        if (fund == null || rescuesGiven >= config.maxRescuesPerLevel) return;
        // plantManager plants any seed type, so only rescue when the player has none at all
        if (CountAllSeeds(fund) > 0 || AnyCropGrowing()) return;

        rescuesGiven++;
        fund.AddItem(level.seedItem, config.rescueSeedAmount);
        GameFeedback.Show(
            "Out of seeds? A friendly neighbor gave you " + config.rescueSeedAmount + " " +
            ItemName(level.seedItem, config.rescueSeedAmount) + ". Don't give up!",
            GameMessageKind.Info);
    }

    private static int CountAllSeeds(FarmFund fund)
    {
        int total = 0;
        foreach (FarmInventoryItemId id in System.Enum.GetValues(typeof(FarmInventoryItemId)))
        {
            if (id.ToString().Contains("Seed")) total += fund.GetItemCount(id);
        }
        return total;
    }

    private static bool AnyCropGrowing()
    {
        foreach (Plant plant in FindObjectsByType<Plant>(FindObjectsSortMode.None))
        {
            if (plant != null && plant.plantedTile != null) return true; // template plants have no tile
        }
        return false;
    }

    // ---------------------------------------------------------------- shop hints
    private IEnumerator ShopHintAfterDelay()
    {
        yield return new WaitForSeconds(0.8f);
        EvaluateShopHint(true);
    }

    // Tells the player how many seeds the NEXT level needs, so they don't leave the shop short.
    private void EvaluateShopHint(bool announce)
    {
        FarmFund fund = FarmFund.Instance;
        if (config == null || fund == null) return;

        GameBalanceConfig.LevelSettings next = config.NextAfter(previousScene);
        if (next == null || next.harvestGoal <= 0) return;
        // a level that isn't in Build Profiles yet can't be the shop's next stop - stay quiet
        if (!Application.CanStreamedLevelBeLoaded(next.sceneName)) return;

        int have = fund.GetItemCount(next.seedItem);
        int missing = next.harvestGoal - have;
        string nextName = string.IsNullOrEmpty(next.displayName) ? next.sceneName : next.displayName;

        if (missing > 0)
        {
            if (announce)
            {
                GameFeedback.Show(
                    nextName + " needs " + next.harvestGoal + " harvests. You have " + have + " " +
                    ItemName(next.seedItem, have) + " - buy at least " + missing + " more.",
                    GameMessageKind.Info);
            }
            shopWasShortOnSeeds = true;
        }
        else if (announce || shopWasShortOnSeeds)
        {
            GameFeedback.Show(
                "You have enough " + ItemName(next.seedItem, 2) + " for " + nextName + ". Good luck!",
                GameMessageKind.Success);
            shopWasShortOnSeeds = false;
        }
    }

    private string ItemName(FarmInventoryItemId id, int amount)
    {
        string name = itemNames.TryGetValue(id, out string found) ? found.ToLowerInvariant() : "seed";
        return amount == 1 ? name : name + "s";
    }
}
