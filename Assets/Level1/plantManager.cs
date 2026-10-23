using UnityEngine;
using UnityEngine.InputSystem;

public class plantManager : MonoBehaviour
{
    [Header("Plant Configuration Assets")]
    [Tooltip("List or array of available Plant Data scriptable objects (e.g., Corn, Tomato, etc.)")]
    public PlantData[] availablePlants;

    [Tooltip("Default plant prefab fallback if none is selected")]
    public GameObject plantPrefab;

    [Tooltip("Key to press to plant (while standing on a watered tile). Uses the new Input System Key enum")]
    public Key plantKey = Key.E;

    [Tooltip("Spawn offset relative to the tile position (X/Z horizontal offset)")]
    public Vector3 spawnOffset = Vector3.zero;

    [Tooltip("Additional height above the tile to spawn the plant")]
    public float spawnHeight = 0.05f;

    [Tooltip("Show on-screen spawn offset tuning menu during Play mode")]
    public bool showSpawnMenu = true;

    private PlayerInteractPoint playerInteractPoint;
    private seedCounter seedCounter;

    private bool plantingAllowed = true;
    private DayNightControl dayNightControl;
    private TimeDisplayUI timeDisplay;
    private Rect spawnMenuRect = new Rect(10, 10, 220, 160);

    void Start()
    {
        playerInteractPoint = FindObjectOfType<PlayerInteractPoint>();
        if (playerInteractPoint == null)
            Debug.LogWarning("plantManager: PlayerInteractPoint not found in scene.");
        
        seedCounter = FindObjectOfType<seedCounter>();
        if (seedCounter == null)
            Debug.LogWarning("plantManager: seedCounter not found in scene. Planting won't decrement seeds.");

        // HW6 Task 1: listen to the "new season" event so planting is locked during Winter
        dayNightControl = GameObject.Find("DayNightController")?.GetComponent<DayNightControl>();
        if (dayNightControl != null)
        {
            dayNightControl.newSeasonEvent.AddListener(OnNewSeason);
            plantingAllowed = dayNightControl.GetSeason() != DayNightControl.Season.Winter;
        }
        timeDisplay = FindAnyObjectByType<TimeDisplayUI>();
    }

    private void OnNewSeason(DayNightControl.Season newSeason)
    {
        plantingAllowed = newSeason != DayNightControl.Season.Winter;
        Debug.Log("plantManager: season is now " + newSeason + ", planting allowed = " + plantingAllowed);
    }

    private void OnDestroy()
    {
        if (dayNightControl != null)
            dayNightControl.newSeasonEvent.RemoveListener(OnNewSeason);
    }

    void Update()
    {
        var k = Keyboard.current;
        if (k == null) return;

        // guard against invalid key
        if (plantKey == Key.None) return;

        if (k[plantKey].wasPressedThisFrame)
        {
            TryPlantAtPlayerTile();
        }
    }

    void OnGUI()
    {
        if (!Application.isPlaying) return;
        if (!showSpawnMenu) return;

        spawnMenuRect = GUI.Window(123456, spawnMenuRect, SpawnMenuWindow, "Plant Spawn Adjust");
    }

    private void SpawnMenuWindow(int id)
    {
        GUILayout.BeginVertical();
        GUILayout.Label($"Offset X: {spawnOffset.x:F2}");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("-X", GUILayout.Width(50))) spawnOffset.x -= 0.1f;
        if (GUILayout.Button("+X", GUILayout.Width(50))) spawnOffset.x += 0.1f;
        GUILayout.EndHorizontal();

        GUILayout.Label($"Offset Z: {spawnOffset.z:F2}");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("-Z", GUILayout.Width(50))) spawnOffset.z -= 0.1f;
        if (GUILayout.Button("+Z", GUILayout.Width(50))) spawnOffset.z += 0.1f;
        GUILayout.EndHorizontal();

        GUILayout.Label($"Height: {spawnHeight:F2}");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("-Y", GUILayout.Width(50))) spawnHeight -= 0.01f;
        if (GUILayout.Button("+Y", GUILayout.Width(50))) spawnHeight += 0.01f;
        GUILayout.EndHorizontal();

        GUILayout.Space(6);
        if (GUILayout.Button("Reset", GUILayout.Height(22))) { spawnOffset = Vector3.zero; spawnHeight = 0.05f; }
        GUILayout.EndVertical();
        GUI.DragWindow(new Rect(0, 0, 10000, 20));
    }

    // Visualize spawn position for the current tile in the Scene/Game view
    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        if (playerInteractPoint == null) playerInteractPoint = FindObjectOfType<PlayerInteractPoint>();
        if (playerInteractPoint == null) return;
        GameObject curTile = playerInteractPoint.GetCurrentTile();
        if (curTile == null) return;
        Vector3 spawnPos = curTile.transform.position + spawnOffset;
        spawnPos.y += spawnHeight;
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(spawnPos, 0.05f);
        Gizmos.DrawLine(curTile.transform.position + Vector3.up * 0.01f, spawnPos);
    }

    private void TryPlantAtPlayerTile()
    {
        if (playerInteractPoint == null) return;
        GameObject curTile = playerInteractPoint.GetCurrentTile();
        if (curTile == null) return;

        FarmTileControl ft = curTile.GetComponent<FarmTileControl>();
        if (ft == null) return;

        Plant[] existing = FindObjectsByType<Plant>(FindObjectsSortMode.None);

        foreach (Plant plant in existing)
        {
            if (plant != null && plant.plantedTile == curTile)
            {
                // Scene 5 tool rules: only slot 3 can harvest.
                if (Scene5InventoryHotbar.ToolRulesActiveInScene5 && Scene5InventoryHotbar.SelectedSlotOneBased != 3)
                {
                    Debug.Log("Scene5 rule: select slot 3 (Scythe) to harvest crops.");
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

                FarmFund.Instance.AddHarvestedPlant();

                Debug.Log(
                    "Plant harvested! Total harvested: " +
                    FarmFund.Instance.HarvestedPlants
                );
                Destroy(plant.gameObject);
                return;
            }
        }

        // only allow planting on Watered tiles
        if (ft.tileCond != FarmTileControl.FarmTileCond.Watered)
        {
            Debug.Log("Cannot plant here. Tile is not watered.");
            return;
        }

        if (!plantingAllowed)
        {
            Debug.Log("Cannot plant during Winter.");
            if (timeDisplay != null) timeDisplay.ShowMessage("It's Winter - nothing can be planted until Spring.");
            return;
        }

        // Scene 5 tool rules: only slot 4 can plant seeds.
        if (Scene5InventoryHotbar.ToolRulesActiveInScene5 && Scene5InventoryHotbar.SelectedSlotOneBased != 4)
        {
            Debug.Log("Scene5 rule: select slot 4 (Seeds) to plant.");
            return;
        }

        // Determine which plant data to use based on current selection / inventory
        PlantData selectedPlantData = GetActivePlantData();
        GameObject prefabToSpawn = (selectedPlantData != null && selectedPlantData.smallPlantPrefab != null) 
                                   ? selectedPlantData.smallPlantPrefab 
                                   : plantPrefab;

        if (prefabToSpawn == null)
        {
            Debug.LogError("plantManager: No valid plant prefab available to spawn!");
            return;
        }

        // ensure player has seeds
        bool usedSeed;
        if (FarmFund.Instance == null)
        {
            Debug.LogError("Farm Fund is missing.");
            return;
        }

        usedSeed = FarmFund.Instance.TryUseAnySeed();
        if (seedCounter != null)
        {
            seedCounter.SendMessage("UpdateSeedUI", SendMessageOptions.DontRequireReceiver);
        }

        if (!usedSeed)
        {
            Debug.Log("No seeds left to plant.");
            return;
        }

        // spawn plant at tile position with offset and height
        Vector3 spawnPos = curTile.transform.position + spawnOffset;
        spawnPos.y += spawnHeight; 
        GameObject newPlant = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity, this.transform);
        
        // Setup plant component and pass the ScriptableObject reference
        Plant plantComp = newPlant.GetComponent<Plant>();
        if (plantComp != null)
        {
            plantComp.plantedTile = curTile;
            
            // If your Plant script has a reference field for PlantData, assign it here dynamically:
            // plantComp.plantData = selectedPlantData;

            plantComp.InitializeAsPlanted();
        }
        else
        {
            Debug.LogWarning("Spawned plant prefab does not contain a Plant component.");
        }
    }

    /// <label>Helper method to select which PlantData to instantiate</label>
    private PlantData GetActivePlantData()
    {
        if (availablePlants == null || availablePlants.Length == 0) return null;

        // Example logic: Map active hotbar items or default to the first/second plant asset.
        // If you have multiple seed types, you can check which seed is active here.
        // For demonstration, if index 0 is corn and index 1 is tomato:
        int index = 0; 
        
        // You can tie this index to your inventory system selection if applicable, e.g.:
        // if (Scene5InventoryHotbar.SelectedSlotOneBased == 4) index = 0; // Corn
        // else if (/* another condition */) index = 1; // Tomato

        if (index < availablePlants.Length)
        {
            return availablePlants[index];
        }
        return availablePlants[0];
    }
}