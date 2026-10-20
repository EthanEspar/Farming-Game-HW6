using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "ShopSceneConfig",
    menuName = "Farming Game/Shop Scene Config"
)]
public sealed class ShopSceneConfig : ScriptableObject
{
    [Serializable]
    public sealed class Visit
    {
        [Tooltip("A descriptive name shown only in the Inspector.")]
        public string displayName = "Shop visit";

        [Tooltip("The scenes that can lead to this version of the shop.")]
        public string[] entrySceneNames = Array.Empty<string>();

        [Tooltip("The scene loaded by the shop's continue button.")]
        public string nextSceneName;

        public string continueButtonLabel = "Continue";

        [TextArea]
        public string instructions = "Buy what you need, then continue.";

        [TextArea]
        public string announcement;

        [Tooltip("Shows the panel containing selling and upgrade controls.")]
        public bool showMarketPanel;

        [Tooltip("Shows the Speed Boots offer during this visit.")]
        public bool showSpeedBootsOffer;

        [Tooltip("Shows the Bag offer during this visit.")]
        public bool showBagOffer;

        [Tooltip("Shows the second plant seed offer during this visit.")]
        public bool showSecondSeedOffer;

        [Tooltip("Prevents continuing until the player owns at least one seed.")]
        public bool requireSeedsToContinue;

        [Tooltip("Prevents continuing until the player owns the Bag.")]
        public bool requireBagToContinue;

        [Min(0)]
        [Tooltip("Minimum second-plant seeds needed before continuing. Zero disables the check.")]
        public int minimumSecondSeedsToContinue;

        public bool Matches(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName) || entrySceneNames == null)
                return false;

            foreach (string candidate in entrySceneNames)
            {
                if (string.Equals(candidate, sceneName, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }
    }

    [Tooltip("Used when the shop is opened directly or from an unlisted scene.")]
    [SerializeField] private Visit fallbackVisit = new Visit();

    [Tooltip("Add one entry for every level that returns to this shop scene.")]
    [SerializeField] private List<Visit> visits = new List<Visit>();

    public Visit ResolveVisit(string entrySceneName)
    {
        foreach (Visit visit in visits)
        {
            if (visit != null && visit.Matches(entrySceneName))
                return visit;
        }

        return fallbackVisit;
    }
}
