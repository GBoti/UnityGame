using UnityEngine;
using TMPro;
using UnityEngine.UI;
using DishevelledBadger.FlashFrostVale.Networking;

namespace DishevelledBadger.FlashFrostVale.Menus
{
    public class JoinLobbyMenu : MonoBehaviour
    {
        [SerializeField] private NetworkManagerFFV networkManager = null;

        [Header("UI")]
        [SerializeField] private GameObject landingPagePanel = null;
        [SerializeField] private TMP_InputField ipAddressInputField = null;
        [SerializeField] private Button joinButton;

        [SerializeField] private PlayerNameInput playerNameInput;

        private void OnEnable()
        {
            NetworkManagerFFV.OnClientConnectedToManager += HandleClientConnected;
            NetworkManagerFFV.OnClientDisconnectedFromManager += HandleClientDisconnected;

            if (PlayerPrefs.HasKey("LastIPAddress"))
            {
                ipAddressInputField.text = PlayerPrefs.GetString("LastIPAddress");
            }
            else
            {
                ipAddressInputField.text = "localhost"; // Default IP for testing
            }
        }

        private void OnDisable()
        {
            NetworkManagerFFV.OnClientConnectedToManager -= HandleClientConnected;
            NetworkManagerFFV.OnClientDisconnectedFromManager -= HandleClientDisconnected;
        }

        public void JoinLobby()
        {
            playerNameInput.SavePlayerName();
            
            string ipAddress = ipAddressInputField.text;
            if (string.IsNullOrWhiteSpace(ipAddress))
            {
                ipAddress = "localhost";
                Debug.LogWarning("JoinLobbyMenu: IP address was empty, defaulting to localhost.");
            }
            PlayerPrefs.SetString("LastIPAddress", ipAddress);

            networkManager.networkAddress = ipAddress;
            networkManager.StartClient();

            joinButton.interactable = false;
            Debug.Log($"JoinLobbyMenu: Attempting to join lobby at IP: {ipAddress}");
        }

        private void HandleClientConnected()
        {
            joinButton.interactable = true;

            gameObject.SetActive(false);
            landingPagePanel.SetActive(false);
            Debug.Log("JoinLobbyMenu: Client successfully connected. Hiding menu.");
        }

        private void HandleClientDisconnected()
        {
            joinButton.interactable = true;
            gameObject.SetActive(true);
            landingPagePanel.SetActive(true);
            Debug.Log("JoinLobbyMenu: Client disconnected. Displaying menu again.");
        }
    }
}
