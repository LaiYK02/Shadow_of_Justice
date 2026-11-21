using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>Selects and activates exactly one eligible interactable.</summary>
public class PlayerInteractionManager : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private Camera playerCamera;
    [SerializeField, Range(1f, 45f)] private float coneHalfAngle = 45f;
    [SerializeField] private LayerMask raycastLayers = ~0;
    [SerializeField] private KeyCode interactionKey = KeyCode.E;

    private PlayerInteractable selected;
    private PlayerInteractable promptTarget;
    private CharacterCombat playerCombat;
    public PlayerInteractable Selected => selected;

    private void Start()
    {
        if (player == null) Debug.LogError("PlayerInteractionManager: Assign Player.", this);
        else playerCombat = player.GetComponent<CharacterCombat>();
        if (playerCamera == null) Debug.LogError("PlayerInteractionManager: Assign Player Camera.", this);
    }

    private void Update()
    {
        if (playerCombat != null && playerCombat.IsDead)
        {
            selected = null;
            SetPromptTarget(null);
            return;
        }

        SelectBest();
        if (selected != null && InteractPressed())
        {
            PlayerInteractable target = selected;
            selected = null;
            SetPromptTarget(null);
            if (target != null && target.isActiveAndEnabled && target.CanInteract) target.Interact();
        }
    }

    private void SelectBest()
    {
        PlayerInteractable best = null;
        PlayerInteractable bestHint = null;
        float bestAngle = coneHalfAngle;
        float bestHintAngle = coneHalfAngle;
        if (player != null && playerCamera != null)
        {
            var candidates = PlayerInteractable.ActiveInteractables;
            for (int i = 0; i < candidates.Count; i++)
            {
                PlayerInteractable candidate = candidates[i];
                if (candidate == null || !candidate.isActiveAndEnabled || !candidate.CanInteract || candidate.TargetCollider == null) continue;

                Vector3 point = candidate.TargetCollider.bounds.center;
                Vector3 toTarget = point - playerCamera.transform.position;
                float distanceFromPlayer = Vector3.Distance(player.position, point);
                if (distanceFromPlayer > candidate.InteractionRange || toTarget.sqrMagnitude < 0.0001f) continue;

                float angle = Vector3.Angle(playerCamera.transform.forward, toTarget);
                if (angle <= bestHintAngle)
                {
                    bestHint = candidate;
                    bestHintAngle = angle;
                }

                if (angle > bestAngle) continue;
                if (!HasLineOfSight(candidate, toTarget)) continue;
                best = candidate;
                bestAngle = angle;
            }
        }
        selected = best;
        // Keep a blocked target's hint visible without allowing interaction through the obstacle.
        SetPromptTarget(best ?? bestHint);
    }

    private bool HasLineOfSight(PlayerInteractable target, Vector3 toTarget)
    {
        RaycastHit[] hits = Physics.RaycastAll(playerCamera.transform.position, toTarget.normalized,
            toTarget.magnitude + 0.2f, raycastLayers, QueryTriggerInteraction.Ignore);
        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null) continue;
            Transform t = hit.collider.transform;
            if (t == player || t.IsChildOf(player)) continue;
            GameObject prompt = target.Prompt;
            if (prompt != null && (t == prompt.transform || t.IsChildOf(prompt.transform))) continue;
            return target.IsTargetCollider(hit.collider);
        }
        return false;
    }

    private void SetPromptTarget(PlayerInteractable next)
    {
        if (promptTarget != next)
        {
            if (promptTarget != null) promptTarget.SetPromptVisible(false);
            promptTarget = next;
        }
        if (promptTarget != null) promptTarget.SetPromptVisible(true, playerCamera);
    }

    private void OnDisable()
    {
        selected = null;
        SetPromptTarget(null);
    }

    private bool InteractPressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (interactionKey == KeyCode.E && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(interactionKey)) return true;
#endif
        return false;
    }
}
