using Fusion;
using Fusion.Sockets;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using FusionHelpers;

namespace TeamBasedShooter
{
	public enum EPlayerInputButton
	{
		Jump,
		PrimaryAttack,
		SecondaryAttack,
		Aiming,
		Reload,
		SwitchCharacter,
		Ready,
		Revive,
		Pause
	}

	public struct NetworkInputData : INetworkInput
	{
		public Vector2 MoveDirection;
		public NetworkButtons Buttons;

		public Vector3 AimPoint;
		public Vector3 AimRigPoint;
		public Quaternion LookRotation;
	}

	public class InputController : NetworkBehaviour, INetworkRunnerCallbacks
	{
		private NetworkInputData _accumulatedInput;

		public override void Spawned()
		{
			if (Object.HasInputAuthority)
			{
				Runner.AddCallbacks(this);
			}
		}

		private void Update()
		{
			if (!HasInputAuthority) return;

			if (Camera.main == null) return;

			var keyboard = Keyboard.current;
			var mouse = Mouse.current;

			if (keyboard == null) return;
			if (mouse == null) return;

			bool canPlay = true;

			if (Runner.TryGetSingleton(out GameManager gameManager))
			{
				canPlay = gameManager.CurrentPlayState == GameManager.PlayState.LEVEL;
			}

			if (Object.TryGetComponent(out Player player))
			{
				canPlay = !player.BlockInput;
			}

			_accumulatedInput.Buttons.Set(EPlayerInputButton.Pause, keyboard.escapeKey.isPressed);

			if (canPlay)
			{
				// Keyboard handling
				var moveDirection = Vector2.zero;

				if (keyboard.wKey.isPressed) { moveDirection += Vector2.up; }
				if (keyboard.sKey.isPressed) { moveDirection += Vector2.down; }
				if (keyboard.aKey.isPressed) { moveDirection += Vector2.left; }
				if (keyboard.dKey.isPressed) { moveDirection += Vector2.right; }

				_accumulatedInput.MoveDirection = moveDirection.normalized;

				_accumulatedInput.Buttons.Set(EPlayerInputButton.Jump, keyboard.spaceKey.isPressed);
				_accumulatedInput.Buttons.Set(EPlayerInputButton.Reload, keyboard.rKey.isPressed);
				_accumulatedInput.Buttons.Set(EPlayerInputButton.Revive, keyboard.eKey.isPressed);

				// Mouse handling
				Vector3 playerAimDirection;
				Vector3 playerAimPoint;

				Ray aimPointRay = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));

				_accumulatedInput.AimRigPoint = aimPointRay.GetPoint(7f);

				Ray ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
				if (Physics.Raycast(ray, out RaycastHit rayCastHit))
				{
					playerAimDirection = ray.direction;
					playerAimPoint = rayCastHit.point;

					playerAimDirection.y = 0;
					playerAimDirection.Normalize();
					Quaternion lookRotation = Quaternion.LookRotation(playerAimDirection);

					_accumulatedInput.LookRotation = lookRotation;
					_accumulatedInput.AimPoint = playerAimPoint;
				}

				_accumulatedInput.Buttons.Set(EPlayerInputButton.PrimaryAttack, mouse.leftButton.isPressed);
				_accumulatedInput.Buttons.Set(EPlayerInputButton.Aiming, mouse.rightButton.isPressed);
				_accumulatedInput.Buttons.Set(EPlayerInputButton.SecondaryAttack, keyboard.ctrlKey.isPressed);
			}
			else
			{
				_accumulatedInput.Buttons.Set(EPlayerInputButton.Ready, keyboard.rKey.isPressed);
				_accumulatedInput.Buttons.Set(EPlayerInputButton.SwitchCharacter, keyboard.tKey.isPressed);
			}
		}

		public void OnInput(NetworkRunner runner, NetworkInput networkInput)
		{
			networkInput.Set(_accumulatedInput);
		}

		public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
		public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
		public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
		public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
		public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
		public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }

		public void OnConnectedToServer(NetworkRunner runner) { }
		public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }

		public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
		public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
		public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
		public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
		public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
		public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
		public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }

		public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
		public void OnSceneLoadDone(NetworkRunner runner) { }
		public void OnSceneLoadStart(NetworkRunner runner) { }
	}
}
