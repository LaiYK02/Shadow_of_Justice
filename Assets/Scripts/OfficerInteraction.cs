using System;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class OfficerInteraction : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private DialogueManager dialogueManager;
    [Tooltip("Assign WorldSpaceCanvas or the ButtonHint object.")]
    [SerializeField] private GameObject interactionPrompt;

    [Header("Interaction Settings")]
    [SerializeField, Min(0.1f)] private float interactionRange = 3f;
    [Tooltip("How wide the camera aiming cone is. Increase this to make aiming easier.")]
    [SerializeField, Range(1f, 45f)] private float aimAngle = 18f;
    [SerializeField] private LayerMask raycastLayers = ~0;

    [Header("First Conversation")]
    [SerializeField]
    private DialogueLine[] firstConversation =
    {
        new DialogueLine
        {
            speakerName = "Fellow Officer",
            dialogueText = "Welcome to the district. Since this is your first day, I'll brief you on your first assignment."
        },
        new DialogueLine
        {
            speakerName = "Fellow Officer",
            dialogueText = "Your task is to begin investigating a report and gather the facts before drawing conclusions. Follow the evidence and remember that the law applies to everyone."
        },
        new DialogueLine
        {
            speakerName = "Fellow Officer",
            dialogueText = "When you're ready, start by checking the nearby area. I'll be here if you need a reminder."
        }
    };

    [Header("Repeat Conversation")]
    [SerializeField]
    private DialogueLine[] repeatConversation =
    {
        new DialogueLine
        {
            speakerName = "Fellow Officer",
            dialogueText = "To recap: investigate the report, check the nearby area, and gather evidence before making any conclusions. Follow the law and let the facts guide you."
        }
    };

    private bool firstConversationCompleted;
    private bool waitingForFirstConversationToEnd;
    private Collider officerCollider;

    private void Awake()
    {
        officerCollider = GetComponentInChildren<Collider>();

        if (interactionPrompt != null)
            interactionPrompt.SetActive(false);
    }

    private void Start()
    {
        if (player == null)
            Debug.LogError("OfficerInteraction: Assign Player in the Inspector.", this);
        if (playerCamera == null)
            Debug.LogError("OfficerInteraction: Assign Main Camera in the Inspector.", this);
        if (dialogueManager == null)
            Debug.LogError("OfficerInteraction: Assign DialogueManager in the Inspector.", this);
        if (interactionPrompt == null)
            Debug.LogWarning("OfficerInteraction: Assign WorldSpaceCanvas or ButtonHint to Interaction Prompt.", this);
        if (officerCollider == null)
            Debug.LogError("OfficerInteraction: Add a non-trigger Collider to FellowOfficer.", this);
    }

    private void OnEnable()
    {
        if (dialogueManager != null)
            dialogueManager.DialogueEnded += HandleDialogueEnded;
    }

    private void OnDisable()
    {
        if (dialogueManager != null)
            dialogueManager.DialogueEnded -= HandleDialogueEnded;
    }

    private void Update()
    {
        if (player == null || playerCamera == null || dialogueManager == null || officerCollider == null)
        {
            SetPromptVisible(false);
            return;
        }

        if (dialogueManager.IsPlaying)
        {
            SetPromptVisible(false);
            return;
        }

        bool inRange = Vector3.Distance(player.position, officerCollider.bounds.center) <= interactionRange;
        bool lookingAtOfficer = inRange && IsLookingAtOfficer();

        SetPromptVisible(lookingAtOfficer);

        if (lookingAtOfficer && InteractPressed())
        {
            SetPromptVisible(false);

            if (firstConversationCompleted)
            {
                dialogueManager.StartDialogue(repeatConversation);
            }
            else
            {
                waitingForFirstConversationToEnd = true;
                dialogueManager.StartDialogue(firstConversation);
            }
        }
    }

    private bool IsLookingAtOfficer()
    {
        Vector3 targetPoint = officerCollider.bounds.center;
        Vector3 toTarget = targetPoint - playerCamera.transform.position;

        if (toTarget.sqrMagnitude < 0.001f)
            return true;

        // A wider cone means players do not need to aim at one exact pixel.
        float angle = Vector3.Angle(playerCamera.transform.forward, toTarget);
        if (angle > aimAngle)
            return false;

        // Ensure the officer is not hidden behind a wall or another object.
        Vector3 direction = toTarget.normalized;
        float distance = toTarget.magnitude;
        RaycastHit[] hits = Physics.RaycastAll(
            playerCamera.transform.position,
            direction,
            distance + 0.2f,
            raycastLayers,
            QueryTriggerInteraction.Ignore
        );

        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null)
                continue;

            Transform hitTransform = hit.collider.transform;

            if (player != null &&
                (hitTransform == player || hitTransform.IsChildOf(player)))
                continue;

            if (interactionPrompt != null &&
                (hitTransform == interactionPrompt.transform ||
                 hitTransform.IsChildOf(interactionPrompt.transform)))
                continue;

            return hitTransform == transform || hitTransform.IsChildOf(transform);
        }

        // If the ray ends at the officer's collider boundary, accept it.
        return false;
    }

    private void HandleDialogueEnded()
    {
        if (!waitingForFirstConversationToEnd)
            return;

        waitingForFirstConversationToEnd = false;
        firstConversationCompleted = true;
    }

    private void SetPromptVisible(bool visible)
    {
        if (interactionPrompt == null)
            return;

        if (interactionPrompt.activeSelf != visible)
            interactionPrompt.SetActive(visible);

        if (visible && playerCamera != null)
            interactionPrompt.transform.rotation = playerCamera.transform.rotation;
    }

    private bool InteractPressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.E))
            return true;
#endif
        return false;
    }
}
