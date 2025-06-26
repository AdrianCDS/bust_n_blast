using Fusion;
using Fusion.Addons.SimpleKCC;
using FusionHelpers;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using System.Collections.Generic;
using System.Linq;

namespace TeamBasedShooter
{
    public class TemplatePlayer : FusionPlayer
    {
        [SerializeField, Tooltip("Player Index")]
        private int playerIndex;

        [SerializeField, Tooltip("Player Team")]
        private Team playerTeam;

        // Dictionary to track which characters have had their attachments set
        private Dictionary<Character, bool> attachmentsInitialized = new Dictionary<Character, bool>();

        [Header("Setup")]
        private SimpleKCC KCC;
        public HitboxRoot HitboxRoot;

        [Header("Other")]
        [SerializeField] private float _respawnTime = 5f;

        [Networked] public Stage stage { get; set; }
        [Networked] private TickTimer respawnTimer { get; set; }

        [Networked] public int NetworkedCharacterIndex { get; set; }

        public enum Stage
        {
            New,
            TeleportOut,
            TeleportIn,
            Active,
            Dead
        }

        public bool isRespawningDone => stage == Stage.TeleportIn && respawnTimer.Expired(Runner);
        private float _respawnInSeconds = -1;
        private ChangeDetector _changes;

        [Networked]
        private NetworkButtons PreviousButtons { get; set; }

        private static readonly string[] CharacterNames = { "Bulwark", "Maverick", "Zaphyr", "Nadja" };

        private void Awake()
        {
            KCC = GetComponent<SimpleKCC>();
        }

        public override void InitNetworkState()
        {
            stage = Stage.New;
        }

        public override void Spawned()
        {
            base.Spawned();

            DontDestroyOnLoad(gameObject);

            playerIndex = PlayerIndex;
            playerTeam = PlayerTeam;

            _changes = GetChangeDetector(ChangeDetector.Source.SimulationState);

            ResetReady();

            OnStageChanged();
            OnPlayerCharacterChanged();

            _respawnInSeconds = 0;
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            base.Despawned(runner, hasState);
        }

        public override void FixedUpdateNetwork()
        {
            if (Object.HasStateAuthority)
            {
                CheckRespawn();

                if (isRespawningDone)
                {
                    ResetPlayer();
                }
            }

            if (GetInput(out NetworkInputData input))
            {
                ProcessInput(input);
            }
        }

        public override void Render()
        {
            foreach (var change in _changes.DetectChanges(this))
            {
                switch (change)
                {
                    case nameof(stage):
                        OnStageChanged();
                        break;
                    case nameof(NetworkedCharacterIndex):
                        OnPlayerCharacterChanged();
                        break;
                }
            }
        }

        private void ProcessInput(NetworkInputData input)
        {
            if (input.Buttons.WasPressed(PreviousButtons, EPlayerInputButton.Ready)) ToggleReady();

            if (input.Buttons.IsSet(EPlayerInputButton.Pause) && Runner.TryGetSingleton(out GameManager gameManager))
            {
                gameManager.Restart();
            }

            PreviousButtons = input.Buttons;
        }

        public void OnCharacterButtonClicked(int characterIndex)
        {
            if (Object.HasInputAuthority)
            {
                RPC_RequestCharacterChange(characterIndex);
            }
        }

        public override void Respawn(float inSeconds = 0)
        {
            _respawnInSeconds = inSeconds;
        }

        private void CheckRespawn()
        {
            if (_respawnInSeconds >= 0)
            {
                _respawnInSeconds -= Runner.DeltaTime;

                if (_respawnInSeconds <= 0)
                {
                    SpawnPoint spawnPoint = Runner.GetLevelManager().GetPlayerSpawnPoint(PlayerId, PlayerTeam);
                    if (spawnPoint == null)
                    {
                        _respawnInSeconds = Runner.DeltaTime;
                        return;
                    }

                    _respawnInSeconds = -1;

                    respawnTimer = TickTimer.CreateFromSeconds(Runner, 1);

                    Transform spawnAt = spawnPoint.transform;

                    KCC.SetPosition(spawnAt.position);
                    KCC.SetLookRotation(spawnAt.rotation);

                    spawnPoint.IsFree = false;
                    spawnPoint.OwnedBy = PlayerId;

                    if (stage != Stage.Active) stage = Stage.TeleportIn;
                }
            }
        }

        public void OnDeath()
        {
            stage = Stage.Dead;
            Respawn(_respawnTime);
        }

        public void OnStageChanged()
        {
            switch (stage)
            {
                case Stage.TeleportIn:
                    break;
                case Stage.Active:
                    break;
                case Stage.Dead:
                    break;
                case Stage.TeleportOut:
                    break;
            }
        }

        private void ResetPlayer()
        {
            stage = Stage.Active;


            KCC.SetColliderLayer(LayerMask.NameToLayer("PlayerKCC"));
            KCC.SetCollisionLayerMask(LayerMask.GetMask("Default", "Environment", "PlayerKCC"));

            HitboxRoot.HitboxRootActive = true;
        }

        public override void TeleportOut()
        {
            if (stage == Stage.Dead || stage == Stage.TeleportOut)
                return;

            if (Object.HasStateAuthority)
                stage = Stage.TeleportOut;
        }

        private void OnPlayerCharacterChanged()
        {
            foreach (string charName in CharacterNames)
            {
                Transform characterTransform = transform.Find(charName);
                if (characterTransform != null)
                {
                    characterTransform.gameObject.SetActive(charName == PlayerCharacter.ToString());

                    // If this is the active character, equip its attachments
                    // if (charName == PlayerCharacter.ToString())
                    // {
                    //     // Only set attachments if they haven't been set before for this character
                    //     if (!attachmentsInitialized.ContainsKey(PlayerCharacter) || !attachmentsInitialized[PlayerCharacter])
                    //     {
                    //         SetCharacterAttachments(characterTransform);
                    //         attachmentsInitialized[PlayerCharacter] = true;
                    //     }
                    // }
                }
            }
        }

        // private void SetCharacterAttachments(Transform characterTransform)
        // {
        //     Dictionary<string, string> playerOwnedItems = PlayFabManager.Instance.playerShop.playerOwnedItems;

        //     string characterName = PlayerCharacter.ToString().ToLower();
        //     string attachmentPrefix = characterName + "_attachment_";

        //     Transform[] allChildTransforms = characterTransform.GetComponentsInChildren<Transform>(true);

        //     foreach (var item in playerOwnedItems)
        //     {
        //         if (item.Key.StartsWith(attachmentPrefix) && item.Value.ToLower() == "true")
        //         {
        //             foreach (Transform child in allChildTransforms)
        //             {
        //                 if (child.name == item.Key)
        //                 {
        //                     child.gameObject.SetActive(true);
        //                     break;
        //                 }
        //             }
        //         }
        //     }
        // }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        public void RPC_RequestCharacterChange(int characterIndex)
        {
            NetworkedCharacterIndex = characterIndex;
            SwitchCharacter(NetworkedCharacterIndex);
        }
    }
}
