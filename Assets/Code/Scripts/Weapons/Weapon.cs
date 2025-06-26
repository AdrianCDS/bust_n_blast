using Fusion;
using UnityEngine;
using FusionHelpers;

namespace TeamBasedShooter
{
    public abstract class Weapon : NetworkBehaviour
    {

        public abstract Sprite Icon { get; set; }

        public virtual void Attack(Vector3 attackSource, Vector3 attackDirection, bool justPressed, bool wasReleased) { }
        public virtual void Attack(bool justPressed, bool wasReleased) { }

        public virtual NetworkBool IsReloading { get; set; }
        public virtual int MaxClipAmmo { get; set; } = 0;
        public virtual int ClipAmmo { get; set; } = 0;
        public virtual int RemainingAmmo { get; set; } = 0;
        public virtual int StartAmmo { get; set; } = 0;

        public virtual void Reload() { }
        public virtual float GetReloadProgress() => 0f;
    }
}
