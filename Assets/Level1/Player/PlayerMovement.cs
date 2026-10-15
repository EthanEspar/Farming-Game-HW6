using UnityEngine;
using UnityEngine.InputSystem;

// Simple player movement using CharacterController and the new Input System.
// - Reads the "Move" Input Action (Vector2)
// - Moves the CharacterController and sets Animator bool (IsMoving)
// - Rotate the model to face movement direction
public class PlayerMovement : MonoBehaviour
{
    [Tooltip("CharacterController on the player (required)")]
    public CharacterController controller;

    [Tooltip("Animator to update (optional). Will set the boolean parameter named moveBoolName when moving.")]
    public Animator animator;

    [Tooltip("Name of the Animator bool parameter used for movement (default: IsMoving)")]
    public string moveBoolName = "IsMoving";

    [Tooltip("Movement speed in units per second")]
    public float moveSpeed = 3.5f;

    [Tooltip("Rotate smoothly toward movement direction")]
    public float rotationSpeed = 10f;

    private InputAction moveAction;
    private Vector2 moveInput = Vector2.zero;
    [Tooltip("Enable verbose debug logs for movement input (helps diagnose input issues)")]
    public bool debugLogs = false;

    void Start()
    {
        // try to auto-find a CharacterController on this object or nearby
        if (controller == null)
            controller = GetComponent<CharacterController>();
        if (controller == null)
            controller = GetComponentInChildren<CharacterController>(true);
        if (controller == null)
            controller = GetComponentInParent<CharacterController>();

        // final fallback: find any CharacterController in the scene (not ideal)
        if (controller == null)
            controller = FindObjectOfType<CharacterController>();

        if (controller == null)
            Debug.LogWarning($"PlayerMovement.Start: CharacterController not found on '{gameObject.name}' or nearby. Please assign in inspector.");
        else
            Debug.Log($"PlayerMovement.Start: using CharacterController on '{controller.gameObject.name}'");

        // find the Move action if present
        moveAction = InputSystem.actions.FindAction("Move");
        Debug.Log($"PlayerMovement.Start on '{gameObject.name}': controller={(controller!=null)}, moveActionFound={(moveAction!=null)}");
    }

    void OnEnable()
    {
        Debug.Log($"PlayerMovement.OnEnable on '{gameObject.name}'");
    }

    void Update()
    {
        // read input either from Move action or legacy WASD
        if (moveAction != null)
        {
            moveInput = moveAction.ReadValue<Vector2>();
        }
        else
        {
            // Try new Input System keyboard as fallback
            var kb = Keyboard.current;
            if (kb != null)
            {
                float x = 0f, y = 0f;
                if (kb.aKey.isPressed) x -= 1f;
                if (kb.dKey.isPressed) x += 1f;
                if (kb.sKey.isPressed) y -= 1f;
                if (kb.wKey.isPressed) y += 1f;
                moveInput = new Vector2(x, y);
            }
            else
            {
                // legacy Input manager fallback
                float x = Input.GetAxisRaw("Horizontal");
                float y = Input.GetAxisRaw("Vertical");
                moveInput = new Vector2(x, y);
            }
        }

        Vector3 move = new Vector3(moveInput.x, 0f, moveInput.y);
        if (move.sqrMagnitude > 1f) move.Normalize();

        Vector3 worldMove = transform.TransformDirection(move) * moveSpeed * Time.deltaTime;

        if (controller != null)
        {
            controller.Move(worldMove);
        }
        else
        {
            // fallback: move transform directly
            transform.position += worldMove;
        }

        // rotation to face movement direction (camera-relative if desired)
        if (move.sqrMagnitude > 0.001f)
        {
            Vector3 lookDir = new Vector3(move.x, 0f, move.y);
            Quaternion targetRot = Quaternion.LookRotation(lookDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }

        // update animator
        bool isMoving = move.sqrMagnitude > 0.01f;
        if (debugLogs)
        {
            Debug.Log($"PlayerMovement: moveInput={moveInput} isMoving={isMoving} moveActionFound={(moveAction!=null)}");
        }
        if (animator != null && !string.IsNullOrEmpty(moveBoolName))
            animator.SetBool(moveBoolName, isMoving);
    }
}
