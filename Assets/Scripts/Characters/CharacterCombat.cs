using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Shared health, combat state, and animation controls for a character.</summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(-10)]
public class CharacterCombat : MonoBehaviour
{
    // Match the fixed blend from the block states to Idle in the Y Bot Animator.
    private const float BlockRecoveryDuration = 0.5f;
    private const float ParryEntryTimeout = 0.5f;
    private const float MinimumBlockDot = 0.70710678f;
    private const int FirstPerfectBlockFrame = 0;
    private const int LastPerfectBlockFrame = 30;
    private const int FirstAttackHitFrame = 22;
    private const int LastAttackHitFrame = 35;

    private static readonly int MeleeAttackState = Animator.StringToHash("Standing Melee Attack");
    private static readonly int ParryState = Animator.StringToHash("Blocking");
    private static readonly int ParryStatePath = Animator.StringToHash("Base Layer.Blocking");
    private static readonly int Moving = Animator.StringToHash("IsMoving");
    private static readonly int Sprinting = Animator.StringToHash("IsSprinting");
    private static readonly int Attacking = Animator.StringToHash("IsAttacking");
    private static readonly int Talking = Animator.StringToHash("IsTalking");
    private static readonly int Blocking = Animator.StringToHash("IsBlocking");
    private static readonly int Parrying = Animator.StringToHash("IsParrying");
    private static readonly int Stunned = Animator.StringToHash("IsStunned");
    private static readonly int HitReacting = Animator.StringToHash("IsHitReacting");
    private static readonly int Hit = Animator.StringToHash("Hit");
    private static readonly int Dead = Animator.StringToHash("IsDead");

    [SerializeField] private CharacterAttackHitbox attackHitbox;
    [SerializeField] private Slider healthBar;
    [SerializeField] private bool canTakeDamage = true;
    [SerializeField, Min(1f)] private float maxHealth = 100f;
    [SerializeField, Min(0.01f)] private float hitReactionDuration = 1f;
    [SerializeField, Min(0f)] private float perfectBlockStunDuration = 3f;

    private enum BlockMode { None, Guard, Parry }

    private readonly List<AnimatorClipInfo> stateClips = new(1);
    private Animator animator;
    private bool isTalking;
    private bool perfectBlockAvailable;
    private BlockMode blockMode;
    private float parryEntryDeadline;
    private float blockRecoveryEndsAt;
    private float attackEndsAt;
    private float hitReactionEndsAt;
    private float stunEndsAt;

    public event System.Action PerfectBlocked;
    public event System.Action GuardBlocked;

    public float Health { get; private set; }
    public bool IsDead => Health <= 0f;
    public bool IsBlocking => blockMode != BlockMode.None;
    internal bool IsGuarding => blockMode == BlockMode.Guard;
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
            ClearStun();

        if (blockMode == BlockMode.Parry)
        {
            bool isPlayingParry = TryGetAnimationState(ParryState, out var state);
            if (isPlayingParry ? state.normalizedTime >= 1f : Time.time >= parryEntryDeadline)
                EndBlock();
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
        if (!CanAct || IsAttacking || IsBlocking || duration <= 0f || !HasAnimator ||
            attackHitbox == null || !attackHitbox.isActiveAndEnabled)
            return false;

        attackEndsAt = Time.time + duration;
        SetLocomotion(false, false);
        SetAnimatorBool(Attacking, true);
        return true;
    }

    internal bool IsAttackHitWindowOpen()
    {
        return IsAttacking && CanAct &&
            IsWithinAnimationFrames(MeleeAttackState, FirstAttackHitFrame, LastAttackHitFrame);
    }

    private bool TryGetAnimationState(int stateHash, out AnimatorStateInfo state)
    {
        state = default;
        if (!HasAnimator)
            return false;
        const int layer = 0;
        state = animator.IsInTransition(layer)
            ? animator.GetNextAnimatorStateInfo(layer)
            : animator.GetCurrentAnimatorStateInfo(layer);
        return state.shortNameHash == stateHash;
    }

    private bool IsWithinAnimationFrames(int stateHash, int firstFrame, int lastFrame)
    {
        if (!TryGetAnimationState(stateHash, out var state))
            return false;

        const int layer = 0;
        if (animator.IsInTransition(layer))
            animator.GetNextAnimatorClipInfo(layer, stateClips);
        else
            animator.GetCurrentAnimatorClipInfo(layer, stateClips);
        if (stateClips.Count == 0)
            return false;

        AnimationClip clip = stateClips[0].clip;
        float sourceFrame = state.normalizedTime * Mathf.Round(clip.length * clip.frameRate);
        return sourceFrame >= firstFrame && sourceFrame < lastFrame + 1;
    }

    public bool TryBeginBlock() => TryStartBlock(BlockMode.Guard);

    internal bool TryBeginParry()
    {
        if (!HasAnimator || !animator.HasState(0, ParryStatePath) || !TryStartBlock(BlockMode.Parry))
            return false;

        perfectBlockAvailable = true;
        parryEntryDeadline = Time.time + ParryEntryTimeout;
        return true;
    }

    private bool TryStartBlock(BlockMode mode)
    {
        if (!CanAct || IsAttacking || IsBlocking)
            return false;

        blockMode = mode;
        SetLocomotion(false, false);
        SetAnimatorBool(mode == BlockMode.Guard ? Blocking : Parrying, true);
        return true;
    }

    public void EndBlock()
    {
        if (!IsBlocking)
            return;

        int parameter = IsGuarding ? Blocking : Parrying;
        blockMode = BlockMode.None;
        perfectBlockAvailable = false;
        blockRecoveryEndsAt = Time.time + BlockRecoveryDuration;
        SetAnimatorBool(parameter, false);
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

        if (IsBlocking && IsAttackerInFront(attacker))
        {
            if (IsGuarding)
            {
                GuardBlocked?.Invoke();
                return false;
            }

            if (perfectBlockAvailable &&
                IsWithinAnimationFrames(ParryState, FirstPerfectBlockFrame, LastPerfectBlockFrame))
            {
                perfectBlockAvailable = false;
                attacker.ApplyStun(perfectBlockStunDuration);
                PerfectBlocked?.Invoke();
                return false;
            }
        }

        Health = Mathf.Max(0f, Health - (IsStunned ? damage * 2f : damage));
        UpdateHealthBar();

        if (IsDead)
        {
            CancelActions();
            ClearStun();
            ClearHitReaction();
            SetTalking(false);
            SetAnimatorBool(Dead, true);
        }
        else
            PlayHitReaction();

        return true;
    }

    private bool IsAttackerInFront(CharacterCombat attacker)
    {
        if (attacker == null)
            return false;

        Vector3 toAttacker = Vector3.ProjectOnPlane(attacker.transform.position - transform.position, Vector3.up);
        Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
        float facingDot = Vector3.Dot(forward.normalized, toAttacker.normalized);
        return facingDot >= MinimumBlockDot || Mathf.Approximately(facingDot, MinimumBlockDot);
    }

    internal void PlayHitReaction()
    {
        if (IsDead)
            return;

        CancelActions();
        ClearStun();
        hitReactionEndsAt = Time.time + hitReactionDuration;
        SetAnimatorBool(HitReacting, true);
        if (HasAnimator)
            animator.SetTrigger(Hit);
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
            attackHitbox.ResetSwing();
        SetAnimatorBool(Attacking, false);
    }

    private void ClearHitReaction()
    {
        hitReactionEndsAt = 0f;
        SetAnimatorBool(HitReacting, false);
        if (HasAnimator)
            animator.ResetTrigger(Hit);
    }

    private void ClearStun()
    {
        if (!IsStunned)
            return;

        stunEndsAt = 0f;
        SetAnimatorBool(Stunned, false);
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
