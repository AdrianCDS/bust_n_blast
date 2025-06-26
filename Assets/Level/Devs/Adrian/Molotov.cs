using TeamBasedShooter;
using Fusion;
using UnityEngine;

public class Molotov : Weapon
{
    [Header("Fire Setup")]
    public float Damage = 10f;
    public int FireRate = 200;
    public LayerMask HitMask;
    public float MaxHitDistance = 200f;

    [Header("Ammo")]
    public float ReloadTime = 1.5f;

    public override Sprite Icon { get; set; }

    [Header("Visuals")]
    public Sprite icon;

    [Header("Fire Effect")]
    public Transform FirePoint;
    public StandaloneProjectile ProjectilePrefab;

    [Header("Sounds")]
    public AudioSource FireSound;

    [Networked]
    public override NetworkBool IsReloading { get; set; }

    [Networked]
    private int _fireCount { get; set; }
    [Networked]
    private TickTimer MolotovCooldown { get; set; }

    private int _visibleFireCount;


    public override void Spawned()
    {
        Icon = icon;

        _visibleFireCount = _fireCount;
    }

    public override void FixedUpdateNetwork()
    {
        if (IsReloading && MolotovCooldown.ExpiredOrNotRunning(Runner))
        {
            IsReloading = false;
            MolotovCooldown = TickTimer.CreateFromSeconds(Runner, 0.5f);
        }
    }

    public override void Render()
    {
        if (_visibleFireCount < _fireCount)
        {
            PlayFireEffect();
        }

        _visibleFireCount = _fireCount;
    }

    public override float GetReloadProgress()
    {
        if (MolotovCooldown.ExpiredOrNotRunning(Runner))
            return 1f;

        return 1f - MolotovCooldown.RemainingTime(Runner).GetValueOrDefault() / ReloadTime;
    }

    public override void Attack(bool justPressed, bool wasReleased)
    {
        if (justPressed == false) return;

        if (MolotovCooldown.ExpiredOrNotRunning(Runner) == false) return;

        var projectileDirection = -FirePoint.forward;

        ThrowMolotov(FirePoint.position, projectileDirection);

        IsReloading = true;
        MolotovCooldown = TickTimer.CreateFromSeconds(Runner, ReloadTime);
    }

    private void ThrowMolotov(Vector3 firePosition, Vector3 fireDirection)
    {
        if (HasInputAuthority)
        {
            RPC_SpawnCocktailMolotov(firePosition, fireDirection);
        }

        _fireCount++;
    }

    private void PlayFireEffect()
    {
        if (FireSound)
        {
            FireSound.PlayOneShot(FireSound.clip);
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_SpawnCocktailMolotov(Vector3 firePosition, Vector3 fireDirection)
    {
        var projectile = Runner.Spawn(ProjectilePrefab, FirePoint.position, FirePoint.rotation, Object.InputAuthority);
        projectile.Fire(firePosition, fireDirection);
    }
}
