using UnityEngine;
using TMPro;

public class TimeDisplayUI : MonoBehaviour
{
    [Header("Text fields on this canvas")]
    public TMP_Text dayValueText;
    public TMP_Text weekdayValueText;
    public TMP_Text seasonValueText;
    public TMP_Text timeValueText;

    [Tooltip("The whole TIME cell. Turn Show Time Of Day off to hide it.")]
    public GameObject timeCell;
    public bool showTimeOfDay = true;

    [Header("Announcement banner (new season, winter warnings, etc.)")]
    public GameObject announcementBanner;
    public TMP_Text announcementText;
    public float announcementSeconds = 3.5f;

    [Header("Season colors")]
    public Color springColor = new Color(0.55f, 0.9f, 0.45f);
    public Color summerColor = new Color(1f, 0.85f, 0.3f);
    public Color fallColor = new Color(1f, 0.55f, 0.2f);
    public Color winterColor = new Color(0.6f, 0.85f, 1f);

    private DayNightControl dayNightControl;
    private float announcementTimer = 0f;

    void Start()
    {
        // same lookup style the farm tiles and plants use
        GameObject controllerObject = GameObject.Find("DayNightController");
        if (controllerObject != null)
            dayNightControl = controllerObject.GetComponent<DayNightControl>();
        if (dayNightControl == null)
            dayNightControl = FindAnyObjectByType<DayNightControl>();

        // hide the banner unless another script already showed a message
        if (announcementBanner != null && announcementTimer <= 0f) announcementBanner.SetActive(false);
        if (timeCell != null) timeCell.SetActive(showTimeOfDay);

        if (dayNightControl == null)
        {
            Debug.LogWarning("TimeDisplayUI: no DayNightControl found in this scene.");
            return;
        }

        // register as a listener of both events
        dayNightControl.dayPassedEvent.AddListener(OnDayPassed);
        dayNightControl.newSeasonEvent.AddListener(OnNewSeason);

        RefreshCalendarText();
    }

    void Update()
    {
        // the clock changes every frame, so it is polled instead of event-driven
        if (dayNightControl != null && showTimeOfDay && timeValueText != null)
            timeValueText.text = dayNightControl.GetClockTimeString();

        if (announcementTimer > 0f)
        {
            announcementTimer -= Time.deltaTime;
            if (announcementTimer <= 0f && announcementBanner != null)
                announcementBanner.SetActive(false);
        }
    }

    // ---------- event listeners ----------
    private void OnDayPassed()
    {
        RefreshCalendarText();
    }

    private void OnNewSeason(DayNightControl.Season newSeason)
    {
        RefreshCalendarText();
        switch (newSeason)
        {
            case DayNightControl.Season.Spring:
                ShowMessage("Spring has arrived! Planting is open again.");
                break;
            case DayNightControl.Season.Summer:
                ShowMessage("Summer has arrived!");
                break;
            case DayNightControl.Season.Fall:
                ShowMessage("Fall has arrived! Winter is coming...");
                break;
            case DayNightControl.Season.Winter:
                ShowMessage("Winter is here! All crops died and nothing can be planted.");
                break;
        }
    }

    // ---------- display ----------
    private void RefreshCalendarText()
    {
        if (dayNightControl == null) return;

        DayNightControl.Season season = dayNightControl.GetSeason();

        if (dayValueText != null)
            dayValueText.text = "Day " + dayNightControl.Get_DaysPassedInGame();
        if (weekdayValueText != null)
            weekdayValueText.text = dayNightControl.GetWeekDay().ToString();
        if (seasonValueText != null)
        {
            seasonValueText.text = season.ToString() + "  <size=70%>Wk " + dayNightControl.GetWeekOfSeason()
                                   + "/" + dayNightControl.weeksPerSeason + "</size>";
            seasonValueText.color = GetSeasonColor(season);
        }
    }

    private Color GetSeasonColor(DayNightControl.Season season)
    {
        switch (season)
        {
            case DayNightControl.Season.Spring: return springColor;
            case DayNightControl.Season.Summer: return summerColor;
            case DayNightControl.Season.Fall: return fallColor;
            default: return winterColor;
        }
    }

    // Public so other scripts can show a short message (e.g. plantManager: "Can't plant in Winter")
    public void ShowMessage(string message)
    {
        if (announcementText != null) announcementText.text = message;
        if (announcementBanner != null) announcementBanner.SetActive(true);
        announcementTimer = announcementSeconds;
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
