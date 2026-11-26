using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class CharacterAttackHitbox : MonoBehaviour
{
    [SerializeField, Min(0f)] private float damage = 10f;
    [SerializeField] private LayerMask targetLayers = ~0;
    [SerializeField] private CharacterCombat owner;
    [SerializeField] private CharacterCombat opponent;
    [SerializeField] private SuspectEscapeTarget suspectTarget;

    private readonly HashSet<CharacterCombat> targetsHit = new HashSet<CharacterCombat>();
    private BoxCollider attackCollider;
    private CharacterCombat suspect;
    private float attackDamageBonus;
    private float flyingKickDamageBonus;

    internal float AttackDamage => damage + attackDamageBonus;
    internal float FlyingKickDamage => damage + flyingKickDamageBonus;

    internal void IncreaseDamage(float amount, bool flyingKick)
    {
        if (flyingKick)
            flyingKickDamageBonus += amount;
        else
            attackDamageBonus += amount;
    }

    private void Awake()
    {
        if (owner == null)
            owner = GetComponentInParent<CharacterCombat>();
        if (suspectTarget != null)
            suspect = suspectTarget.GetComponentInParent<CharacterCombat>();

        attackCollider = GetComponent<BoxCollider>();
        attackCollider.isTrigger = true;
        attackCollider.enabled = false;
    }

    internal void ResetSwing()
    {
        targetsHit.Clear();
        if (attackCollider != null)
            attackCollider.enabled = false;
    }

    private void OnDisable() => ResetSwing();

    private void LateUpdate()
    {
        bool hitWindowOpen = owner != null && owner.IsAttackHitWindowOpen();
        if (attackCollider.enabled != hitWindowOpen)
            attackCollider.enabled = hitWindowOpen;
        if (!hitWindowOpen)
            return;

        // CharacterControllers and scripted NPCs can move between physics steps.
        Physics.SyncTransforms();
        Vector3 scale = transform.lossyScale;
        scale = new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
        Vector3 center = transform.TransformPoint(attackCollider.center);
        Vector3 extents = Vector3.Scale(attackCollider.size * 0.5f, scale);
        Collider[] overlaps = Physics.OverlapBox(center, extents, transform.rotation,
            targetLayers, QueryTriggerInteraction.Ignore);

        foreach (Collider overlap in overlaps)
        {
            CharacterCombat target = overlap.GetComponentInParent<CharacterCombat>();
            if (target == null || target == owner || (opponent != null && target != opponent))
                continue;

            bool catchingSuspect = target == suspect && !suspectTarget.IsInCombat;
            if (catchingSuspect && (!suspectTarget.IsEscaping || suspectTarget.IsStopped || target.IsDead))
                continue;

            if (!targetsHit.Add(target))
                continue;

            bool hitLanded = true;
            if (catchingSuspect)
            {
                suspectTarget.StopEscaping();
                target.PlayHitReaction(owner);
            }
            else
                hitLanded = target.ReceiveHit(owner.IsFlyingKicking ? FlyingKickDamage : AttackDamage, owner);

            if (hitLanded)
                owner.ConfirmAttackHit();

            // A perfect block can interrupt this swing while processing the overlaps.
            if (!owner.IsAttacking)
                break;
        }
    }
}
