
using System;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class MissionObjectiveInteractable : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private MissionFlowController missionFlow;
    [SerializeField] private Collider evidenceCollider;

    [Tooltip("Assign the World Space Canvas or ButtonHint object.")]
    [SerializeField] private GameObject interactionPrompt;

    [Header("Objective")]
    [SerializeField] private string objectiveId = "victim";

    [Header("Interaction Settings")]
    [SerializeField, Min(0.1f)] private float interactionRange = 2.5f;

    [Tooltip("How wide the camera aiming cone is.")]
    [SerializeField, Range(1f, 45f)] private float aimAngle = 45f;

    [SerializeField] private LayerMask raycastLayers = ~0;

    private bool completed;

    private void Awake()
    {
        if (evidenceCollider == null)
            evidenceCollider = GetComponentInChildren<Collider>();

        if (interactionPrompt != null)
            interactionPrompt.SetActive(false);
    }

    private void Start()
    {
        if (player == null)
            Debug.LogError("MissionObjectiveInteractable: Assign Player.", this);

        if (playerCamera == null)
            Debug.LogError("MissionObjectiveInteractable: Assign Main Camera.", this);

        if (missionFlow == null)
            Debug.LogError("MissionObjectiveInteractable: Assign MissionFlowController.", this);

        if (evidenceCollider == null)
            Debug.LogError("MissionObjectiveInteractable: Evidence needs a Collider.", this);

        if (interactionPrompt == null)
            Debug.LogWarning("MissionObjectiveInteractable: Assign an interaction prompt.", this);
    }

    private void Update()
    {
        if (completed ||
            player == null ||
            playerCamera == null ||
            missionFlow == null ||
            evidenceCollider == null)
        {
            SetPromptVisible(false);
            return;
        }

        // Evidence cannot be interacted with before Mission 1 starts.
        if (!missionFlow.CanInteract)
        {
            SetPromptVisible(false);
            return;
        }

        float distance = Vector3.Distance(
            player.position,
            evidenceCollider.bounds.center
        );

        bool inRange = distance <= interactionRange;
        bool lookingAtEvidence = inRange && IsLookingAtEvidence();

        SetPromptVisible(lookingAtEvidence);

        if (lookingAtEvidence && InteractPressed())
        {
            completed = true;

            SetPromptVisible(false);

            missionFlow.CompleteObjective(objectiveId);

            Debug.Log("Evidence inspected: " + objectiveId, this);
        }
    }

    private bool IsLookingAtEvidence()
    {
        Vector3 targetPoint = evidenceCollider.bounds.center;

        Vector3 toTarget =
            targetPoint - playerCamera.transform.position;

        if (toTarget.sqrMagnitude < 0.001f)
            return true;

        float angle = Vector3.Angle(
            playerCamera.transform.forward,
            toTarget
        );

        if (angle > aimAngle)
            return false;

        RaycastHit[] hits = Physics.RaycastAll(
            playerCamera.transform.position,
            toTarget.normalized,
            toTarget.magnitude + 0.2f,
            raycastLayers,
            QueryTriggerInteraction.Ignore
        );

        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null)
                continue;

            Transform hitTransform = hit.collider.transform;

            // Ignore the player and its child objects.
            if (hitTransform == player ||
                hitTransform.IsChildOf(player))
            {
                continue;
            }

            // Ignore the interaction prompt.
            if (interactionPrompt != null &&
                (hitTransform == interactionPrompt.transform ||
                 hitTransform.IsChildOf(interactionPrompt.transform)))
            {
                continue;
            }

            // The first object hit must belong to this evidence object.
            return hitTransform == transform ||
                   hitTransform.IsChildOf(transform);
        }

        return false;
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
        if (Keyboard.current != null &&
            Keyboard.current.eKey.wasPressedThisFrame)
        {
            return true;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.E))
            return true;
#endif

        return false;
    }
}
