using UnityEngine;
using UnityEngine.UI;

/// <summary>Shared health, combat state, and animation controls for a character.</summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(-10)]
public class CharacterCombat : MonoBehaviour
{
    // Match the fixed blend from Standing Block Idle to Idle in the Y Bot Animator.
    private const float BlockRecoveryDuration = 0.5f;

    private static readonly int Moving = Animator.StringToHash("IsMoving");
    private static readonly int Sprinting = Animator.StringToHash("IsSprinting");
    private static readonly int Attacking = Animator.StringToHash("IsAttacking");
    private static readonly int Talking = Animator.StringToHash("IsTalking");
    private static readonly int Blocking = Animator.StringToHash("IsBlocking");
    private static readonly int Stunned = Animator.StringToHash("IsStunned");
    private static readonly int HitReacting = Animator.StringToHash("IsHitReacting");
    private static readonly int Hit = Animator.StringToHash("Hit");
    private static readonly int Dead = Animator.StringToHash("IsDead");

    [SerializeField] private CharacterAttackHitbox attackHitbox;
    [SerializeField] private Slider healthBar;
    [SerializeField] private bool canTakeDamage = true;
    [SerializeField, Min(1f)] private float maxHealth = 100f;
    [SerializeField, Min(0.01f)] private float hitReactionDuration = 1f;
    [SerializeField, Min(0f)] private float perfectBlockWindow = 0.5f;
    [SerializeField, Min(0f)] private float perfectBlockStunDuration = 3f;

    private Animator animator;
    private bool isTalking;
    private bool perfectBlockAvailable;
    private float blockStartedAt;
    private float blockRecoveryEndsAt;
    private float attackEndsAt;
    private float hitReactionEndsAt;
    private float stunEndsAt;

    public event System.Action PerfectBlocked;

    public float Health { get; private set; }
    public bool IsDead => Health <= 0f;
    public bool IsBlocking { get; private set; }
    public bool IsAttacking => attackEndsAt > 0f;
    public bool IsHitReacting => hitReactionEndsAt > 0f;
    public bool IsStunned => stunEndsAt > 0f;
    public bool CanAct => isActiveAndEnabled && !IsDead && !IsStunned && !IsHitReacting &&
        !isTalking && Time.time >= blockRecoveryEndsAt;
    internal Transform ModelTransform => animator != null ? animator.transform : null;

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>(true);
        Health = maxHealth;
        UpdateHealthBar();
    }

    private void Update()
    {
        if (IsAttacking && Time.time >= attackEndsAt)
            CancelAttack();

        if (IsHitReacting && Time.time >= hitReactionEndsAt)
            ClearHitReaction();

        if (IsStunned && Time.time >= stunEndsAt)
        {
            stunEndsAt = 0f;
            SetAnimatorBool(Stunned, false);
        }
    }

    private void OnDisable()
    {
        CancelActions();
        SetTalking(false);
    }

    public void SetLocomotion(bool moving, bool sprinting)
    {
        SetAnimatorBool(Moving, moving);
        SetAnimatorBool(Sprinting, moving && sprinting);
    }

    public void SetTalking(bool talking)
    {
        isTalking = talking && !IsDead;
        if (isTalking)
            CancelActions();
        SetAnimatorBool(Talking, isTalking);
    }

    public bool TryAttack(float duration)
    {
        if (!CanAct || IsAttacking || IsBlocking || duration <= 0f ||
            attackHitbox == null || !attackHitbox.isActiveAndEnabled)
            return false;

        attackEndsAt = Time.time + duration;
        SetLocomotion(false, false);
        SetAnimatorBool(Attacking, true);
        attackHitbox.SetAttackActive(true);
        return true;
    }

    public bool TryBeginBlock()
    {
        if (!CanAct || IsAttacking || IsBlocking)
            return false;

        blockStartedAt = Time.time;
        perfectBlockAvailable = true;
        IsBlocking = true;
        SetLocomotion(false, false);
        SetAnimatorBool(Blocking, true);
        return true;
    }

    public void EndBlock()
    {
        if (!IsBlocking)
            return;

        IsBlocking = false;
        perfectBlockAvailable = false;
        blockRecoveryEndsAt = Time.time + BlockRecoveryDuration;
        SetAnimatorBool(Blocking, false);
    }

    public void CancelActions()
    {
        EndBlock();
        CancelAttack();
        SetLocomotion(false, false);
    }

    /// <returns>True if the hit dealt damage.</returns>
    public bool ReceiveHit(float damage, CharacterCombat attacker)
    {
        if (!isActiveAndEnabled || !canTakeDamage || IsDead || damage <= 0f || attacker == this)
            return false;

        if (IsBlocking)
        {
            if (perfectBlockAvailable && attacker != null && Time.time - blockStartedAt <= perfectBlockWindow)
            {
                perfectBlockAvailable = false;
                attacker.ApplyStun(perfectBlockStunDuration);
                PerfectBlocked?.Invoke();
            }

            return false;
        }

        Health = Mathf.Max(0f, Health - damage);
        UpdateHealthBar();
        CancelActions();

        if (IsDead)
        {
            ClearHitReaction();
            SetTalking(false);
            SetAnimatorBool(Dead, true);
        }
        else if (!IsStunned)
        {
            hitReactionEndsAt = Time.time + hitReactionDuration;
            SetAnimatorBool(HitReacting, true);
            if (HasAnimator)
                animator.SetTrigger(Hit);
        }

        return true;
    }

    private void ApplyStun(float duration)
    {
        if (!isActiveAndEnabled || IsDead || duration <= 0f)
            return;

        CancelActions();
        ClearHitReaction();
        stunEndsAt = Mathf.Max(stunEndsAt, Time.time + duration);
        SetAnimatorBool(Stunned, true);
    }

    private void CancelAttack()
    {
        if (!IsAttacking)
            return;

        attackEndsAt = 0f;
        if (attackHitbox != null)
            attackHitbox.SetAttackActive(false);
        SetAnimatorBool(Attacking, false);
    }

    private void ClearHitReaction()
    {
        hitReactionEndsAt = 0f;
        SetAnimatorBool(HitReacting, false);
        if (HasAnimator)
            animator.ResetTrigger(Hit);
    }

    private void UpdateHealthBar()
    {
        if (healthBar != null)
            healthBar.normalizedValue = Health / maxHealth;
    }

    private bool HasAnimator => animator != null && animator.runtimeAnimatorController != null;

    private void SetAnimatorBool(int parameter, bool value)
    {
        if (HasAnimator)
            animator.SetBool(parameter, value);
    }
}
