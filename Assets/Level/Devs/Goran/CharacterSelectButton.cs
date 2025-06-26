using UnityEngine;
using UnityEngine.UI;
using Fusion;

namespace TeamBasedShooter
{
    public class CharacterSelectButton : MonoBehaviour
    {
        public int characterIndex;

        private Button button;
        private TemplatePlayer player;

        private void Awake()
        {
            button = GetComponent<Button>();
            button.onClick.AddListener(SelectCharacter);
        }

        public void Initialize(TemplatePlayer templatePlayer)
        {
            player = templatePlayer;
        }

        private void SelectCharacter()
        {
            if (player != null && player.Object.HasInputAuthority)
            {
                player.OnCharacterButtonClicked(characterIndex);
            }
        }
    }
}