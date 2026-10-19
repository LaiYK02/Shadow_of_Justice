
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform cameraTarget;
    [SerializeField] private DialogueManager dialogueManager;

    [Header("Movement")]
    [SerializeField, Min(0f)] private float moveSpeed = 5f;
    [SerializeField] private float gravity = -20f;

    private CharacterController characterController;
    private float verticalVelocity;
    private bool controlsUnlocked;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
    }

    private void OnEnable()
    {
        if (dialogueManager == null)
            return;

        dialogueManager.DialogueStarted += HandleDialogueStarted;
        dialogueManager.DialogueEnded += HandleDialogueEnded;
    }

    private void Start()
    {
        // Keep movement disabled until the opening dialogue finishes.
        controlsUnlocked = false;
    }

    private void OnDisable()
    {
        if (dialogueManager == null)
            return;

        dialogueManager.DialogueStarted -= HandleDialogueStarted;
        dialogueManager.DialogueEnded -= HandleDialogueEnded;
    }

    private void Update()
    {
        if (!controlsUnlocked || cameraTarget == null)
            return;

        Vector2 input = ReadMovementInput();

        // Use the camera direction, but ignore its vertical angle.
        Vector3 forward = cameraTarget.forward;
        Vector3 right = cameraTarget.right;

        forward.y = 0f;
        right.y = 0f;

        forward.Normalize();
        right.Normalize();

        Vector3 moveDirection =
            forward * input.y + right * input.x;

        // Prevent diagonal movement from being faster.
        moveDirection = Vector3.ClampMagnitude(moveDirection, 1f);

        if (characterController.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 movement =
            moveDirection * moveSpeed +
            Vector3.up * verticalVelocity;

        characterController.Move(movement * Time.deltaTime);
    }

    private Vector2 ReadMovementInput()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            Vector2 input = Vector2.zero;

            if (Keyboard.current.wKey.isPressed) input.y += 1f;
            if (Keyboard.current.sKey.isPressed) input.y -= 1f;
            if (Keyboard.current.dKey.isPressed) input.x += 1f;
            if (Keyboard.current.aKey.isPressed) input.x -= 1f;

            return Vector2.ClampMagnitude(input, 1f);
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        return Vector2.ClampMagnitude(
            new Vector2(
                Input.GetAxisRaw("Horizontal"),
                Input.GetAxisRaw("Vertical")
            ),
            1f
        );
#else
        return Vector2.zero;
#endif
    }

    private void HandleDialogueStarted()
    {
        controlsUnlocked = false;
    }

    private void HandleDialogueEnded()
    {
        controlsUnlocked = true;
    }
}
