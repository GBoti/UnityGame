using UnityEngine;
using Mirror; // Mirror networking library.
using System; // For Action event type.
using System.Linq; // For LINQ operations like Any().
using System.Collections.Generic; // For Lists.
using DishevelledBadger.FlashFrostVale.Globals; // Access to DebugManager.

// Needs reading through

// Namespace for network-related classes.
namespace DishevelledBadger.FlashFrostVale.Networking
{
    /// <summary>
    /// Custom NetworkManager for the game, handling lobby logic, player spawning,
    /// and client/server lifecycle events.
    /// </summary>
    public class NetworkManagerFFV : NetworkManager
    {
        // Scene to switch to when in the menu/lobby. Assigned in Inspector.
        [Scene] [SerializeField] private string menuScene = string.Empty;

        [Header("Lobby")]
        // Prefab for the NetworkLobbyPlayerFFV object. MUST be assigned in Inspector.
        [SerializeField] private NetworkLobbyPlayerFFV lobbyPlayerPrefab = null;

        // Event invoked on clients when they successfully connect to this manager/server.
        public static event Action OnClientConnectedToManager;
        // Event invoked on clients when they disconnect from this manager/server.
        public static event Action OnClientDisconnectedFromManager;

        // List of lobby players currently connected to the server (server-side only).
        public readonly List<NetworkLobbyPlayerFFV> ServerLobbyPlayers = new List<NetworkLobbyPlayerFFV>();

        // Static list of lobby players visible on the current client (client-side only).
        // Used by UI systems like LobbyPagePanel to display players.
        public static readonly List<NetworkLobbyPlayerFFV> ClientSideLobbyPlayers = new List<NetworkLobbyPlayerFFV>();
        // Event invoked on clients when ClientSideLobbyPlayers list changes (player added/removed).
        public static event Action OnClientSidePlayerListChanged;


        /// <summary>
        /// Called on the server when the server starts.
        /// Loads spawnable prefabs from Resources.
        /// </summary>
        public override void OnStartServer()
        {
            base.OnStartServer();
            // Automatically register all GameObjects in the "Resources/SpawnablePrefabs" folder.
            // Note: The playerPrefab (lobbyPlayerPrefab) is handled separately by OnServerAddPlayer.
            spawnPrefabs = Resources.LoadAll<GameObject>("SpawnablePrefabs").ToList();

            // Safety check for the essential lobbyPlayerPrefab.
            if (lobbyPlayerPrefab == null && DebugManager.DebugModeEnabled)
            {
                Debug.LogError("NetworkManagerFFV: lobbyPlayerPrefab is NULL in OnStartServer! Make sure it's assigned in the Inspector.");
            }
            if (DebugManager.DebugModeEnabled)
                Debug.Log("NetworkManagerFFV: Server started.");
        }

        /// <summary>
        /// Called on the client when the client starts.
        /// Registers spawnable prefabs loaded from Resources.
        /// </summary>
        public override void OnStartClient()
        {
            base.OnStartClient();
            GameObject[] prefabsToRegister = Resources.LoadAll<GameObject>("SpawnablePrefabs");
            foreach (GameObject prefab in prefabsToRegister)
            {
                Mirror.ClientScene.RegisterPrefab(prefab); // Register with Mirror's client scene.
            }
            if (DebugManager.DebugModeEnabled)
                Debug.Log("NetworkManagerFFV: Client started.");
        }

        /// <summary>
        /// Called on the client when it successfully connects to the server.
        /// Invokes the OnClientConnectedToManager event.
        /// </summary>
        public override void OnClientConnect() // Note: Parameter 'NetworkConnection conn' is obsolete for this specific override if not used.
        {
            base.OnClientConnect();
            if (DebugManager.DebugModeEnabled)
                Debug.Log("NetworkManagerFFV: Client connected to server.");
            OnClientConnectedToManager?.Invoke();
        }

        /// <summary>
        /// Called on the client when it disconnects from the server.
        /// Clears client-side player tracking and invokes disconnection events.
        /// </summary>
        public override void OnClientDisconnect() // Note: Parameter 'NetworkConnection conn' is obsolete here.
        {
            base.OnClientDisconnect();
            if (DebugManager.DebugModeEnabled)
                Debug.Log("NetworkManagerFFV: Client disconnected from server.");
            ClientSideLobbyPlayers.Clear(); // Clear the local list of players.
            OnClientSidePlayerListChanged?.Invoke(); // Notify UI systems.
            OnClientDisconnectedFromManager?.Invoke();
        }

        /// <summary>
        /// Called on the server when a new client connects (before a player object is created for it).
        /// Checks if the server is full.
        /// </summary>
        public override void OnServerConnect(NetworkConnectionToClient conn)
        {
            base.OnServerConnect(conn);
            // If server is at max capacity, disconnect the new client.
            if (numPlayers >= maxConnections)
            {
                if (DebugManager.DebugModeEnabled)
                    Debug.LogWarning($"NetworkManagerFFV: Server full. Disconnecting client {conn.connectionId}");
                conn.Disconnect();
                return;
            }
            if (DebugManager.DebugModeEnabled) // Log successful connection.
                Debug.Log($"NetworkManagerFFV: Client {conn.connectionId} connected to server. Current numPlayers: {numPlayers + 1} (including this new connection).");
        }

        /// <summary>
        /// Called on the server when a client requests to add a player (e.g., after connecting).
        /// Instantiates the lobbyPlayerPrefab for the new connection.
        /// </summary>
        public override void OnServerAddPlayer(NetworkConnectionToClient conn)
        {
            if (DebugManager.DebugModeEnabled)
                Debug.Log($"NetworkManagerFFV: OnServerAddPlayer called for connection {conn.connectionId}. Current scene: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().path}");

            // Critical check: Ensure the lobbyPlayerPrefab is assigned in the Inspector.
            if (lobbyPlayerPrefab == null)
            {
                if (DebugManager.DebugModeEnabled)
                    Debug.LogError($"NetworkManagerFFV: lobbyPlayerPrefab is NULL in OnServerAddPlayer! Cannot spawn player. Ensure it is assigned in the NetworkManager's Inspector.");
                conn.Disconnect(); // Disconnect client if prefab is missing.
                return;
            }

            // Only add players if the server is in the designated menu/lobby scene.
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != menuScene)
            {
                if (DebugManager.DebugModeEnabled)
                    Debug.LogWarning($"NetworkManagerFFV: Trying to add player in wrong scene: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().path}. Expected: {menuScene}. Player not added for conn {conn.connectionId}.");
                // Optionally disconnect, for now, just don't add the player.
                return;
            }

            // Instantiate the player object for this connection.
            NetworkLobbyPlayerFFV lobbyPlayerInstance = Instantiate(lobbyPlayerPrefab);
            // Note: IsLeader status is now primarily set within NetworkLobbyPlayerFFV.OnStartServer -> RegisterPlayerOnServer
            // to ensure it's part of the server-side registration logic.

            // Add the instantiated player object to the network for the given connection.
            NetworkServer.AddPlayerForConnection(conn, lobbyPlayerInstance.gameObject);

            if (DebugManager.DebugModeEnabled)
                Debug.Log($"NetworkManagerFFV: Player object (netId: {lobbyPlayerInstance.netId}) spawned for connection {conn.connectionId}.");
        }

        /// <summary>
        /// Registers a NetworkLobbyPlayerFFV instance on the server.
        /// Adds it to the ServerLobbyPlayers list and sets its leader status.
        /// Called from NetworkLobbyPlayerFFV.OnStartServer().
        /// </summary>
        /// <param name="player">The player instance to register.</param>
        public void RegisterPlayerOnServer(NetworkLobbyPlayerFFV player)
        {
            if (!ServerLobbyPlayers.Contains(player)) // Avoid duplicates.
            {
                // The first player to register becomes the leader.
                bool isLeader = ServerLobbyPlayers.Count == 0;
                player.IsLeader = isLeader; // Set the SyncVar on the server instance.

                ServerLobbyPlayers.Add(player);
                if (DebugManager.DebugModeEnabled)
                    Debug.Log($"NetworkManagerFFV: Player {player.DisplayName} ({player.netId}) registered on server. Total server players: {ServerLobbyPlayers.Count}. IsLeader set to: {player.IsLeader}");

                NotifyPlayersOfReadyStateChange(); // Update server's knowledge of overall readiness.
            }
        }

        /// <summary>
        /// Unregisters a NetworkLobbyPlayerFFV instance from the server, typically on disconnect.
        /// Removes it from ServerLobbyPlayers, recalculates leader, and updates ready state.
        /// </summary>
        /// <param name="player">The player instance to unregister.</param>
        public void UnregisterPlayerOnServer(NetworkLobbyPlayerFFV player)
        {
            if (ServerLobbyPlayers.Remove(player)) // If player was found and removed.
            {
                if (DebugManager.DebugModeEnabled)
                    Debug.Log($"NetworkManagerFFV: Player {player.DisplayName} ({player.netId}) unregistered from server. Total server players: {ServerLobbyPlayers.Count}.");
                RecalculateLeaderOnServer(); // If the leader disconnected, assign a new one.
                NotifyPlayersOfReadyStateChange(); // Update overall readiness.
            }
        }

        /// <summary>
        /// Server-only. If the current leader disconnects or no leader exists, assigns a new leader from the list.
        /// </summary>
        [Server]
        private void RecalculateLeaderOnServer()
        {
            if (ServerLobbyPlayers.Count > 0) // If there are any players left.
            {
                bool leaderAlreadyExists = ServerLobbyPlayers.Any(p => p.IsLeader);

                if (!leaderAlreadyExists) // If no current leader (e.g., previous one left).
                {
                    ServerLobbyPlayers[0].IsLeader = true; // Assign leadership to the first player in the list.
                    if (DebugManager.DebugModeEnabled)
                        Debug.Log($"NetworkManagerFFV: New leader assigned on server: {ServerLobbyPlayers[0].DisplayName} ({ServerLobbyPlayers[0].netId})");

                    // Ensure all other players are not leaders.
                    for (int i = 1; i < ServerLobbyPlayers.Count; i++)
                    {
                        ServerLobbyPlayers[i].IsLeader = false;
                    }
                }
                // Optional: Could add a check to ensure only ONE leader exists if multiple were somehow set.
            }
            else // No players left.
            {
                if (DebugManager.DebugModeEnabled) Debug.Log("NetworkManagerFFV: No players left, no leader to assign.");
            }
        }

        /// <summary>
        /// Server-only. Called when a player's ready state might have changed.
        /// Primarily for server-side logic or logging regarding game readiness.
        /// Client UI for start button is now handled client-side by LobbyPagePanel.
        /// </summary>
        [Server]
        public void NotifyPlayersOfReadyStateChange()
        {
            if (DebugManager.DebugModeEnabled)
            {
                bool readyToStart = IsReadyToStart(); // Check if game can be started.
                Debug.Log($"NetworkManagerFFV.NotifyPlayersOfReadyStateChange: Server check - All players ready? {readyToStart}");
            }
        }

        /// <summary>
        /// Server-only. Checks if all conditions are met to start the game.
        /// (All players have names, all players are ready).
        /// </summary>
        /// <returns>True if the game is ready to start, false otherwise.</returns>
        [Server]
        public bool IsReadyToStart()
        {
            // In non-test mode, require at least one player.
            if (ServerLobbyPlayers.Count == 0 && !DebugManager.TestModeEnabled)
            {
                if (DebugManager.DebugModeEnabled) Debug.Log("NetworkManagerFFV.IsReadyToStart: No players in lobby, not ready.");
                return false;
            }
            // Check if any player is null (shouldn't happen), has no name, or name is still "Loading...".
            if (ServerLobbyPlayers.Any(p => p == null || string.IsNullOrWhiteSpace(p.DisplayName) || p.DisplayName == "Loading..."))
            {
                if (DebugManager.DebugModeEnabled) Debug.Log("NetworkManagerFFV.IsReadyToStart: Not all players have set their display names. Not ready.");
                return false;
            }
            // Check if any player is not ready.
            if (ServerLobbyPlayers.Any(p => p == null || !p.IsReady))
            {
                if (DebugManager.DebugModeEnabled) Debug.Log("NetworkManagerFFV.IsReadyToStart: Not all players are ready. Not ready.");
                return false;
            }

            if (DebugManager.DebugModeEnabled && ServerLobbyPlayers.Count > 0) Debug.Log("NetworkManagerFFV.IsReadyToStart: All conditions met. Ready to start!");
            // Game is ready if players exist OR if in test mode (allowing solo start).
            return ServerLobbyPlayers.Count > 0 || DebugManager.TestModeEnabled;
        }

        /// <summary>
        /// Server-only. Called by the leader (via Command) to start the game.
        /// Changes the scene if all players are ready.
        /// </summary>
        [Server]
        public void StartGame()
        {
            if (DebugManager.DebugModeEnabled)
                Debug.Log("NetworkManagerFFV.StartGame: Attempting to start game.");

            if (!IsReadyToStart()) // Double-check readiness.
            {
                if (DebugManager.DebugModeEnabled)
                    Debug.LogWarning("NetworkManagerFFV.StartGame: Cannot start game: Conditions not met!");
                return;
            }
            if (DebugManager.DebugModeEnabled)
                Debug.Log("NetworkManagerFFV.StartGame: Starting game for all players!");

            // TODO: Replace "YourGameSceneName" with the actual name of your game scene.
            // ServerChangeScene("YourGameSceneName"); 
        }

        /// <summary>
        /// Called on the server when a client disconnects.
        /// Unregisters the player associated with the disconnected connection.
        /// </summary>
        public override void OnServerDisconnect(NetworkConnectionToClient conn)
        {
            if (DebugManager.DebugModeEnabled)
                Debug.Log($"NetworkManagerFFV.OnServerDisconnect: Connection {conn.connectionId} disconnected.");

            // If the disconnected client had a player object associated with it.
            if (conn.identity != null)
            {
                NetworkLobbyPlayerFFV player = conn.identity.GetComponent<NetworkLobbyPlayerFFV>();
                if (player != null)
                {
                    UnregisterPlayerOnServer(player); // Handle player removal logic.
                }
                else if (DebugManager.DebugModeEnabled)
                {
                    Debug.LogWarning($"NetworkManagerFFV.OnServerDisconnect: Disconnected connection {conn.connectionId} had no NetworkLobbyPlayerFFV identity.");
                }
            }
            base.OnServerDisconnect(conn); // Call Mirror's base implementation.
        }

        /// <summary>
        /// Called on the server when the server is stopped.
        /// Clears the server-side list of lobby players.
        /// </summary>
        public override void OnStopServer()
        {
            ServerLobbyPlayers.Clear();
            if (DebugManager.DebugModeEnabled)
                Debug.Log("NetworkManagerFFV: Server stopped. Server lobby players cleared.");
            base.OnStopServer();
        }

        /// <summary>
        /// Called on the client when the client is stopped.
        /// Clears the client-side list of lobby players and notifies UI.
        /// </summary>
        public override void OnStopClient()
        {
            ClientSideLobbyPlayers.Clear();
            OnClientSidePlayerListChanged?.Invoke(); // Notify UI systems.
            if (DebugManager.DebugModeEnabled)
                Debug.Log("NetworkManagerFFV: Client stopped. Client-side lobby players cleared.");
            base.OnStopClient();
        }

        /// <summary>
        /// Static method called by NetworkLobbyPlayerFFV instances on clients to add themselves to the client-side tracking list.
        /// </summary>
        /// <param name="player">The player instance to add.</param>
        public static void AddClientSidePlayer(NetworkLobbyPlayerFFV player)
        {
            if (!ClientSideLobbyPlayers.Contains(player)) // Avoid duplicates.
            {
                ClientSideLobbyPlayers.Add(player);
                OnClientSidePlayerListChanged?.Invoke(); // Notify UI.
                if (DebugManager.DebugModeEnabled) Debug.Log($"NetworkManagerFFV: Added {player.netId} to ClientSideLobbyPlayers. Count: {ClientSideLobbyPlayers.Count}");
            }
        }

        /// <summary>
        /// Static method called by NetworkLobbyPlayerFFV instances on clients to remove themselves from the client-side tracking list.
        /// </summary>
        /// <param name="player">The player instance to remove.</param>
        public static void RemoveClientSidePlayer(NetworkLobbyPlayerFFV player)
        {
            if (ClientSideLobbyPlayers.Remove(player)) // If player was found and removed.
            {
                OnClientSidePlayerListChanged?.Invoke(); // Notify UI.
                if (DebugManager.DebugModeEnabled) Debug.Log($"NetworkManagerFFV: Removed {player.netId} from ClientSideLobbyPlayers. Count: {ClientSideLobbyPlayers.Count}");
            }
        }
    }
}
