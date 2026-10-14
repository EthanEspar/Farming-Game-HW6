using UnityEngine;
using TMPro;

// Updates a UI text showing player funds when all farm tiles are watered.
public class fundCounter : MonoBehaviour
{
    [Tooltip("Reference to the TextMeshProUGUI that displays the funds value")]
    public TextMeshProUGUI fundsText;

    [Tooltip("Optional: a TextMeshProUGUI used to show level completion message")]
    public TextMeshProUGUI levelCompleteText;

    [Tooltip("Optional: parent GameObject for the level complete UI (will be activated when level is beaten)")]
    public GameObject levelCompletePanel;

    [Tooltip("Optional: a button GameObject to show when the level is completed (will be enabled when all tiles are watered)")]
    public GameObject levelCompleteButton;

    [Tooltip("Initial funds value shown in the UI")]
    public int startingFunds = 0;

    [Tooltip("Funds value to set when all tiles are watered")]
    public int fundsWhenAllWatered = 10;

    //private int currentFunds;
    private bool rewardGiven = false;

    private FarmTileControl[] farmTiles;

    [System.Obsolete]
    void Start()
    {
        //currentFunds = startingFunds;
        UpdateFundsUI();

        // hide completion button initially if provided
        if (levelCompleteButton != null)
            levelCompleteButton.SetActive(false);

        // find all farm tiles in the scene via their component
        farmTiles = FindObjectsOfType<FarmTileControl>();
    }

    [System.Obsolete]
    void Update()
    {
        if (rewardGiven) return; // already set

        // If tiles weren't found at Start (they may be instantiated later), try finding them again
        if (farmTiles == null || farmTiles.Length == 0)
        {
            farmTiles = FindObjectsOfType<FarmTileControl>();
            if (farmTiles == null || farmTiles.Length == 0)
            {
                // still nothing to check
                return;
            }
        }

        // check whether every tile is in the Watered state
        bool allWatered = true;
        foreach (var t in farmTiles)
        {
            if (t == null || t.tileCond != FarmTileControl.FarmTileCond.Watered)
            {
                allWatered = false;
                break;
            }
        }

        if (allWatered)
        {
            //currentFunds = fundsWhenAllWatered;
            //new logic
            if (FarmFund.Instance != null)
            {
                FarmFund.Instance.AddMoney(fundsWhenAllWatered);
            }
            UpdateFundsUI();
            // update level complete text (do not change active state)
            if (levelCompleteText != null)
            {
                levelCompleteText.text = "You beat level 1";
            }
            // enable completion button if assigned
            if (levelCompleteButton != null)
            {
                levelCompleteButton.SetActive(true);
            }
            rewardGiven = true;
        }
    }

    private void UpdateFundsUI()
    {
        if (fundsText != null && FarmFund.Instance != null)
        {
            fundsText.text = FarmFund.Instance.Money.ToString();
        }
    }
}
