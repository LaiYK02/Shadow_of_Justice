using UnityEngine;

[DisallowMultipleComponent]
public sealed class GrabCarBoxInteraction : PlayerInteractable
{
    [SerializeField] private DistrictNavigationPanel navigation;

    public override bool CanInteract => navigation != null && navigation.CanOpen;

    public override void Interact()
    {
        if (CanInteract)
            navigation.Open();
    }
}
