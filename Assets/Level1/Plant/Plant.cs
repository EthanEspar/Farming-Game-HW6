using UnityEngine;

public class Plant : MonoBehaviour
{
    [Header("Plant Configuration")]
    public PlantData plantData;

    [Tooltip("Child GameObject representing the small (planted) model")]
    public GameObject smallModel;

    [Tooltip("Child GameObject representing the mid (growing) model")]
    public GameObject midModel;

    [Tooltip("Child GameObject representing the large (mature) model")]
    public GameObject largeModel;

    // Legacy settings for plants without PlantData
    [Tooltip("Days until middle growth stage")]
    public int daysToMid = 2;

    [Tooltip("Days until mature growth stage")]
    public int daysToLarge = 4;

    [HideInInspector]
    public GameObject plantedTile;

    private int daysPassedSincePlanting = 0;
    private DayNightControl dayNightControl;

    // Read growth settings from PlantData when available
    private int MidGrowthDays
    {
        get
        {
            return plantData != null
                ? plantData.daysToMid
                : daysToMid;
        }
    }

    private int MatureGrowthDays
    {
        get
        {
            return plantData != null
                ? plantData.daysToLarge
                : daysToLarge;
        }
    }

    public bool IsMature
    {
        get
        {
            return daysPassedSincePlanting >= MatureGrowthDays;
        }
    }

    void Awake()
    {
        // Automatically find stage models if needed
        if (smallModel == null || midModel == null || largeModel == null)
        {
            int i = 0;

            foreach (Transform t in transform)
            {
                if (i == 0 && smallModel == null)
                    smallModel = t.gameObject;
                else if (i == 1 && midModel == null)
                    midModel = t.gameObject;
                else if (i == 2 && largeModel == null)
                    largeModel = t.gameObject;

                i++;
            }
        }
    }

    void Start()
    {
        SetStageSmall();

        dayNightControl = GameObject.Find("DayNightController")
            ?.GetComponent<DayNightControl>();

        if (dayNightControl != null)
        {
            dayNightControl.dayPassedEvent.AddListener(OnDayPassed);
            dayNightControl.newSeasonEvent.AddListener(OnNewSeason);
        }
    }

    private void OnNewSeason(DayNightControl.Season newSeason)
    {
        // Plants with PlantData disappear when their growing season ends
        if (plantData != null)
        {
            if (newSeason != plantData.growingSeason)
            {
                Debug.Log(
                    plantData.plantName +
                    " cannot survive in " +
                    newSeason +
                    ". Removing plant."
                );

                Destroy(gameObject);
            }
        }
        // Preserve original winter behavior for older plants
        else if (
            newSeason == DayNightControl.Season.Winter &&
            plantedTile != null
        )
        {
            Debug.Log(
                "Winter arrived - removing plant on " +
                plantedTile.name
            );

            Destroy(gameObject);
        }
    }

    public void InitializeAsPlanted()
    {
        daysPassedSincePlanting = 0;
        SetStageSmall();
    }

    private void OnDayPassed()
    {
        daysPassedSincePlanting++;

        if (daysPassedSincePlanting >= MatureGrowthDays)
        {
            SetStageLarge();
        }
        else if (daysPassedSincePlanting >= MidGrowthDays)
        {
            SetStageMid();
        }
    }

    private void SetStageSmall()
    {
        if (smallModel != null)
            smallModel.SetActive(true);

        if (midModel != null)
            midModel.SetActive(false);

        if (largeModel != null)
            largeModel.SetActive(false);
    }

    private void SetStageMid()
    {
        if (smallModel != null)
            smallModel.SetActive(false);

        if (midModel != null)
            midModel.SetActive(true);

        if (largeModel != null)
            largeModel.SetActive(false);
    }

    private void SetStageLarge()
    {
        if (smallModel != null)
            smallModel.SetActive(false);

        if (midModel != null)
            midModel.SetActive(false);

        if (largeModel != null)
            largeModel.SetActive(true);
    }

    private void OnDestroy()
    {
        if (dayNightControl != null)
        {
            dayNightControl.dayPassedEvent.RemoveListener(OnDayPassed);
            dayNightControl.newSeasonEvent.RemoveListener(OnNewSeason);
        }
    }
}