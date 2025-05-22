using UnityEngine;
using TMPro;
using UnityEngine.UI;
using DishevelledBadger.FlashFrostVale.Networking; // Namespace for network-related classes.
using DishevelledBadger.FlashFrostVale.Globals; // Namespace for project wide helpers like DebugManager.

// Namespace for menu-related scripts.
namespace DishevelledBadger.FlashFrostVale.Menus
{
    /// <summary>
    /// Manages the UI panel and logic for a client to join an existing lobby.
    /// </summary>
    public class JoinLobbyMenu : MonoBehaviour
    {
        // Reference to the custom NetworkManager. Assigned in Inspector.
        [SerializeField] private NetworkManagerFFV networkManager = null;

        [Header("UI")]
        // Reference to the main landing page panel, to be hidden when this menu is active or connection succeeds. Assigned in Inspector.
        [SerializeField] private GameObject landingPagePanel = null;
        // Input field for the user to enter the IP address of the host. Assigned in Inspector.
        [SerializeField] private TMP_InputField ipAddressInputField = null;
        // Button to initiate the connection attempt. Assigned in Inspector.
        [SerializeField] private Button joinButton = null;
        // Reference to the script that handles player name input and saving. Assigned in Inspector.
        [SerializeField] private PlayerNameInput playerNameInput = null;

        /// <summary>
        /// Called when the GameObject becomes enabled and active.
        /// Subscribes to network connection events and loads the last used IP address.
        /// </summary>
        private void OnEnable()
        {
            // Subscribe to events from the NetworkManager to handle connection success/failure.
            NetworkManagerFFV.OnClientConnectedToManager += HandleClientConnected;
            NetworkManagerFFV.OnClientDisconnectedFromManager += HandleClientDisconnected;

            // Load the last entered IP address from PlayerPrefs, or default to "localhost".
            if (PlayerPrefs.HasKey("LastIPAddress"))
            {
                ipAddressInputField.text = PlayerPrefs.GetString("LastIPAddress");
            }
            else
            {
                ipAddressInputField.text = "localhost"; // Default for easy local testing.
            }
        }

        /// <summary>
        /// Called when the GameObject becomes disabled or inactive.
        /// Unsubscribes from network connection events to prevent memory leaks.
        /// </summary>
        private void OnDisable()
        {
            NetworkManagerFFV.OnClientConnectedToManager -= HandleClientConnected;
            NetworkManagerFFV.OnClientDisconnectedFromManager -= HandleClientDisconnected;
        }

        /// <summary>
        /// Called when the "Join" button is clicked.
        /// Saves the player's name, sets the network address, and starts the client connection.
        /// </summary>
        public void JoinLobby()
        {
            // Ensure the player's chosen name is saved before attempting to connect.
            playerNameInput.SavePlayerName();

            string ipAddress = ipAddressInputField.text;
            // Default to "localhost" if the IP address field is empty.
            if (string.IsNullOrWhiteSpace(ipAddress))
            {
                ipAddress = "localhost";
                if (DebugManager.DebugModeEnabled) Debug.LogWarning("JoinLobbyMenu: IP address was empty, defaulting to localhost.");
            }
            // Save the entered IP address for the next session.
            PlayerPrefs.SetString("LastIPAddress", ipAddress);

            // Configure the NetworkManager with the target IP and start the client.
            networkManager.networkAddress = ipAddress;
            networkManager.StartClient();

            // Disable the join button to prevent multiple connection attempts.
            joinButton.interactable = false;
            if (DebugManager.DebugModeEnabled) Debug.Log($"JoinLobbyMenu: Attempting to join lobby at IP: {ipAddress}");
        }

        /// <summary>
        /// Handles the event when the client successfully connects to the server (via NetworkManager).
        /// Hides this join menu and the landing page.
        /// </summary>
        private void HandleClientConnected()
        {
            joinButton.interactable = true; // Re-enable button in case of future use.

            gameObject.SetActive(false); // Hide this join lobby panel.
            if (landingPagePanel != null) landingPagePanel.SetActive(false); // Hide the main landing page.
            if (DebugManager.DebugModeEnabled) Debug.Log("JoinLobbyMenu: Client successfully connected. Hiding menu.");
        }

        /// <summary>
        /// Handles the event when the client disconnects from the server.
        /// Re-enables the join button and shows this menu and the landing page again.
        /// </summary>
        private void HandleClientDisconnected()
        {
            joinButton.interactable = true;
            gameObject.SetActive(true); // Show this join lobby panel again.
            if (landingPagePanel != null) landingPagePanel.SetActive(true); // Show the main landing page.
            if (DebugManager.DebugModeEnabled) Debug.Log("JoinLobbyMenu: Client disconnected. Displaying menu again.");
        }
    }
}
