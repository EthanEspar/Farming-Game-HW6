using UnityEngine;
using UnityEngine.InputSystem;

public class plantManager : MonoBehaviour
{
    [Tooltip("Plant prefab to spawn (parent object with three stage models as children)")]
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
    private Rect spawnMenuRect = new Rect(10, 10, 220, 160);

    void Start()
    {
        playerInteractPoint = FindObjectOfType<PlayerInteractPoint>();
        if (playerInteractPoint == null)
            Debug.LogWarning("plantManager: PlayerInteractPoint not found in scene.");
        if (plantPrefab == null)
            Debug.LogWarning("plantManager: plantPrefab is not assigned.");
        seedCounter = FindObjectOfType<seedCounter>();
        if (seedCounter == null)
            Debug.LogWarning("plantManager: seedCounter not found in scene. Planting won't decrement seeds.");
    }

    void Update()
    {
        // Use the Input System keyboard instead of the legacy Input class
        var k = Keyboard.current;
        if (k == null) return;

        // guard against invalid key
        if (plantKey == Key.None) return;

        if (k[plantKey].wasPressedThisFrame)
        {
            TryPlantAtPlayerTile();
        }
    }

    // Draw an on-screen menu in Play mode to tweak spawn offset/height
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
        if (playerInteractPoint == null || plantPrefab == null) return;
        GameObject curTile = playerInteractPoint.GetCurrentTile();
        if (curTile == null) return;

        FarmTileControl ft = curTile.GetComponent<FarmTileControl>();
        if (ft == null) return;

        // only allow planting on Watered tiles
        if (ft.tileCond != FarmTileControl.FarmTileCond.Watered)
        {
            Debug.Log("Cannot plant here. Tile is not watered.");
            return;
        }

       // Check whether this tile already has a plant.
        Plant[] existing = FindObjectsByType<Plant>(FindObjectsSortMode.None);

        foreach (Plant plant in existing)
        {
        if (plant != null && plant.plantedTile == curTile)
        {
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

            Debug.Log("Plant harvested! Total harvested: " +FarmFund.Instance.HarvestedPlants
        );
        Destroy(plant.gameObject);
        return;
        }
    }
        // ensure player has seeds
        if (seedCounter != null)
        {
            if (!seedCounter.TryUseSeed())
            {
                Debug.Log("No seeds left to plant.");
                return;
            }
        }

        // spawn plant at tile position with offset and height
        Vector3 spawnPos = curTile.transform.position + spawnOffset;
        spawnPos.y += spawnHeight; // raise slightly so it sits on the tile
        GameObject newPlant = Instantiate(plantPrefab, spawnPos, Quaternion.identity, this.transform);
        // try to set plantedTile on Plant script
        Plant plantComp = newPlant.GetComponent<Plant>();
        if (plantComp != null)
        {
            plantComp.plantedTile = curTile;
            plantComp.InitializeAsPlanted();
        }
        else
        {
            Debug.LogWarning("Spawned plant prefab does not contain a Plant component.");
        }
    }
}
