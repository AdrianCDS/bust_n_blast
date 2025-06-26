using TeamBasedShooter;

using UnityEngine;
using Fusion;
using Fusion.Addons.SimpleKCC;

public class Jetpack : Weapon
{
    [Header("Setup")]
    public Player playerToBoost;
    public SimpleKCC playerKCC;

    [Header("Ammo")]
    public float BoostForce = 10f;
    public float SpeedBoost = 10f;
    public float BoostCooldown = 5.0f;

    public override Sprite Icon { get; set; }

    [Header("Visuals")]
    public Sprite icon;
    public GameObject FireBoostEffectPrefab;

    [Header("Sounds")]
    public AudioSource BoostSound;

    [Networked]
    private TickTimer JetpackCooldown { get; set; }

    [Networked]
    private TickTimer JetpackEffectCooldown { get; set; }

    private bool startedBoost;

    [Networked]
    public override NetworkBool IsReloading { get; set; }

    [Networked]
    private int BoostCount { get; set; }

    private int visibleBoostCount;

    public override int MaxClipAmmo { get; set; }

    [Networked]
    public override int ClipAmmo { get; set; }

    public int maxClipAmmo = 1;

    public override void Spawned()
    {
        Icon = icon;

        visibleBoostCount = BoostCount;

        if (HasStateAuthority)
        {
            MaxClipAmmo = maxClipAmmo;
            ClipAmmo = MaxClipAmmo;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (startedBoost && JetpackEffectCooldown.ExpiredOrNotRunning(Runner))
        {
            startedBoost = false;
            playerToBoost.MoveSpeed -= SpeedBoost;

            if (Object.HasStateAuthority)
            {
                RPC_SetFireVisible(false);
            }
        }

        if (IsReloading && JetpackCooldown.ExpiredOrNotRunning(Runner))
        {
            IsReloading = false;
            JetpackEffectCooldown = TickTimer.CreateFromSeconds(Runner, 0.5f);
        }
    }

    public override void Render()
    {
        if (visibleBoostCount < BoostCount)
        {
            PlayFireEffect();
        }

        visibleBoostCount = BoostCount;
    }

    public override void Attack(bool justPressed, bool wasReleased)
    {
        if (justPressed == false) return;

        if (JetpackCooldown.ExpiredOrNotRunning(Runner) == false) return;

        if (playerKCC.IsGrounded == false) return;

        TriggerJetpackAction(playerToBoost, playerKCC);

        startedBoost = true;
        IsReloading = true;
        JetpackCooldown = TickTimer.CreateFromSeconds(Runner, BoostCooldown);
        BoostCount++;

        JetpackEffectCooldown = TickTimer.CreateFromSeconds(Runner, 2f);
    }

    public override float GetReloadProgress()
    {
        if (JetpackCooldown.ExpiredOrNotRunning(Runner))
            return 1f;

        return 1f - JetpackCooldown.RemainingTime(Runner).GetValueOrDefault() / BoostCooldown;
    }

    private void TriggerJetpackAction(Player player, SimpleKCC playerKCC)
    {
        if (playerKCC.IsGrounded)
        {
            player.MovePlayer(default, BoostForce);
            player.MoveSpeed += SpeedBoost;
        }
    }

    private void PlayFireEffect()
    {
        if (BoostSound)
        {
            BoostSound.PlayOneShot(BoostSound.clip);
        }

        if (Object.HasStateAuthority)
        {
            RPC_SetFireVisible(true);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_SetFireVisible(bool isVisible)
    {
        FireBoostEffectPrefab.SetActive(isVisible);
    }
}
