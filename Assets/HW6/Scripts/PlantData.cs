using UnityEngine;

[CreateAssetMenu(fileName = "NewPlantData", menuName = "Farming/Plant Data")]
public class PlantData : ScriptableObject
{
    [Header("Plant Information")]
    public string plantName;
    public int seedAmount = 1;

    [Header("Growth & Seasons")]
    [Tooltip("Select which seasons this plant is allowed to be planted and grow in")]
    public DayNightControl.Season[] plantingSeasons; 
    
    public float growSpeed = 1.0f;   // Multiplier for growth rate

    [Header("Growth Stage Visuals")]
    public GameObject smallPlantPrefab; // Small size (planted)
    public GameObject midPlantPrefab;   // Mid size (growing)
    public GameObject largePlantPrefab; // Large size (mature)

    // Helper method to verify if the crop can grow in a given season
    public bool CanGrowInSeason(DayNightControl.Season currentSeason)
    {
        if (plantingSeasons == null || plantingSeasons.Length == 0) return true; // Default to allow if empty

        foreach (var season in plantingSeasons)
        {
            if (season == currentSeason) return true;
        }
        return false;
    }
}