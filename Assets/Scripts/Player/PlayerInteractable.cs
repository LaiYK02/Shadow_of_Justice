using UnityEngine;

/// <summary>Common contract used by PlayerInteractionManager.</summary>
public abstract class PlayerInteractable : MonoBehaviour
{
    private static readonly System.Collections.Generic.List<PlayerInteractable> Active = new();
    public static System.Collections.Generic.IReadOnlyList<PlayerInteractable> ActiveInteractables => Active;

    [Header("Interaction")]
    [SerializeField, Min(0.1f)] private float interactionRange = 2.5f;
    [SerializeField] private Collider targetCollider;
    [Tooltip("World Space Canvas or ButtonHint object.")]
    [SerializeField] private GameObject interactionPrompt;

    public float InteractionRange => interactionRange;
    public Collider TargetCollider => targetCollider;
    public GameObject Prompt => interactionPrompt;
    public abstract bool CanInteract { get; }
    public abstract void Interact();

    protected virtual void Awake()
    {
        if (targetCollider == null) targetCollider = GetComponentInChildren<Collider>();
        SetPromptVisible(false);
    }

    protected virtual void OnEnable()
    {
        if (!Active.Contains(this)) Active.Add(this);
    }

    protected virtual void OnDisable()
    {
        Active.Remove(this);
        SetPromptVisible(false);
    }

    public void SetPromptVisible(bool visible, Camera viewCamera = null)
    {
        if (interactionPrompt == null) return;
        if (interactionPrompt.activeSelf != visible) interactionPrompt.SetActive(visible);
        if (visible && viewCamera != null) interactionPrompt.transform.rotation = viewCamera.transform.rotation;
    }

    public bool IsTargetCollider(Collider candidate)
    {
        return candidate != null && targetCollider != null &&
               (candidate == targetCollider || candidate.transform.IsChildOf(transform));
    }

    protected virtual void Start()
    {
        if (targetCollider == null) Debug.LogError($"{name}: Assign/add a target Collider.", this);
        if (interactionPrompt == null) Debug.LogWarning($"{name}: Assign the world-space interaction prompt.", this);
    }
}
