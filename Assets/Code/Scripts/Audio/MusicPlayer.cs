using UnityEngine;

namespace TeamBasedShooter
{
    public class MusicPlayer : MonoBehaviour
    {
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private float _currentVolume = 0.5f;
        [SerializeField] private float _fadeDuration = 2f;
        private float _fadeSpeed;

        public static MusicPlayer Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                Initialize();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Initialize()
        {
            if (_audioSource == null)
            {
                _audioSource = GetComponent<AudioSource>();
            }

            SetVolume(_currentVolume);
            _fadeSpeed = 1f / _fadeDuration;
        }

        private void Start()
        {
            if (_audioSource != null)
            {
                _audioSource.Play();
            }
        }

        public void SetVolume(float volume)
        {
            _currentVolume = Mathf.Clamp(volume, 0f, 1f);
            if (_audioSource != null)
            {
                _audioSource.volume = _currentVolume;
            }
        }

        public void FadeOutMusic()
        {
            StartCoroutine(FadeVolume(0f));
        }

        public void FadeInMusic()
        {
            StartCoroutine(FadeVolume(_currentVolume));
        }

        private System.Collections.IEnumerator FadeVolume(float targetVolume)
        {
            float startVolume = _audioSource.volume;

            for (float t = 0; t < _fadeDuration; t += Time.deltaTime)
            {
                float newVolume = Mathf.Lerp(startVolume, targetVolume, t / _fadeDuration);
                _audioSource.volume = newVolume;
                yield return null;
            }

            _audioSource.volume = targetVolume;

            if (targetVolume <= 0)
            {
                _audioSource.Stop();
            }
        }

        private System.Collections.IEnumerator Replay(float newVolume)
        {
            float targetVolume = 0f;
            float startVolume = _audioSource.volume;

            for (float t = 0; t < _fadeDuration; t += Time.deltaTime)
            {
                float volume = Mathf.Lerp(startVolume, targetVolume, t / _fadeDuration);
                _audioSource.volume = volume;
                yield return null;
            }

            _audioSource.volume = targetVolume;

            if (targetVolume <= 0)
            {
                _audioSource.Stop();
                SetVolume(newVolume);
                _audioSource.Play();
            }
        }

        public void PlayMusic()
        {
            _audioSource.Play();
        }

        public void StopMusic()
        {
            _audioSource.Stop();
        }

        public void ReplayMusic(float newVolume)
        {
            StartCoroutine(Replay(newVolume));
        }
    }
}
