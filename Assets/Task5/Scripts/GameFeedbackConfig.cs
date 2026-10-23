using UnityEngine;


[CreateAssetMenu(
    fileName = "GameFeedbackConfig",
    menuName = "Farming Game/Game Feedback Config"
)]
public sealed class GameFeedbackConfig : ScriptableObject
{
    [Header("Shop sounds")]
    [Tooltip("Plays when money is spent (seeds, bag, upgrades).")]
    public AudioClip buySound;
    [Tooltip("Plays when harvested crops are sold for money.")]
    public AudioClip sellSound;

    [Header("Farm sounds")]
    public AudioClip plantSound;
    public AudioClip harvestSound;
    [Tooltip("Plays when a level's goal is reached.")]
    public AudioClip levelCompleteSound;
    [Tooltip("Plays when DayNightControl fires newSeasonEvent.")]
    public AudioClip newSeasonSound;
    [Tooltip("Plays with every warning message (wrong tool, no seeds, not enough money...).")]
    public AudioClip errorSound;

    [Header("Volume")]
    [Range(0f, 1f)] public float effectsVolume = 0.8f;

    [Header("On-screen messages")]
    [Min(0.5f)] public float messageSeconds = 3.2f;
    [Range(1, 5)] public int maxMessagesOnScreen = 3;
    [Tooltip("Distance from the top of the screen (1080p units) where messages appear.")]
    public float messageTopOffset = 235f;
}
