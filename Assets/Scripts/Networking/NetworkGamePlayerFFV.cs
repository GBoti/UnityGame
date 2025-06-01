using UnityEngine;
using TMPro;
using Mirror;
using DishevelledBadger.FlashFrostVale.Globals;
using DishevelledBadger.FlashFrostVale.Player;
using DishevelledBadger.FlashFrostVale.Server;
using DishevelledBadger.FlashFrostVale.SharedData;
using System.Linq;
using System.Collections;


namespace DishevelledBadger.FlashFrostVale.Networking
{
    // Represents a player in the game scene, handling synced data, server logic, and client-side interactions.
    public class NetworkGamePlayerFFV : NetworkBehaviour
    {
        // Constants for client-side map request logic.
        private const float MAP_REQUEST_TIMEOUT_SECONDS = 15f;  // Max time to wait for map data after a request.
        private const int MAX_MAP_REQUEST_RETRIES = 3;          // How many times to retry map request if it fails/times out.
        private const float RETRY_DELAY_SECONDS = 2f;           // Delay between map request retries.

        [Header("Synced Data")] // Data synchronized from server to all clients.
        [SyncVar(hook = nameof(OnDisplayNameChangedHook))] // Player's name, hook updates GameObject name.
        private string gameDisplayName = "Loading...";

        [SyncVar(hook = nameof(OnPlayerColorIndexChangedHook))] // Index for player's color, hook updates material.
        public int PlayerColorIndex = -1;

        public Material ActualPlayerMaterial { get; private set; } // Client-side material derived from PlayerColorIndex.


        [Header("Server-Side Game Logic References")] // References for server-side operations.
        [Tooltip("Colony component, expected to be on this same GameObject.")]
        [SerializeField] public Colony colony; // Manages this player's colony resources and buildings.

        [Header("Client-Side UI (Local Player Only)")] // UI elements controlled by the local player.
        [SerializeField] private TMP_Text messageTextUI; // For displaying messages to the local player.
        [SerializeField] private CameraController cameraController; // Local player's camera controller.
        [SerializeField] private InfoPanel infoPanelUI; // UI panel for displaying hex/building info.
        [SerializeField] private GameObject buildingTypesUI; // UI for showing available building types.
        [SerializeField] private AudioListener audioListenerComponent; // Audio listener, enabled only for local player.


        [Header("Local Player Hex Selection")] // Settings for local player's hex interaction.
        [Tooltip("Material to apply to the 'BackgroundHex' of a selected hex. Assign in Prefab Inspector.")]
        public Material selectedHexMaterial; // Material used to highlight the selected hex.

        private TriangleHex _currentlySelectedVisualHex; // Backing field for the currently selected hex visual.
        public TriangleHex currentSelectedHexVisual { get; private set; } // Public read-only access to selected hex.

        private NetworkManagerFFV _gameManagerInstance; // Cached reference to the NetworkManager.
        private NetworkManagerFFV GameManager => _gameManagerInstance ??= NetworkManager.singleton as NetworkManagerFFV; // Lazy-loaded GameManager.

        #region Server Code

        public override void OnStartServer() // Called on the server when this object is spawned.
        {
            base.OnStartServer();
            if (GameManager != null) GameManager.RegisterGamePlayerOnServer(this); // Register with NetworkManager.
            else if (DebugManager.DebugModeEnabled) Debug.LogError($"SERVER [{netId}]: GameManager not found!");
        }

        public override void OnStopServer() // Called on the server when this object is destroyed.
        {
            if (GameManager != null) GameManager.UnregisterGamePlayerOnServer(this); // Unregister.
            base.OnStopServer();
        }

        /// <summary>[SERVER] Sets the player's display name. This is a SyncVar, so changes propagate.</summary>
        [Server] // This method can only be called on the server.
        public void ServerSetDisplayName(string displayName)
        {
            if (DebugManager.DebugModeEnabled) Debug.Log($"SERVER [{netId}]: Setting name to '{displayName}'.");
            this.gameDisplayName = displayName;
        }

        /// <summary>[SERVER] Sets the player's color index. This is a SyncVar.</summary>
        [Server]
        public void ServerSetPlayerColorIndex(int index)
        {
            if (DebugManager.DebugModeEnabled) Debug.Log($"SERVER [{netId}]: Setting color index to {index} for {gameDisplayName}.");
            this.PlayerColorIndex = index;
        }

        /// <summary>[SERVER] Orders the placement of the player's initial colony building.</summary>
        [Server]
        public void ServerOrderInitialBuildingPlacement(Vector2Int coordinates, BuildingDatabase buildingDB)
        {
            // Ensure all necessary components and data are available.
            if (colony == null || colony.mainBuildingTypeData == null || buildingDB == null || MapManager.Instance == null)
            { /* Log error */ return; }

            BuildingTypeData buildingDataToPlace = colony.mainBuildingTypeData; // Get data for the main colony building.
            int buildingTypeId = buildingDataToPlace.buildingId; // Get its unique ID.

            if (DebugManager.DebugModeEnabled) Debug.Log($"SERVER [{netId}]: Player {GetGameDisplayName()} placing initial '{buildingDataToPlace.buildingName}' at {coordinates}.");

            // Update the server's authoritative map data (SyncedHexTiles in MapManager) with the new building.
            bool mapDataUpdated = MapManager.Instance.ServerUpdateMapDataWithBuilding(coordinates, buildingTypeId, this.netId);
            if (!mapDataUpdated) { /* Log error: Failed to update shared map data */ return; }

            // Update the server's internal logical grid representation.
            HexGridLayout gridLayout = FindFirstObjectByType<HexGridLayout>(); // Find the server's logical grid.
            if (gridLayout != null && gridLayout.serverLogicalHexes != null)
            {
                TriangleHex logicalHex = gridLayout.serverLogicalHexes.FirstOrDefault(h => h.IndexCoordinates == coordinates);
                if (logicalHex != null)
                {
                    if (logicalHex.Occupant != null && DebugManager.DebugModeEnabled) Debug.LogWarning($"SERVER [{netId}]: Logical hex {coordinates} already occupied. Overwriting.");
                    // Instantiate the building's logical representation (server-side GameObject).
                    if (buildingDataToPlace.buildingVisualAndLogicPrefab != null)
                    {
                        GameObject buildingGOLogical = Instantiate(buildingDataToPlace.buildingVisualAndLogicPrefab);
                        Building buildingLogicInstance = buildingGOLogical.GetComponent<Building>();
                        if (buildingLogicInstance != null)
                        {
                            buildingLogicInstance.name = buildingDataToPlace.buildingName; // Set name for server-side clarity.
                            logicalHex.Occupant = buildingLogicInstance; // Assign to logical hex.
                            if (colony.Structures != null) colony.Structures.Add(buildingLogicInstance); // Add to colony's list of structures.
                        }
                        else { Destroy(buildingGOLogical); /* Log error: Prefab missing Building component */ }
                    }
                    // else: Log error: BuildingTypeData missing prefab.
                }
                // else: Log error: Could not find logical hex.
            }
            // else: Log warning: HexGridLayout not found.
            if (DebugManager.DebugModeEnabled) Debug.Log($"SERVER [{netId}]: Initial building placement for {GetGameDisplayName()} at {coordinates} completed.");
        }
        #endregion

        #region Client Code

        public override void OnStartClient() // Called on all clients when this object is spawned.
        {
            base.OnStartClient();
            NetworkManagerFFV.AddClientSideGamePlayer(this); // Add to static client-side list for UI/local tracking.

            if (DebugManager.DebugModeEnabled) Debug.Log($"CLIENT [{netId}]: OnStartClient. Name: {gameDisplayName}, Local: {isLocalPlayer}, ColorIdx: {PlayerColorIndex}.");

            // Manually trigger SyncVar hooks on start for initial state synchronization on this client.
            OnDisplayNameChangedHook(gameDisplayName, gameDisplayName); // Set initial name.
            OnPlayerColorIndexChangedHook(PlayerColorIndex, PlayerColorIndex); // Set initial color.

            // Enable audio listener only for the local player to avoid multiple active listeners.
            if (audioListenerComponent != null) audioListenerComponent.enabled = isLocalPlayer;
        }

        public override void OnStartLocalPlayer() // Called only on the client that owns this player object.
        {
            base.OnStartLocalPlayer();
            InitializeLocalPlayerSetup(); // Perform setup specific to the local player (camera, UI).
            StartCoroutine(RequestMapWhenReadyCoroutine()); // Begin process to request and load map data.
        }

        // Coroutine for the local player to request map data from the server, with retries.
        private IEnumerator RequestMapWhenReadyCoroutine()
        {
            int currentRetries = 0;
            bool mapLoadSuccessfullyCompleted = false; // Changed variable name for clarity

            // Loop for retrying map request.
            while (currentRetries <= MAX_MAP_REQUEST_RETRIES && !mapLoadSuccessfullyCompleted)
            {
                // Wait until MapManager is available and network client is active and ready.
                while (MapManager.Instance == null || !NetworkClient.active || !NetworkClient.ready)
                { /* Log waiting state */ yield return null; }

                // Additional wait for NetworkClient.ready and valid MapManager.netId (ensures it's spawned and initialized).
                float readyWaitTimer = 0f; const float readyWaitTimeout = 5f;
                while ((!NetworkClient.ready || MapManager.Instance.netId == 0) && readyWaitTimer < readyWaitTimeout)
                { /* Log waiting for ready/netId */ readyWaitTimer += Time.deltaTime; yield return null; }

                if (!NetworkClient.ready || MapManager.Instance.netId == 0) // If still not ready after timeout.
                {
                    currentRetries++;
                    if (currentRetries <= MAX_MAP_REQUEST_RETRIES) yield return new WaitForSeconds(RETRY_DELAY_SECONDS);
                    continue; // Go to next retry.
                }

                // Conditions met, attempt to request map data.
                MapManager.Instance.ClientHardResetMapLoadState(); // Reset any previous client map load state.
                MapManager.Instance.CmdRequestMapChunk(true, this.connectionToClient); // Send command to server.

                // Wait for map loading to complete on client, or timeout.
                float requestTimer = 0f;
                while (requestTimer < MAP_REQUEST_TIMEOUT_SECONDS)
                {
                    if (MapManager.Instance.IsInitialMapLoadCompleteClient()) // Check if MapManager reports completion.
                    { mapLoadSuccessfullyCompleted = true; break; }
                    requestTimer += Time.deltaTime; yield return null;
                }

                if (mapLoadSuccessfullyCompleted) { /* Log success */ break; } // Exit retry loop on success.
                else // Map request timed out for this attempt.
                {
                    currentRetries++;
                    if (currentRetries <= MAX_MAP_REQUEST_RETRIES) yield return new WaitForSeconds(RETRY_DELAY_SECONDS);
                }
            }

            if (!mapLoadSuccessfullyCompleted) { /* Log final failure after all retries */ }
        }

        // Sets up components and UI specific to the local player.
        private void InitializeLocalPlayerSetup()
        {
            if (DebugManager.DebugModeEnabled) Debug.Log($"CLIENT [{netId}]: LOCAL PLAYER {GetGameDisplayName()} initializing local components.");
            // Activate and setup camera controller.
            if (cameraController != null) cameraController.gameObject.SetActive(true);
            else if (DebugManager.DebugModeEnabled) Debug.LogError($"CLIENT [{netId}]: CameraController NULL for local player!");
            // Initialize UI elements (hide/clear them initially).
            if (messageTextUI != null) { messageTextUI.text = ""; messageTextUI.gameObject.SetActive(false); }
            if (infoPanelUI != null) infoPanelUI.Hide();
            if (buildingTypesUI != null) buildingTypesUI.SetActive(false);
        }

        public override void OnStopClient() // Called on all clients when this object is destroyed or client stops.
        {
            NetworkManagerFFV.RemoveClientSideGamePlayer(this); // Remove from static client-side list.
            if (DebugManager.DebugModeEnabled && (!string.IsNullOrEmpty(gameDisplayName) && gameDisplayName != "Loading..."))
                Debug.Log($"CLIENT [{netId}]: OnStopClient: Player {gameDisplayName} stopped.");
            base.OnStopClient();
        }

        // Hook for SyncVar 'gameDisplayName'. Updates the GameObject's name for editor clarity.
        void OnDisplayNameChangedHook(string oldName, string newName)
        {
            if (DebugManager.DebugModeEnabled) Debug.Log($"CLIENT [{netId}]: NameChangedHook {(isLocalPlayer ? "LOCAL" : "REMOTE")}: '{oldName}' to '{newName}'.");
            gameObject.name = $"GamePlayer_{newName}_[{netId}]"; // Set GameObject name.
        }

        // Hook for SyncVar 'PlayerColorIndex'. Sets the actual player material.
        void OnPlayerColorIndexChangedHook(int oldIndex, int newIndex)
        {
            if (DebugManager.DebugModeEnabled) Debug.Log($"CLIENT [{netId}]: ColorIndexChangedHook for {gameDisplayName} ({(isLocalPlayer ? "LOCAL" : "REMOTE")}): {oldIndex} to {newIndex}.");
            // Attempt to get the material from GameManager's list.
            if (newIndex >= 0 && GameManager != null && GameManager.playerColors != null && newIndex < GameManager.playerColors.Count)
            {
                ActualPlayerMaterial = GameManager.playerColors[newIndex]; // Assign material.
            }
            else // Invalid index or missing resources.
            {
                ActualPlayerMaterial = null; // Default to no specific material.
                if (DebugManager.DebugModeEnabled) Debug.LogWarning($"CLIENT [{netId}]: Could not set ActualPlayerMaterial for {gameDisplayName}. Invalid index/GameManager/playerColors.");
            }
        }

        /// <summary>CLIENT (Local Player): Manages hex selection, highlighting, and UI updates.</summary>
        public void ClientManageHexSelection(TriangleHex clickedHexVisual)
        {
            if (!isLocalPlayer) return; // Only the local player can manage their selection.

            // Deselect previous hex if different from the newly clicked one.
            if (_currentlySelectedVisualHex != null && _currentlySelectedVisualHex != clickedHexVisual)
            {
                _currentlySelectedVisualHex.DeselectVisual();
                if (buildingTypesUI != null) buildingTypesUI.SetActive(false); // Hide build options.
            }

            if (clickedHexVisual == _currentlySelectedVisualHex) // Clicked the same hex again (toggle deselection).
            {
                clickedHexVisual.DeselectVisual();
                _currentlySelectedVisualHex = null; // Clear selection.
                if (infoPanelUI != null) infoPanelUI.Hide(); // Hide info panel.
                if (buildingTypesUI != null) buildingTypesUI.SetActive(false);
            }
            else // New hex selected (or first selection).
            {
                _currentlySelectedVisualHex = clickedHexVisual;
                if (_currentlySelectedVisualHex != null)
                {
                    if (selectedHexMaterial == null && DebugManager.DebugModeEnabled) Debug.LogWarning($"CLIENT [{netId}]: selectedHexMaterial NULL!");
                    _currentlySelectedVisualHex.SelectVisual(selectedHexMaterial); // Highlight new selection.

                    // Fetch and display data for the selected hex.
                    if (MapManager.Instance != null && infoPanelUI != null)
                    {
                        // Attempt to get data from server-side authoritative cache (which uses SyncedHexTiles as fallback).
                        HexTileData selectedData = MapManager.Instance.GetServerInternalTileData(_currentlySelectedVisualHex.IndexCoordinates);

                        if (selectedData.typeId != -999) // -999 might indicate invalid/not found.
                        {
                            infoPanelUI.ShowHexData(selectedData); // Show data in info panel.
                        }
                        // else: Log error or hide panel if no valid data found.
                    }
                    // else: Log error if MapManager or infoPanelUI is null.
                }
            }
        }
        #endregion

        /// <summary>Gets the player's display name.</summary>
        public string GetGameDisplayName() => gameDisplayName;
    }
}