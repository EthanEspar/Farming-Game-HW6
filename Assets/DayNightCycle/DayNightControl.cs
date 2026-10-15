using UnityEngine;
using TMPro; // Important for TextMeshPro
using UnityEngine.Events;
<<<<<<< Updated upstream
using UnityEngine.InputSystem;

public class DayNightControl : MonoBehaviour
{
    // ---------- States (enums, same style as FarmTileControl.FarmTileCond) ----------
    public enum WeekDay
    {
        Monday, Tuesday, Wednesday, Thursday, Friday, Saturday, Sunday
    }
    public enum Season
    {
        Spring, Summer, Fall, Winter
    }

=======

public class DayNightControl : MonoBehaviour
{
>>>>>>> Stashed changes
    public Light sunLight; // Assign your directional light in the Inspector
    public float cycleLengthInSeconds = 60f; //time of a whole day, can be adjusted in Unity editor

    [SerializeField]
    private int DaysPassedInGame = 1;
    private float currentTimeOfDay = 0f; // 0 to 1, representing percentage through the day

<<<<<<< Updated upstream
    [Header("Calendar (HW6)")]
    [Tooltip("How many 7-day weeks one season lasts. Set this to 1 while debugging so Winter shows up quickly.")]
    public int weeksPerSeason = 2;

    [Tooltip("Current day-of-week state. Calculated from Days Passed In Game - shown here so you can watch it change.")]
    public WeekDay currentWeekDay = WeekDay.Monday;

    [Tooltip("Current season state. Calculated from Days Passed In Game and Weeks Per Season.")]
    public Season currentSeason = Season.Spring;

    [Header("Old 'Days: 1' label (optional - leave empty once the Time Display Canvas replaces it)")]
    public TMP_Text dayLable;

    [Header("Debug")]
    [Tooltip("Press this key in Play mode to jump to the next day instantly (fires the same events as a real day passing).")]
    public bool allowSkipDayKey = true;
    public Key skipDayKey = Key.N;

    //this event gets fired when a day finishes and will let all the other objects that listens to this event know
    public UnityEvent dayPassedEvent = new UnityEvent();

    //HW6: this event gets fired when the season changes (Spring -> Summer -> Fall -> Winter -> Spring)
    //listeners receive the new season as a parameter
    public UnityEvent<Season> newSeasonEvent = new UnityEvent<Season>();

    void Awake()
    {
        UpdateCalendarStates();
    }

    // keeps the Inspector values correct when you change Days Passed In Game / Weeks Per Season by hand
    void OnValidate()
    {
        if (weeksPerSeason < 1) weeksPerSeason = 1;
        if (DaysPassedInGame < 1) DaysPassedInGame = 1;
        UpdateCalendarStates();
    }

    void Update()
    {

=======
    public TMP_Text dayLable;

    //this event gets fired when a day finishes and will let all the other objects that listens to this event know
    public UnityEvent dayPassedEvent=new UnityEvent();

    void Update()
    {
        
>>>>>>> Stashed changes
        currentTimeOfDay += Time.deltaTime / cycleLengthInSeconds;// adjust a small amount of time per frame

        if (currentTimeOfDay >= 1f)
        {
            currentTimeOfDay = 0f; // Reset to start a new day
<<<<<<< Updated upstream
            AdvanceOneDay();
        }

        // debug shortcut: skip to the next day
        if (allowSkipDayKey && Keyboard.current != null && skipDayKey != Key.None
            && Keyboard.current[skipDayKey].wasPressedThisFrame)
        {
            Debug.Log("DayNightControl: debug key pressed - skipping to the next day");
            AdvanceOneDay();
        }


=======
            DaysPassedInGame++;
            dayLable.text="Days:  "+DaysPassedInGame.ToString();

            dayPassedEvent.Invoke();//make announcement to all listeners
        }

        // Calculate sun's rotation based on time of day
        // 0 degrees for sunrise, 180 for sunset, 360 for next sunrise
>>>>>>> Stashed changes
        float sunRotationX = Mathf.Lerp(0f, 360f, currentTimeOfDay);

        // Apply rotation to the sun light
        sunLight.transform.rotation = Quaternion.Euler(sunRotationX, 0f, 0f);
<<<<<<< Updated upstream
    }

    private void AdvanceOneDay()
    {
        Season seasonBefore = currentSeason;

        DaysPassedInGame++;
        UpdateCalendarStates();

        if (dayLable != null)
            dayLable.text = "Days:  " + DaysPassedInGame.ToString();

        Debug.Log("New day: Day " + DaysPassedInGame + ", " + currentWeekDay + ", " + currentSeason);
        dayPassedEvent.Invoke();//make announcement to all listeners

        // state transition on the Season state machine -> announce the new season
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

    // ---------------- public API for other scripts ----------------

=======

        // Optional: Adjust other elements, like skybox, light source intensity, and so on
        // sunLight.intensity = 
        // RenderSettings.fogColor = 
        // RenderSettings.skybox.SetFloat
    }
>>>>>>> Stashed changes
    public int Get_DaysPassedInGame() {
        return DaysPassedInGame;
    }

<<<<<<< Updated upstream
    public WeekDay GetWeekDay()
    {
        return currentWeekDay;
    }

    public Season GetSeason()
    {
        return currentSeason;
    }

    public int GetDaysPerSeason()
    {
        return 7 * Mathf.Max(1, weeksPerSeason);
    }

    // 1-based week number inside the current season (e.g. "Week 2 of Spring")
    public int GetWeekOfSeason()
    {
        int dayIndex = Mathf.Max(0, DaysPassedInGame - 1);
        return (dayIndex % GetDaysPerSeason()) / 7 + 1;
    }

    // 0..1 through the day
    public float GetTimeOfDay01()
    {
        return currentTimeOfDay;
    }

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

    //provide API for other objects to add themselves to the listner list
    //objects in this list can receive callback when each day passessss

=======
    //provide API for other objects to add themselves to the listner list
    //objects in this list can receive callback when each day passessss
>>>>>>> Stashed changes
    public void listenToDayPassEvent(GameObject listener)
    {
        //when the day pass event is triggered, the farm tile's ADayPassed() function will be called
        dayPassedEvent.AddListener(listener.GetComponent<FarmTileControl>().ADayPassed);
    }
}
