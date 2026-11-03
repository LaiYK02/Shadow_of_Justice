using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PlayerAttackHitbox : MonoBehaviour
{
    [SerializeField] private SuspectEscapeTarget suspectTarget;

    private Collider attackCollider;
    private bool attackActive;
    private bool alreadyHitTarget;

    private void Awake()
    {
        attackCollider = GetComponent<Collider>();
        attackCollider.isTrigger = true;
        attackCollider.enabled = false;
    }

    public void SetAttackActive(bool active)
    {
        attackActive = active;
        alreadyHitTarget = false;
        if (attackCollider != null)
            attackCollider.enabled = active;
    }

    private void Update()
    {
        if (!attackActive || alreadyHitTarget || attackCollider == null)
            return;

        Bounds bounds = attackCollider.bounds;
        Collider[] overlaps = Physics.OverlapBox(bounds.center, bounds.extents);
        foreach (Collider overlap in overlaps)
        {
            if (overlap != null && overlap != attackCollider)
                TryHit(overlap);

            if (alreadyHitTarget)
                return;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        TryHit(other);
    }

    private void TryHit(Collider other)
    {
        if (!attackActive || alreadyHitTarget || other == null)
            return;

        SuspectEscapeTarget target = other.GetComponentInParent<SuspectEscapeTarget>();
        if (target == null || !target.IsEscaping ||
            (suspectTarget != null && target != suspectTarget))
            return;

        alreadyHitTarget = true;
        target.StopEscaping();
        Debug.Log("Attack hit the escaping suspect.", target);
    }
}
