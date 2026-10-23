using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "GameBalanceConfig",
    menuName = "Farming Game/Game Balance Config"
)]
public sealed class GameBalanceConfig : ScriptableObject
{
    [Serializable]
    public sealed class LevelSettings
    {
        [Tooltip("Exact scene name, as listed in Build Profiles.")]
        public string sceneName;
        [Tooltip("Name shown to the player, e.g. 'Level 2'.")]
        public string displayName;
        [Tooltip("The seed this level is played with (used for shop hints and rescue seeds).")]
        public FarmInventoryItemId seedItem = FarmInventoryItemId.FirstPlantSeed;

        [Header("Overrides (-1 = keep the scene's value)")]
        [Tooltip("Seconds per in-game day (DayNightControl.cycleLengthInSeconds).")]
        public float dayLengthSeconds = -1f;
        [Tooltip("DayNightControl.weeksPerSeason")]
        public int weeksPerSeason = -1;
        [Tooltip("HarvestCounter.harvestGoal. 0 = this level has no harvest goal (water-only level).")]
        public int harvestGoal = -1;
        [Tooltip("PlayerMovement.moveSpeed before the Speed Boots bonus.")]
        public float playerSpeed = -1f;

        [Header("Message shown when the level starts")]
        [TextArea(2, 4)]
        [Tooltip("{goal} = harvest goal, {reward} = money for watering every tile.")]
        public string objective;
    }

    [Tooltip("Farm levels in the order they are played.")]
    public List<LevelSettings> levels = new List<LevelSettings>();

    [Header("Shop")]
    public string shopSceneName = "Scene2-Shop";

    [Header("Speed Boots (custom upgrade)")]
    [Tooltip("Multiplies PlayerMovement.moveSpeed in every level after the boots are bought.")]
    public float speedBootsMultiplier = 1.5f;

    [Header("Never get stuck")]
    [Tooltip("If the player has no seeds and nothing growing, give a few seeds so the level can still be won.")]
    public bool giveRescueSeeds = true;
    [Min(1)] public int rescueSeedAmount = 2;
    [Min(0)] public int maxRescuesPerLevel = 3;

    [Header("Fairness")]
    [Tooltip("Stops 'water every tile' from also unlocking the next-level button in harvest levels.")]
    public bool harvestGoalIsOnlyWinCondition = true;

    public LevelSettings Find(string sceneName)
    {
        foreach (LevelSettings level in levels)
        {
            if (level != null && level.sceneName == sceneName)
                return level;
        }
        return null;
    }

    public LevelSettings NextAfter(string sceneName)
    {
        for (int i = 0; i < levels.Count - 1; i++)
        {
            if (levels[i] != null && levels[i].sceneName == sceneName)
                return levels[i + 1];
        }
        return null;
    }
}
