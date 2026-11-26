using UnityEngine;
using UnityEngine.AI;

/// <summary>Controls navigation and melee attacks after an enemy joins a fight.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyCombatController : MonoBehaviour
{
    private const float RepathInterval = 0.2f;
    private const float AttackFacingTolerance = 12f;

    [SerializeField] private CharacterCombat combat;
    [SerializeField] private DialogueManager dialogueManager;
    [SerializeField] private Canvas healthDisplay;
    [SerializeField, Min(0.1f)] private float attackRange = 1f;
    [SerializeField, Min(0.1f)] private float attackDuration = 1f;
    [SerializeField, Min(0f)] private float attackCooldown = 0.65f;

    private NavMeshAgent agent;
    private CharacterCombat target;
    private float nextRepathTime;
    private float nextAttackTime;

    public bool IsFighting => target != null;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false;
        agent.enabled = false;
        if (healthDisplay != null)
            healthDisplay.enabled = false;
    }

    public bool TryBeginFight(CharacterCombat opponent)
    {
        if (IsFighting || opponent == null || opponent == combat || opponent.IsDead ||
            combat == null || combat.IsDead || !isActiveAndEnabled)
            return false;

        agent.enabled = true;
        if (!agent.isOnNavMesh)
        {
            agent.enabled = false;
            Debug.LogError("The enemy could not join the fight because it is outside the NavMesh.", this);
            return false;
        }

        target = opponent;
        nextRepathTime = Time.time;
        nextAttackTime = Time.time;
        if (healthDisplay != null)
            healthDisplay.enabled = true;
        return true;
    }

    private void Update()
    {
        if (Time.timeScale <= 0f)
            return;

        if (target == null || target.IsDead || combat == null || combat.IsDead)
        {
            EndFight();
            return;
        }

        bool inDialogue = dialogueManager != null && dialogueManager.IsPlaying;
        if (healthDisplay != null)
            healthDisplay.enabled = !inDialogue;

        if (inDialogue)
            combat.CancelActions();
        if (inDialogue || !agent.isOnNavMesh || !combat.CanAct || combat.IsAttacking)
        {
            StopMoving();
            return;
        }

        Vector3 targetOffset = target.transform.position - transform.position;
        float heightDifference = Mathf.Abs(targetOffset.y + agent.baseOffset);
        targetOffset.y = 0f;
        if (targetOffset.sqrMagnitude <= attackRange * attackRange && heightDifference <= attackRange)
        {
            StopMoving();
            Face(targetOffset);
            if (Time.time >= nextAttackTime &&
                Vector3.Angle(transform.forward, targetOffset) <= AttackFacingTolerance &&
                combat.TryAttack(attackDuration))
                nextAttackTime = Time.time + attackDuration + attackCooldown;
            return;
        }

        agent.isStopped = false;
        if (Time.time >= nextRepathTime)
        {
            nextRepathTime = Time.time + RepathInterval;
            var filter = new NavMeshQueryFilter { agentTypeID = agent.agentTypeID, areaMask = agent.areaMask };
            if (NavMesh.SamplePosition(target.transform.position, out NavMeshHit point, 2f, filter))
                agent.SetDestination(point.position);
            else
                agent.ResetPath();
        }

        Vector3 velocity = agent.velocity;
        velocity.y = 0f;
        bool moving = velocity.sqrMagnitude > 0.04f;
        if (moving)
            Face(velocity);
        combat.SetLocomotion(moving, moving);
    }

    private void LateUpdate()
    {
        if (healthDisplay != null && healthDisplay.enabled && healthDisplay.worldCamera != null)
            healthDisplay.transform.rotation = healthDisplay.worldCamera.transform.rotation;
    }

    private void OnDisable() => EndFight();

    private void EndFight()
    {
        target = null;
        if (agent != null && agent.enabled)
        {
            agent.enabled = false;
            if (combat != null)
                combat.CancelActions();
        }
        if (healthDisplay != null)
            healthDisplay.enabled = false;
    }

    private void StopMoving()
    {
        if (agent.isOnNavMesh)
            agent.isStopped = true;
        combat.SetLocomotion(false, false);
    }

    private void Face(Vector3 direction)
    {
        if (direction.sqrMagnitude <= 0.0001f)
            return;

        transform.rotation = Quaternion.RotateTowards(transform.rotation,
            Quaternion.LookRotation(direction), agent.angularSpeed * Time.deltaTime);
    }
}
