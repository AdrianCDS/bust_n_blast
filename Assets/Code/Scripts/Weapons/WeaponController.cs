using Fusion;
using UnityEngine;

namespace TeamBasedShooter
{
    public enum EWeaponType
    {
        Pistol,
        Rifle,
        RPG,
        Minigun,
        Machete
    }

    public enum EAbilityType
    {
        None,
        PlacingTool,
        Jetpack,
        Molotov,
        TearGas
    }

    public class WeaponController : NetworkBehaviour
    {
        [SerializeField] private EWeaponType primaryWeaponType;
        [SerializeField] private EAbilityType secondaryWeaponType;

        public Transform FireTransform;

        [Networked, HideInInspector]
        public Weapon CurrentWeapon { get; set; }

        [Networked, HideInInspector]
        public Weapon SecondaryWeapon { get; set; }

        public override void Spawned()
        {
            if (HasStateAuthority)
            {
                switch (primaryWeaponType)
                {
                    case EWeaponType.Pistol:
                        CurrentWeapon = GetComponentInChildren<Pistol>();
                        break;

                    case EWeaponType.Rifle:
                        CurrentWeapon = GetComponentInChildren<Rifle>();
                        break;

                    case EWeaponType.RPG:
                        CurrentWeapon = GetComponentInChildren<RPG>();
                        break;

                    case EWeaponType.Minigun:
                        CurrentWeapon = GetComponentInChildren<Minigun>();
                        break;

                    case EWeaponType.Machete:
                        CurrentWeapon = GetComponentInChildren<Machete>();
                        break;

                    default:
                        CurrentWeapon = null;
                        break;
                }

                switch (secondaryWeaponType)
                {
                    case EAbilityType.None:
                        SecondaryWeapon = null;
                        break;

                    case EAbilityType.PlacingTool:
                        SecondaryWeapon = GetComponentInChildren<PlacingTool>();
                        break;

                    case EAbilityType.Jetpack:
                        SecondaryWeapon = GetComponentInChildren<Jetpack>();
                        break;

                    case EAbilityType.TearGas:
                        SecondaryWeapon = GetComponentInChildren<Throwable>();
                        break;

                    case EAbilityType.Molotov:
                        SecondaryWeapon = GetComponentInChildren<Molotov>();
                        break;

                    default:
                        SecondaryWeapon = null;
                        break;
                }
            }
        }

        public void PrimaryAttack(bool justPressed, bool wasReleased, Vector3 playerAimPoint)
        {
            if (CurrentWeapon == null) return;

            Vector3 fireDirection = (playerAimPoint - FireTransform.position).normalized;

            CurrentWeapon.Attack(FireTransform.position, fireDirection, justPressed, wasReleased);
        }

        public void SecondaryAttack(bool justPressed, bool wasReleased, Vector3 playerAimPoint)
        {
            if (SecondaryWeapon == null) return;

            SecondaryWeapon.Attack(justPressed, wasReleased);
        }

        public void Reload()
        {
            if (CurrentWeapon == null) return;

            CurrentWeapon.Reload();
        }
    }
}
