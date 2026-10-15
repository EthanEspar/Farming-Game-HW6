using UnityEngine;
using TMPro; // Important for TextMeshPro
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class DayNightControl : MonoBehaviour
{
    // ---------- States (enums) ----------
    public enum WeekDay { Monday, Tuesday, Wednesday, Thursday, Friday, Saturday, Sunday }
    public enum Season { Spring, Summer, Fall, Winter }

    public Light sunLight; // Assign directional light in Inspector
    public float cycleLengthInSeconds = 60f; // length of full day

    [SerializeField]
    private int DaysPassedInGame = 1;
    private float currentTimeOfDay = 0f; // 0..1 through the day

    [Header("Calendar (HW6)")]
    [Tooltip("How many 7-day weeks one season lasts.")]
    public int weeksPerSeason = 2;

    [Tooltip("Current day-of-week (calculated)")]
    public WeekDay currentWeekDay = WeekDay.Monday;

    [Tooltip("Current season (calculated)")]
    public Season currentSeason = Season.Spring;

    [Header("Old 'Days: 1' label (optional - leave empty once the Time Display Canvas replaces it)")]
    public TMP_Text dayLable;

    [Header("Debug")]
    [Tooltip("Press this key in Play mode to jump to the next day instantly.")]
    public bool allowSkipDayKey = true;
    public Key skipDayKey = Key.N;

    // events
    public UnityEvent dayPassedEvent = new UnityEvent();
    public UnityEvent<Season> newSeasonEvent = new UnityEvent<Season>();

    void Awake() { UpdateCalendarStates(); }

    void OnValidate()
    {
        if (weeksPerSeason < 1) weeksPerSeason = 1;
        if (DaysPassedInGame < 1) DaysPassedInGame = 1;
        UpdateCalendarStates();
    }

    void Update()
    {
        currentTimeOfDay += Time.deltaTime / cycleLengthInSeconds;
        if (currentTimeOfDay >= 1f)
        {
            currentTimeOfDay = 0f;
            AdvanceOneDay();
        }

        // debug skip
        if (allowSkipDayKey && Keyboard.current != null && skipDayKey != Key.None
            && Keyboard.current[skipDayKey].wasPressedThisFrame)
        {
            Debug.Log("DayNightControl: debug key pressed - skipping to the next day");
            AdvanceOneDay();
        }

        // sun rotation
        float sunRotationX = Mathf.Lerp(0f, 360f, currentTimeOfDay);
        if (sunLight != null) sunLight.transform.rotation = Quaternion.Euler(sunRotationX, 0f, 0f);
    }

    private void AdvanceOneDay()
    {
        Season seasonBefore = currentSeason;
        DaysPassedInGame++;
        UpdateCalendarStates();

        if (dayLable != null) dayLable.text = "Days:  " + DaysPassedInGame.ToString();

        Debug.Log("New day: Day " + DaysPassedInGame + ", " + currentWeekDay + ", " + currentSeason);
        dayPassedEvent.Invoke();

        if (currentSeason != seasonBefore)
        {
            Debug.Log("New season event: " + currentSeason);
            newSeasonEvent.Invoke(currentSeason);
        }
    }

    private void UpdateCalendarStates()
    {
        int dayIndex = Mathf.Max(0, DaysPassedInGame - 1);
        currentWeekDay = (WeekDay)(dayIndex % 7);
        currentSeason = (Season)((dayIndex / GetDaysPerSeason()) % 4);
    }

    // public API
    public int Get_DaysPassedInGame() { return DaysPassedInGame; }
    public WeekDay GetWeekDay() { return currentWeekDay; }
    public Season GetSeason() { return currentSeason; }
    public int GetDaysPerSeason() { return 7 * Mathf.Max(1, weeksPerSeason); }
    public int GetWeekOfSeason()
    {
        int dayIndex = Mathf.Max(0, DaysPassedInGame - 1);
        return (dayIndex % GetDaysPerSeason()) / 7 + 1;
    }
    public float GetTimeOfDay01() { return currentTimeOfDay; }

    public string GetClockTimeString()
    {
        float hours = (6f + currentTimeOfDay * 24f) % 24f;
        int hour24 = Mathf.FloorToInt(hours);
        int minutes = Mathf.FloorToInt((hours - hour24) * 60f);
        minutes = (minutes / 10) * 10;
        string amPm = hour24 < 12 ? "AM" : "PM";
        int hour12 = hour24 % 12;
        if (hour12 == 0) hour12 = 12;
        return hour12 + ":" + minutes.ToString("00") + " " + amPm;
    }

    // allow other objects to listen for day passing
    public void listenToDayPassEvent(GameObject listener)
    {
        dayPassedEvent.AddListener(listener.GetComponent<FarmTileControl>().ADayPassed);
    }
}
