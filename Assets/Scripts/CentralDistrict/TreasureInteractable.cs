using UnityEngine;

[DisallowMultipleComponent]
public sealed class TreasureInteractable : PlayerInteractable
{
    private const int CoinReward = 50;

    private PlayerHUDController playerHUD;
    private TreasureHunt treasureHunt;
    private bool collected;

    public override bool CanInteract => !collected && playerHUD != null && playerHUD.IsVisible;

    protected override void Awake()
    {
        base.Awake();
        playerHUD = FindFirstObjectByType<PlayerHUDController>();
        treasureHunt = GetComponentInParent<TreasureHunt>();
        if (treasureHunt != null && treasureHunt.WasCollected(name))
        {
            collected = true;
            gameObject.SetActive(false);
        }
    }

    public override void Interact()
    {
        if (!CanInteract)
            return;

        collected = true;
        gameObject.SetActive(false);
        playerHUD.AddRewards(CoinReward, 0);
        if (treasureHunt != null)
            treasureHunt.RecordPickup(name, playerHUD);
    }
}
