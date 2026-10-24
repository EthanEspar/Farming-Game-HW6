

using UnityEngine;
using UnityEngine.InputSystem;

// A configurable plant type and its corresponding seed.
[System.Serializable]
public class PlantOption
{
    public PlantData plantData;
    public FarmInventoryItemId seedItem;
}

public class plantManager : MonoBehaviour
{
    [Header("Plant Types")]
    public PlantOption[] plantOptions;

    [Tooltip("Legacy plant prefab (kept for compatibility)")]
    public GameObject plantPrefab;

    [Tooltip("Key to press to plant")]
    public Key plantKey = Key.E;

    [Tooltip("Spawn offset relative to the tile")]
    public Vector3 spawnOffset = Vector3.zero;

    [Tooltip("Additional height above the tile")]
    public float spawnHeight = 0.05f;

    [Tooltip("Show spawn adjustment menu")]
    public bool showSpawnMenu = true;

    private PlayerInteractPoint playerInteractPoint;
    private seedCounter seedCounter;
    private DayNightControl dayNightControl;
    private TimeDisplayUI timeDisplay;

    private bool plantingAllowed = true;

    private Rect spawnMenuRect = new Rect(10, 10, 220, 160);

    void Start()
    {
        playerInteractPoint = FindObjectOfType<PlayerInteractPoint>();

        if (playerInteractPoint == null)
            Debug.LogWarning("plantManager: PlayerInteractPoint not found.");

        if (plantOptions == null || plantOptions.Length == 0)
            Debug.LogWarning("plantManager: No Plant Options configured.");

        seedCounter = FindObjectOfType<seedCounter>();

        if (seedCounter == null)
            Debug.LogWarning("plantManager: seedCounter not found.");

        dayNightControl = GameObject.Find("DayNightController")
            ?.GetComponent<DayNightControl>();

        if (dayNightControl != null)
        {
            dayNightControl.newSeasonEvent.AddListener(OnNewSeason);

            plantingAllowed =
                dayNightControl.GetSeason() !=
                DayNightControl.Season.Winter;
        }

        timeDisplay = FindAnyObjectByType<TimeDisplayUI>();
    }

    private void OnNewSeason(DayNightControl.Season newSeason)
    {
        plantingAllowed =
            newSeason != DayNightControl.Season.Winter;

        Debug.Log(
            "plantManager: season is now " +
            newSeason +
            ", planting allowed = " +
            plantingAllowed
        );
    }

    private void OnDestroy()
    {
        if (dayNightControl != null)
            dayNightControl.newSeasonEvent.RemoveListener(OnNewSeason);
    }

    void Update()
    {
        var keyboard = Keyboard.current;

        if (keyboard == null || plantKey == Key.None)
            return;

        if (keyboard[plantKey].wasPressedThisFrame)
            TryPlantAtPlayerTile();
    }

    void OnGUI()
    {
        if (!Application.isPlaying || !showSpawnMenu)
            return;

        spawnMenuRect = GUI.Window(
            123456,
            spawnMenuRect,
            SpawnMenuWindow,
            "Plant Spawn Adjust"
        );
    }

    private void SpawnMenuWindow(int id)
    {
        GUILayout.BeginVertical();

        GUILayout.Label($"Offset X: {spawnOffset.x:F2}");

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("-X", GUILayout.Width(50)))
            spawnOffset.x -= 0.1f;

        if (GUILayout.Button("+X", GUILayout.Width(50)))
            spawnOffset.x += 0.1f;

        GUILayout.EndHorizontal();

        GUILayout.Label($"Offset Z: {spawnOffset.z:F2}");

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("-Z", GUILayout.Width(50)))
            spawnOffset.z -= 0.1f;

        if (GUILayout.Button("+Z", GUILayout.Width(50)))
            spawnOffset.z += 0.1f;

        GUILayout.EndHorizontal();

        GUILayout.Label($"Height: {spawnHeight:F2}");

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("-Y", GUILayout.Width(50)))
            spawnHeight -= 0.01f;

        if (GUILayout.Button("+Y", GUILayout.Width(50)))
            spawnHeight += 0.01f;

        GUILayout.EndHorizontal();

        GUILayout.Space(6);

        if (GUILayout.Button("Reset", GUILayout.Height(22)))
        {
            spawnOffset = Vector3.zero;
            spawnHeight = 0.05f;
        }

        GUILayout.EndVertical();

        GUI.DragWindow(new Rect(0, 0, 10000, 20));
    }

    void OnDrawGizmos()
    {
        if (!Application.isPlaying)
            return;

        if (playerInteractPoint == null)
            playerInteractPoint = FindObjectOfType<PlayerInteractPoint>();

        if (playerInteractPoint == null)
            return;

        GameObject curTile = playerInteractPoint.GetCurrentTile();

        if (curTile == null)
            return;

        Vector3 spawnPos = curTile.transform.position + spawnOffset;
        spawnPos.y += spawnHeight;

        Gizmos.color = Color.green;
        Gizmos.DrawSphere(spawnPos, 0.05f);

        Gizmos.DrawLine(
            curTile.transform.position + Vector3.up * 0.01f,
            spawnPos
        );
    }

    private void TryPlantAtPlayerTile()
    {
        if (playerInteractPoint == null)
            return;

        GameObject curTile = playerInteractPoint.GetCurrentTile();

        if (curTile == null)
            return;

        FarmTileControl ft = curTile.GetComponent<FarmTileControl>();

        if (ft == null)
            return;

        // Check for an existing plant on this tile.
        Plant[] existing =
            FindObjectsByType<Plant>(FindObjectsSortMode.None);

        foreach (Plant plant in existing)
        {
            if (plant == null || plant.plantedTile != curTile)
                continue;

            // Harvesting requires the scythe in Scene 5+.
            if (
                Scene5InventoryHotbar.ToolRulesActiveInScene5 &&
                Scene5InventoryHotbar.SelectedSlotOneBased != 3
            )
            {
                Debug.Log("Select slot 3 (Scythe) to harvest.");
                return;
            }

            if (!plant.IsMature)
            {
                Debug.Log("The plant is not ready to harvest.");
                return;
            }

            if (FarmFund.Instance == null)
            {
                Debug.LogError("Farm Fund is missing.");
                return;
            }

            // Existing shared harvest counter.
            FarmFund.Instance.AddHarvestedPlant();

            Debug.Log(
                "Plant harvested! Total harvested: " +
                FarmFund.Instance.HarvestedPlants
            );

            Destroy(plant.gameObject);
            return;
        }

        // Only plant on watered tiles.
        if (ft.tileCond != FarmTileControl.FarmTileCond.Watered)
        {
            Debug.Log("Cannot plant here. Tile is not watered.");
            return;
        }

        // Keep the existing winter restriction.
        if (!plantingAllowed)
        {
            Debug.Log("Cannot plant during Winter.");

            if (timeDisplay != null)
            {
                timeDisplay.ShowMessage(
                    "It's Winter - nothing can be planted until Spring."
                );
            }

            return;
        }

        // Planting requires the seed slot in Scene 5+.
        if (
            Scene5InventoryHotbar.ToolRulesActiveInScene5 &&
            Scene5InventoryHotbar.SelectedSlotOneBased != 4
        )
        {
            Debug.Log("Select slot 4 (Seeds) to plant.");
            return;
        }

        if (FarmFund.Instance == null)
        {
            Debug.LogError("Farm Fund is missing.");
            return;
        }

        if (dayNightControl == null)
        {
            Debug.LogWarning("DayNightController is missing.");
            return;
        }

        DayNightControl.Season currentSeason =
            dayNightControl.GetSeason();

        PlantData selectedPlantData = null;
        FarmInventoryItemId selectedSeedItem = default;
        bool foundSeed = false;

        // Search configured plants for one that grows this season
        // and has an available seed.
        if (plantOptions != null)
        {
            foreach (PlantOption option in plantOptions)
            {
                if (option == null || option.plantData == null)
                    continue;

                PlantData data = option.plantData;

                if (data.plantPrefab == null)
                    continue;

                if (data.growingSeason != currentSeason)
                    continue;

                // Only seed inventory items can be used to plant.
                if (
                    option.seedItem != FarmInventoryItemId.FirstPlantSeed &&
                    option.seedItem != FarmInventoryItemId.SecondPlantSeed
                )
                    continue;

                if (FarmFund.Instance.GetItemCount(option.seedItem) <= 0)
                    continue;

                selectedPlantData = data;
                selectedSeedItem = option.seedItem;
                foundSeed = true;
                break;
            }
        }

        if (!foundSeed || selectedPlantData == null)
        {
            Debug.Log("No seeds available for the current season.");

            if (timeDisplay != null)
            {
                timeDisplay.ShowMessage(
                    "No seeds available for this season."
                );
            }

            return;
        }

        // Consume one seed of the selected type.
        if (!FarmFund.Instance.TryRemoveItem(selectedSeedItem))
        {
            Debug.Log("Failed to consume the selected seed.");
            return;
        }

        // Update the seed counter UI.
        if (seedCounter != null)
        {
            seedCounter.SendMessage(
                "UpdateSeedUI",
                SendMessageOptions.DontRequireReceiver
            );
        }

        // Find the visible center of the farm tile.
        Vector3 spawnPos = curTile.transform.position;

        Renderer tileRenderer = curTile.GetComponent<Renderer>();

        if (tileRenderer == null)
        {
            tileRenderer = curTile.GetComponentInChildren<Renderer>();
        }

        if (tileRenderer != null)
        {
            spawnPos = tileRenderer.bounds.center;
            spawnPos.y = tileRenderer.bounds.max.y;
        }

        // Apply any optional adjustments.
        spawnPos += spawnOffset;
        spawnPos.y += spawnHeight;

        // Spawn the selected plant's prefab.
        GameObject newPlant = Instantiate(
            selectedPlantData.plantPrefab,
            spawnPos,
            Quaternion.identity,
            this.transform
        );

        Plant plantComp = newPlant.GetComponent<Plant>();

        if (plantComp != null)
        {
            plantComp.plantData = selectedPlantData;
            plantComp.plantedTile = curTile;
            plantComp.InitializeAsPlanted();

            Debug.Log(
                selectedPlantData.plantName +
                " planted successfully!"
            );
        }
        else
        {
            Debug.LogWarning(
                "Spawned plant prefab does not contain a Plant component."
            );
        }
    }
}
