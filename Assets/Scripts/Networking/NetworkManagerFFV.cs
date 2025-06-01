using UnityEngine;
using Mirror;
using System;
using System.Linq;
using System.Collections.Generic;
using DishevelledBadger.FlashFrostVale.Globals;
using UnityEngine.SceneManagement;
using DishevelledBadger.FlashFrostVale.Player;
using DishevelledBadger.FlashFrostVale.Server;

namespace DishevelledBadger.FlashFrostVale.Networking
{
    // Custom NetworkManager for FlashFrostVale, handling game flow, player spawning, and scene management.
    public class NetworkManagerFFV : NetworkManager
    {
        [Header("Scene Configuration")] // Inspector settings for scene management.
        [Scene] [SerializeField] private string menuScene = string.Empty; // Lobby/menu scene path.
        [Scene] [SerializeField] private string gameScene = string.Empty; // Main game scene path.

        [Header("Player Prefabs")] // Prefabs for player objects.
        [SerializeField] private NetworkLobbyPlayerFFV lobbyPlayerPrefab = null; // Prefab for players in the lobby.
        [SerializeField] private NetworkGamePlayerFFV gamePlayerPrefab = null;  // Prefab for players in the game.

        [Header("Game Settings")] // General game configuration.
        [Tooltip("List of materials to assign to players for identification.")]
        public List<Material> playerColors = new List<Material>(); // Colors assigned to players for identification. Ensure populated in Inspector.
        [Tooltip("Reference to the BuildingDatabase ScriptableObject. Loaded from Resources if not assigned.")]
        public BuildingDatabase buildingDatabase; // ScriptableObject containing all building data.

        // Server-side lists of connected player objects.
        public readonly List<NetworkLobbyPlayerFFV> ServerLobbyPlayers = new List<NetworkLobbyPlayerFFV>();
        public readonly List<NetworkGamePlayerFFV> ServerGamePlayers = new List<NetworkGamePlayerFFV>();

        // Static client-side lists for UI or local logic to track players.
        public static readonly List<NetworkLobbyPlayerFFV> ClientSideLobbyPlayers = new List<NetworkLobbyPlayerFFV>();
        public static readonly List<NetworkGamePlayerFFV> ClientSideGamePlayers = new List<NetworkGamePlayerFFV>();

        // Events for various network lifecycle moments.
        public static event Action OnClientConnectedToManager;      // Client successfully connected.
        public static event Action OnClientDisconnectedFromManager; // Client disconnected.
        public static event Action<NetworkConnectionToClient> OnServerReadiedPlayer; // Server confirmed a player is ready.
        public static event Action OnClientSideLobbyPlayerListChanged; // Client-side lobby player list updated.
        public static event Action<NetworkConnectionToClient> OnServerDisconnectedClient; // Server detected a client disconnect.

        #region Unity Lifecycle Callbacks
        public override void Awake()
        {
            base.Awake();
            // Load BuildingDatabase from Resources if not assigned in Inspector.
            if (buildingDatabase == null)
            {
                buildingDatabase = Resources.Load<BuildingDatabase>("Data/GlobalBuildingDatabase");
                // Log error if still not found after attempting to load from Resources.
                if (buildingDatabase == null && DebugManager.DebugModeEnabled)
                {
                    Debug.LogError("NetworkManagerFFV: BuildingDatabase not found in Resources or not assigned in Inspector!");
                }
            }
            // Log warning if playerColors list is unassigned or empty.
            if (playerColors == null || playerColors.Count == 0)
            {
                if (DebugManager.DebugModeEnabled) Debug.LogWarning("NetworkManagerFFV: PlayerColors list is not assigned or empty in the Inspector. Player colors may not work correctly.");
            }
        }
        #endregion

        #region Server Lifecycle Callbacks
        public override void OnStartServer() // Called when the server starts.
        {
            base.OnStartServer();
            spawnPrefabs = Resources.LoadAll<GameObject>("SpawnablePrefabs").ToList(); // Load all network-spawnable prefabs from Resources folder.

            // Log errors for unassigned essential player prefabs.
            if (lobbyPlayerPrefab == null && DebugManager.DebugModeEnabled)
                Debug.LogError("NetworkManagerFFV: lobbyPlayerPrefab is NULL! Assign it in the Inspector.");
            if (gamePlayerPrefab == null && DebugManager.DebugModeEnabled)
                Debug.LogError("NetworkManagerFFV: gamePlayerPrefab is NULL! Assign it in the Inspector.");
            // Check BuildingDatabase again, as it's critical.
            if (buildingDatabase == null && DebugManager.DebugModeEnabled)
                Debug.LogError("NetworkManagerFFV: BuildingDatabase is still null in OnStartServer!");

            if (DebugManager.DebugModeEnabled)
                Debug.Log("NetworkManagerFFV: Server started.");
        }

        /// <summary>
        /// Called on the client when the client starts.
        /// Registers spawnable prefabs loaded from Resources.
        /// </summary>
        public override void OnStartClient() // Called when the client starts.
        {
            base.OnStartClient();
            // Register all spawnable prefabs on the client for network instantiation.
            GameObject[] prefabsToRegister = Resources.LoadAll<GameObject>("SpawnablePrefabs");
            foreach (GameObject prefab in prefabsToRegister)
            {
                if (prefab != null) NetworkClient.RegisterPrefab(prefab);
            }
            // Also register specific player prefabs if not in "SpawnablePrefabs" folder.
            if (lobbyPlayerPrefab != null) NetworkClient.RegisterPrefab(lobbyPlayerPrefab.gameObject);
            if (gamePlayerPrefab != null) NetworkClient.RegisterPrefab(gamePlayerPrefab.gameObject);

            if (DebugManager.DebugModeEnabled)
                Debug.Log("NetworkManagerFFV: Client started.");
        }

        public override void OnServerConnect(NetworkConnectionToClient conn) // Server: new client connection attempt.
        {
            base.OnServerConnect(conn);
            // Reject connections if server is not in the designated menu scene.
            if (SceneManager.GetActiveScene().path != menuScene)
            {
                if (DebugManager.DebugModeEnabled)
                    Debug.LogWarning($"NetworkManagerFFV: Client {conn.connectionId} tried to connect while server not in menu scene ({SceneManager.GetActiveScene().path}). Disconnecting.");
                conn.Disconnect();
                return;
            }

            // Reject connections if the server is already at maximum player capacity.
            if (numPlayers >= maxConnections)
            {
                if (DebugManager.DebugModeEnabled)
                    Debug.LogWarning($"NetworkManagerFFV: Server full ({numPlayers}/{maxConnections}). Disconnecting client {conn.connectionId}");
                conn.Disconnect();
                return;
            }
            if (DebugManager.DebugModeEnabled)
                Debug.Log($"NetworkManagerFFV: Client {conn.connectionId} connected. Current numPlayers (before AddPlayer): {numPlayers}.");
        }

        // Override of OnServerAddLobbyPlayer to add extra functionality when a player is added to the server
        public override void OnServerAddLobbyPlayer(NetworkConnectionToClient conn)
        {
            if (DebugManager.DebugModeEnabled)
                Debug.Log($"NetworkManagerFFV: OnServerAddLobbyPlayer called for conn {conn.connectionId}. Scene: {SceneManager.GetActiveScene().path}");

            // Only spawn a lobby player if in the menu scene.
            if (SceneManager.GetActiveScene().path == menuScene)
            {
                if (lobbyPlayerPrefab == null) // Check if lobby player prefab is assigned.
                {
                    if (DebugManager.DebugModeEnabled) Debug.LogError("NetworkManagerFFV: lobbyPlayerPrefab is NULL. Cannot spawn lobby player.");
                    conn.Disconnect(); // Disconnect if cannot spawn player.
                    return;
                }
                GameObject lobbyPlayerObject = Instantiate(lobbyPlayerPrefab.gameObject); // Instantiate lobby player.
                NetworkServer.AddPlayerForConnection(conn, lobbyPlayerObject); // Associate player object with connection.
                if (DebugManager.DebugModeEnabled)
                    Debug.Log($"NetworkManagerFFV: LobbyPlayer (netId: {lobbyPlayerObject.GetComponent<NetworkIdentity>().netId}) spawned for conn {conn.connectionId}.");
            }
            else // If not in menu scene (e.g., game already started), do not spawn a lobby player.
            {
                if (DebugManager.DebugModeEnabled)
                    Debug.LogWarning($"NetworkManagerFFV: OnServerAddLobbyPlayer in unexpected scene: {SceneManager.GetActiveScene().path}. No player spawned for conn {conn.connectionId}.");
            }
        }

        public override void ServerChangeScene(string newSceneName) // Server: Initiates a scene change for all connected clients.
        {
            if (string.IsNullOrEmpty(newSceneName)) // Validate new scene name.
            {
                if (DebugManager.DebugModeEnabled) Debug.LogError("NetworkManagerFFV [SERVER]: ServerChangeScene called with null or empty newSceneName.");
                return;
            }

            // If changing from the menu scene to the game scene, replace lobby players with game players.
            if (SceneManager.GetActiveScene().path == menuScene && newSceneName.Contains(gameScene))
            {
                if (DebugManager.DebugModeEnabled) Debug.Log($"NetworkManagerFFV [SERVER]: Changing scene from LOBBY ({menuScene}) to GAME ({newSceneName}). Replacing players.");

                // Iterate backwards for safe removal/modification if ServerLobbyPlayers list were modified directly (though it's cleared later).
                for (int i = ServerLobbyPlayers.Count - 1; i >= 0; i--)
                {
                    NetworkLobbyPlayerFFV lobbyPlayer = ServerLobbyPlayers[i];
                    // Skip if lobby player or its connection is invalid.
                    if (lobbyPlayer == null || lobbyPlayer.connectionToClient == null)
                    {
                        if (DebugManager.DebugModeEnabled) Debug.LogWarning($"NetworkManagerFFV [SERVER]: Null lobby player or connection at index {i} during scene change. Skipping.");
                        continue;
                    }

                    NetworkConnectionToClient conn = lobbyPlayer.connectionToClient;
                    GameObject gamePlayerObject = Instantiate(gamePlayerPrefab.gameObject); // Instantiate game player.
                    NetworkGamePlayerFFV gamePlayerInstance = gamePlayerObject.GetComponent<NetworkGamePlayerFFV>();

                    if (gamePlayerInstance == null) // Ensure game player prefab has the correct component.
                    {
                        if (DebugManager.DebugModeEnabled) Debug.LogError($"NetworkManagerFFV [SERVER]: gamePlayerPrefab for {lobbyPlayer.DisplayName} is missing NetworkGamePlayerFFV component. Destroying instantiated object.");
                        Destroy(gamePlayerObject);
                        continue;
                    }

                    gamePlayerInstance.ServerSetDisplayName(lobbyPlayer.DisplayName); // Transfer display name.

                    // Assign player color based on their order in the lobby.
                    if (playerColors != null && playerColors.Count > 0)
                    {
                        gamePlayerInstance.ServerSetPlayerColorIndex(playerColors.Count); // There must be at least as many colors set in inspector as max players
                        if (DebugManager.DebugModeEnabled) Debug.Log($"NetworkManagerFFV [SERVER]: Assigned color index {playerColors.Count} to {gamePlayerInstance.GetGameDisplayName()}");
                    }
                    else if (DebugManager.DebugModeEnabled)
                    {
                        Debug.LogWarning("NetworkManagerFFV [SERVER]: playerColors list is empty or null. Cannot assign color index.");
                    }

                    // Replace the connection's player object with the new game player object.
                    NetworkServer.ReplacePlayerForConnection(conn, gamePlayerObject, ReplacePlayerOptions.KeepAuthority);
                    DontDestroyOnLoad(gamePlayerObject); // Make game player object persist if manager does.
                    if (DebugManager.DebugModeEnabled) Debug.Log($"NetworkManagerFFV [SERVER]: Replaced LobbyPlayer {lobbyPlayer.DisplayName} with GamePlayer {gamePlayerInstance.GetGameDisplayName()} (netId {gamePlayerInstance.netId}) for conn {conn.connectionId}.");
                }
                ServerLobbyPlayers.Clear(); // Clear the server's list of lobby players.
            }

            base.ServerChangeScene(newSceneName); // Execute Mirror's base scene change logic.

            if (newSceneName.Contains(gameScene)) // Post-scene change log.
            {
                if (DebugManager.DebugModeEnabled) Debug.Log($"NetworkManagerFFV [SERVER]: Server has finished changing to scene {newSceneName}.");
            }
        }

        public override void OnServerReady(NetworkConnectionToClient conn) // Server: Client signals its readiness after loading a new scene.
        {
            base.OnServerReady(conn);
            if (DebugManager.DebugModeEnabled)
                Debug.Log($"NetworkManagerFFV [SERVER]: Connection {conn.connectionId} is READY in current scene: {SceneManager.GetActiveScene().name}.");

            OnServerReadiedPlayer?.Invoke(conn); // Invoke event for other systems.

            // If server is in the game scene and the ready connection has a valid player identity:
            if (SceneManager.GetActiveScene().path.Contains(gameScene) && conn.identity != null)
            {
                NetworkGamePlayerFFV gamePlayer = conn.identity.GetComponent<NetworkGamePlayerFFV>();
                if (gamePlayer != null) // If the player identity is a game player:
                {
                    if (DebugManager.DebugModeEnabled)
                        Debug.Log($"NetworkManagerFFV [SERVER]: GamePlayer {gamePlayer.GetGameDisplayName()} (netId {gamePlayer.netId}) is ready in game scene. Attempting initial building placement.");

                    Colony colony = gamePlayer.GetComponent<Colony>(); // Get player's colony component.
                    // Check for all necessary conditions and references to place initial building.
                    if (colony != null && colony.mainBuildingTypeData != null && buildingDatabase != null && MapManager.Instance != null)
                    {
                        HexGridLayout gridLayout = FindFirstObjectByType<HexGridLayout>(); // Find the server's grid layout.
                        if (gridLayout == null || gridLayout.serverLogicalHexes == null || gridLayout.serverLogicalHexes.Count == 0)
                        {
                            if (DebugManager.DebugModeEnabled)
                                Debug.LogError($"NetworkManagerFFV [SERVER]: HexGridLayout, its serverLogicalHexes, or the list is null/empty. Cannot place initial building for {gamePlayer.GetGameDisplayName()}. serverLogicalHexes.Count: {(gridLayout?.serverLogicalHexes?.Count ?? -1)}");
                            return;
                        }

                        if (DebugManager.DebugModeEnabled)
                            Debug.Log($"NetworkManagerFFV [SERVER]: gridLayout.serverLogicalHexes.Count = {gridLayout.serverLogicalHexes.Count} for {gamePlayer.GetGameDisplayName()} before filtering for start position.");

                        // Find all suitable, unoccupied hexes of the desired starting terrain type.
                        List<TriangleHex> availableStartPositions = gridLayout.serverLogicalHexes.FindAll(h =>
                            h != null &&
                            h.Terrain == colony.desiredHexTypeForStart && // Matches desired terrain.
                            h.Occupant == null && // Server logical hex is unoccupied.
                            MapManager.Instance != null && // MapManager exists.
                            MapManager.Instance.GetServerInternalTileData(h.IndexCoordinates).occupantBuildingTypeId == -1 // Synced tile data also shows unoccupied.
                        );

                        if (DebugManager.DebugModeEnabled) // Log count of available positions.
                        {
                            Debug.Log($"NetworkManagerFFV [SERVER]: Found {availableStartPositions.Count} available '{colony.desiredHexTypeForStart}' start positions for {gamePlayer.GetGameDisplayName()}.");
                            // If none found, log some details from the first few hexes for diagnostics.
                            if (availableStartPositions.Count == 0 && gridLayout.serverLogicalHexes.Count > 0)
                            {
                                int checkLimit = Mathf.Min(10, gridLayout.serverLogicalHexes.Count);
                                Debug.LogWarning($"--- Start positions check for {gamePlayer.GetGameDisplayName()} ({colony.desiredHexTypeForStart}) ---");
                                for (int i = 0; i < checkLimit; i++)
                                {
                                    var h = gridLayout.serverLogicalHexes[i];
                                    if (h != null)
                                    {
                                        var tileData = MapManager.Instance.GetServerInternalTileData(h.IndexCoordinates);
                                        Debug.LogWarning($"Inspect Hex {h.IndexCoordinates}: Terrain='{h.Terrain}', OccupantNullOnLogicHex={h.Occupant == null}, TileDataOccupiedByID={tileData.occupantBuildingTypeId != -1}");
                                    }
                                }
                                Debug.LogWarning($"--- End of start positions check ---");
                            }
                        }

                        if (availableStartPositions.Count > 0) // If suitable positions found:
                        {
                            // Choose a random start hex from the available list.
                            TriangleHex chosenStartHexLogic = availableStartPositions[UnityEngine.Random.Range(0, availableStartPositions.Count)];
                            Vector2Int placementCoords = chosenStartHexLogic.IndexCoordinates;

                            if (DebugManager.DebugModeEnabled)
                                Debug.Log($"NetworkManagerFFV [SERVER]: Player {gamePlayer.GetGameDisplayName()} assigned start hex {chosenStartHexLogic.IndexCoordinates}. Ordering initial building at {placementCoords}.");

                            // Instruct the game player object (server-side) to place its initial building.
                            gamePlayer.ServerOrderInitialBuildingPlacement(placementCoords, buildingDatabase);
                        }
                        else // No suitable start positions found.
                        {
                            if (DebugManager.DebugModeEnabled)
                                Debug.LogError($"NetworkManagerFFV [SERVER]: NO available '{colony.desiredHexTypeForStart}' start positions found for player {gamePlayer.GetGameDisplayName()}. Building not placed. Check map generation and criteria.");
                        }
                    }
                    else // Prerequisites for initial building placement not met.
                    {
                        if (DebugManager.DebugModeEnabled)
                            Debug.LogError($"NetworkManagerFFV [SERVER]: Prerequisites for placing initial building not met for {gamePlayer?.GetGameDisplayName()}. ColonyNull: {colony == null}, MainBuildingNull: {colony?.mainBuildingTypeData == null}, DB Null: {buildingDatabase == null}, MapManagerNull: {MapManager.Instance == null}");
                    }
                }
                else if (DebugManager.DebugModeEnabled) // Player identity on connection is not a game player.
                {
                    Debug.LogWarning($"NetworkManagerFFV [SERVER]: Conn {conn.connectionId} is ready in game scene, but its identity is not a NetworkGamePlayerFFV.");
                }
            }
        }

        public override void OnStopClient() // Client: Called when this client stops or disconnects.
        {
            ClientSideLobbyPlayers.Clear(); // Clear local list of lobby players.
            OnClientSideLobbyPlayerListChanged?.Invoke(); // Notify UI or other systems.
            ClientSideGamePlayers.Clear(); // Clear local list of game players.
            if (DebugManager.DebugModeEnabled)
                Debug.Log("NetworkManagerFFV: Client stopped. Client-side player lists cleared.");
            base.OnStopClient();
        }

        public override void OnClientConnect() // Client: Called when this client successfully connects to the server.
        {
            base.OnClientConnect();
            if (DebugManager.DebugModeEnabled)
                Debug.Log("NetworkManagerFFV: Client connected to server.");
            OnClientConnectedToManager?.Invoke(); // Invoke connection event.
        }

        public override void OnClientDisconnect() // Client: Called when this client disconnects from the server.
        {
            if (DebugManager.DebugModeEnabled)
                Debug.Log("NetworkManagerFFV: Client disconnected from server.");

            ClientSideLobbyPlayers.Clear(); // Clear local lists on disconnect.
            OnClientSideLobbyPlayerListChanged?.Invoke();
            ClientSideGamePlayers.Clear();

            OnClientDisconnectedFromManager?.Invoke(); // Invoke disconnect event.
            base.OnClientDisconnect();
        }
        #endregion

        #region Player Registration & Management
        // Server: Registers a lobby player object to the server-side list.
        public void RegisterLobbyPlayerOnServer(NetworkLobbyPlayerFFV player)
        {
            if (!ServerLobbyPlayers.Contains(player)) // Avoid duplicates.
            {
                player.IsLeader = ServerLobbyPlayers.Count == 0; // First player to join is the leader.
                ServerLobbyPlayers.Add(player);
                if (DebugManager.DebugModeEnabled)
                    Debug.Log($"NetworkManagerFFV: Player {player.DisplayName} ({player.netId}) registered in LOBBY on server. IsLeader: {player.IsLeader}. Total: {ServerLobbyPlayers.Count}");
                NotifyPlayersOfReadyStateChange(); // Trigger updates related to ready status.
            }
        }

        // Server: Unregisters a lobby player (e.g., on disconnect from lobby).
        public void UnregisterLobbyPlayerOnServer(NetworkLobbyPlayerFFV player)
        {
            if (ServerLobbyPlayers.Remove(player)) // If player was successfully removed.
            {
                if (DebugManager.DebugModeEnabled)
                    Debug.Log($"NetworkManagerFFV: Player {player.DisplayName} ({player.netId}) unregistered from LOBBY on server. Total: {ServerLobbyPlayers.Count}.");
                NotifyPlayersOfReadyStateChange();
            }
        }

        // Server: Called to notify all players about changes in ready states (e.g., for UI updates).
        [Server]
        public void NotifyPlayersOfReadyStateChange()
        {
            // This method might send an RPC or update a SyncVar that clients observe.
            if (DebugManager.DebugModeEnabled)
            {
                Debug.Log($"NetworkManagerFFV.NotifyPlayersOfReadyStateChange: Server check - All players ready? {IsReadyToStart()}");
            }
        }

        // Server: Determines if all conditions to start the game are met.
        [Server]
        public bool IsReadyToStart()
        {
            // Not ready if no players (unless in test mode, which might allow starting solo).
            if (ServerLobbyPlayers.Count == 0)
            {
                if (DebugManager.DebugModeEnabled) Debug.Log("NetworkManagerFFV.IsReadyToStart: No players in lobby (and not TestMode), not ready.");
                return false;
            }
            // Not ready if any player has a null/empty/placeholder display name.
            if (ServerLobbyPlayers.Any(p => p == null || string.IsNullOrWhiteSpace(p.DisplayName) || p.DisplayName == "Loading..."))
            {
                if (DebugManager.DebugModeEnabled) Debug.Log("NetworkManagerFFV.IsReadyToStart: Not all players have valid display names. Not ready.");
                return false;
            }
            // Not ready if any player is not marked as ready.
            if (ServerLobbyPlayers.Any(p => p == null || !p.IsReady))
            {
                if (DebugManager.DebugModeEnabled) Debug.Log("NetworkManagerFFV.IsReadyToStart: Not all players are ready. Not ready.");
                return false;
            }

            if (DebugManager.DebugModeEnabled) Debug.Log("NetworkManagerFFV.IsReadyToStart: All conditions met. Ready to start!");
            return true; // All checks passed.
        }

        // Server: Initiates the game start if currently in menu and all players are ready.
        [Server]
        public void StartGame()
        {
            if (DebugManager.DebugModeEnabled)
                Debug.Log("NetworkManagerFFV.StartGame: Attempting to start game.");
            // Can only start from the menu scene.
            if (SceneManager.GetActiveScene().path == menuScene)
            {
                if (!IsReadyToStart()) // Check if all players are ready.
                {
                    if (DebugManager.DebugModeEnabled)
                        Debug.LogWarning("NetworkManagerFFV.StartGame: Cannot start game: Conditions not met (e.g., not all players ready).");
                    return;
                }
                if (DebugManager.DebugModeEnabled)
                    Debug.Log("NetworkManagerFFV.StartGame: Starting game for all players!");

                ServerChangeScene(gameScene); // Change to the game scene.
            }
            else if (DebugManager.DebugModeEnabled) // Attempt to start from a non-menu scene.
            {
                Debug.LogWarning("NetworkManagerFFV.StartGame: Attempted to start game while not on menu scene.");
            }
        }

        // Server: Registers a game player object (after scene change from lobby to game).
        public void RegisterGamePlayerOnServer(NetworkGamePlayerFFV player)
        {
            if (player == null) // Guard against null player object.
            {
                if (DebugManager.DebugModeEnabled) Debug.LogError("NetworkManagerFFV [SERVER]: RegisterGamePlayerOnServer called with a null player.");
                return;
            }
            if (!ServerGamePlayers.Contains(player)) // Avoid duplicates.
            {
                ServerGamePlayers.Add(player);
                if (DebugManager.DebugModeEnabled)
                    Debug.Log($"NetworkManagerFFV [SERVER]: GamePlayer {player.GetGameDisplayName()} (netId:{player.netId}) registered. Total game players: {ServerGamePlayers.Count}.");

                Colony colony = player.GetComponent<Colony>(); // Get associated colony.
                if (colony != null)
                {
                    colony.InitializeColonyOnServer(); // Initialize the player's colony data.
                    if (DebugManager.DebugModeEnabled) Debug.Log($"NetworkManagerFFV [SERVER]: Initialized colony for {player.GetGameDisplayName()}.");
                }
                else if (DebugManager.DebugModeEnabled) // Log error if Colony component is missing.
                {
                    Debug.LogError($"NetworkManagerFFV [SERVER]: GamePlayer {player.GetGameDisplayName()} is missing Colony component!");
                }
            }
        }

        // Server: Unregisters a game player (e.g., on disconnect during the game).
        public void UnregisterGamePlayerOnServer(NetworkGamePlayerFFV player)
        {
            if (player == null) return; // Guard against null.
            if (ServerGamePlayers.Remove(player)) // If player was in the list and removed.
            {
                if (DebugManager.DebugModeEnabled)
                    Debug.Log($"NetworkManagerFFV [SERVER]: GamePlayer {player.GetGameDisplayName()} (netId:{player.netId}) unregistered. Total game players: {ServerGamePlayers.Count}.");
            }
        }

        // Static Client-Side: Adds a lobby player to the local client-side list (for UI, etc.).
        public static void AddClientSideLobbyPlayer(NetworkLobbyPlayerFFV player)
        {
            if (!ClientSideLobbyPlayers.Contains(player)) // Avoid duplicates.
            {
                ClientSideLobbyPlayers.Add(player);
                OnClientSideLobbyPlayerListChanged?.Invoke(); // Notify listeners of change.
                if (DebugManager.DebugModeEnabled) Debug.Log($"NetworkManagerFFV: Added {player.DisplayName} ({player.netId}) to ClientSideLobbyPlayers. Count: {ClientSideLobbyPlayers.Count}");
            }
        }

        // Static Client-Side: Removes a lobby player from the local client-side list.
        public static void RemoveClientSideLobbyPlayer(NetworkLobbyPlayerFFV player)
        {
            if (ClientSideLobbyPlayers.Remove(player)) // If player was removed.
            {
                OnClientSideLobbyPlayerListChanged?.Invoke(); // Notify listeners.
                if (DebugManager.DebugModeEnabled) Debug.Log($"NetworkManagerFFV: Removed {player.DisplayName} ({player.netId}) from ClientSideLobbyPlayers. Count: {ClientSideLobbyPlayers.Count}");
            }
        }

        // Static Client-Side: Adds a game player to the local client-side list.
        public static void AddClientSideGamePlayer(NetworkGamePlayerFFV player)
        {
            if (!ClientSideGamePlayers.Contains(player)) // Avoid duplicates.
            {
                ClientSideGamePlayers.Add(player);
                if (DebugManager.DebugModeEnabled) Debug.Log($"NetworkManagerFFV: Added {player.GetGameDisplayName()} ({player.netId}) to ClientSideGamePlayers. Count: {ClientSideGamePlayers.Count}");
            }
        }

        // Static Client-Side: Removes a game player from the local client-side list.
        public static void RemoveClientSideGamePlayer(NetworkGamePlayerFFV player)
        {
            if (ClientSideGamePlayers.Remove(player)) // If player was removed.
            {
                if (DebugManager.DebugModeEnabled) Debug.Log($"NetworkManagerFFV: Removed {player.GetGameDisplayName()} ({player.netId}) from ClientSideGamePlayers. Count: {ClientSideGamePlayers.Count}");
            }
        }
        #endregion
    }
}