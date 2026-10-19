
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class ThirdPersonCameraLook : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform cameraTarget;
    [SerializeField] private DialogueManager dialogueManager;

    [Header("Mouse Settings")]
    [SerializeField, Min(0f)] private float sensitivity = 0.12f;
    [SerializeField] private float minimumPitch = -30f;
    [SerializeField] private float maximumPitch = 65f;
    [SerializeField] private bool invertY;

    private float yaw;
    private float pitch;
    private bool controlsUnlocked;

    private void Start()
    {
        if (cameraTarget == null)
        {
            Debug.LogError(
                "ThirdPersonCameraLook: Camera Target is missing."
            );
            enabled = false;
            return;
        }

        // Preserve the target's starting orientation.
        Vector3 angles = cameraTarget.localEulerAngles;

        yaw = angles.y;
        pitch = angles.x;

        if (pitch > 180f)
            pitch -= 360f;

        pitch = Mathf.Clamp(pitch, minimumPitch, maximumPitch);

        controlsUnlocked = false;
        SetCursorState(false);
    }

    private void OnEnable()
    {
        if (dialogueManager == null)
            return;

        dialogueManager.DialogueStarted += HandleDialogueStarted;
        dialogueManager.DialogueEnded += HandleDialogueEnded;
    }

    private void OnDisable()
    {
        if (dialogueManager != null)
        {
            dialogueManager.DialogueStarted -= HandleDialogueStarted;
            dialogueManager.DialogueEnded -= HandleDialogueEnded;
        }

        SetCursorState(false);
    }

    private void Update()
    {
        if (!controlsUnlocked || cameraTarget == null)
            return;

        Vector2 mouseDelta = ReadMouseDelta();

        yaw += mouseDelta.x * sensitivity;

        float verticalInput = invertY
            ? mouseDelta.y
            : -mouseDelta.y;

        pitch += verticalInput * sensitivity;
        pitch = Mathf.Clamp(pitch, minimumPitch, maximumPitch);

        // Rotate only the camera target, not the Player model.
        cameraTarget.localRotation =
            Quaternion.Euler(pitch, yaw, 0f);
    }

    private Vector2 ReadMouseDelta()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
            return Mouse.current.delta.ReadValue();
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        return new Vector2(
            Input.GetAxis("Mouse X"),
            Input.GetAxis("Mouse Y")
        );
#else
        return Vector2.zero;
#endif
    }

    private void HandleDialogueStarted()
    {
        controlsUnlocked = false;
        SetCursorState(false);
    }

    private void HandleDialogueEnded()
    {
        controlsUnlocked = true;
        SetCursorState(true);
    }

    private void SetCursorState(bool locked)
    {
        Cursor.lockState = locked
            ? CursorLockMode.Locked
            : CursorLockMode.None;

        Cursor.visible = !locked;
    }
}
