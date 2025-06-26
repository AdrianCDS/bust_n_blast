using UnityEngine;

namespace TeamBasedShooter
{
    public class CharacterSelection : MonoBehaviour
    {
        public CharacterSelectButton[] characterButtons;

        public void RegisterPlayer(TemplatePlayer player)
        {
            if (player == null) return;

            foreach (var button in characterButtons)
            {
                button.Initialize(player);
            }
        }
    }
}