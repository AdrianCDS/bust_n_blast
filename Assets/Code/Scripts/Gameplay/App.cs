using Fusion;
using Fusion.Addons.Physics;
using FusionHelpers;
using TMPro;
using UnityEngine;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace TeamBasedShooter
{
    public class App : MonoBehaviour
    {
        [SerializeField] private LevelManager _levelManager;
        [SerializeField] private GameManager _gameManagerPrefab;
        [SerializeField] private TextMeshProUGUI _progress;
        [SerializeField] private Panel _uiStart;
        [SerializeField] private Panel _uiProgress;
        [SerializeField] private GameObject _uiGame;

        private FusionLauncher.ConnectionStatus _status = FusionLauncher.ConnectionStatus.Disconnected;

        public NetworkRunner RunnerPrefab;
        public HathoraClient ClientPrefab;

        private void Awake()
        {
            DontDestroyOnLoad(this);

            _levelManager.onStatusUpdate = OnConnectionStatusUpdate;
        }

        private void Start()
        {
            OnConnectionStatusUpdate(null, FusionLauncher.ConnectionStatus.Disconnected, "");
        }

        private void Update()
        {
            if (_uiProgress.IsShowing)
            {
                if (_status != FusionLauncher.ConnectionStatus.Connected && Input.GetKeyUp(KeyCode.Escape))
                {
                    FusionLauncher launcher = FindObjectOfType<FusionLauncher>();
                    if (launcher != null)
                    {
                        launcher.Terminate();
                    }
                }

                UpdateUI();
            }
        }

        public void OnQuickPlay()
        {
            if (ToggleUI(_uiStart))
            {
                FusionLauncher.Launch(RunnerPrefab, ClientPrefab, _gameManagerPrefab, _levelManager, false, OnConnectionStatusUpdate);
            }
        }

        public void OnServerPlay()
        {
            if (ToggleUI(_uiStart))
            {
                FusionLauncher.Launch(RunnerPrefab, ClientPrefab, _gameManagerPrefab, _levelManager, true, OnConnectionStatusUpdate);
            }
        }

        private bool ToggleUI(Panel ui)
        {
            if (!ui.IsShowing) return false;

            ui.SetVisible(false);
            return true;
        }

        public void OnConnectionStatusUpdate(NetworkRunner runner, FusionLauncher.ConnectionStatus status, string reason)
        {
            if (!this) return;

            if (status != _status)
            {
                switch (status)
                {
                    case FusionLauncher.ConnectionStatus.Disconnected:
                        ErrorBox.Show("Disconnected!", reason, () => { });

                        var playFabMenuController = FindObjectOfType<PlayFabMenuController>();
                        if (playFabMenuController != null)
                        {
                            playFabMenuController.LoadUserSettings();
                        }
                        
                        break;
                    case FusionLauncher.ConnectionStatus.Failed:
                        ErrorBox.Show("Error!", reason, () => { });
                        break;
                    case FusionLauncher.ConnectionStatus.Loading:
                        MusicPlayer.Instance.FadeOutMusic();
                        break;
                }
            }

            _status = status;
            UpdateUI();
        }

        private void UpdateUI()
        {
            bool intro = false;
            bool progress = false;
            bool running = false;

            switch (_status)
            {
                case FusionLauncher.ConnectionStatus.Disconnected:
                    _progress.text = "Disconnected!";
                    intro = true;
                    break;
                case FusionLauncher.ConnectionStatus.Failed:
                    _progress.text = "Failed!";
                    intro = true;
                    break;
                case FusionLauncher.ConnectionStatus.Connecting:
                    _progress.text = "Searching";
                    progress = true;
                    break;
                case FusionLauncher.ConnectionStatus.Connected:
                    _progress.text = "Connected";
                    progress = true;
                    break;
                case FusionLauncher.ConnectionStatus.Loading:
                    _progress.text = "Connecting";
                    progress = true;
                    break;
                case FusionLauncher.ConnectionStatus.Loaded:
                    running = true;
                    break;
            }

            _uiStart.SetVisible(intro);
            _uiProgress.SetVisible(progress);
            _uiGame.SetActive(running);
        }
    }
}
