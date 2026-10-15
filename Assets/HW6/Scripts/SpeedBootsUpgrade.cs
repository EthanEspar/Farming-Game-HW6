using UnityEngine;

public class SpeedBootsUpgrade : MonoBehaviour
{
    [Tooltip("1.5 = 50% faster. Keep the shop's 'Speed Boots Bonus Percent' in sync.")]
    public float speedMultiplier = 1.5f;

    [Tooltip("Show a banner on the Time Display Canvas when the boots are equipped")]
    public bool announce = true;

    void Start()
    {
        if (FarmFund.Instance == null || !FarmFund.Instance.HasSpeedUpgrade)
            return; // no upgrade bought -> normal speed

        PlayerMove playerMove = GetComponent<PlayerMove>();
        if (playerMove == null)
        {
            Debug.LogWarning("SpeedBootsUpgrade: put this script on the same object as PlayerMove.");
            return;
        }

        playerMove.forwardSpeed *= speedMultiplier;
        playerMove.sidewaySpeed *= speedMultiplier;
        Debug.Log("Speed Boots equipped: speed is now " + playerMove.forwardSpeed);

        if (announce)
        {
            TimeDisplayUI timeDisplay = FindAnyObjectByType<TimeDisplayUI>();
            if (timeDisplay != null)
                timeDisplay.ShowMessage("Speed Boots equipped! You walk " + Mathf.RoundToInt((speedMultiplier - 1f) * 100f) + "% faster.");
        }
    }
}
