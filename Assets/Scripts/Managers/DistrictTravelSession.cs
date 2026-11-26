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
    internal static readonly Dictionary<string, HashSet<string>> CollectedTreasures = new();

    private static (District destination, PlayerTravelProgress.State player)? journey;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    internal static void Reset()
    {
        ArrestedFirstSuspect = null;
        NextDestination = null;
        CollectedTreasures.Clear();
        MerchantShop.ResetUpgrades();
        journey = null;
    }

    internal static void Depart(District destination, PlayerTravelProgress.State player)
    {
        journey = (destination, player);
        NextDestination = null;
    }

    internal static bool TryArrive(District district, out PlayerTravelProgress.State player)
    {
        if (journey is { } trip && trip.destination == district)
        {
            player = trip.player;
            journey = null;
            return true;
        }

        player = default;
        return false;
    }
}
