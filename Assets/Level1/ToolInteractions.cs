using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;

// Controls visibility of three tool GameObjects based on the tile interaction state.
// toolOne: appears when tile is tilled (first interaction)
// toolTwo: appears when tile is watered (second interaction)
// toolThree: appears when a fully grown plant is harvested (via FarmFund.harvestEvent or plantManager call)
public class ToolInteractions : MonoBehaviour
{
    [Tooltip("Tool to show when a tile was tilled (first interaction)")]
    public GameObject toolOne;

    [Tooltip("Tool to show when a tile was watered (second interaction)")]
    public GameObject toolTwo;

    [Tooltip("Tool to show when a fully grown crop is harvested")]
    public GameObject toolThree;

    [Tooltip("How long (seconds) a tool stays visible after an interaction if the player doesn't press again")]
    public float visibleDuration = 1f;

    private PlayerInteractPoint playerInteractPoint;
    private InputAction interactEAction;

    private GameObject lastInteractedTile;
    private Coroutine hideCoroutine;

    void Start()
    {
        // hide all tools initially
        HideAllTools();

        playerInteractPoint = FindObjectOfType<PlayerInteractPoint>();
        if (playerInteractPoint != null)
        {
            playerInteractPoint.AfterTileInteracted += OnAfterTileInteracted;
            Debug.Log("ToolInteractions: subscribed to PlayerInteractPoint.AfterTileInteracted");
        }
        else
        {
            Debug.LogWarning("ToolInteractions: PlayerInteractPoint not found in scene.");
        }

        // subscribe to Interact action (E) if present
        interactEAction = InputSystem.actions.FindAction("Interact");
        if (interactEAction != null)
        {
            interactEAction.performed += OnInteractEPerformed;
            Debug.Log("ToolInteractions: subscribed to InputSystem 'Interact' action");
        }

        // subscribe to harvest events from FarmFund if available
        if (FarmFund.Instance != null)
        {
            FarmFund.Instance.harvestEvent.AddListener(ShowHarvestTool);
            Debug.Log("ToolInteractions: subscribed to FarmFund.harvestEvent");
        }

    }

    void OnDestroy()
    {
        if (playerInteractPoint != null)
            playerInteractPoint.AfterTileInteracted -= OnAfterTileInteracted;
        if (interactEAction != null)
            interactEAction.performed -= OnInteractEPerformed;
        if (FarmFund.Instance != null)
            FarmFund.Instance.harvestEvent.RemoveListener(ShowHarvestTool);
    }

    void Update()
    {
        // hide tools when player moves away from the last interacted tile
        if (lastInteractedTile != null && playerInteractPoint != null)
        {
            var cur = playerInteractPoint.GetCurrentTile();
            if (cur == null || cur != lastInteractedTile)
            {
                if (hideCoroutine != null) { StopCoroutine(hideCoroutine); hideCoroutine = null; }
                HideAllTools();
                lastInteractedTile = null;
            }
        }

        // movement handled by PlayerMovement script; no animator control here

        // legacy E fallback
        if (interactEAction == null)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                playerInteractPoint?.PerformInteraction();
            }
        }
    }

    // movement is handled by PlayerMovement; ToolInteractions no longer sets Animator parameters

    private void OnAfterTileInteracted(GameObject tile)
    {
        Debug.Log("ToolInteractions: OnAfterTileInteracted called for " + (tile?tile.name:"null"));
        if (tile == null) return;
        var farmTile = tile.GetComponent<FarmTileControl>();
        Debug.Log("ToolInteractions: tileCond=" + (farmTile?farmTile.tileCond.ToString():"no farmTile"));
        if (farmTile == null) return;

        lastInteractedTile = tile;

        switch (farmTile.tileCond)
        {
            case FarmTileControl.FarmTileCond.Grass:
                HideAllTools();
                break;
            case FarmTileControl.FarmTileCond.Tilled:
                SetToolVisibility(true, false, false);
                break;
            case FarmTileControl.FarmTileCond.Watered:
                SetToolVisibility(false, true, false);
                break;
            default:
                HideAllTools();
                break;
        }

        if (hideCoroutine != null) { StopCoroutine(hideCoroutine); hideCoroutine = null; }
        hideCoroutine = StartCoroutine(HideAfterDelay());
    }

    // Called by plantManager or FarmFund when a mature plant is harvested
    public void ShowHarvestTool()
    {
        Debug.Log("ToolInteractions: ShowHarvestTool called");
        SetToolVisibility(false, false, true);
        if (hideCoroutine != null) { StopCoroutine(hideCoroutine); hideCoroutine = null; }
        hideCoroutine = StartCoroutine(HideAfterDelay());
    }

    private void OnInteractEPerformed(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
    {
        // forward to PlayerInteractPoint
        if (playerInteractPoint != null)
        {
            playerInteractPoint.PerformInteraction();
        }
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(visibleDuration);
        HideAllTools();
        lastInteractedTile = null;
        hideCoroutine = null;
    }

    private void SetToolVisibility(bool one, bool two, bool three)
    {
        if (toolOne != null) toolOne.SetActive(one);
        if (toolTwo != null) toolTwo.SetActive(two);
        if (toolThree != null) toolThree.SetActive(three);
    }

    private void HideAllTools()
    {
        if (toolOne != null) toolOne.SetActive(false);
        if (toolTwo != null) toolTwo.SetActive(false);
        if (toolThree != null) toolThree.SetActive(false);
    }
}
