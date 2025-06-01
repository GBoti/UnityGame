using UnityEngine;
using System;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using DishevelledBadger.FlashFrostVale.Globals;
using DishevelledBadger.FlashFrostVale.Networking;

namespace DishevelledBadger.FlashFrostVale.Menus
{
    /// <summary>
    /// Manages the UI display for the lobby, showing connected players, their ready status,
    /// and controls for readying up and starting the game (for the host).
    /// This panel observes changes in player data and updates the UI accordingly.
    /// </summary>
    public class LobbyPagePanel : MonoBehaviour
    {
        [Header("UI References")]
        // Prefab for a single player's entry in the lobby list. Assigned in Inspector.
        [SerializeField] private GameObject playerEntryPrefab;
        // Parent Transform under which player entry UI GameObjects will be instantiated. Assigned in Inspector.
        [SerializeField] private Transform playerListParent;
        // Button for the local player to toggle their ready state. Assigned in Inspector.
        [SerializeField] private Button readyButton;
        // Button for the host/leader to start the game. Assigned in Inspector.
        [SerializeField] private Button hostStartGameButton;

        // Stores instantiated UI GameObjects, keyed by their corresponding NetworkLobbyPlayerFFV instance.
        private readonly Dictionary<NetworkLobbyPlayerFFV, GameObject> _playerUiEntries = new Dictionary<NetworkLobbyPlayerFFV, GameObject>();
        // Stores event handlers for each player's OnPlayerDataUpdated event to allow proper unsubscription.
        private readonly Dictionary<NetworkLobbyPlayerFFV, Action> _playerEventHandlers = new Dictionary<NetworkLobbyPlayerFFV, System.Action>();
        // Cached reference to the local player's NetworkLobbyPlayerFFV instance.
        private NetworkLobbyPlayerFFV _localLobbyPlayerInstance;

        /// <summary>
        /// Called when the GameObject becomes enabled and active.
        /// Subscribes to player list changes and refreshes the UI.
        /// </summary>
        private void OnEnable()
        {
            if (DebugManager.DebugModeEnabled) Debug.Log("LobbyPagePanel: OnEnable called.");
            // Subscribe to the event that fires when the client-side list of players changes.
            NetworkManagerFFV.OnClientSideLobbyPlayerListChanged += HandlePlayerListChanged;
            // Perform an initial refresh of the player list UI.
            RefreshPlayerListUI();
        }

        /// <summary>
        /// Called when the GameObject becomes disabled or inactive.
        /// Unsubscribes from events to prevent errors and memory leaks.
        /// </summary>
        private void OnDisable()
        {
            if (DebugManager.DebugModeEnabled) Debug.Log("LobbyPagePanel: OnDisable called.");
            NetworkManagerFFV.OnClientSideLobbyPlayerListChanged -= HandlePlayerListChanged;
            // Clear all UI entries and unsubscribe from individual player events.
            ClearAllPlayerEntriesAndUnsubscribe();
        }

        /// <summary>
        /// Called before the first frame update.
        /// Initializes button click listeners.
        /// </summary>
        private void Start()
        {
            if (readyButton != null)
            {
                readyButton.onClick.AddListener(OnReadyButtonClicked);
            }
            else if (DebugManager.DebugModeEnabled) Debug.LogError("LobbyPagePanel: ReadyButton is not assigned!");

            if (hostStartGameButton != null)
            {
                hostStartGameButton.onClick.AddListener(OnHostStartGameButtonClicked);
            }
            else if (DebugManager.DebugModeEnabled) Debug.LogError("LobbyPagePanel: HostStartGameButton is not assigned!");

            // Initial UI state for buttons is handled by RefreshPlayerListUI via OnEnable.
        }

        /// <summary>
        /// Handles the event from NetworkManagerFFV when the list of players known to the client changes.
        /// </summary>
        private void HandlePlayerListChanged()
        {
            if (DebugManager.DebugModeEnabled) Debug.Log("LobbyPagePanel: HandlePlayerListChanged called due to NetworkManagerFFV event.");
            RefreshPlayerListUI(); // Rebuild the entire UI list.
        }

        /// <summary>
        /// Handles the OnPlayerDataUpdated event from a specific NetworkLobbyPlayerFFV instance.
        /// Updates the UI for that specific player and refreshes shared controls.
        /// </summary>
        /// <param name="player">The player whose data was updated.</param>
        private void OnSpecificPlayerUpdated(NetworkLobbyPlayerFFV player)
        {
            if (player == null)
            {
                if (DebugManager.DebugModeEnabled) Debug.LogWarning("LobbyPagePanel: OnSpecificPlayerUpdated called with a null player.");
                return;
            }

            if (DebugManager.DebugModeEnabled) Debug.Log($"LobbyPagePanel: OnSpecificPlayerUpdated called for player {player.DisplayName} ({player.netId}).");
            // Try to find the UI entry for this player and update it.
            if (_playerUiEntries.TryGetValue(player, out GameObject uiEntry))
            {
                UpdateSinglePlayerEntryUI(player, uiEntry);
            }
            else if (DebugManager.DebugModeEnabled)
            {
                Debug.LogWarning($"LobbyPagePanel: OnSpecificPlayerUpdated for player {player.DisplayName} ({player.netId}) but no UI entry found. Player might have been removed or list is refreshing.");
            }

            // If the updated player is the local player, refresh local controls (e.g., ready button text).
            if (player == _localLobbyPlayerInstance)
            {
                UpdateLocalPlayerControls();
            }
            // Always update the host start button state, as any player's ready status can affect it.
            UpdateHostStartButtonState();
        }

        /// <summary>
        /// Clears all existing player UI entries and rebuilds them based on the current ClientSideLobbyPlayers list.
        /// Also updates local player controls and the host start button state.
        /// </summary>
        private void RefreshPlayerListUI()
        {
            if (DebugManager.DebugModeEnabled) Debug.Log("LobbyPagePanel: RefreshPlayerListUI called.");
            ClearAllPlayerEntriesAndUnsubscribe(); // Clean up old UI and event subscriptions.

            _localLobbyPlayerInstance = null; // Reset cached local player.

            if (NetworkManagerFFV.ClientSideLobbyPlayers == null)
            {
                if (DebugManager.DebugModeEnabled) Debug.LogWarning("LobbyPagePanel: NetworkManagerFFV.ClientSideLobbyPlayers is null.");
                return;
            }

            // Create a copy for safe iteration.
            List<NetworkLobbyPlayerFFV> currentPlayers = new List<NetworkLobbyPlayerFFV>(NetworkManagerFFV.ClientSideLobbyPlayers);

            if (DebugManager.DebugModeEnabled) Debug.Log($"LobbyPagePanel: Refreshing UI for {currentPlayers.Count} players in ClientSideLobbyPlayers.");

            foreach (NetworkLobbyPlayerFFV player in currentPlayers)
            {
                if (player == null)
                {
                    if (DebugManager.DebugModeEnabled) Debug.LogWarning("LobbyPagePanel: Found a null player in ClientSideLobbyPlayers list during refresh.");
                    continue;
                }

                CreatePlayerEntryUI(player); // Create UI and subscribe to player's updates.
                if (player.isLocalPlayer) // Check if this player is the one running on this game instance.
                {
                    _localLobbyPlayerInstance = player; // Cache local player reference.
                    if (DebugManager.DebugModeEnabled) Debug.Log($"LobbyPagePanel: Local player instance identified: {player.DisplayName} ({player.netId})");
                }
            }
            UpdateLocalPlayerControls(); // Update ready button text etc.
            UpdateHostStartButtonState();  // Update visibility/interactability of start game button.
        }

        /// <summary>
        /// Instantiates a UI entry for a given player, subscribes to their data updates, and updates the UI.
        /// </summary>
        /// <param name="player">The NetworkLobbyPlayerFFV to create a UI entry for.</param>
        private void CreatePlayerEntryUI(NetworkLobbyPlayerFFV player)
        {
            if (playerEntryPrefab == null) { if (DebugManager.DebugModeEnabled) Debug.LogError("LobbyPagePanel: playerEntryPrefab is null!"); return; }
            if (playerListParent == null) { if (DebugManager.DebugModeEnabled) Debug.LogError("LobbyPagePanel: playerListParent is null!"); return; }

            // Defensive check, should ideally not happen if ClearAllPlayerEntriesAndUnsubscribe works correctly.
            if (_playerUiEntries.ContainsKey(player))
            {
                if (DebugManager.DebugModeEnabled) Debug.LogWarning($"LobbyPagePanel.CreatePlayerEntryUI: Player {player.DisplayName} ({player.netId}) already has a UI entry. This might happen if events fire rapidly. Updating existing.");
                UpdateSinglePlayerEntryUI(player, _playerUiEntries[player]); // Attempt to update existing.
                return;
            }

            GameObject playerEntryGO = Instantiate(playerEntryPrefab, playerListParent); // Instantiate the prefab.
            _playerUiEntries.Add(player, playerEntryGO); // Store the UI element.

            // Create and store the event handler for this specific player.
            System.Action handler = () => OnSpecificPlayerUpdated(player);
            _playerEventHandlers[player] = handler;
            player.OnPlayerDataUpdated += handler;  // Subscribe to the player's data update event.

            UpdateSinglePlayerEntryUI(player, playerEntryGO); // Perform initial UI update for this new entry.
            if (DebugManager.DebugModeEnabled) Debug.Log($"LobbyPagePanel.CreatePlayerEntryUI: Created UI for {player.DisplayName} ({player.netId}). Subscribed to its OnPlayerDataUpdated.");
        }

        /// <summary>
        /// Updates the visual elements (name, ready status, background) of a single player's UI entry.
        /// </summary>
        /// <param name="player">The player whose data to display.</param>
        /// <param name="playerEntryGO">The UI GameObject for this player's entry.</param>
        private void UpdateSinglePlayerEntryUI(NetworkLobbyPlayerFFV player, GameObject playerEntryGO)
        {
            if (player == null || playerEntryGO == null) { if (DebugManager.DebugModeEnabled) Debug.LogError("LobbyPagePanel.UpdateSinglePlayerEntryUI: Player or GameObject is null."); return; }

            // Find UI components within the instantiated player entry.
            TMP_Text nameText = playerEntryGO.transform.Find("NameText")?.GetComponent<TMP_Text>();
            TMP_Text readyText = playerEntryGO.transform.Find("ReadyText")?.GetComponent<TMP_Text>();
            Image backgroundImage = playerEntryGO.GetComponent<Image>();

            if (nameText != null) nameText.text = player.DisplayName; // Set player name.
            else if (DebugManager.DebugModeEnabled) Debug.LogError($"LobbyPagePanel: 'NameText' TMP_Text component not found in playerEntryPrefab for {player.DisplayName}");

            if (readyText != null) readyText.text = player.IsReady ? "<color=green>READY</color>" : "<color=red>NOT READY</color>"; // Set ready status with color.
            else if (DebugManager.DebugModeEnabled) Debug.LogError($"LobbyPagePanel: 'ReadyText' TMP_Text component not found in playerEntryPrefab for {player.DisplayName}");

            // Change background color based on local player and leader status.
            if (backgroundImage != null)
            {
                if (player.IsLeader)
                { // Leader has a distinct color.
                    backgroundImage.color = player.isLocalPlayer ? new Color(1.0f, 0.9f, 0.5f, 1f) : new Color(0.8f, 0.7f, 0.3f, 1f);
                }
                else
                { // Non-leader color.
                    backgroundImage.color = player.isLocalPlayer ? new Color(0.7f, 0.8f, 1.0f, 1f) : new Color(0.6f, 0.6f, 0.6f, 1f);
                }
            }
            if (DebugManager.DebugModeEnabled) Debug.Log($"LobbyPagePanel.UpdateSinglePlayerEntryUI: Updated UI for {player.DisplayName} ({player.netId}), Name: '{player.DisplayName}', Ready: {player.IsReady}, Leader: {player.IsLeader}");
        }

        /// <summary>
        /// Updates the local player's "Ready" button text and visibility.
        /// </summary>
        private void UpdateLocalPlayerControls()
        {
            if (readyButton == null) return;
            TMP_Text buttonText = readyButton.GetComponentInChildren<TMP_Text>();
            if (buttonText == null) { if (DebugManager.DebugModeEnabled) Debug.LogWarning("LobbyPagePanel: ReadyButton has no TMP_Text child."); return; }

            if (_localLobbyPlayerInstance != null) // If there is a local player instance.
            {
                readyButton.gameObject.SetActive(true); // Show the button.
                // Set button text based on ready state.
                buttonText.text = _localLobbyPlayerInstance.IsReady ? "Cancel Ready" : "Ready Up";
                if (DebugManager.DebugModeEnabled) Debug.Log($"LobbyPagePanel.UpdateLocalPlayerControls: Local player is '{_localLobbyPlayerInstance.DisplayName}'. Ready button text: '{buttonText.text}'");
            }
            else // No local player instance found.
            {
                readyButton.gameObject.SetActive(false); // Hide the button.
                if (DebugManager.DebugModeEnabled) Debug.Log("LobbyPagePanel.UpdateLocalPlayerControls: No local player instance found, hiding ready button.");
            }
        }

        /// <summary>
        /// Updates the visibility and interactability of the "Host Start Game" button.
        /// </summary>
        private void UpdateHostStartButtonState()
        {
            if (hostStartGameButton == null) return;

            // Button is active only if the local player is the leader.
            if (_localLobbyPlayerInstance != null && _localLobbyPlayerInstance.IsLeader)
            {
                hostStartGameButton.gameObject.SetActive(true);
                bool allReadyAndNamed = true; // Flag to check if all players are ready and named.
                // In non-test mode, at least one player is required.
                if (NetworkManagerFFV.ClientSideLobbyPlayers.Count == 0 && !DebugManager.TestModeEnabled) allReadyAndNamed = false;

                // Check each player in the lobby.
                foreach (NetworkLobbyPlayerFFV p in NetworkManagerFFV.ClientSideLobbyPlayers)
                {
                    // If any player is null, not ready, or has a default/empty name, not all are ready.
                    if (p == null || !p.IsReady || string.IsNullOrWhiteSpace(p.DisplayName) || p.DisplayName == "Loading...")
                    {
                        allReadyAndNamed = false;
                        break;
                    }
                }
                hostStartGameButton.interactable = allReadyAndNamed; // Enable button only if all conditions met.
                if (DebugManager.DebugModeEnabled) Debug.Log($"LobbyPagePanel.UpdateHostStartButtonState: Local player '{_localLobbyPlayerInstance.DisplayName}' is leader. AllReadyAndNamed: {allReadyAndNamed}. Button interactable: {hostStartGameButton.interactable}");
            }
            else // Local player is not leader, or no local player.
            {
                hostStartGameButton.gameObject.SetActive(false); // Hide button.
                hostStartGameButton.interactable = false; // Disable button.
                if (DebugManager.DebugModeEnabled && _localLobbyPlayerInstance != null) Debug.Log($"LobbyPagePanel.UpdateHostStartButtonState: Local player '{_localLobbyPlayerInstance.DisplayName}' is NOT leader. Hiding/disabling button.");
                else if (DebugManager.DebugModeEnabled) Debug.Log("LobbyPagePanel.UpdateHostStartButtonState: No local player instance found or not leader. Hiding/disabling button.");
            }
        }

        /// <summary>
        /// Called when the "Ready" button is clicked by the local player.
        /// Sends a command to the server to toggle the ready state.
        /// </summary>
        private void OnReadyButtonClicked()
        {
            if (_localLobbyPlayerInstance != null)
            {
                if (DebugManager.DebugModeEnabled) Debug.Log($"LobbyPagePanel.OnReadyButtonClicked: Local player {_localLobbyPlayerInstance.DisplayName} sending CmdReadyUp.");
                _localLobbyPlayerInstance.CmdReadyUp(); // Tell server to toggle ready state.
            }
            else if (DebugManager.DebugModeEnabled) Debug.LogWarning("LobbyPagePanel.OnReadyButtonClicked: LocalLobbyPlayer not found!");
        }

        /// <summary>
        /// Called when the "Host Start Game" button is clicked by the host/leader.
        /// Sends a command to the server to start the game.
        /// </summary>
        private void OnHostStartGameButtonClicked()
        {
            if (_localLobbyPlayerInstance != null && _localLobbyPlayerInstance.IsLeader)
            {
                if (DebugManager.DebugModeEnabled) Debug.Log($"LobbyPagePanel.OnHostStartGameButtonClicked: Local leader {_localLobbyPlayerInstance.DisplayName} sending CmdStartGame.");
                _localLobbyPlayerInstance.CmdStartGame(); // Tell server to start the game.
            }
            else if (DebugManager.DebugModeEnabled) Debug.LogWarning("LobbyPagePanel.OnHostStartGameButtonClicked: Conditions not met (not local leader).");
        }

        /// <summary>
        /// Destroys all player UI entries and unsubscribes from their OnPlayerDataUpdated events.
        /// </summary>
        private void ClearAllPlayerEntriesAndUnsubscribe()
        {
            if (DebugManager.DebugModeEnabled) Debug.Log("LobbyPagePanel: ClearAllPlayerEntriesAndUnsubscribe called.");

            // Iterating over a copy so the handler are definitely dealt with.
            List<NetworkLobbyPlayerFFV> playersToUnsubscribe = new List<NetworkLobbyPlayerFFV>(_playerEventHandlers.Keys);

            foreach (NetworkLobbyPlayerFFV player in playersToUnsubscribe)
            {
                if (player != null && _playerEventHandlers.TryGetValue(player, out System.Action handler))
                {
                    player.OnPlayerDataUpdated -= handler; // Unsubscribe the stored handler.
                    _playerEventHandlers.Remove(player); // Remove from tracking.
                    if (DebugManager.DebugModeEnabled) Debug.Log($"LobbyPagePanel: Unsubscribed from OnPlayerDataUpdated for {player.DisplayName} ({player.netId}).");
                }
            }

            // Destroy the UI GameObjects.
            foreach (GameObject uiEntry in _playerUiEntries.Values)
            {
                if (uiEntry != null)
                {
                    Destroy(uiEntry);
                }
            }
            _playerUiEntries.Clear(); // Clear the dictionary of UI entries.
            _localLobbyPlayerInstance = null; // Reset cached local player.
        }
    }
}