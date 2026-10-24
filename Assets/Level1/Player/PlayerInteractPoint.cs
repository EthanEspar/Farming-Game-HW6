
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using System;

public class PlayerInteractPoint : MonoBehaviour
{
    [SerializeField]
    private GameObject curTile;

    private InputAction interactAction;

    private bool curInteractionTriggered = false;

    private StaminaBarScript stamina;

    [SerializeField]
    private Animator animator;

    [Tooltip("Minimum time between interactions in seconds")]
    public float interactionDebounce = 0.25f;

    private float lastInteractionTime = -10f;

    private void Start()
    {
        interactAction = InputSystem.actions.FindAction("Jump");

        if (interactAction != null)
        {
            interactAction.performed += OnTileInteractPerformed;
        }

        stamina = FindObjectOfType<StaminaBarScript>();

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (animator != null)
        {
            Debug.Log(
                "PlayerInteractPoint: found Animator -> " +
                animator.gameObject.name
            );
        }
    }

    // Event invoked after a tile has been interacted with
    public event Action<GameObject> AfterTileInteracted;

    private void OnTileInteractPerformed(
        InputAction.CallbackContext context
    )
    {
        PerformInteraction();
    }

    public void PerformInteraction()
    {
        // Scene 5 tool rules
        if (Scene5InventoryHotbar.ToolRulesActiveInScene5)
        {
            if (curTile == null)
                return;

            var gatedTile = curTile.GetComponent<FarmTileControl>();

            if (gatedTile == null)
                return;

            int slot = Scene5InventoryHotbar.SelectedSlotOneBased;

            if (
                gatedTile.tileCond ==
                FarmTileControl.FarmTileCond.Grass &&
                slot != 1
            )
            {
                Debug.Log(
                    "Scene5 rule: select slot 1 (Till) to till grass."
                );
                return;
            }

            if (
                gatedTile.tileCond ==
                FarmTileControl.FarmTileCond.Tilled &&
                slot != 2
            )
            {
                Debug.Log(
                    "Scene5 rule: select slot 2 (Water) to water tilled soil."
                );
                return;
            }
        }

        // Prevent duplicate interactions
        if (
            Time.time - lastInteractionTime <
            interactionDebounce
        )
        {
            Debug.Log("Interact ignored due to debounce");
            return;
        }

        lastInteractionTime = Time.time;

        Debug.Log("Interact pressed");

        if (curTile == null)
            return;

        var tile = curTile.GetComponent<FarmTileControl>();

        if (tile == null)
            return;

        // Consume stamina
        if (stamina != null)
        {
            if (!stamina.TryConsume(stamina.waterDrainAmount))
            {
                Debug.Log(
                    "Not enough stamina to perform action."
                );
                return;
            }
        }

        // Interact with the farm tile
        tile.InteractWithFarmTile();

        AfterTileInteracted?.Invoke(curTile);

        // Trigger interaction animation
        try
        {
            if (animator != null)
            {
                animator.SetTrigger("InteractTrigger");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning(
                "PlayerInteractPoint: failed to set " +
                "InteractTrigger on animator: " +
                ex.Message
            );
        }
    }

    private void Update()
    {
        var keyboard = Keyboard.current;

        if (keyboard != null)
        {
            if (keyboard.spaceKey.wasPressedThisFrame)
            {
                PerformInteraction();
            }

            if (keyboard.eKey.wasPressedThisFrame)
            {
                PerformInteraction();
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag.Contains("Farm Tile"))
        {
            Debug.Log(
                "Player is interacting with Tile " +
                other.name
            );

            FarmTileControl tile =
                other.GetComponent<FarmTileControl>();

            if (tile != null)
            {
                tile.EnterTile();
            }

            // Remember the tile the player entered
            curTile = other.gameObject;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.tag.Contains("Farm Tile"))
        {
            Debug.Log(
                "Player is leaving Tile " +
                other.name
            );

            FarmTileControl tile =
                other.GetComponent<FarmTileControl>();

            if (tile != null)
            {
                tile.ExitTile();
            }

            // FIX: Stop remembering a tile after leaving it
            if (curTile == other.gameObject)
            {
                curTile = null;
            }
        }
    }

    // Return the currently detected farm tile
    public GameObject GetCurrentTile()
    {
        return curTile;
    }

    private void OnDestroy()
    {
        // Clean up the input event subscription
        if (interactAction != null)
        {
            interactAction.performed -= OnTileInteractPerformed;
        }
    }
}
