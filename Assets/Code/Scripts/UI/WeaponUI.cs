using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TeamBasedShooter
{
    public class WeaponUI : MonoBehaviour
    {
        public Image WeaponIcon;
        public Image WeaponIconShadow;

        public Image SecondaryWeaponIcon;
        public Image SecondaryWeaponIconShadow;

        public TextMeshProUGUI ClipAmmo;
        public TextMeshProUGUI RemainingAmmo;
        public TextMeshProUGUI SecondaryAmmo;
        public Image ReloadProgress;
        public Image SecondaryReloadProgress;

        private Weapon _weapon;
        private Weapon _secondaryWeapon;
        private int _lastClipAmmo;
        private int _lastRemainingAmmo;

        public void UpdateWeapons(WeaponController weapons)
        {
            SetWeapon(weapons.CurrentWeapon);
            SetSecondaryWeapon(weapons.SecondaryWeapon);

            if (_weapon == null) return;

            UpdateReloadProgress();

            if (_weapon.ClipAmmo == _lastClipAmmo && _weapon.RemainingAmmo == _lastRemainingAmmo)
                return;

            ClipAmmo.text = _weapon.ClipAmmo.ToString();
            RemainingAmmo.text = _weapon.RemainingAmmo.ToString();

            SecondaryAmmo.text = "∞";

            _lastClipAmmo = _weapon.ClipAmmo;
            _lastRemainingAmmo = _weapon.RemainingAmmo;
        }

        private void SetWeapon(Weapon weapon)
        {
            if (weapon == _weapon)
                return;

            _weapon = weapon;

            if (weapon == null)
                return;

            WeaponIcon.sprite = weapon.Icon;
            WeaponIconShadow.sprite = weapon.Icon;
        }

        private void SetSecondaryWeapon(Weapon weapon)
        {
            if (weapon == _secondaryWeapon)
                return;

            _secondaryWeapon = weapon;

            if (weapon == null)
                return;

            SecondaryWeaponIcon.sprite = weapon.Icon;
            SecondaryWeaponIconShadow.sprite = weapon.Icon;
        }

        private void UpdateReloadProgress()
        {
            if (_weapon.IsReloading)
            {
                ReloadProgress.fillAmount = _weapon.GetReloadProgress();
            }
            else
            {
                ReloadProgress.fillAmount = _weapon.ClipAmmo / (float)_weapon.MaxClipAmmo;
            }

            if (_secondaryWeapon.IsReloading)
            {
                SecondaryReloadProgress.fillAmount = _secondaryWeapon.GetReloadProgress();
            }
            else
            {
                SecondaryReloadProgress.fillAmount = 1f;
            }
        }
    }
}
