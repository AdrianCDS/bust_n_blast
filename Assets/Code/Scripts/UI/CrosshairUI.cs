using UnityEngine;

namespace TeamBasedShooter
{
    public class CrosshairUI : MonoBehaviour
    {

        private Animation anim;

        private int _lastAmmo;

        private void Awake()
        {
            anim = GetComponent<Animation>();
        }

        public void UpdateCrosshair(WeaponController weaponController)
        {
            int currentAmmo = weaponController.CurrentWeapon.ClipAmmo;

            if (anim.isPlaying || weaponController.CurrentWeapon.IsReloading) return;
            if (currentAmmo == _lastAmmo) return;

            if (currentAmmo < _lastAmmo)
            {
                anim.Play("CrosshairExpansion");
            }

            _lastAmmo = currentAmmo;
        }
    }
}
