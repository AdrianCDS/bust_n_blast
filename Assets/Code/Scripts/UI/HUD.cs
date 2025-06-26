using Fusion;
using UnityEngine;

namespace TeamBasedShooter
{
    public class HUD : MonoBehaviour
    {
        public HealthUI HealthUI;
        public WeaponUI WeaponUI;
        public CrosshairUI CrosshairUI;

        public InfoUI InfoUI;

        public void UpdatePlayer(Player player, GameManager gameManager)
        {
            if (player == null) return;

            HealthUI.UpdateHealth(player.Health);
            WeaponUI.UpdateWeapons(player.WeaponController);
            CrosshairUI.UpdateCrosshair(player.WeaponController);

            InfoUI.UpdateSessionInfo(player, gameManager);

            CrosshairUI.gameObject.SetActive(player.Health.IsAlive && gameManager.CurrentPlayState != FusionHelpers.FusionSession.PlayState.ENDGAME);
        }
    }
}
