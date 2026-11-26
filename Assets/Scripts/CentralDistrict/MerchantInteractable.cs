using UnityEngine;

public sealed class MerchantInteractable : PlayerInteractable
{
    [SerializeField] private MerchantShop shop;

    public override bool CanInteract => shop != null && shop.CanOpen;

    public override void Interact() => shop.Open();
}
