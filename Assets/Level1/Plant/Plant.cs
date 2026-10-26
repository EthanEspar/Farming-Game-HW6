using UnityEngine;

public class Plant : MonoBehaviour
{
    [Header("Scriptable Object Data")]
    [Tooltip("Assign the corresponding PlantData asset (e.g., CornData, TomatoData) here.")]
    public PlantData plantData;

    [Tooltip("Child GameObject representing the small (planted) model")]
    public GameObject smallModel;
    [Tooltip("Child GameObject representing the mid (growing) model")]
    public GameObject midModel;
    [Tooltip("Child GameObject representing the large (mature) model")]
    public GameObject largeModel;

    [Header("Growth Timings (Can override or default from PlantData)")]
    public int daysToMid = 2;
    public int daysToLarge = 4;

    [HideInInspector]
    public GameObject plantedTile;

    // Runtime-created stage children when spawning from ScriptableObject stage prefabs.
    private bool builtFromPlantDataStages = false;

    private int daysPassedSincePlanting = 0;
    public bool IsMature
    {
        get
        {
            return daysPassedSincePlanting >= daysToLarge;
        }
    }

    private DayNightControl dayNightControl;

    void Awake()
    {
        // If plantData is assigned, pull default values/prefabs from it if needed
        if (plantData != null)
        {
            // You can initialize values from plantData here if you configure them in the ScriptableObject
        }

        // auto-assign child models if not assigned
        if (smallModel == null || midModel == null || largeModel == null)
        {
            int i = 0;
            foreach (Transform t in transform)
            {
                if (i == 0 && smallModel == null) smallModel = t.gameObject;
                else if (i == 1 && midModel == null) midModel = t.gameObject;
                else if (i == 2 && largeModel == null) largeModel = t.gameObject;
                i++;
            }
        }
    }

    void Start()
    {
        // default stage: small model active
        SetStageSmall();

        // register to day passed event
        dayNightControl = GameObject.Find("DayNightController")?.GetComponent<DayNightControl>();
        if (dayNightControl != null)
        {
            dayNightControl.dayPassedEvent.AddListener(OnDayPassed);
            dayNightControl.newSeasonEvent.AddListener(OnNewSeason);

            // Check immediately on spawn if it belongs in the current season
            CheckSeasonValidity(dayNightControl.GetSeason());
        }
    }

    private void OnNewSeason(DayNightControl.Season newSeason)
    {
        CheckSeasonValidity(newSeason);
    }

    private void CheckSeasonValidity(DayNightControl.Season currentSeason)
    {
        if (plantData != null)
        {
            // If the current season is NOT in the plant's allowed growing seasons, make it disappear/destroy
            if (!plantData.CanGrowInSeason(currentSeason))
            {
                Debug.Log($"{plantData.plantName} disappears or cannot survive during {currentSeason} on {plantedTile?.name}");
                Destroy(gameObject);
                return;
            }
        }
    }

    public void InitializeAsPlanted()
    {
        BuildStageModelsFromPlantDataIfNeeded();
        EnsureStageModelsFromChildrenIfMissing();
        daysPassedSincePlanting = 0;
        SetStageSmall();
    }

    private void BuildStageModelsFromPlantDataIfNeeded()
    {
        if (builtFromPlantDataStages) return;
        if (plantData == null) return;

        // If the prefab already has stage children assigned, do not rebuild.
        if (smallModel != null && midModel != null && largeModel != null)
        {
            builtFromPlantDataStages = true;
            return;
        }

        // Build child stage objects from PlantData stage prefabs and align to root.
        if (plantData.smallPlantPrefab != null)
        {
            smallModel = Instantiate(plantData.smallPlantPrefab, transform.position, transform.rotation, transform);
            smallModel.transform.localPosition = Vector3.zero;
            smallModel.transform.localRotation = Quaternion.identity;
        }
        if (plantData.midPlantPrefab != null)
        {
            midModel = Instantiate(plantData.midPlantPrefab, transform.position, transform.rotation, transform);
            midModel.transform.localPosition = Vector3.zero;
            midModel.transform.localRotation = Quaternion.identity;
        }
        if (plantData.largePlantPrefab != null)
        {
            largeModel = Instantiate(plantData.largePlantPrefab, transform.position, transform.rotation, transform);
            largeModel.transform.localPosition = Vector3.zero;
            largeModel.transform.localRotation = Quaternion.identity;
        }

        EnsureStageModelsFromChildrenIfMissing();
        builtFromPlantDataStages = true;
    }

    private void EnsureStageModelsFromChildrenIfMissing()
    {
        // Hard fallback: if any stage reference is missing, auto-assign from existing children
        // on the spawned plant root (works when plantPrefab already contains 3 stage children).
        if (smallModel != null && midModel != null && largeModel != null) return;

        int i = 0;
        foreach (Transform t in transform)
        {
            if (i == 0 && smallModel == null) smallModel = t.gameObject;
            else if (i == 1 && midModel == null) midModel = t.gameObject;
            else if (i == 2 && largeModel == null) largeModel = t.gameObject;
            i++;
        }

        if (smallModel == null)
            Debug.LogWarning("Plant: missing small stage model on " + name);
        if (midModel == null)
            Debug.LogWarning("Plant: missing mid stage model on " + name);
        if (largeModel == null)
            Debug.LogWarning("Plant: missing large stage model on " + name);
    }

    private void OnDayPassed()
    {
        // Apply growth speed multiplier from Scriptable Object if desired
        float speedMultiplier = (plantData != null) ? plantData.growSpeed : 1.0f;
        
        // Increment days based on grow speed weighting
        daysPassedSincePlanting += Mathf.Max(1, Mathf.RoundToInt(1f * speedMultiplier));

        if (daysPassedSincePlanting >= daysToLarge)
        {
            SetStageLarge();
        }
        else if (daysPassedSincePlanting >= daysToMid)
        {
            SetStageMid();
        }
    }

    private void SetStageSmall()
    {
        if (smallModel != null) smallModel.SetActive(true);
        if (midModel != null) midModel.SetActive(false);
        if (largeModel != null) largeModel.SetActive(false);
    }

    private void SetStageMid()
    {
        if (smallModel != null) smallModel.SetActive(false);
        if (midModel != null) midModel.SetActive(true);
        if (largeModel != null) largeModel.SetActive(false);
    }

    private void SetStageLarge()
    {
        if (smallModel != null) smallModel.SetActive(false);
        if (midModel != null) midModel.SetActive(false);
        if (largeModel != null) largeModel.SetActive(true);
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