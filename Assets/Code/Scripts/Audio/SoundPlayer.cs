using UnityEngine;
using Fusion;

namespace TeamBasedShooter
{
    public class SoundPlayer : NetworkBehaviour
    {
        [SerializeField] private AudioSource _endgameSound;
        [SerializeField] private AudioSource _killSound;

        public void PlayEndgameSound()
        {
            _endgameSound.PlayOneShot(_endgameSound.clip);
        }

        public void PlayKillSound()
        {
            _killSound.PlayOneShot(_killSound.clip);
        }
    }
}
