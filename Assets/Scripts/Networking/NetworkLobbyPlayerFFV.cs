using UnityEngine;
using Mirror; // Mirror networking library.
using System; // For Action event type.
using DishevelledBadger.FlashFrostVale.Globals; // Access to DebugManager.
using System.IO; // For Path operations in test mode name generation.

//Needs reading through

// Namespace for network-related classes.
namespace DishevelledBadger.FlashFrostVale.Networking
{
    /// <summary>
    /// Represents a player in the lobby. This NetworkBehaviour synchronizes player data
    /// (DisplayName, IsReady, IsLeader) and handles player-specific commands.
    /// It also notifies local UI systems of data changes via the OnPlayerDataUpdated event.
    /// </summary>
    public class NetworkLobbyPlayerFFV : NetworkBehaviour
    {
        // Player's display name. Synchronized from server to clients. Hook updates UI.
        // Prefab default for this field MUST be "Loading..." for hooks to fire correctly on initial sync.
        [SerializeField] // Shown in Inspector for debugging, but primarily managed by code.
        [SyncVar(hook = nameof(OnPlayerNameChangedHook))]
        public string DisplayName = "Loading...";

        // Player's ready state. Synchronized from server to clients. Hook updates UI.
        // Prefab default for this field MUST be false.
        [SerializeField]
        [SyncVar(hook = nameof(OnPlayerReadyChangedHook))]
        public bool IsReady = false;

        // Indicates if this player is the lobby leader/host. Synchronized. Hook updates UI.
        // Prefab default for this field MUST be false.
        [SerializeField]
        [SyncVar(hook = nameof(OnIsLeaderChangedHook))]
        private bool _isLeader = false; // Backing field for the IsLeader property.

        /// <summary>
        /// Public property to access the leader status. Can only be set by the server.
        /// </summary>
        public bool IsLeader
        {
            get => _isLeader;
            [Server] // Attribute ensures only the server can modify this part of the property.
            set => _isLeader = value; // Setting this on server triggers the SyncVar and hook.
        }

        // Event invoked on clients when this player's DisplayName, IsReady, or IsLeader changes.
        // The LobbyPagePanel subscribes to this to update the UI for this specific player.
        public event Action OnPlayerDataUpdated;

        // Cached reference to the NetworkManager instance.
        private NetworkManagerFFV _lobbyManager;
        // Property to get the NetworkManager, caching it on first access.
        private NetworkManagerFFV LobbyManager => _lobbyManager ??= NetworkManager.singleton as NetworkManagerFFV;

        /// <summary>
        /// Called on the server when this player object is initialized.
        /// Registers this player with the server-side player list in NetworkManagerFFV.
        /// </summary>
        public override void OnStartServer()
        {
            base.OnStartServer();
            if (LobbyManager != null)
            {
                // The NetworkManager will handle setting IsLeader status during registration.
                LobbyManager.RegisterPlayerOnServer(this);
            }
            else if (DebugManager.DebugModeEnabled)
            {
                Debug.LogError($"SERVER [{netId}]: NetworkLobbyPlayerFFV.OnStartServer: LobbyManager (NetworkManagerFFV) not found!");
            }
        }

        /// <summary>
        /// Called on all clients (including host client) when this player object is initialized for them.
        /// Adds this player to the client-side tracking list and, if it's the local player, determines and sends its name.
        /// </summary>
        public override void OnStartClient()
        {
            base.OnStartClient();
            // Add this player instance to the static list tracked by NetworkManagerFFV for UI purposes.
            NetworkManagerFFV.AddClientSidePlayer(this);

            if (DebugManager.DebugModeEnabled)
                Debug.Log($"CLIENT [{netId}]: NetworkLobbyPlayerFFV.OnStartClient. Initial Values - Name: '{DisplayName}', IsLocal: {isLocalPlayer}, IsLeader: {IsLeader}, IsReady: {IsReady}");

            // If this instance represents the player on this local machine:
            if (isLocalPlayer)
            {
                string localPlayerName;
                // Determine name based on TestMode.
                if (DebugManager.TestModeEnabled)
                {
                    string projectPath = Application.dataPath; // e.g., ".../UnityGame/Assets"
                    string projectRootFolder = Path.GetDirectoryName(projectPath); // e.g., ".../UnityGame"
                    string folderName = Path.GetFileName(projectRootFolder); // Extracts "UnityGame" or "UnityGame_clone_0"
                    localPlayerName = folderName.Contains("_clone_")
                        ? "Clone_" + folderName.Substring(folderName.LastIndexOf("_", StringComparison.Ordinal) + 1)
                        : "Host_Player";
                    if (DebugManager.DebugModeEnabled)
                        Debug.Log($"CLIENT [{netId}]: LOCAL CLIENT in Test Mode. Derived name: '{localPlayerName}' from path '{projectRootFolder}'");
                }
                else // Production mode: use name from PlayerNameInput.
                {
                    localPlayerName = Menus.PlayerNameInput.DisplayName;
                    if (string.IsNullOrWhiteSpace(localPlayerName)) // Fallback if name wasn't set.
                    {
                        localPlayerName = "Player" + UnityEngine.Random.Range(100, 999);
                        if (DebugManager.DebugModeEnabled)
                            Debug.LogWarning($"CLIENT [{netId}]: LOCAL CLIENT in Production Mode. PlayerNameInput.DisplayName was empty, using default: '{localPlayerName}'");
                    }
                    else if (DebugManager.DebugModeEnabled)
                    {
                        Debug.Log($"CLIENT [{netId}]: LOCAL CLIENT in Production Mode. Using PlayerNameInput.DisplayName: '{localPlayerName}'");
                    }
                }
                CmdSetMyDisplayName(localPlayerName); // Send the determined name to the server.
            }

            // Invoke event for initial UI setup with current (likely "Loading...") data.
            // Hooks will fire for subsequent server-driven changes.
            OnPlayerDataUpdated?.Invoke();
            if (DebugManager.DebugModeEnabled)
                Debug.Log($"CLIENT [{netId}]: OnStartClient finished. Initial OnPlayerDataUpdated invoked for '{DisplayName}'.");
        }

        /// <summary>
        /// Called on all clients when this player object is being destroyed or removed for them.
        /// Removes this player from the client-side tracking list.
        /// </summary>
        public override void OnStopClient()
        {
            base.OnStopClient();
            // Remove from static list; LobbyPagePanel will be notified via OnClientSidePlayerListChanged.
            NetworkManagerFFV.RemoveClientSidePlayer(this);
            if (DebugManager.DebugModeEnabled)
                Debug.Log($"CLIENT [{netId}]: NetworkLobbyPlayerFFV.OnStopClient: Player {DisplayName} (netId {netId}) stopped on client.");
        }

        /// <summary>
        /// Called on the server when this player object is being destroyed or removed.
        /// Unregisters the player from the server-side list in NetworkManagerFFV.
        /// </summary>
        public override void OnStopServer()
        {
            if (LobbyManager != null)
            {
                LobbyManager.UnregisterPlayerOnServer(this);
            }
            base.OnStopServer();
        }

        // --- SyncVar Hooks (Called on clients when the corresponding SyncVar changes on the server) ---

        /// <summary>
        /// Hook for the DisplayName SyncVar. Called on clients when DisplayName changes.
        /// Invokes OnPlayerDataUpdated to notify UI.
        /// </summary>
        private void OnPlayerNameChangedHook(string oldName, string newName)
        {
            if (DebugManager.DebugModeEnabled)
                Debug.Log($"CLIENT [{netId}]: OnPlayerNameChangedHook fired! Old: '{oldName}', New: '{newName}'. IsLocal: {isLocalPlayer}. IsClient: {isClient}, IsServer: {isServer}. Invoking OnPlayerDataUpdated.");
            // Only invoke the UI update event if this code is running on a client.
            if (isClient) OnPlayerDataUpdated?.Invoke();
        }

        /// <summary>
        /// Hook for the IsReady SyncVar. Called on clients when IsReady changes.
        /// Invokes OnPlayerDataUpdated to notify UI. Also notifies server manager if change occurs on server.
        /// </summary>
        private void OnPlayerReadyChangedHook(bool oldReady, bool newReady)
        {
            if (DebugManager.DebugModeEnabled)
                Debug.Log($"CLIENT [{netId}]: OnPlayerReadyChangedHook fired! Old: '{oldReady}', New: '{newReady}'. IsLocal: {isLocalPlayer}. IsClient: {isClient}, IsServer: {isServer}. Invoking OnPlayerDataUpdated.");
            if (isClient) OnPlayerDataUpdated?.Invoke();

            // If this hook is running on the server (e.g., host client), notify the manager.
            if (isServer)
            {
                LobbyManager?.NotifyPlayersOfReadyStateChange();
            }
        }

        /// <summary>
        /// Hook for the _isLeader SyncVar. Called on clients when _isLeader changes.
        /// Invokes OnPlayerDataUpdated to notify UI.
        /// </summary>
        private void OnIsLeaderChangedHook(bool oldLeader, bool newLeader)
        {
            if (DebugManager.DebugModeEnabled)
                Debug.Log($"CLIENT [{netId}]: OnIsLeaderChangedHook fired! Old: '{oldLeader}', New: '{newLeader}'. IsLocal: {isLocalPlayer}. IsClient: {isClient}, IsServer: {isServer}. Invoking OnPlayerDataUpdated.");
            if (isClient) OnPlayerDataUpdated?.Invoke();
        }

        // --- Commands (Sent from a client to the server) ---

        /// <summary>
        /// Command sent by the local client to the server to set its display name.
        /// </summary>
        /// <param name="nameFromClient">The name determined by the client.</param>
        [Command]
        private void CmdSetMyDisplayName(string nameFromClient)
        {
            if (string.IsNullOrWhiteSpace(nameFromClient))
            {
                // Server-side fallback if client sends an invalid name.
                nameFromClient = "ServerInvalidName_" + connectionToClient.connectionId;
            }

            if (DebugManager.DebugModeEnabled)
                Debug.Log($"SERVER [{netId}]: CmdSetMyDisplayName received from conn {connectionToClient.connectionId}. Current DisplayName on server: '{DisplayName}', Setting to: '{nameFromClient}'.");

            DisplayName = nameFromClient; // Update the SyncVar on the server. This will propagate to all clients.

            if (DebugManager.DebugModeEnabled)
                Debug.Log($"SERVER [{netId}]: SyncVar DisplayName on server is now '{DisplayName}' for conn {connectionToClient.connectionId}. Hook should fire on clients.");
        }

        /// <summary>
        /// Command sent by the local client to the server to toggle its ready state.
        /// </summary>
        [Command]
        public void CmdReadyUp()
        {
            if (DebugManager.DebugModeEnabled)
                Debug.Log($"SERVER [{netId}]: CmdReadyUp received from {DisplayName} (conn {connectionToClient.connectionId}). Current IsReady: {IsReady}. Toggling.");
            IsReady = !IsReady; // Toggle and update the SyncVar on the server.
            if (DebugManager.DebugModeEnabled)
                Debug.Log($"SERVER [{netId}]: SyncVar IsReady on server is now {IsReady} for {DisplayName}. Hook should fire on clients.");
        }

        /// <summary>
        /// Command sent by the local client (if leader) to the server to start the game.
        /// </summary>
        [Command]
        public void CmdStartGame()
        {
            if (DebugManager.DebugModeEnabled)
                Debug.Log($"SERVER [{netId}]: CmdStartGame received from {DisplayName} (conn {connectionToClient.connectionId}).");

            // Security check: Only the leader can start the game.
            if (!this.IsLeader)
            {
                if (DebugManager.DebugModeEnabled)
                    Debug.LogWarning($"SERVER [{netId}]: Player {DisplayName} (conn: {connectionToClient.connectionId}) sent CmdStartGame but is NOT THE LEADER on server. Current leader status: {this.IsLeader}. Ignoring.");
                return;
            }
            // Tell the NetworkManager to initiate game start procedures.
            LobbyManager?.StartGame();
        }
    }
}