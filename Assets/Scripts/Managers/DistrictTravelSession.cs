using System.Collections.Generic;
using UnityEngine;

internal enum District
{
    CentralDistrict,
    OldTown,
    HarbourDistrict
}

internal static class DistrictTravelSession
{
    internal static bool? ArrestedFirstSuspect { get; set; }
    internal static District? NextDestination { get; set; }
    internal static bool HasVisitedOldTown { get; private set; }
    internal static bool HasStartedMissionTwo { get; set; }
    internal static readonly Dictionary<string, HashSet<string>> CollectedTreasures = new();

    private static (District destination, PlayerTravelProgress.State player)? journey;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    internal static void Reset()
    {
        ArrestedFirstSuspect = null;
        NextDestination = null;
        HasVisitedOldTown = false;
        HasStartedMissionTwo = false;
        CollectedTreasures.Clear();
        MerchantShop.ResetUpgrades();
        journey = null;
    }

    internal static void Depart(District destination, PlayerTravelProgress.State player)
    {
        journey = (destination, player);
        if (NextDestination == destination)
            NextDestination = null;
    }

    internal static bool CanTravel(District from, District to)
    {
        if (from == to)
            return false;
        if (NextDestination == to)
            return true;

        return HasVisitedOldTown &&
            (from == District.CentralDistrict && to == District.OldTown ||
             from == District.OldTown && to == District.CentralDistrict);
    }

    internal static bool IsArriving(District district) => journey?.destination == district;

    internal static bool TryArrive(District district, out PlayerTravelProgress.State player)
    {
        if (journey is { } trip && trip.destination == district)
        {
            player = trip.player;
            journey = null;
            if (district == District.OldTown)
                HasVisitedOldTown = true;
            return true;
        }

        player = default;
        return false;
    }
}
