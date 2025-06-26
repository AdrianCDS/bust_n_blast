using Fusion;
using FusionHelpers;
using UnityEngine;

namespace TeamBasedShooter
{
    public class UIManager : NetworkBehaviour
    {
        public HUD PlayerHUD;

        private GameManager gameManager;

        private void Update()
        {
            if (!Runner) return;

            gameManager = Runner.TryGetSingleton(out GameManager gm) ? gm : null;

            if (!gameManager) return;

            var playerObject = Runner.GetPlayerObject(Runner.LocalPlayer);

            if (playerObject != null && playerObject.TryGetComponent<TemplatePlayer>(out TemplatePlayer templatePlayer))
            {
                LevelManager lm = Runner.GetLevelManager();
                lm.readyUpManager.UpdateReadyText(templatePlayer.IsReady());
            }

            if (playerObject != null && playerObject.TryGetComponent<Player>(out Player player))
            {
                PlayerHUD.gameObject.SetActive(true);
                PlayerHUD.UpdatePlayer(player, gameManager);
            }
            else
            {
                PlayerHUD.gameObject.SetActive(false);
            }
        }
    }
}
