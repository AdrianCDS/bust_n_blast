using System;
using System.Collections.Generic;
using Fusion;
using FusionHelpers;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace TeamBasedShooter
{
	public class ReadyUpManager : NetworkBehaviour
	{
		[SerializeField] private GameObject _disconnectInfoText;
		[SerializeField] private GameObject _readyupInfoText;
		[SerializeField] private TMP_Text _readyText;
		[SerializeField] private TMP_Text _playersCountText;
		[SerializeField] private GameObject _lobbyInfo;
		[SerializeField] private TMP_Text _attackersReadyCountText;
		[SerializeField] private TMP_Text _defendersReadyCountText;

		private float _delay;

		public void UpdateUI(GameManager.PlayState playState, IEnumerable<FusionPlayer> allPlayers, Action onAllPlayersReady)
		{
			if (_delay > 0)
			{
				_delay -= Time.deltaTime;
				return;
			}

			if (playState != GameManager.PlayState.LOBBY)
			{
				gameObject.SetActive(false);
				return;
			}

			gameObject.SetActive(true);

			int playerCount = 0, readyCount = 0, attackersReadyCount = 0, defendersReadyCount = 0;
			foreach (FusionPlayer fusionPlayer in allPlayers)
			{
				if (fusionPlayer.Ready)
				{
					readyCount++;

					if (fusionPlayer.PlayerTeam == Team.Attackers) attackersReadyCount++;
					else if (fusionPlayer.PlayerTeam == Team.Defenders) defendersReadyCount++;
				}

				playerCount++;
			}

			bool allPlayersReady = readyCount != 0 && playerCount != 0 && readyCount == playerCount;

			_disconnectInfoText.SetActive(!allPlayersReady);
			_readyupInfoText.SetActive(!allPlayersReady);

			UpdateReadyInfo(attackersReadyCount, defendersReadyCount);
			_lobbyInfo.SetActive(!allPlayersReady);

			if (allPlayersReady)
			{
				_delay = 2.0f;
				onAllPlayersReady();
			}
		}

		public void UpdateLobbyInfo(int playerCount)
		{
			_playersCountText.text = $"{playerCount} / 4";
		}

		public void UpdateReadyInfo(int attackersReadyCount, int defendersReadyCount)
		{
			_attackersReadyCountText.text = $"{attackersReadyCount} attackers are ready";
			_defendersReadyCountText.text = $"{defendersReadyCount} defenders are ready";
		}

		public void UpdateReadyText(bool ready)
		{
			_readyText.text = ready ? "NOT READY" : "READY";
		}
	}
}
