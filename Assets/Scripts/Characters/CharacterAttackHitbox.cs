using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class CharacterAttackHitbox : MonoBehaviour
{
    [SerializeField, Min(0f)] private float damage = 25f;
    [SerializeField] private LayerMask targetLayers = ~0;
    [SerializeField] private SuspectEscapeTarget suspectTarget;

    private readonly HashSet<CharacterCombat> targetsHit = new HashSet<CharacterCombat>();
    private BoxCollider attackCollider;
    private CharacterCombat owner;
    private CharacterCombat suspect;

    private void Awake()
    {
        owner = GetComponentInParent<CharacterCombat>();
        if (suspectTarget != null)
            suspect = suspectTarget.GetComponentInParent<CharacterCombat>();

        attackCollider = GetComponent<BoxCollider>();
        attackCollider.isTrigger = true;
        attackCollider.enabled = false;
    }

    internal void SetAttackActive(bool active)
    {
        targetsHit.Clear();
        if (attackCollider != null)
            attackCollider.enabled = active;
    }

    private void OnDisable() => SetAttackActive(false);

    private void LateUpdate()
    {
        if (!attackCollider.enabled || owner == null || !owner.CanAct || !owner.IsAttacking)
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
            if (target == null || target == owner)
                continue;

            bool isSuspect = suspect != null && target == suspect;
            if (isSuspect && (!suspectTarget.IsEscaping || suspectTarget.IsStopped))
                continue;

            if (!targetsHit.Add(target))
                continue;

            if (target.ReceiveHit(damage, owner) && isSuspect)
                suspectTarget.StopEscaping();

            // A perfect block can interrupt this swing while processing the overlaps.
            if (!owner.IsAttacking)
                break;
        }
    }
}
