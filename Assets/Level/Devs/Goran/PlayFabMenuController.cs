using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using System.Collections.Generic;
using FusionHelpers;
using Newtonsoft.Json;

namespace TeamBasedShooter
{
    public class PlayFabMenuController : MonoBehaviour
    {
        public PlayerSettings playerSettings;
        public PlayerShop playerShop;

        private void Start()
        {
            PlayerPrefs.SetString("DeviceId", SystemInfo.deviceUniqueIdentifier);
            Login();
        }

        private void Login()
        {
            var request = new LoginWithCustomIDRequest
            {
                CustomId = SystemInfo.deviceUniqueIdentifier,
                CreateAccount = true
            };

            PlayFabClientAPI.LoginWithCustomID(request, OnLoginSuccess, OnError);
        }

        private void OnLoginSuccess(LoginResult result)
        {
            LoadUserSettings();
        }

        public void LoadUserSettings()
        {
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
                var ownedItemsDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(result.Data["OwnedItems"].Value);

                playerSettings.SetSettings(result.Data["Username"].Value, result.Data["Rank"].Value, result.Data["RankXp"].Value);

                playerShop.SetBalance(result.Data["Balance"].Value);
                playerShop.SetItems(ownedItemsDict);
            }
            else
            {
                var defaultItems = new Dictionary<string, string>
                {
                    { "maverick_attachment_01", "true" },
                    { "bulwark_attachment_01", "true" },
                    { "nadja_attachment_01", "true" },
                    { "zaphyr_attachment_01", "true" },
                };

                string ownedItemsJson = JsonConvert.SerializeObject(defaultItems);

                var request = new UpdateUserDataRequest
                {
                    Data = new Dictionary<string, string> {
                    { "Username", "" },
                    { "Rank", "rank_1"},
                    { "RankXp", "0"},
                    { "OwnedItems", ownedItemsJson},
                    { "Balance", "0"}
                }
                };

                playerShop.SetBalance("0");
                playerShop.SetItems(defaultItems);
                PlayFabClientAPI.UpdateUserData(request, OnDataSend, OnError);
            }
        }

        private void OnDataSend(UpdateUserDataResult result)
        {
            playerSettings.SetSettings(playerSettings.Username, playerSettings.Rank, playerSettings.RankXp);
        }

        private void OnError(PlayFabError result) { }

        public void SaveUserSettings()
        {
            var request = new UpdateUserDataRequest
            {
                Data = new Dictionary<string, string> {
                    { "Username", playerSettings.Username }
                }
            };

            PlayFabClientAPI.UpdateUserData(request, OnDataSend, OnError);
        }

        public void SaveUserItems()
        {
            string ownedItemsJson = JsonConvert.SerializeObject(playerShop.playerOwnedItems);

            var request = new UpdateUserDataRequest
            {
                Data = new Dictionary<string, string> {
                    { "Balance", playerShop.playerBalance},
                    { "OwnedItems", ownedItemsJson},
                }
            };

            PlayFabClientAPI.UpdateUserData(request, OnDataSend, OnError);
        }
    }
}