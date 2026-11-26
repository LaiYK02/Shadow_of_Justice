using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerTravelProgress : MonoBehaviour
{
    [SerializeField] private District district;
    [SerializeField] private PlayerHUDController hud;
    [SerializeField] private PlayerMovement movement;
    [SerializeField] private CharacterCombat combat;

    internal struct State
    {
        internal int Coins;
        internal int Reputation;
        internal float MaxHealth;
        internal float Health;
        internal float MaxEnergy;
        internal float Energy;
        internal float EnergyRecovery;
        internal bool EnergyExhausted;
        internal float Attack;
        internal float FlyingKick;
    }

    private void Start()
    {
        if (!DistrictTravelSession.TryArrive(district, out State state))
            return;

        hud.RestoreTravelBalances(state.Coins, state.Reputation);
        combat.RestoreTravelHealth(state.MaxHealth, state.Health);
        movement.RestoreTravelEnergy(state.MaxEnergy, state.Energy, state.EnergyRecovery, state.EnergyExhausted);
        CharacterAttackHitbox hitbox = combat.AttackHitbox;
        hitbox.IncreaseDamage(state.Attack - hitbox.AttackDamage, false);
        hitbox.IncreaseDamage(state.FlyingKick - hitbox.FlyingKickDamage, true);
    }

    internal State Capture()
    {
        return new State
        {
            Coins = hud.Coins,
            Reputation = hud.Reputation,
            MaxHealth = combat.MaxHealth,
            Health = combat.Health,
            MaxEnergy = movement.MaxEnergy,
            Energy = movement.AvailableEnergy,
            EnergyExhausted = movement.EnergyLocked,
            EnergyRecovery = movement.EnergyRecoveryPerSecond,
            Attack = combat.AttackHitbox.AttackDamage,
            FlyingKick = combat.AttackHitbox.FlyingKickDamage
        };
    }
}
