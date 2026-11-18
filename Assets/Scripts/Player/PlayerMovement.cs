using UnityEngine;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(CharacterCombat))]
public class PlayerMovement : MonoBehaviour
{
    private const float HoldBlockThreshold = 0.15f;

    [Header("References")]
    [SerializeField] private Transform cameraTarget;
    [SerializeField] private DialogueManager dialogueManager;

    [Header("Movement")]
    [SerializeField, Min(0f)] private float moveSpeed = 5f;
    [SerializeField, Min(0f)] private float sprintSpeed = 10f;
    [SerializeField, Min(0f)] private float turnSpeed = 720f;
    [SerializeField] private Slider energyBar;
    [SerializeField, Min(0f)] private float energyDrainPerSecond = 0.2f;
    [SerializeField, Min(0f)] private float energyRecoveryPerSecond = 0.12f;
    [SerializeField] private float gravity = -20f;

    [Header("Combat Energy")]
    [SerializeField, Min(0f)] private float attackEnergyCost = 0.25f;
    [SerializeField, Min(0.01f)] private float attackDuration = 1f;
    [SerializeField, Range(0f, 1f)] private float normalBlockEnergyCost = 0.15f;
    [SerializeField, Range(0f, 1f)] private float perfectBlockEnergyRefund = 0.25f;

    private CharacterController characterController;
    private CharacterCombat combat;
    private float verticalVelocity;
    private bool controlsUnlocked;
    private bool hasStarted;
    private bool blockPressPending;
    private float blockPressedAt;

    internal void FaceForCutscene(Vector3 direction)
    {
        // Camera look is disabled during the cutscene, so preserve its world rotation here.
        Quaternion cameraRotation = cameraTarget != null ? cameraTarget.rotation : Quaternion.identity;
        transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        if (cameraTarget != null)
            cameraTarget.rotation = cameraRotation;
    }

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        combat = GetComponent<CharacterCombat>();
    }

    private void OnEnable()
    {
        combat.PerfectBlocked += RestorePerfectBlockEnergy;
        combat.GuardBlocked += ConsumeGuardEnergy;
        if (hasStarted)
            controlsUnlocked = dialogueManager == null || !dialogueManager.IsPlaying;

        if (dialogueManager == null)
            return;

        dialogueManager.DialogueStarted += HandleDialogueStarted;
        dialogueManager.DialogueEnded += HandleDialogueEnded;
    }

    private void Start()
    {
        // CentralDistrict unlocks controls after the opening dialogue.
        controlsUnlocked = dialogueManager == null;
        hasStarted = true;
    }

    private void OnDisable()
    {
        combat.PerfectBlocked -= RestorePerfectBlockEnergy;
        combat.GuardBlocked -= ConsumeGuardEnergy;
        blockPressPending = false;
        combat.CancelActions();

        if (dialogueManager == null)
            return;

        dialogueManager.DialogueStarted -= HandleDialogueStarted;
        dialogueManager.DialogueEnded -= HandleDialogueEnded;
    }

    private void Update()
    {
        if (!controlsUnlocked || cameraTarget == null || combat.IsDead)
            return;

        UpdateBlock();
        if (!combat.IsBlocking && AttackPressed() && AvailableEnergy >= attackEnergyCost &&
            combat.TryAttack(attackDuration))
            ChangeEnergy(-attackEnergyCost);

        if (!combat.CanAct || combat.IsAttacking || combat.IsBlocking)
        {
            MoveWithGravity(Vector3.zero);
            return;
        }

        Vector2 input = ReadMovementInput();

        // Use the horizontal camera direction to move and face the player.
        Vector3 forward = cameraTarget.forward;
        Vector3 right = cameraTarget.right;
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        Vector3 moveDirection = Vector3.ClampMagnitude(forward * input.y + right * input.x, 1f);
        if (moveDirection.sqrMagnitude > 0.001f)
        {
            Quaternion facingDirection = Quaternion.LookRotation(moveDirection, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, facingDirection,
                turnSpeed * Time.deltaTime);
        }

        bool moving = input.sqrMagnitude > 0.001f;
        bool sprinting = moving && SprintPressed() && (energyBar == null || AvailableEnergy > 0f);
        combat.SetLocomotion(moving, sprinting);

        float energyChange = sprinting ? -energyDrainPerSecond : energyRecoveryPerSecond;
        ChangeEnergy(energyChange * Time.deltaTime);
        MoveWithGravity(moveDirection * (sprinting ? sprintSpeed : moveSpeed));
    }

    private void MoveWithGravity(Vector3 horizontalVelocity)
    {
        if (characterController.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;

        verticalVelocity += gravity * Time.deltaTime;
        Vector3 movement = horizontalVelocity + Vector3.up * verticalVelocity;
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
            new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")), 1f);
#else
        return Vector2.zero;
#endif
    }

    private bool SprintPressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null &&
            (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed))
            return true;
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
#else
        return false;
#endif
    }

    private bool AttackPressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            return true;
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetMouseButtonDown(0);
#else
        return false;
#endif
    }

    private void UpdateBlock()
    {
        ReadBlockInput(out bool held, out bool pressed);
        if (combat.IsGuarding && (!held || AvailableEnergy <= 0f))
            combat.EndBlock();

        if (!combat.CanAct || combat.IsAttacking || combat.IsBlocking || AvailableEnergy <= 0f)
        {
            blockPressPending = false;
            return;
        }

        if (pressed)
        {
            blockPressPending = true;
            blockPressedAt = Time.time;
        }

        if (!blockPressPending)
            return;

        bool reachedHoldThreshold = Time.time - blockPressedAt >= HoldBlockThreshold;
        if (held && reachedHoldThreshold)
        {
            blockPressPending = false;
            combat.TryBeginBlock();
        }
        else if (!held)
        {
            blockPressPending = false;
            if (!reachedHoldThreshold)
                combat.TryBeginParry();
        }
    }

    private static void ReadBlockInput(out bool held, out bool pressed)
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            held = Mouse.current.rightButton.isPressed;
            pressed = Mouse.current.rightButton.wasPressedThisFrame;
            return;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        held = Input.GetMouseButton(1);
        pressed = Input.GetMouseButtonDown(1);
#else
        held = false;
        pressed = false;
#endif
    }

    private void HandleDialogueStarted()
    {
        controlsUnlocked = false;
        blockPressPending = false;
        combat.CancelActions();
    }

    private void HandleDialogueEnded()
    {
        controlsUnlocked = true;
    }

    private float AvailableEnergy => energyBar == null ? 0f : energyBar.normalizedValue;

    private void ChangeEnergy(float amount)
    {
        if (energyBar != null)
            energyBar.normalizedValue = Mathf.Clamp01(energyBar.normalizedValue + amount);
    }

    private void RestorePerfectBlockEnergy()
    {
        ChangeEnergy(perfectBlockEnergyRefund);
    }

    private void ConsumeGuardEnergy()
    {
        ChangeEnergy(-normalBlockEnergyCost);
        if (AvailableEnergy <= 0f)
            combat.EndBlock();
    }
}
