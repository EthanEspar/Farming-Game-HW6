using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class PlayerInteractPoint:MonoBehaviour
{
    [SerializeField]
    private GameObject curTile;
    private InputAction interactAction;

    private bool curInteractionTriggered= false;//each space press might trigger multiple times across multiple frames
    private StaminaBarScript stamina;

    private void Start()
    {
        interactAction = InputSystem.actions.FindAction("Jump");
        //register OnTileInteractPerformed to the action so it gets called when the space key is pressed
        interactAction.performed += OnTileInteractPerformed;

        // find stamina manager in scene
        stamina = FindObjectOfType<StaminaBarScript>();
    }
    //this callback method will make sure each Space Key press only call the InteractWithFarmTile() once
    //do not use Update() and check Action.IsPressed() since this function can trigger multiple times 
    //across multiple frames each time the player press the Space Key
    private void OnTileInteractPerformed(InputAction.CallbackContext context)
    {
        Debug.Log("Space is pressed");//player press space to interact with the current tile
        if (curTile == null) return;
        var tile = curTile.GetComponent<FarmTileControl>();
        if (tile == null) return;

        // consume stamina for every space press before interacting
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
