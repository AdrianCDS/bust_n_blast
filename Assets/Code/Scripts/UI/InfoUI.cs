using UnityEngine;
using TMPro;
using FusionHelpers;

namespace TeamBasedShooter
{
    public class InfoUI : MonoBehaviour
    {
        public TMP_Text TimerText;
        public TMP_Text AttackersPointsText;
        public TMP_Text DefendersPointsText;
        public GameObject EndgamePopup;
        private bool endgameVisible = false;

        public GameObject WinEmblemImage;
        public GameObject LostEmblemImage;

        public TextMeshProUGUI Winner;
        public TextMeshProUGUI PointsInfo;

        private GameManager activeGameManager;

        public GameObject PauseMenu;

        private void Awake()
        {
            EndgamePopup.SetActive(false);
            PauseMenu.SetActive(false);
        }

        public void UpdateSessionInfo(Player player, GameManager gameManager)
        {
            var attackersPoints = gameManager.AttackersTotalPoints;
            var defendersPoints = gameManager.DefendersTotalPoints;

            float remainingTime = gameManager.RemainingTime.RemainingTime(gameManager.Runner) ?? 0f;
            int minutes = Mathf.FloorToInt(remainingTime / 60);
            int seconds = Mathf.FloorToInt(remainingTime % 60);

            TimerText.text = $"{minutes:00}:{seconds:00}";
            AttackersPointsText.text = $"{attackersPoints}p";
            DefendersPointsText.text = $"{defendersPoints}p";

            if (gameManager.RemainingTime.Expired(gameManager.Runner) && !endgameVisible)
            {
                EndgamePopup.SetActive(true);
                endgameVisible = true;

                Winner.text = ComputeVictoryOrDefeatStatus(player, gameManager);
                PointsInfo.text = ComputeAdditionalWinLossMessage(player, gameManager);

                if (Winner.text == "VICTORY")
                {
                    WinEmblemImage.SetActive(true);
                    LostEmblemImage.SetActive(false);
                }
                else
                {
                    WinEmblemImage.SetActive(false);
                    LostEmblemImage.SetActive(true);
                }


                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;

                activeGameManager = gameManager;
            }

            if (!endgameVisible && player.BlockInput)
            {
                PauseMenu.SetActive(true);

                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;

                activeGameManager = gameManager;
            }

            if (!endgameVisible && !player.BlockInput)
            {
                PauseMenu.SetActive(false);

                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private string ComputeVictoryOrDefeatStatus(Player localPlayer, GameManager gm)
        {
            Team localPlayerTeam = localPlayer.PlayerTeam;

            if ((gm.AttackersTotalPoints >= gm.DefendersTotalPoints && localPlayerTeam == Team.Attackers) || (gm.DefendersTotalPoints > gm.AttackersTotalPoints && localPlayerTeam == Team.Defenders))
            {
                // local player is the same team as winning team -> victory
                return "VICTORY";
            }
            else
            {
                // player did not win -> defeat
                return "DEFEAT";
            }
        }

        private string ComputeAdditionalWinLossMessage(Player localPlayer, GameManager gm)
        {
            Team localPlayerTeam = localPlayer.PlayerTeam;

            if (gm.AttackersTotalPoints >= gm.DefendersTotalPoints && localPlayerTeam == Team.Attackers)
            {
                // attackers won and local player is attacker -> victory
                return "The city was successfully destroyed";
            }
            else if (gm.DefendersTotalPoints > gm.AttackersTotalPoints && localPlayerTeam == Team.Defenders)
            {
                // defenders won and local player is defender -> victory
                return "The bombing was successfully stopped";
            }
            else if (gm.AttackersTotalPoints >= gm.DefendersTotalPoints && localPlayerTeam == Team.Defenders)
            {
                // attackers won and local player is defender -> defeat
                return "The city has been destroyed";
            }
            else if (gm.DefendersTotalPoints > gm.AttackersTotalPoints && localPlayerTeam == Team.Attackers)
            {
                // defenders won and local player is attacker -> defeat
                return "Your squad has been stopped";
            }
            else
            {
                // fallback case, should never reach this
                return "Game has ended";
            }
        }

        public void OnExitToMainMenu()
        {
            if (activeGameManager == null) return;

            activeGameManager.Restart();
        }
    }
}
