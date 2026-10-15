using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using System;

public class PlayerInteractPoint:MonoBehaviour
{
    [SerializeField]
    private GameObject curTile;
    private InputAction interactAction;

    private bool curInteractionTriggered= false;//each space press might trigger multiple times across multiple frames
    private StaminaBarScript stamina;
    [SerializeField]
    private Animator animator;

    [Tooltip("Minimum time between interactions in seconds to avoid double-triggering")]
    public float interactionDebounce = 0.25f;
    private float lastInteractionTime = -10f;

    private void Start()
    {
        interactAction = InputSystem.actions.FindAction("Jump");
        //register OnTileInteractPerformed to the action so it gets called when the space key is pressed
        if (interactAction != null)
            interactAction.performed += OnTileInteractPerformed;

        // find stamina manager in scene
        stamina = FindObjectOfType<StaminaBarScript>();
        // try to find an Animator on the player (or its children) to trigger interaction animations
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (animator != null) Debug.Log("PlayerInteractPoint: found Animator -> " + animator.gameObject.name);
    }

    // Event invoked after a tile has been interacted with and its state updated
    public event Action<GameObject> AfterTileInteracted;

    //this callback method will make sure each Space Key press only call the InteractWithFarmTile() once
    //do not use Update() and check Action.IsPressed() since this function can trigger multiple times 
    //across multiple frames each time the player press the Space Key
    private void OnTileInteractPerformed(InputAction.CallbackContext context)
    {
        PerformInteraction();
    }

    // Perform interaction logic used by multiple input paths (space action, E action, legacy E key)
    // Keep PerformInteraction public so other scripts can trigger an interaction (e.g. on E key)
    public void PerformInteraction()
    {
        // debounce to avoid multiple components or duplicate callbacks triggering multiple interactions
        if (Time.time - lastInteractionTime < interactionDebounce)
        {
            Debug.Log("Interact ignored due to debounce");
            return;
        }
        lastInteractionTime = Time.time;

        Debug.Log("Interact pressed");
        if (curTile == null) return;
        var tile = curTile.GetComponent<FarmTileControl>();
        if (tile == null) return;

        // consume stamina for every interaction
        if (stamina != null)
        {
            if (!stamina.TryConsume(stamina.waterDrainAmount))
            {
                Debug.Log("Not enough stamina to perform action.");
                return;
            }
        }

        // perform the interaction
        tile.InteractWithFarmTile();
        // notify listeners that the interaction finished and the tile state may have changed
        AfterTileInteracted?.Invoke(curTile);
        // trigger character interaction animation if available
        try
        {
            if (animator != null)
            {
                animator.SetTrigger("InteractTrigger");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("PlayerInteractPoint: failed to set InteractTrigger on animator: " + ex.Message);
        }
    }

    private void Update()
    {
        // Use the Input System keyboard for legacy key fallbacks (no direct use of UnityEngine.Input)
        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.spaceKey.wasPressedThisFrame)
                PerformInteraction();
            if (keyboard.eKey.wasPressedThisFrame)
                PerformInteraction();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.tag.Contains("Farm Tile"))
        {
            Debug.Log("Player is interacting with Tile " + other.name);
            other.GetComponent<FarmTileControl>().EnterTile();//call the tile object's public API to interact with it
            curTile = other.gameObject;//store which Tile the player is interacting with
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.tag.Contains("Farm Tile"))
        {
            Debug.Log("Player is leaving Tile " + other.name);
            other.GetComponent<FarmTileControl>().ExitTile();
        }
    }

    // Provide read-only access to the current tile the player is standing on
    public GameObject GetCurrentTile()
    {
        return curTile;
    }
}
