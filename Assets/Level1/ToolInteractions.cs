using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;

// Controls visibility of three tool GameObjects based on the tile interaction state.
// toolOne: appears when tile is tilled (first interaction)
// toolTwo: appears when tile is watered (second interaction)
// toolThree: appears when a fully grown plant is harvested (via FarmFund.harvestEvent or plantManager call)
public class ToolInteractions : MonoBehaviour
{
    [System.Serializable]
    public class SlotVisibilityBinding
    {
        [Range(1, 10)] public int slotNumber = 1;
        public GameObject objectToShow;
    }

    [Tooltip("Tool to show when a tile was tilled (first interaction)")]
    public GameObject toolOne;

    [Tooltip("Tool to show when a tile was watered (second interaction)")]
    public GameObject toolTwo;

    [Tooltip("Tool to show when a fully grown crop is harvested")]
    public GameObject toolThree;

    [Header("Optional drag-and-drop hotbar visibility")]
    [Tooltip("If populated, these bindings control which object is shown for each selected hotbar slot in Scene 5.")]
    public List<SlotVisibilityBinding> hotbarSlotBindings = new List<SlotVisibilityBinding>();

    private PlayerInteractPoint playerInteractPoint;
    private InputAction interactEAction;
    [SerializeField]
    private Animator playerAnimator;

    void Start()
    {
        // hide all tools initially
        HideAllTools();

        playerInteractPoint = FindObjectOfType<PlayerInteractPoint>();
        if (playerInteractPoint != null)
        {
            playerInteractPoint.AfterTileInteracted += OnAfterTileInteracted;
            Debug.Log("ToolInteractions: subscribed to PlayerInteractPoint.AfterTileInteracted");
            // try to get the player's animator so we can trigger interaction animations
            if (playerAnimator == null && playerInteractPoint != null)
            {
                playerAnimator = playerInteractPoint.GetComponentInChildren<Animator>();
                if (playerAnimator != null) Debug.Log("ToolInteractions: found player Animator via PlayerInteractPoint -> " + playerAnimator.gameObject.name);
            }
            // fallback: try PlayerMovement
            if (playerAnimator == null)
            {
                var pm = FindObjectOfType<PlayerMovement>();
                if (pm != null)
                {
                    playerAnimator = pm.GetComponentInChildren<Animator>();
                    if (playerAnimator != null) Debug.Log("ToolInteractions: found player Animator via PlayerMovement -> " + playerAnimator.gameObject.name);
                }
            }
            // final fallback: inspect all Animators in scene and pick one that looks like a player
            if (playerAnimator == null)
            {
                var all = FindObjectsOfType<Animator>();
                Debug.Log("ToolInteractions: searching all animators, count=" + all.Length);
                foreach (var a in all)
                {
                    Debug.Log("ToolInteractions: animator candidate: " + a.gameObject.name);
                    if (a.gameObject.name.ToLower().Contains("player") || a.gameObject.name.ToLower().Contains("character") || a.gameObject.name.ToLower().Contains("peasant"))
                    {
                        playerAnimator = a;
                        Debug.Log("ToolInteractions: selected animator candidate -> " + a.gameObject.name);
                        break;
                    }
                }
                if (playerAnimator == null && all.Length == 1) { playerAnimator = all[0]; Debug.Log("ToolInteractions: selected sole animator -> " + playerAnimator.gameObject.name); }
            }
            if (playerAnimator == null) Debug.LogWarning("ToolInteractions: no player Animator found; assign playerAnimator in the inspector to enable interaction animations.");
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
        // In Scene 5, show tool based on selected inventory slot (1=till, 2=water, 3=scythe)
        if (Scene5InventoryHotbar.ToolRulesActiveInScene5)
        {
            int slot = Scene5InventoryHotbar.SelectedSlotOneBased;
            if (hotbarSlotBindings != null && hotbarSlotBindings.Count > 0)
            {
                ApplyBoundObjectVisibility(slot);
            }
            else
            {
                if (slot == 1) SetToolVisibility(true, false, false);
                else if (slot == 2) SetToolVisibility(false, true, false);
                else if (slot == 3) SetToolVisibility(false, false, true);
                else HideAllTools();
            }
        }

        // legacy E fallback (Input System keyboard)
        if (interactEAction == null)
        {
            var kb = Keyboard.current;
            if (kb != null && kb.eKey.wasPressedThisFrame)
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

        // Tool visibility is driven by selected inventory slot, not by tile action state.

        // trigger player interaction animation if available
        try
        {
            if (playerAnimator != null)
            {
                playerAnimator.SetTrigger("InteractTrigger");
                Debug.Log("ToolInteractions: fired InteractTrigger on player animator");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("ToolInteractions: failed to set InteractTrigger on player animator: " + ex.Message);
        }
    }

    // Called by plantManager or FarmFund when a mature plant is harvested
    public void ShowHarvestTool()
    {
        Debug.Log("ToolInteractions: ShowHarvestTool called");
        // Tool visibility is driven by selected inventory slot, not by harvest action.

        // also trigger animation on harvest
        try
        {
            if (playerAnimator != null)
            {
                playerAnimator.SetTrigger("InteractTrigger");
                Debug.Log("ToolInteractions: fired InteractTrigger on player animator (harvest)");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("ToolInteractions: failed to set InteractTrigger on player animator during harvest: " + ex.Message);
        }
    }

    private void OnInteractEPerformed(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
    {
        // forward to PlayerInteractPoint
        if (playerInteractPoint != null)
        {
            playerInteractPoint.PerformInteraction();
        }
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

    private void ApplyBoundObjectVisibility(int selectedSlot)
    {
        // Hide default tools so only bound objects are shown when using custom bindings
        HideAllTools();

        // Hide all bound objects first
        for (int i = 0; i < hotbarSlotBindings.Count; i++)
        {
            var binding = hotbarSlotBindings[i];
            if (binding != null && binding.objectToShow != null)
                binding.objectToShow.SetActive(false);
        }

        // Show the first matching bound object for this slot
        for (int i = 0; i < hotbarSlotBindings.Count; i++)
        {
            var binding = hotbarSlotBindings[i];
            if (binding == null || binding.objectToShow == null) continue;
            if (binding.slotNumber == selectedSlot)
            {
                binding.objectToShow.SetActive(true);
                break;
            }
        }
    }
}
