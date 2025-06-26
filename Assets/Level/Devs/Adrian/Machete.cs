using TeamBasedShooter;
using UnityEngine;
using Fusion;
using System.Collections.Generic;

public class Machete : Weapon
{
    [Header("Setup")]
    public Animator Animator;

    [Header("Attack")]
    public float Damage = 10f;
    public int AttackRate = 100;
    public LayerMask HitMask;
    public float AttackRadius = 0.8f;

    public override Sprite Icon { get; set; }

    [Header("Ammo")]
    public int maxClipAmmo = 1;
    public int startAmmo = 1;

    [Header("Visuals")]
    public Sprite icon;
    public AudioSource SlashSound;
    public AudioSource GruntSound;

    [Networked]
    private int SlashCount { get; set; }

    [Networked]
    public int ComboCount { get; set; }

    [Networked]
    private TickTimer AttackCooldown { get; set; }

    public bool HasAmmo => ClipAmmo > 0 || RemainingAmmo > 0;

    [Networked]
    public override int ClipAmmo { get; set; }

    [Networked]
    public override int RemainingAmmo { get; set; }

    private int _visibleSlashCount;

    private int _attackTicks;

    private readonly List<LagCompensatedHit> _hits = new List<LagCompensatedHit>();

    public override void Spawned()
    {
         if (HasStateAuthority)
        {
            MaxClipAmmo = maxClipAmmo;
            StartAmmo = startAmmo;

            ClipAmmo = Mathf.Clamp(StartAmmo, 0, MaxClipAmmo);
            RemainingAmmo = StartAmmo - ClipAmmo;
        }

        Icon = icon;

        _visibleSlashCount = SlashCount;

        float attackTime = 60f / AttackRate;
        _attackTicks = Mathf.CeilToInt(attackTime / Runner.DeltaTime);
    }

    public override void FixedUpdateNetwork()
    {

    }

    public override void Render()
    {
        if (_visibleSlashCount < SlashCount)
        {
            PlaySlashEffect();
            PlayAttackCombo();
        }

        _visibleSlashCount = SlashCount;
    }

    public override void Attack(Vector3 slashPosition, Vector3 slashDirection, bool justPressed, bool wasReleased)
    {
        if (justPressed == false) return;

        if (AttackCooldown.ExpiredOrNotRunning(Runner) == false) return;

        UnityEngine.Random.InitState(Runner.Tick * unchecked((int)Object.Id.Raw));

        ComboCount++;

        StartSwordCombo(slashPosition);

        AttackCooldown = TickTimer.CreateFromTicks(Runner, _attackTicks);
    }

    private void StartSwordCombo(Vector3 slashPosition)
    {
        var hitOptions = HitOptions.SubtickAccuracy | HitOptions.IncludePhysX | HitOptions.IgnoreInputAuthority;
        Runner.LagCompensation.OverlapSphere(slashPosition, AttackRadius, player: Object.InputAuthority, _hits, options: hitOptions);

        // Tracks players already damaged
        HashSet<Health> damagedPlayers = new HashSet<Health>(); // We use this so we skip damaging all player's hitboxes, only one

        foreach (var h in _hits)
        {
            if (h.Hitbox == null) continue;

            Health hitEnemyHealth = h.Hitbox.GetComponentInParent<Health>();

            if (damagedPlayers.Contains(hitEnemyHealth)) continue;

            Vector3 targetPosition = h.Point;
            Vector3 directionToTarget = (targetPosition - slashPosition).normalized;

            ApplyDamage(h.Hitbox, targetPosition, directionToTarget);

            damagedPlayers.Add(hitEnemyHealth);
        }

        SlashCount++;
    }

    private void ApplyDamage(Hitbox enemyHitbox, Vector3 position, Vector3 direction)
    {
        var enemyHealth = enemyHitbox.Root.GetComponent<Health>();
        if (enemyHealth == null || enemyHealth.IsAlive == false) return;

        bool isCriticalHit = ComboCount == 3;

        float damage = Damage * ComboCount;

        if (enemyHealth.ApplyDamage(Object.InputAuthority, damage, position, direction, isCriticalHit) == false)
        {
            return;
        }
    }

    private void PlaySlashEffect()
    {
        if (ComboCount != 2)
        {
            SlashSound.PlayOneShot(SlashSound.clip);
        }
        else
        {
            GruntSound.PlayOneShot(GruntSound.clip);
        }
    }

    private void PlayAttackCombo()
    {
        switch (ComboCount)
        {
            case 1:
                Animator.SetTrigger("Attack1");
                break;
            case 2:
                Animator.SetTrigger("Attack2");
                break;
            case 3:
                Animator.SetTrigger("Attack3");
                ComboCount = 0;
                break;
            default:
                break;
        }
    }
}
