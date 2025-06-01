using UnityEngine;
using DishevelledBadger.FlashFrostVale.Networking;
using DishevelledBadger.FlashFrostVale.Globals;

namespace DishevelledBadger.FlashFrostVale.Menus
{
    /// <summary>
    /// Handles main menu actions, specifically hosting a new lobby.
    /// </summary>
    public class MainMenu : MonoBehaviour
    {
        // Reference to the custom NetworkManager. Assigned in Inspector.
        [SerializeField] private NetworkManagerFFV networkManager = null;

        [Header("UI")]
        // Reference to the initial landing page panel. Assigned in Inspector.
        [SerializeField] private GameObject landingPagePanel = null;
        // Reference to the lobby page panel that shows connected players. Assigned in Inspector.
        [SerializeField] private GameObject lobbyPagePanel = null;

        /// <summary>
        /// Called when the "Host Lobby" button is clicked.
        /// Starts the network host and transitions UI to the lobby panel.
        /// </summary>
        public void HostLobby()
        {
            if (networkManager == null)
            {
                if (DebugManager.DebugModeEnabled) Debug.LogError("MainMenu.HostLobby: NetworkManagerFFV is not assigned!");
                return;
            }
            // Tell the NetworkManager to start a host session (server + client).
            networkManager.StartHost();

            // Switch UI panels.
            if (landingPagePanel != null) landingPagePanel.SetActive(false); // Hide the landing page.
            if (lobbyPagePanel != null) lobbyPagePanel.SetActive(true); // Show the lobby page.
        }
    }
}
