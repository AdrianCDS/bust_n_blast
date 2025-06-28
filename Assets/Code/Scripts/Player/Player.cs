using Fusion;
using Fusion.Addons.SimpleKCC;
using FusionHelpers;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using System.Collections.Generic;
using Newtonsoft.Json;
using PlayFab;
using PlayFab.ClientModels;

namespace TeamBasedShooter
{
    public class Player : FusionPlayer
    {
        [SerializeField, Tooltip("Player Index")]
        private int playerIndex;

        [SerializeField, Tooltip("Player Team")]
        private Team playerTeam;

        [Header("Setup")]
        private SimpleKCC KCC;
        public AimingPoint playerAimAt;
        public GameObject[] CharacterAttachments;

        private CameraController cameraController;

        [Header("Components")]
        public HitboxRoot HitboxRoot;
        public WeaponController WeaponController;
        public Health Health;
        public Animator Animator;
        public Rig playerWeaponRigLayer;

        [Header("Movement")]
        public float MoveSpeed = 6f;
        public float JumpImpulse;
        public float JumpForce = 10f;
        public float UpGravity = 15f;
        public float DownGravity = 25f;
        public float GroundAcceleration = 55f;
        public float GroundDeceleration = 25f;
        public float AirAcceleration = 25f;
        public float AirDeceleration = 1.3f;

        [Header("Sounds")]
        public AudioSource JumpSound;
        public AudioSource DeathSound;

        [Header("PlayFab Settings")]
        public GameObject[] playerAttachments;

        [Header("Other")]
        [SerializeField] private float _respawnTime = 5f;

        [Networked]
        private Vector3 MoveVelocity { get; set; }

        [Networked]
        private int _jumpCount { get; set; }

        private int _visibleJumpCount;

        [Networked]
        private int _deathCount { get; set; }

        private int _visibleDeathCount;

        [Networked] public Stage stage { get; set; }
        [Networked] private TickTimer respawnTimer { get; set; }
        [Networked] private TickTimer invulnerabilityTimer { get; set; }
        [Networked] public string ActiveAttachmentIndex { get; set; }

        private readonly List<LagCompensatedHit> _npcsDetected = new List<LagCompensatedHit>();

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

        [Networked] public bool BlockInput { get; set; }

        [Networked]
        private NetworkButtons PreviousButtons { get; set; }

        private void Awake()
        {
            KCC = GetComponent<SimpleKCC>();
            cameraController = GetComponent<CameraController>();
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

            _respawnInSeconds = 0;

            if (Runner.TryGetSingleton(out GameManager gameManager))
            {
                if (gameManager.Winner != Team.None)
                {
                    PlayerEvents.NotifyOnGameEnded(gameManager.Winner);
                }
            }

            if (Object.HasInputAuthority)
            {
                LoginPlayer();
            }
        }

        private void LoginPlayer()
        {
            if (PlayerLoggedIn) return;

            var request = new LoginWithCustomIDRequest
            {
                CustomId = PlayerDeviceId,
                CreateAccount = true
            };

            PlayFabClientAPI.LoginWithCustomID(request, OnLoginSuccess, OnError);
        }

        private void OnLoginSuccess(LoginResult result)
        {
            PlayerLoggedIn = true;
            PlayFabClientAPI.GetUserData(new GetUserDataRequest(), OnDataReceived, OnError);
        }

        private void OnDataReceived(GetUserDataResult result)
        {
            if (result.Data == null) return;

            if (result.Data.ContainsKey("Username")
                    && result.Data.ContainsKey("Rank")
                    && result.Data.ContainsKey("RankXp")
                    && result.Data.ContainsKey("Balance")
                    && result.Data.ContainsKey("OwnedItems"))
            {
                var playerOwnedItems = JsonConvert.DeserializeObject<Dictionary<string, string>>(result.Data["OwnedItems"].Value);

                string activeAttachmentIndex = FindActiveAttachment(playerOwnedItems);
                RPC_SetPlayerAttachments(activeAttachmentIndex, result.Data["RankXp"].Value, result.Data["Rank"].Value, result.Data["Balance"].Value);
            }
        }

        private string FindActiveAttachment(Dictionary<string, string> playerOwnedItems)
        {
            if (playerOwnedItems == null) return null;

            string characterPrefix = PlayerCharacter.ToString().ToLower() + "_attachment_";

            foreach (var item in playerOwnedItems)
            {
                if (item.Key.StartsWith(characterPrefix) && item.Value.ToLower() == "true")
                {
                    string index = item.Key.Substring(item.Key.LastIndexOf('_') + 1);
                    return index;
                }
            }

            return null;
        }

        private void OnError(PlayFabError result) { }

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

            if (Health.IsAlive == false)
            {
                MovePlayer();

                KCC.SetColliderLayer(LayerMask.NameToLayer("Ignore Raycast"));
                KCC.SetCollisionLayerMask(LayerMask.GetMask("Environment", "Ground"));

                HitboxRoot.HitboxRootActive = false;

                return;
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
                    case nameof(ActiveAttachmentIndex):
                        SetActiveAttachment();
                        break;
                }
            }

            var moveVelocity = GetAnimationMoveVelocity();

            Animator.SetFloat("InputX", moveVelocity.x, 0.05f, Time.deltaTime);
            Animator.SetFloat("InputY", moveVelocity.z, 0.05f, Time.deltaTime);
            Animator.SetBool("IsGrounded", KCC.IsGrounded);
            Animator.SetFloat("MovementSpeed", moveVelocity.magnitude);
            Animator.SetBool("IsAlive", Health.IsAlive);
            Animator.SetBool("IsMoving", moveVelocity.x != 0 && moveVelocity.z != 0);

            if (playerWeaponRigLayer)
            {
                Animator.SetLayerWeight(1, Health.IsAlive ? 1 : 0);
                playerWeaponRigLayer.weight = Health.IsAlive ? 1 : 0;
            }

            if (_visibleJumpCount < _jumpCount)
            {
                Animator.SetTrigger("Jump");
                JumpSound.PlayOneShot(JumpSound.clip);
            }

            if (Animator.GetCurrentAnimatorStateInfo(0).IsName("Jump") && KCC.IsGrounded)
            {
                Animator.SetBool("IsGrounded", false);
            }

            if (_visibleDeathCount < _deathCount)
            {
                DeathSound.PlayOneShot(DeathSound.clip);
            }

            _visibleJumpCount = _jumpCount;
            _visibleDeathCount = _deathCount;
        }

        private void ProcessInput(NetworkInputData input)
        {
            if (input.Buttons.IsSet(EPlayerInputButton.Pause))
            {
                bool pausePressed = input.Buttons.WasPressed(PreviousButtons, EPlayerInputButton.Pause);
                if (pausePressed)
                {
                    BlockInput = !BlockInput;
                }
            }

            if (input.AimRigPoint != Vector3.zero)
            {
                var _smoothedLookRotation = Quaternion.Slerp(KCC.LookRotation, input.LookRotation, Runner.DeltaTime * 10f);
                KCC.SetLookRotation(_smoothedLookRotation);

                // KCC.SetLookRotation(input.LookRotation);
            }

            KCC.SetGravity(KCC.RealVelocity.y >= 0f ? -UpGravity : -DownGravity);

            var inputDirection = KCC.TransformRotation * new Vector3(input.MoveDirection.x, 0f, input.MoveDirection.y);
            JumpImpulse = 0f;
            if (input.Buttons.WasPressed(PreviousButtons, EPlayerInputButton.Jump) && KCC.IsGrounded)
            {
                JumpImpulse = JumpForce;
            }

            MovePlayer(inputDirection * MoveSpeed, JumpImpulse);

            if (KCC.HasJumped)
            {
                _jumpCount++;
            }

            if (input.Buttons.IsSet(EPlayerInputButton.Aiming) && PlayerCharacter != Character.Zaphyr)
            {
                cameraController.IsAiming(true);
            }
            else
            {
                cameraController.IsAiming(false);
            }

            if (input.Buttons.IsSet(EPlayerInputButton.PrimaryAttack))
            {
                bool justPressed = input.Buttons.WasPressed(PreviousButtons, EPlayerInputButton.PrimaryAttack);
                bool wasReleased = false;

                WeaponController.PrimaryAttack(justPressed, wasReleased, input.AimPoint);
            }
            else if (!input.Buttons.IsSet(EPlayerInputButton.PrimaryAttack) && PreviousButtons.IsSet(EPlayerInputButton.PrimaryAttack))
            {
                bool justPressed = input.Buttons.WasPressed(PreviousButtons, EPlayerInputButton.PrimaryAttack);
                bool wasReleased = true;

                WeaponController.PrimaryAttack(justPressed, wasReleased, input.AimPoint);
            }

            if (input.Buttons.IsSet(EPlayerInputButton.Reload))
            {
                WeaponController.Reload();
            }

            if (input.Buttons.IsSet(EPlayerInputButton.Revive) && PlayerTeam == Team.Defenders)
            {
                MaybeReviveNPC();
            }

            if (input.Buttons.IsSet(EPlayerInputButton.SecondaryAttack))
            {
                bool justPressed = input.Buttons.WasPressed(PreviousButtons, EPlayerInputButton.SecondaryAttack);
                bool wasReleased = false;

                WeaponController.SecondaryAttack(justPressed, wasReleased, input.AimPoint);
            }

            playerAimAt.SetNewAimPoint(input.AimRigPoint);

            PreviousButtons = input.Buttons;
        }

        public void MovePlayer(Vector3 desiredMoveVelocity = default, float jumpImpulse = default)
        {
            float acceleration;

            if (desiredMoveVelocity == Vector3.zero)
            {
                acceleration = KCC.IsGrounded == true ? GroundDeceleration : AirDeceleration;
            }
            else
            {
                acceleration = KCC.IsGrounded == true ? GroundAcceleration : AirAcceleration;
            }

            MoveVelocity = Vector3.Lerp(MoveVelocity, desiredMoveVelocity, acceleration * Runner.DeltaTime);
            KCC.Move(MoveVelocity, jumpImpulse);
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

                    if (HasStateAuthority)
                    {
                        Health.CurrentHealth = Health.MaxHealth;
                        WeaponController.CurrentWeapon.ClipAmmo = WeaponController.CurrentWeapon.MaxClipAmmo;
                        WeaponController.CurrentWeapon.RemainingAmmo = WeaponController.CurrentWeapon.StartAmmo;
                    }

                    respawnTimer = TickTimer.CreateFromSeconds(Runner, 1);
                    invulnerabilityTimer = TickTimer.CreateFromSeconds(Runner, 1);

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

            _deathCount++;

            // If a player died, give points based on team
            if (Runner.TryGetSingleton(out GameManager gameManager))
            {
                switch (PlayerTeam)
                {
                    case Team.Attackers:
                        gameManager.DefendersTotalPoints += 100;
                        break;
                    case Team.Defenders:
                        gameManager.AttackersTotalPoints += 0;
                        break;
                    default:
                        break;
                }
            }

            Respawn(_respawnTime);
        }

        public void OnStageChanged()
        {
            switch (stage)
            {
                case Stage.New:
                    break;
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
            KCC.SetCollisionLayerMask(LayerMask.GetMask("Default", "Environment", "PlayerKCC", "Ground"));

            HitboxRoot.HitboxRootActive = true;
        }

        public override void TeleportOut()
        {
            if (stage == Stage.Dead || stage == Stage.TeleportOut)
                return;

            if (Object.HasStateAuthority)
                stage = Stage.TeleportOut;
        }

        private Vector3 GetAnimationMoveVelocity()
        {
            if (KCC.RealSpeed < 0.01f)
                return default;

            var velocity = KCC.RealVelocity;
            velocity.y = 0f;

            if (velocity.sqrMagnitude > 1f)
            {
                velocity.Normalize();
            }

            return transform.InverseTransformVector(velocity);
        }

        public void MaybeReviveNPC()
        {
            Vector3 revivePosition = transform.position;
            float reviveRadius = 5.0f;

            var hitOptions = HitOptions.SubtickAccuracy | HitOptions.IncludePhysX | HitOptions.IgnoreInputAuthority;

            Runner.LagCompensation.OverlapSphere(
                revivePosition,
                reviveRadius,
                player: Object.InputAuthority,
                _npcsDetected,
                options: hitOptions
            );

            HashSet<NPC> revivedNPCs = new HashSet<NPC>();

            foreach (var hit in _npcsDetected)
            {
                if (hit.Hitbox == null) continue;

                NPC npc = hit.Hitbox.GetComponentInParent<NPC>();

                if (npc == null || npc.Health.IsAlive) continue;
                if (revivedNPCs.Contains(npc)) continue;

                if (HasStateAuthority)
                {
                    npc.Revive();
                }

                revivedNPCs.Add(npc);
            }
        }

        private void SetActiveAttachment()
        {
            string characterPrefix = PlayerCharacter.ToString().ToLower() + "_attachment_";

            // Activate only the attachments that are marked as active
            for (int i = 0; i < playerAttachments.Length; i++)
            {
                if (playerAttachments[i] == null)
                    continue;

                string attachmentName = characterPrefix + (i + 1).ToString("D2");
                string attachmentIndex = (i + 1).ToString("D2");

                if (attachmentIndex == ActiveAttachmentIndex)
                {
                    playerAttachments[i].SetActive(true);
                }
            }
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        private void RPC_SetPlayerAttachments(string activeAttachmentName, string playerRankXp, string playerRank, string playerBalance)
        {
            ActiveAttachmentIndex = activeAttachmentName;
            PlayerRankXp = playerRankXp;
            PlayerRank = playerRank;
            PlayerBalance = playerBalance;
        }

        private void ComputeNewBalance()
        {
            int oldBalance = int.Parse(PlayerBalance);
            int newBalance = oldBalance + 100;

            PlayerBalance = newBalance.ToString();
        }

        public void ComputeRankXp(Team winningTeam)
        {
            if (!Object.HasInputAuthority) return;

            bool eligibleForRankChange;
            int currentXp = int.Parse(PlayerRankXp);
            int xpChange = 30;
            int calculatedXp;
            int xpDifference;

            if (PlayerTeam == winningTeam)
            {
                ComputeNewBalance();

                calculatedXp = currentXp + xpChange;

                if (calculatedXp >= 100)
                {
                    eligibleForRankChange = true;
                    xpDifference = calculatedXp - 100;
                }
                else
                {
                    eligibleForRankChange = false;
                    xpDifference = 0;
                }

                MaybeRankUp(eligibleForRankChange, calculatedXp, xpDifference, PlayerRank);
            }
            else
            {
                calculatedXp = currentXp - xpChange;

                if (calculatedXp < 0)
                {
                    eligibleForRankChange = true;
                    xpDifference = Mathf.Abs(calculatedXp);
                }
                else
                {
                    eligibleForRankChange = false;
                    xpDifference = 0;
                }

                MaybeRankDown(eligibleForRankChange, calculatedXp, xpDifference, PlayerRank);
            }

            UpdateUserData();
        }

        private void MaybeRankUp(bool eligibleForRankChange, int calculatedXp, int difference, string currentPlayerRank)
        {
            if (!eligibleForRankChange)
            {
                string newCalculatedXp = calculatedXp.ToString();
                PlayerRankXp = newCalculatedXp;

                return;
            }

            string currentRank = currentPlayerRank;
            int currentRankNumber = int.Parse(currentRank.Split('_')[1]);
            int newRankNumber = Mathf.Min(currentRankNumber + 1, 9);

            string newRankName = $"rank_{newRankNumber}";

            int scaledDiff = currentRank != "rank_9" ? difference : calculatedXp;
            string newRankXp = scaledDiff.ToString();

            PlayerRank = newRankName;
            PlayerRankXp = newRankXp;
        }

        private void MaybeRankDown(bool eligibleForRankChange, int calculatedXp, int difference, string currentPlayerRank)
        {
            if (!eligibleForRankChange)
            {
                string newCalculatedXp = calculatedXp.ToString();
                PlayerRankXp = newCalculatedXp;

                return;
            }

            string currentRank = currentPlayerRank;
            int currentRankNumber = int.Parse(currentRank.Split('_')[1]);
            int newRankNumber = Mathf.Max(currentRankNumber - 1, 1);

            string newRankName = $"rank_{newRankNumber}";

            int scaledDiff = currentRank != "rank_1" ? 100 - difference : 0;
            string newRankXp = scaledDiff.ToString();

            PlayerRank = newRankName;
            PlayerRankXp = newRankXp;
        }

        private void UpdateUserData()
        {
            var request = new UpdateUserDataRequest
            {
                Data = new Dictionary<string, string> {
                    { "Rank", PlayerRank},
                    { "RankXp", PlayerRankXp},
                    { "Balance", PlayerBalance}
                }
            };

            PlayFabClientAPI.UpdateUserData(request, OnDataSend, OnError);
        }

        private void OnDataSend(UpdateUserDataResult result) { }
    }
}
