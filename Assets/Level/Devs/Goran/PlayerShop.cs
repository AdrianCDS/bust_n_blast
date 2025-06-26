using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

namespace TeamBasedShooter
{
    public class PlayerShop : MonoBehaviour
    {
        [SerializeField] private GameObject buyModal;
        [SerializeField] private GameObject insufficientFundsModal;
        [SerializeField] private TMP_Text balanceText;
        [SerializeField] private TMP_Text buyModalItemNameText;
        [SerializeField] private TMP_Text buyModalItemPriceText;
        [SerializeField] private PlayFabMenuController playFabMenuController;

        public Dictionary<string, string> playerOwnedItems;
        public string playerBalance;

        private string selectedItemName;
        private int selectedItemPrice;
        private bool isInitializing = false;

        private Dictionary<ToggleGroup, Toggle> currentTogglesInGroups = new Dictionary<ToggleGroup, Toggle>();

        private void Awake()
        {
            buyModal.SetActive(false);
            insufficientFundsModal.SetActive(false);
        }

        public void SetBalance(string amount)
        {
            playerBalance = amount;
            balanceText.text = $"{playerBalance}$";
        }

        public void SetItems(Dictionary<string, string> ownedItems)
        {
            isInitializing = true;

            Toggle[] toggles = GetComponentsInChildren<Toggle>(true);
            currentTogglesInGroups.Clear();

            foreach (var toggle in toggles)
            {
                string toggleName = toggle.gameObject.name;

                if (ownedItems.TryGetValue(toggleName, out string isEquipped))
                {
                    toggle.isOn = isEquipped.ToLower() == "true";

                    if (toggle.isOn && toggle.group != null)
                    {
                        currentTogglesInGroups[toggle.group] = toggle;
                    }

                    TextMeshProUGUI priceText = toggle.GetComponentInChildren<TextMeshProUGUI>();
                    if (priceText != null)
                    {
                        priceText.text = "";
                    }
                }
            }

            playerOwnedItems = ownedItems;
            isInitializing = false;
        }

        public void HandleToggleValueChanged(Toggle changedToggle)
        {
            if (isInitializing) return;

            if (changedToggle == null) return;

            bool isOn = changedToggle.isOn;
            string itemName = changedToggle.gameObject.name;

            if (isOn)
            {
                if (changedToggle.group != null && !playerOwnedItems.ContainsKey(itemName))
                {
                    if (!currentTogglesInGroups.ContainsKey(changedToggle.group))
                    {
                        foreach (Toggle groupToggle in changedToggle.group.ActiveToggles())
                        {
                            if (groupToggle != changedToggle && playerOwnedItems.ContainsKey(groupToggle.gameObject.name))
                            {
                                currentTogglesInGroups[changedToggle.group] = groupToggle;
                                break;
                            }
                        }
                    }
                }

                ProcessItemSelection(itemName);
            }
        }

        private void ProcessItemSelection(string itemName)
        {
            if (playerOwnedItems != null && playerOwnedItems.ContainsKey(itemName))
            {
                Toggle selectedToggle = FindToggleByName(itemName);
                ToggleGroup toggleGroup = selectedToggle?.group;

                if (toggleGroup != null)
                {
                    foreach (var toggle in toggleGroup.GetComponentsInChildren<Toggle>())
                    {
                        string toggleName = toggle.gameObject.name;
                        if (playerOwnedItems.ContainsKey(toggleName) && toggleName != itemName)
                        {
                            playerOwnedItems[toggleName] = "false";
                        }
                    }
                }

                playerOwnedItems[itemName] = "true";

                if (toggleGroup != null)
                {
                    currentTogglesInGroups[toggleGroup] = selectedToggle;
                }

                playFabMenuController.SaveUserItems();
                return;
            }

            TextMeshProUGUI priceText = FindPriceTextForItem(itemName);
            if (priceText != null && !string.IsNullOrEmpty(priceText.text))
            {
                int price = 0;
                if (int.TryParse(priceText.text, out price))
                {
                    int currentBalance = 0;
                    if (int.TryParse(playerBalance, out currentBalance))
                    {
                        if (currentBalance >= price)
                        {
                            selectedItemName = itemName;
                            selectedItemPrice = price;

                            if (buyModalItemNameText != null)
                            {
                                buyModalItemNameText.text = $"DO YOU WANT TO PURCHASE `{itemName}` ?";

                            }

                            if (buyModalItemPriceText != null)
                            {
                                buyModalItemPriceText.text = $"BUY FOR {price.ToString()}$";
                            }

                            ShowBuyModal();
                        }
                        else
                        {
                            Toggle currentToggle = FindToggleByName(itemName);
                            if (currentToggle != null && currentToggle.group != null)
                            {
                                if (currentTogglesInGroups.TryGetValue(currentToggle.group, out Toggle previousToggle))
                                {

                                    isInitializing = true;
                                    foreach (Toggle groupToggle in currentToggle.group.GetComponentsInChildren<Toggle>())
                                    {
                                        groupToggle.isOn = false;
                                    }

                                    previousToggle.isOn = true;
                                    isInitializing = false;
                                }
                            }

                            ShowInsufficientFundsModal();
                        }
                    }
                }
            }
        }

        private Toggle FindToggleByName(string toggleName)
        {
            Toggle[] toggles = GetComponentsInChildren<Toggle>(true);
            foreach (var toggle in toggles)
            {
                if (toggle.gameObject.name == toggleName)
                {
                    return toggle;
                }
            }
            return null;
        }

        private TextMeshProUGUI FindPriceTextForItem(string itemName)
        {
            Toggle toggle = FindToggleByName(itemName);
            if (toggle != null)
            {
                return toggle.GetComponentInChildren<TextMeshProUGUI>();
            }
            return null;
        }

        public void ConfirmBuyItem()
        {
            if (string.IsNullOrEmpty(selectedItemName) || selectedItemPrice <= 0)
            {
                return;
            }

            // Deduct balance
            int currentBalance = int.Parse(playerBalance);
            currentBalance -= selectedItemPrice;
            SetBalance(currentBalance.ToString());

            // Add item to owned items
            if (playerOwnedItems != null)
            {
                // Find the toggle and its group
                Toggle selectedToggle = FindToggleByName(selectedItemName);
                ToggleGroup toggleGroup = selectedToggle?.group;

                // First, set all items in this toggle group to unequipped
                if (toggleGroup != null)
                {
                    foreach (var toggle in toggleGroup.GetComponentsInChildren<Toggle>())
                    {
                        string toggleName = toggle.gameObject.name;
                        if (playerOwnedItems.ContainsKey(toggleName))
                        {
                            playerOwnedItems[toggleName] = "false";
                        }
                    }
                }

                // Then mark this item as owned and equipped
                playerOwnedItems[selectedItemName] = "true";

                // Update our current toggle tracking
                if (toggleGroup != null)
                {
                    currentTogglesInGroups[toggleGroup] = selectedToggle;
                }

                // Update UI for the purchased item
                if (selectedToggle != null)
                {
                    TextMeshProUGUI priceText = selectedToggle.GetComponentInChildren<TextMeshProUGUI>();
                    if (priceText != null)
                    {
                        priceText.text = "";
                    }
                }
            }

            playFabMenuController.SaveUserItems();

            // Hide the modal without restoring previous toggle
            HideBuyModal(false);
        }

        public void ShowBuyModal()
        {
            buyModal.SetActive(true);
        }

        public void HideBuyModal(bool shouldRestorePreviousToggle = true)
        {
            buyModal.SetActive(false);

            if (shouldRestorePreviousToggle && !string.IsNullOrEmpty(selectedItemName))
            {
                // Find the toggle for the selected item
                Toggle currentToggle = FindToggleByName(selectedItemName);
                if (currentToggle != null && currentToggle.group != null)
                {
                    // Check if we have a previous toggle to restore
                    if (currentTogglesInGroups.TryGetValue(currentToggle.group, out Toggle previousToggle))
                    {
                        // Set all toggles to false first
                        isInitializing = true;
                        foreach (Toggle groupToggle in currentToggle.group.GetComponentsInChildren<Toggle>())
                        {
                            groupToggle.isOn = false;
                        }

                        // Set the previous toggle back to true
                        previousToggle.isOn = true;
                        isInitializing = false;
                    }
                }
            }

            selectedItemName = "";
            selectedItemPrice = 0;
        }

        public void ShowInsufficientFundsModal()
        {
            insufficientFundsModal.SetActive(true);
        }

        public void HideInsufficientFundsModal()
        {
            insufficientFundsModal.SetActive(false);
            selectedItemName = "";
            selectedItemPrice = 0;
        }
    }
}