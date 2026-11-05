
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    private static readonly int IsMovingParameter = Animator.StringToHash("IsMoving");
    private static readonly int IsSprintingParameter = Animator.StringToHash("IsSprinting");
    private static readonly int IsAttackingParameter = Animator.StringToHash("IsAttacking");

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
    [SerializeField] private PlayerAttackHitbox attackHitbox;
    [SerializeField, Min(0f)] private float attackEnergyCost = 0.25f;
    [SerializeField, Min(0.01f)] private float attackDuration = 1f;
    [SerializeField] private float gravity = -20f;

    private CharacterController characterController;
    private Animator playerAnimator;
    private float verticalVelocity;
    private bool controlsUnlocked;
    private bool hasStarted;
    private bool isAttacking;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        playerAnimator = GetComponentInChildren<Animator>(true);

        if (playerAnimator == null)
            Debug.LogWarning("PlayerMovement: No child Animator was found; player movement will continue without animations.", this);
    }

    private void OnEnable()
    {
        if (hasStarted)
            controlsUnlocked = dialogueManager == null || !dialogueManager.IsPlaying;

        if (dialogueManager == null)
            return;

        dialogueManager.DialogueStarted += HandleDialogueStarted;
        dialogueManager.DialogueEnded += HandleDialogueEnded;
    }

    private void Start()
    {
        // Keep movement disabled until the opening dialogue finishes.
        controlsUnlocked = false;
        hasStarted = true;
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
        {
            UpdateAnimationParameters(false, false, false);
            return;
        }

        if (AttackPressed() && !isAttacking)
        {
            TryStartAttack();
            if (isAttacking)
                return;
        }

        if (isAttacking)
        {
            UpdateAnimationParameters(false, false, true);
            return;
        }

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

        if (moveDirection.sqrMagnitude > 0.001f)
        {
            Quaternion facingDirection = Quaternion.LookRotation(moveDirection, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                facingDirection,
                turnSpeed * Time.deltaTime);
        }

        if (characterController.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;

        verticalVelocity += gravity * Time.deltaTime;

        bool tryingToSprint = SprintPressed() && input.sqrMagnitude > 0.001f;
        bool hasEnergy = energyBar == null || energyBar.value > 0f;
        bool sprinting = tryingToSprint && hasEnergy;
        bool isMoving = input.sqrMagnitude > 0.001f;

        UpdateAnimationParameters(isMoving, sprinting, false);

        if (energyBar != null)
        {
            float energyChange = sprinting
                ? -energyDrainPerSecond * Time.deltaTime
                : energyRecoveryPerSecond * Time.deltaTime;
            energyBar.value = Mathf.Clamp(energyBar.value + energyChange,
                energyBar.minValue, energyBar.maxValue);
        }

        float currentSpeed = sprinting ? sprintSpeed : moveSpeed;
        Vector3 movement =
            moveDirection * currentSpeed +
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

    private void TryStartAttack()
    {
        if (attackHitbox == null || energyBar == null || energyBar.value < attackEnergyCost)
            return;

        energyBar.value = Mathf.Max(energyBar.minValue, energyBar.value - attackEnergyCost);
        StartCoroutine(AttackRoutine());
    }

    private IEnumerator AttackRoutine()
    {
        isAttacking = true;
        UpdateAnimationParameters(false, false, true);
        attackHitbox.SetAttackActive(true);
        yield return new WaitForSeconds(attackDuration);
        if (attackHitbox != null)
            attackHitbox.SetAttackActive(false);
        isAttacking = false;
        UpdateAnimationParameters(false, false, false);
    }

    private void HandleDialogueStarted()
    {
        controlsUnlocked = false;
        if (attackHitbox != null)
            attackHitbox.SetAttackActive(false);
        isAttacking = false;
        UpdateAnimationParameters(false, false, false);
    }

    private void HandleDialogueEnded()
    {
        controlsUnlocked = true;
    }

    private void UpdateAnimationParameters(bool isMoving, bool isSprinting, bool isAttackingNow)
    {
        if (playerAnimator == null)
            return;

        playerAnimator.SetBool(IsMovingParameter, isMoving);
        playerAnimator.SetBool(IsSprintingParameter, isSprinting);
        playerAnimator.SetBool(IsAttackingParameter, isAttackingNow);
    }
}
