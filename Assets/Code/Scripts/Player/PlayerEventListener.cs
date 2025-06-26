using Fusion;
using FusionHelpers;
using TeamBasedShooter;

public class PlayerEventListener : NetworkBehaviour
{
    public SoundPlayer soundPlayer;

    void OnEnable()
    {
        PlayerEvents.OnKillConfirmed += HandleOnKillConfirmed;
        PlayerEvents.OnGameEnded += HandleOnGameEnded;
    }

    void OnDisable()
    {
        PlayerEvents.OnKillConfirmed -= HandleOnKillConfirmed;
        PlayerEvents.OnGameEnded -= HandleOnGameEnded;
    }

    private void HandleOnKillConfirmed(PlayerRef killerPlayerRef)
    {
        if (Runner.LocalPlayer == killerPlayerRef)
        {
            soundPlayer.PlayKillSound();
        }
    }

    private void HandleOnGameEnded(Team winner)
    {
        soundPlayer.PlayEndgameSound();

        if (Object.TryGetComponent(out Player player) && Runner.LocalPlayer == player.PlayerId)
        {
            player.ComputeRankXp(winner);
        }
    }
}
