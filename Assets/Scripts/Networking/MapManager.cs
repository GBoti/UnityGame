using UnityEngine;
using Mirror;
using System.Collections.Generic;
using System.Linq;
using DishevelledBadger.FlashFrostVale.Globals;
using DishevelledBadger.FlashFrostVale.SharedData;
using DishevelledBadger.FlashFrostVale.Server;

namespace DishevelledBadger.FlashFrostVale.Networking
{
    // Manages the game map data, generation (server-side), synchronization, and client-side visual representation.
    public class MapManager : NetworkBehaviour
    {
        public static MapManager Instance { get; private set; } // Singleton instance for easy access.

        // SyncList for DYNAMIC tile updates (e.g., building placement) AFTER initial map load.
        // Clients subscribe to its Callback to update visuals for individual tile changes.
        public readonly SyncList<HexTileData> SyncedHexTiles = new SyncList<HexTileData>();

        [Header("Map Generation (Server-Side References)")] // Components used by the server for map creation.
        [Tooltip("Reference to the HexGridLayout GameObject for server-side map logic.")]
        [SerializeField] private HexGridLayout hexGridLogicHost; // Server's logical grid generator.

        [Header("Client-Side Visuals")] // Prefabs and settings for rendering the map on clients.
        [Tooltip("Visual hex prefabs, index maps to typeId.")]
        [SerializeField] private List<GameObject> visualHexTypePrefabs = new List<GameObject>(); // Prefabs for different terrain types.
        [Tooltip("Parent for visual hexes on client.")]
        [SerializeField] private Transform mapVisualsParent; // GameObject to organize instantiated visual tiles.
        [Tooltip("Client-side HexGridLayout for coordinate calculations.")]
        [SerializeField] private HexGridLayout clientCoordinateHelper; // Used by clients to calculate world positions.

        [Header("Client-Side Data")] // Data assets needed by clients.
        [Tooltip("BuildingDatabase. Loaded from Resources if not assigned.")]
        public BuildingDatabase clientBuildingDatabase; // Client's copy of building definitions.

        [Header("Player colors")] // Materials for player identification on visuals.
        [Tooltip("This list needs to be initialized in the same order as in Network manager")]
        public List<Material> playerColors = new List<Material>(); // List of player color materials.

        [Header("Map Boundaries (Synced from Server)")] // World space map extents.
        [SyncVar] public float syncedMapTopEdge;    // Top Z boundary.
        [SyncVar] public float syncedMapLeftEdge;   // Left X boundary.
        [SyncVar] public float syncedMapBottomEdge; // Bottom Z boundary.
        [SyncVar] public float syncedMapRightEdge;  // Right X boundary.

        // Client-side dictionaries to keep track of instantiated visual GameObjects.
        private readonly Dictionary<Vector2Int, GameObject> _clientInstantiatedVisualTiles = new Dictionary<Vector2Int, GameObject>();
        private readonly Dictionary<Vector2Int, GameObject> _clientInstantiatedBuildingVisuals = new Dictionary<Vector2Int, GameObject>();

        // Server-side master list of all hex tile data (not directly synced; sent via RPC chunks initially).
        private List<HexTileData> _serverMapDataInternal = new List<HexTileData>();
        // Server: Tracks chunk request progress for each client.
        private Dictionary<NetworkConnectionToClient, int> _clientChunkRequests = new Dictionary<NetworkConnectionToClient, int>();
        public int chunkSize = 100; // Number of tiles to send per RPC chunk during initial map load.

        // Client-side list assembled from received RPC chunks for the initial map build.
        private List<HexTileData> _clientMapDataInternal = new List<HexTileData>();
        private int _expectedTotalChunks = -1; // Total chunks client expects to receive.
        private int _chunksReceived = 0;      // Chunks received so far by the client.
        private bool _initialMapLoadComplete = false; // Client flag: true after all RPC chunks are processed.

        #region Unity Lifecycle Callbacks

        void Awake()
        {
            // Singleton pattern implementation.
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            if (DebugManager.DebugModeEnabled) Debug.Log($"MapManager [{(isServer ? "SERVER" : "CLIENT")}]: Awake. Instance set.");

            // Load client-side building database from Resources if not assigned in Inspector.
            if (clientBuildingDatabase == null)
            {
                clientBuildingDatabase = Resources.Load<BuildingDatabase>("Data/GlobalBuildingDatabase");
                if (clientBuildingDatabase == null && DebugManager.DebugModeEnabled) Debug.LogError("MapManager: BuildingDatabase missing!");
            }
        }
        #endregion

        #region Server-Side Logic

        public override void OnStartServer() // Called on the server when this NetworkBehaviour starts.
        {
            base.OnStartServer();
            NetworkManagerFFV.OnServerDisconnectedClient += HandleClientDisconnectOnServer; // Subscribe to NM event.
            _clientChunkRequests = new Dictionary<NetworkConnectionToClient, int>(); // Initialize chunk tracker.

            if (hexGridLogicHost == null) { /* Log error: hexGridLogicHost missing */ return; }

            // Generate the server's internal representation of the map.
            _serverMapDataInternal = hexGridLogicHost.GenerateInitialMapLogicAndData();
            if (_serverMapDataInternal == null || _serverMapDataInternal.Count == 0) { /* Log error: map gen failed */ return; }

            SyncedHexTiles.Clear(); // SyncedHexTiles is for dynamic updates, starts empty.
            if (DebugManager.DebugModeEnabled) Debug.Log($"MapManager [SERVER]: Internal map data generated ({_serverMapDataInternal.Count} tiles).");

            // Sync map boundary information to clients.
            syncedMapTopEdge = GlobalConstants.mapTopEdge;
            syncedMapLeftEdge = GlobalConstants.mapLeftEdge;
            syncedMapBottomEdge = GlobalConstants.mapBottomEdge;
            syncedMapRightEdge = GlobalConstants.mapRightEdge;
            if (DebugManager.DebugModeEnabled) Debug.Log($"MapManager [SERVER]: Synced map boundaries set. " +
                $"Map boundaries are : TOP:{syncedMapTopEdge}, RIGHT:{syncedMapRightEdge}, BOTTOM:{syncedMapBottomEdge}, LEFT:{syncedMapLeftEdge}" +
                $"\n GlobalConstants are : TOP:{GlobalConstants.mapTopEdge}, RIGHT:{GlobalConstants.mapRightEdge}, BOTTOM:{GlobalConstants.mapBottomEdge}, LEFT:{GlobalConstants.mapLeftEdge}");
        }

        public override void OnStopServer() // Called on the server when this NetworkBehaviour stops.
        {
            NetworkManagerFFV.OnServerDisconnectedClient -= HandleClientDisconnectOnServer; // Unsubscribe.
            _clientChunkRequests?.Clear(); // Clear collections.
            _serverMapDataInternal?.Clear();
            SyncedHexTiles?.Clear();
            base.OnStopServer();
            if (DebugManager.DebugModeEnabled) Debug.Log("MapManager [SERVER]: Stopped.");
        }

        // Server: Cleans up chunk request tracking if a client disconnects mid-transfer.
        [Server]
        private void HandleClientDisconnectOnServer(NetworkConnectionToClient conn)
        {
            if (_clientChunkRequests != null && conn != null) _clientChunkRequests.Remove(conn);
        }

        /// <summary>[SERVER] Converts a terrain name string to its corresponding typeId (integer).</summary>
        [Server]
        public int GetHexTypeIdFromServerTerrain(string terrainName)
        {
            // Simple mapping from terrain name string to an integer ID.
            switch (terrainName?.ToLower()) // Case-insensitive comparison.
            {
                case "water": return 0;
                case "sand": return 1;
                case "meadow": return 2;
                case "forest": return 3;
                case "mountain": return 4;
                case "snowy peak": return 5;
                default: // Unknown terrain name.
                    if (DebugManager.DebugModeEnabled) Debug.LogWarning($"MapManager [SERVER]: Unknown terrain '{terrainName}'. Defaulting to Meadow (ID 2).");
                    return 2; // Default to Meadow.
            }
        }

        /// <summary>[SERVER] Updates server's internal map data and SyncedHexTiles when a building is placed/removed.</summary>
        [Server]
        public bool ServerUpdateMapDataWithBuilding(Vector2Int coordinates, int buildingTypeId, uint ownerNetId)
        {
            if (_serverMapDataInternal == null) { /* Log error */ return false; }

            // 1. Update the authoritative server-side list (_serverMapDataInternal).
            int internalIndex = _serverMapDataInternal.FindIndex(t => t.coordinates == coordinates); // Find tile by coordinates.
            if (internalIndex == -1) { /* Log error: Tile not found */ return false; }

            HexTileData tileToUpdate = _serverMapDataInternal[internalIndex];
            // Prevent placing a building on an already occupied tile (unless buildingTypeId is -1, meaning clear).
            if (tileToUpdate.occupantBuildingTypeId != -1 && buildingTypeId != -1) { /* Log warning: Tile occupied */ return false; }
            tileToUpdate.occupantBuildingTypeId = buildingTypeId; // Update building ID.
            tileToUpdate.occupantOwnerNetId = ownerNetId;         // Update owner ID.
            _serverMapDataInternal[internalIndex] = tileToUpdate; // Write back to list.

            // 2. Update the SyncedHexTiles list to broadcast this change to clients.
            // This list only contains tiles that have changed *dynamically* after initial RPC load.
            int syncListIndex = SyncedHexTiles.FindIndex(t => t.coordinates == coordinates); // Check if tile is already in SyncList.
            if (syncListIndex != -1) // If yes, update existing entry (triggers OP_SET).
            {
                SyncedHexTiles[syncListIndex] = tileToUpdate;
            }
            else // If no, add new entry for this dynamically changed tile (triggers OP_ADD).
            {
                SyncedHexTiles.Add(tileToUpdate);
            }
            if (DebugManager.DebugModeEnabled) Debug.Log($"MapManager [SERVER]: Updated map data for {coordinates} with building ID {buildingTypeId}.");
            return true;
        }

        /// <summary>[SERVER] Retrieves authoritative tile data for given coordinates from server's internal list.</summary>
        [Server]
        public HexTileData GetServerInternalTileData(Vector2Int coordinates)
        {
            if (_serverMapDataInternal != null)
            {
                // Efficiently find the tile using LINQ's FirstOrDefault.
                HexTileData data = _serverMapDataInternal.FirstOrDefault(tile => tile.coordinates == coordinates);
                // If data.typeId is default (e.g. 0 if struct can't be null) and coords don't match, it means not found.
                if (data.coordinates == coordinates) return data; // Return if found.
            }
            if (DebugManager.DebugModeEnabled) Debug.LogWarning($"MapManager [SERVER]: Tile not found at {coordinates}. Returning default with typeId -999.");
            return new HexTileData { typeId = -999, coordinates = coordinates }; // Return a "not found" marker.
        }

        /// <summary>[COMMAND] Client requests a chunk of map data from the server for initial load.</summary>
        /// <param name="sender">The connection of the client making the request (auto-populated by Mirror).</param>
        [Command(requiresAuthority = false)] // Allow any client to request map data.
        public void CmdRequestMapChunk(bool isInitialOrRetryRequest, NetworkConnectionToClient sender) // 'sender' is automatically provided by Mirror.
        {
            if (_serverMapDataInternal == null || _serverMapDataInternal.Count == 0) // Server map not ready.
            { /* Log error, send empty completion */ TargetReceiveMapChunk(sender, new List<HexTileData>(), 0, 0, true); return; }

            // If it's a new client or a retry, reset their chunk sequence.
            if (isInitialOrRetryRequest || !_clientChunkRequests.ContainsKey(sender))
            {
                _clientChunkRequests[sender] = 0; // Start from chunk 0.
            }

            int currentChunkIndex = _clientChunkRequests[sender]; // Get current chunk index for this client.
            int totalChunks = Mathf.CeilToInt((float)_serverMapDataInternal.Count / chunkSize); // Calculate total chunks.

            if (totalChunks == 0) // Handle empty map case.
            { /* Send empty completion */ TargetReceiveMapChunk(sender, new List<HexTileData>(), 0, 0, true); _clientChunkRequests.Remove(sender); return; }

            if (currentChunkIndex >= totalChunks) // Client already received all chunks or requests out of bounds.
            { /* Send completion signal */ TargetReceiveMapChunk(sender, new List<HexTileData>(), currentChunkIndex, totalChunks, true); _clientChunkRequests.Remove(sender); return; }

            // Prepare and send the current chunk.
            int startIndex = currentChunkIndex * chunkSize;
            int count = Mathf.Min(chunkSize, _serverMapDataInternal.Count - startIndex);
            List<HexTileData> chunkToSend = _serverMapDataInternal.GetRange(startIndex, count);
            bool isFinalChunk = (currentChunkIndex + 1) >= totalChunks;

            if (DebugManager.DebugModeEnabled) Debug.Log($"MapManager [SERVER]: Sending chunk {currentChunkIndex + 1}/{totalChunks} to {sender.connectionId}. Final: {isFinalChunk}");
            TargetReceiveMapChunk(sender, chunkToSend, currentChunkIndex, totalChunks, isFinalChunk); // Send chunk via TargetRpc.

            if (!isFinalChunk) _clientChunkRequests[sender] = currentChunkIndex + 1; // Advance to next chunk for this client.
            else _clientChunkRequests.Remove(sender); // All chunks sent, clean up tracking.
        }

        /// <summary>CLIENT: Checks if the initial RPC-based map load has completed.</summary>
        public bool IsInitialMapLoadCompleteClient() => _initialMapLoadComplete;

        /// <summary>CLIENT: Resets client-side map loading state and clears visuals. Used before retrying map load.</summary>
        public void ClientHardResetMapLoadState()
        {
            if (!isClient) return;
            if (DebugManager.DebugModeEnabled) Debug.Log("MapManager [CLIENT]: Hard resetting client map load state.");
            _clientMapDataInternal.Clear(); // Clear locally assembled map data.
            _expectedTotalChunks = -1;      // Reset chunk tracking.
            _chunksReceived = 0;
            _initialMapLoadComplete = false; // Mark initial load as incomplete.
            ClearClientVisualMap();         // Destroy all visual tiles and buildings.
        }
        #endregion

        #region Client-Side Logic

        public override void OnStartClient() // Called on the client when this NetworkBehaviour starts.
        {
            base.OnStartClient();
            SyncedHexTiles.Callback += OnClientMapDataUpdatedCallback; // Subscribe to dynamic updates.

            // Initialize/reset client-side map state for initial RPC load.
            _clientMapDataInternal.Clear();
            _expectedTotalChunks = -1;
            _chunksReceived = 0;
            _initialMapLoadComplete = false;
            ClearClientVisualMap(); // Ensure clean slate.

            if (!gameObject.activeInHierarchy || !this.enabled) { /* Log error */ return; }
            // Initial map chunk request is now initiated by NetworkGamePlayerFFV.OnStartLocalPlayer.
        }

        public override void OnStopClient() // Called on the client when this NetworkBehaviour stops.
        {
            if (SyncedHexTiles != null) SyncedHexTiles.Callback -= OnClientMapDataUpdatedCallback; // Unsubscribe.
            ClearClientVisualMap(); // Clean up visuals.
            _clientMapDataInternal?.Clear();
            _initialMapLoadComplete = false;
            base.OnStopClient();
            if (DebugManager.DebugModeEnabled) Debug.Log("MapManager [CLIENT]: Stopped.");
        }

        // Client: Callback for dynamic updates to SyncedHexTiles after initial RPC load.
        private void OnClientMapDataUpdatedCallback(SyncList<HexTileData>.Operation op, int itemIndex, HexTileData oldItem, HexTileData newItem)
        {
            // Ignore dynamic updates if initial RPC-based map load isn't complete yet.
            if (!_initialMapLoadComplete) { /* Log warning */ return; }

            if (DebugManager.DebugModeEnabled) Debug.Log($"MapManager [CLIENT]: DYNAMIC SyncedHexTiles UPDATE. Op: {op}, Coords: {newItem.coordinates}");

            switch (op)
            {
                case SyncList<HexTileData>.Operation.OP_ADD:    // New dynamically changed tile added by server.
                case SyncList<HexTileData>.Operation.OP_SET:    // Existing dynamically changed tile updated by server.
                case SyncList<HexTileData>.Operation.OP_INSERT: // (Similar to ADD for SyncList)
                    UpdateSingleVisualTile(newItem); // Update or create the visual for this specific tile.
                    break;
                case SyncList<HexTileData>.Operation.OP_REMOVEAT: // A dynamically changed tile was "reverted" or removed from dynamic list by server.
                    // This means the tile should revert to its state from the initial RPC load.
                    HexTileData originalState = _clientMapDataInternal.FirstOrDefault(t => t.coordinates == oldItem.coordinates);
                    if (originalState.typeId != -999) UpdateSingleVisualTile(originalState); // Revert to RPC state.
                    else ClearSingleVisualTile(oldItem.coordinates); // Or clear if no original state found.
                    break;
                case SyncList<HexTileData>.Operation.OP_CLEAR: // Server cleared all dynamic changes.
                    // Rebuild the entire visual map from the initial RPC-loaded data.
                    RebuildClientVisualMapFromList(_clientMapDataInternal);
                    break;
                    // default: Log unhandled operation.
            }
        }

        /// <summary>[TARGET RPC] Client receives a chunk of map data from the server for initial load.</summary>
        [TargetRpc] // Called by the server, executed on the specific client 'target'.
        public void TargetReceiveMapChunk(NetworkConnection target, List<HexTileData> chunkData, int chunkIndex, int totalChunks, bool isCompleteSignal)
        {
            if (DebugManager.DebugModeEnabled) Debug.Log($"MapManager [CLIENT]: Received Chunk {chunkIndex + 1}/{totalChunks}. Tiles: {chunkData.Count}. CompleteSignal: {isCompleteSignal}");

            // Ignore chunks if initial map already loaded and this isn't a re-completion signal.
            if (_initialMapLoadComplete && !isCompleteSignal) { /* Log warning */ return; }
            if (_initialMapLoadComplete && isCompleteSignal) { /* Log info: already loaded */ return; }

            _clientMapDataInternal.AddRange(chunkData); // Add received tiles to local list.
            _chunksReceived++;                          // Increment received chunk count.

            if (_expectedTotalChunks == -1 && totalChunks > 0) _expectedTotalChunks = totalChunks; // Set expected total if first chunk.

            bool allChunksNowReceived = (_expectedTotalChunks > 0 && _chunksReceived >= _expectedTotalChunks);

            // If this is the final chunk (explicitly signaled) or all expected chunks are now received:
            if (isCompleteSignal || allChunksNowReceived)
            {
                if (!_initialMapLoadComplete) // Ensure map build runs only once.
                {
                    _initialMapLoadComplete = true; // Mark initial RPC load as complete.
                    if (DebugManager.DebugModeEnabled) Debug.Log($"MapManager [CLIENT]: All map chunks processed. Building visual map from RPC data ({_clientMapDataInternal.Count} tiles).");
                    RebuildClientVisualMapFromList(_clientMapDataInternal); // Render the full map.
                }
            }
            else if (totalChunks > 0 && chunkIndex < totalChunks - 1) // If more chunks are expected.
            {
                // Automatically request the next chunk. The `null` for connection is fine for a client calling a Command on a server object it has authority over (like MapManager if player sends to it).
                // Or, if this `CmdRequestMapChunk` is on the MapManager itself, the sender is implicit if called by local player.
                CmdRequestMapChunk(false, null); // Request next part of the map. 'false' means not an initial/retry.
            }
            else if (totalChunks == 0 && isCompleteSignal && !_initialMapLoadComplete) // Empty map completion.
            {
                _initialMapLoadComplete = true;
                RebuildClientVisualMapFromList(_clientMapDataInternal); // Render empty map.
            }
        }

        // Client: Destroys visual GameObjects for a single tile (terrain and building).
        private void ClearSingleVisualTile(Vector2Int coordinates)
        {
            // Destroy terrain visual if it exists.
            if (_clientInstantiatedVisualTiles.TryGetValue(coordinates, out GameObject terrainGO))
            { Destroy(terrainGO); _clientInstantiatedVisualTiles.Remove(coordinates); }
            // Destroy building visual if it exists.
            if (_clientInstantiatedBuildingVisuals.TryGetValue(coordinates, out GameObject buildingGO))
            { Destroy(buildingGO); _clientInstantiatedBuildingVisuals.Remove(coordinates); }
        }

        // Client: Destroys all instantiated visual hexes and buildings.
        private void ClearClientVisualMap()
        {
            if (DebugManager.DebugModeEnabled && (_clientInstantiatedVisualTiles.Count > 0 || _clientInstantiatedBuildingVisuals.Count > 0))
                Debug.Log($"MapManager [CLIENT]: Clearing visuals: {_clientInstantiatedVisualTiles.Count} terrain, {_clientInstantiatedBuildingVisuals.Count} buildings.");

            // Destroy all tracked terrain tiles.
            foreach (GameObject tileGO in _clientInstantiatedVisualTiles.Values) if (tileGO != null) Destroy(tileGO);
            _clientInstantiatedVisualTiles.Clear();
            // Destroy all tracked building visuals.
            foreach (GameObject buildingGO in _clientInstantiatedBuildingVisuals.Values) if (buildingGO != null) Destroy(buildingGO);
            _clientInstantiatedBuildingVisuals.Clear();

            // Fallback: Destroy any remaining children of mapVisualsParent just in case.
            if (mapVisualsParent != null)
            {
                for (int i = mapVisualsParent.childCount - 1; i >= 0; i--) Destroy(mapVisualsParent.GetChild(i).gameObject);
            }
        }

        // Client: Clears existing visuals and rebuilds the entire visual map from a given list of HexTileData.
        // Used for initial RPC load and potentially for full SyncList OP_CLEAR.
        private void RebuildClientVisualMapFromList(List<HexTileData> sourceMapData)
        {
            if (!isClient) return; // Client-only operation.
            // Check for necessary references.
            if (mapVisualsParent == null || visualHexTypePrefabs == null || visualHexTypePrefabs.Count == 0 || clientCoordinateHelper == null)
            { /* Log error: prerequisites missing */ return; }

            ClearClientVisualMap(); // Clear any existing visuals first.
            if (DebugManager.DebugModeEnabled) Debug.Log($"MapManager [CLIENT]: Rebuilding visual map from LIST with {sourceMapData.Count} tiles.");

            foreach (HexTileData tileData in sourceMapData) // Iterate through each tile's data.
            {
                // Validate terrain typeId and prefab availability.
                if (tileData.typeId < 0 || tileData.typeId >= visualHexTypePrefabs.Count || visualHexTypePrefabs[tileData.typeId] == null)
                { /* Log error: invalid typeId or prefab */ continue; }

                GameObject terrainPrefab = visualHexTypePrefabs[tileData.typeId]; // Get terrain prefab.
                Vector3 position = clientCoordinateHelper.GetPositionForHexFromCoordinate(tileData.coordinates); // Calculate world position.
                position.y = 0; // Ensure base is at y=0 before height scaling.

                // Instantiate terrain visual.
                GameObject terrainInstance = Instantiate(terrainPrefab, position, Quaternion.identity, mapVisualsParent);
                terrainInstance.name = $"VisualHex_{tileData.coordinates.x}_{tileData.coordinates.y}_Type{tileData.typeId}";
                terrainInstance.transform.localScale += new Vector3(0, 50f * tileData.height, 0); // Apply height scaling.
                _clientInstantiatedVisualTiles[tileData.coordinates] = terrainInstance; // Track instance.

                // Initialize TriangleHex script on the visual tile if present.
                TriangleHex visualHexScript = terrainInstance.GetComponent<TriangleHex>();
                if (visualHexScript != null)
                {
                    Material hexMat = terrainPrefab.GetComponentInChildren<MeshRenderer>()?.sharedMaterial; // Get material for deselection logic.
                    visualHexScript.InitializeClientVisual(tileData, hexMat); // Pass data and material.
                    if (GlobalHelper.terrainTypes != null && tileData.typeId < GlobalHelper.terrainTypes.Length) // Set terrain string if available.
                        visualHexScript.Terrain = GlobalHelper.terrainTypes[tileData.typeId];
                }
                // else: Log warning if TriangleHex component missing.

                // Instantiate building visual if the tile is occupied.
                if (tileData.occupantBuildingTypeId != -1 && clientBuildingDatabase != null)
                {
                    BuildingTypeData buildingData = clientBuildingDatabase.GetBuildingDataById(tileData.occupantBuildingTypeId);
                    if (buildingData != null && buildingData.buildingVisualAndLogicPrefab != null) // Check building data and prefab.
                    {
                        Vector3 buildingPos = terrainInstance.transform.position; // Position building relative to terrain tile.
                        buildingPos.y = terrainInstance.transform.localScale.y / 50f; // Adjust Y based on terrain height scale.
                        GameObject buildingInstance = Instantiate(buildingData.buildingVisualAndLogicPrefab, buildingPos, Quaternion.identity, terrainInstance.transform); // Parent to terrain.
                        buildingInstance.transform.localScale = GlobalHelper.DivideVector3ByComponents(Vector3.one, terrainInstance.transform.localScale); // Adjust scale relative to parent.
                        buildingInstance.name = $"Building_{buildingData.buildingName}_Owner{tileData.occupantOwnerNetId}_Coords{tileData.coordinates}";
                        _clientInstantiatedBuildingVisuals[tileData.coordinates] = buildingInstance; // Track instance.
                        ApplyPlayerColorToBuildingVisual(buildingInstance, tileData.occupantOwnerNetId); // Set owner's color.
                    }
                    // else: Log warning if building data/prefab not found.
                }
            }
            if (DebugManager.DebugModeEnabled) Debug.Log($"MapManager [CLIENT]: Finished rebuilding. Instantiated {_clientInstantiatedVisualTiles.Count} terrain, {_clientInstantiatedBuildingVisuals.Count} buildings.");
        }

        // Client: Updates or creates a single visual tile based on new data (typically from SyncList update).
        private void UpdateSingleVisualTile(HexTileData newTileData)
        {
            if (!isClient) return; // Client-only.
            if (clientCoordinateHelper == null) { /* Log error */ return; }

            if (DebugManager.DebugModeEnabled) Debug.Log($"MapManager [CLIENT]: Updating single visual tile {newTileData.coordinates}. New OccupantID: {newTileData.occupantBuildingTypeId}");

            GameObject terrainInstance;
            // Check if terrain visual already exists.
            if (_clientInstantiatedVisualTiles.TryGetValue(newTileData.coordinates, out terrainInstance))
            {
                // If exists, update its properties (e.g., height, re-init script).
                TriangleHex hexScript = terrainInstance.GetComponent<TriangleHex>();
                Vector3 currentScale = terrainInstance.transform.localScale;
                float newScaleY = visualHexTypePrefabs[newTileData.typeId].transform.localScale.y + (50f * newTileData.height); // Recalculate Y scale based on base prefab + height
                if (Mathf.Abs(currentScale.y - newScaleY) > 0.01f) terrainInstance.transform.localScale = new Vector3(currentScale.x, newScaleY, currentScale.z);

                if (hexScript != null) hexScript.InitializeClientVisual(newTileData, null); // Re-initialize with new data.
            }
            else // Terrain visual doesn't exist, create it (might happen if OP_ADD/OP_INSERT on SyncList is for a tile not fully processed by RPC yet, or edge cases).
            {
                if (newTileData.typeId < 0 || newTileData.typeId >= visualHexTypePrefabs.Count || visualHexTypePrefabs[newTileData.typeId] == null) { /* Log error */ return; }
                GameObject terrainPrefab = visualHexTypePrefabs[newTileData.typeId];
                Vector3 position = clientCoordinateHelper.GetPositionForHexFromCoordinate(newTileData.coordinates);
                position.y = 0; // Base at y=0.
                terrainInstance = Instantiate(terrainPrefab, position, Quaternion.identity, mapVisualsParent);
                terrainInstance.name = $"VisualHex_{newTileData.coordinates.x}_{newTileData.coordinates.y}_Type{newTileData.typeId}_Dyn";
                terrainInstance.transform.localScale += new Vector3(0, 50f * newTileData.height, 0); // Apply height.
                _clientInstantiatedVisualTiles[newTileData.coordinates] = terrainInstance; // Track.
                TriangleHex visualHexScript = terrainInstance.GetComponent<TriangleHex>();
                if (visualHexScript != null)
                {
                    Material hexMat = terrainPrefab.GetComponentInChildren<MeshRenderer>()?.sharedMaterial;
                    visualHexScript.InitializeClientVisual(newTileData, hexMat);
                }
            }

            // Remove old building visual if one exists.
            if (_clientInstantiatedBuildingVisuals.TryGetValue(newTileData.coordinates, out GameObject oldBuildingGO))
            { if (oldBuildingGO != null) Destroy(oldBuildingGO); _clientInstantiatedBuildingVisuals.Remove(newTileData.coordinates); }

            // Add new building visual if specified in newTileData.
            if (newTileData.occupantBuildingTypeId != -1 && clientBuildingDatabase != null)
            {
                BuildingTypeData buildingData = clientBuildingDatabase.GetBuildingDataById(newTileData.occupantBuildingTypeId);
                if (buildingData != null && buildingData.buildingVisualAndLogicPrefab != null)
                {
                    Vector3 buildingPos = terrainInstance.transform.position; // Position relative to terrain.
                    buildingPos.y = terrainInstance.transform.localScale.y / 50f; // Y pos based on terrain height scale.
                    GameObject newBuildingGO = Instantiate(buildingData.buildingVisualAndLogicPrefab, buildingPos, Quaternion.identity, terrainInstance.transform);
                    newBuildingGO.transform.localScale = GlobalHelper.DivideVector3ByComponents(Vector3.one, terrainInstance.transform.localScale); // Scale relative to parent.
                    newBuildingGO.name = $"Building_{buildingData.buildingName}_Owner{newTileData.occupantOwnerNetId}_Coords{newTileData.coordinates}_Dyn";
                    _clientInstantiatedBuildingVisuals[newTileData.coordinates] = newBuildingGO; // Track.
                    ApplyPlayerColorToBuildingVisual(newBuildingGO, newTileData.occupantOwnerNetId); // Apply owner's color.
                }
            }
            if (DebugManager.DebugModeEnabled) Debug.Log($"MapManager [CLIENT]: Finished updating single visual tile {newTileData.coordinates}.");
        }

        // Client: Applies the owner's player color to a building's visual.
        private void ApplyPlayerColorToBuildingVisual(GameObject buildingGO, uint ownerNetId)
        {
            if (ownerNetId == 0 || buildingGO == null) return; // No owner or no building.

            // Find the owner's NetworkGamePlayerFFV instance from the client-side list.
            NetworkGamePlayerFFV ownerPlayer = NetworkManagerFFV.ClientSideGamePlayers.FirstOrDefault(p => p != null && p.netId == ownerNetId);

            if (ownerPlayer != null && ownerPlayer.ActualPlayerMaterial != null) // If owner and their material found.
            {
                Building buildingComponent = buildingGO.GetComponent<Building>(); // Get Building script on visual.
                if (buildingComponent != null)
                {
                    buildingComponent.SetColor(ownerPlayer.ActualPlayerMaterial); // Call SetColor method.
                }
            }
        }
        #endregion
    }
}