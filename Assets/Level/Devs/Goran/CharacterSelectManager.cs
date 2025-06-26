using UnityEngine;
using UnityEngine.UI;
using Fusion;

namespace TeamBasedShooter
{
    public class CharacterSelectManager : NetworkBehaviour
    {
        public CharacterSelection PlayerCharacterSelection;

        private void Update()
        {
            if (!Runner) return;

            var playerObject = Runner.GetPlayerObject(Runner.LocalPlayer);
            if (playerObject != null && playerObject.TryGetComponent<TemplatePlayer>(out TemplatePlayer player))
            {
                PlayerCharacterSelection.gameObject.SetActive(true);
                PlayerCharacterSelection.RegisterPlayer(player);
            }
            else
            {
                PlayerCharacterSelection.gameObject.SetActive(false);
            }
        }
    }
}