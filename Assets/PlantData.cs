
using UnityEngine;

[CreateAssetMenu(fileName = "NewPlantData", menuName = "Farming/Plant Data")]
public class PlantData : ScriptableObject
{
    [Header("Plant Information")]
    public string plantName;

    [Header("Plant Prefab")]
    public GameObject plantPrefab;

    [Header("Growth Settings")]
    [Min(1)] public int daysToMid = 2;
    [Min(2)] public int daysToLarge = 4;

    [Header("Season Settings")]
    public DayNightControl.Season growingSeason;

    [Header("Seed Settings")]
    [Min(0)] public int seedCost = 1;
}