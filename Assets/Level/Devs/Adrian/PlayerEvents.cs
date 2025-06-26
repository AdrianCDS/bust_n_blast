using System;
using Fusion;
using UnityEngine;
using FusionHelpers;

public static class PlayerEvents
{
    public static event Action<PlayerRef> OnKillConfirmed;
    public static event Action<Team> OnGameEnded;

    public static void NotifyOnKillConfirmed(PlayerRef killerPlayerRef)
    {
        OnKillConfirmed?.Invoke(killerPlayerRef);
    }

    public static void NotifyOnGameEnded(Team winner)
    {
        OnGameEnded?.Invoke(winner);
    }
}
