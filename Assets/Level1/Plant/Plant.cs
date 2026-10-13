using UnityEngine;

// Controls plant growth stages. The prefab should have three child GameObjects:
// smallModel, midModel, largeModel. Assign them in the Inspector or let the script
// find the first three children automatically.
public class Plant : MonoBehaviour
{
    [Tooltip("Child GameObject representing the small (planted) model")]
    public GameObject smallModel;
    [Tooltip("Child GameObject representing the mid (growing) model")]
    public GameObject midModel;
    [Tooltip("Child GameObject representing the large (mature) model")]
    public GameObject largeModel;

    [Tooltip("Days after planting to switch to mid model")]
    public int daysToMid = 2;
    [Tooltip("Days after planting to switch to large model")]
    public int daysToLarge = 4;

    [HideInInspector]
    public GameObject plantedTile;

    private int daysPassedSincePlanting = 0;
    private DayNightControl dayNightControl;

    void Awake()
    {
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
            dayNightControl.dayPassedEvent.RemoveListener(OnDayPassed);
    }
}
