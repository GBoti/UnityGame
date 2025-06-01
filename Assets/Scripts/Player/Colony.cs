using System.Collections.Generic;
using UnityEngine;
using Mirror;
using DishevelledBadger.FlashFrostVale.Server;
using DishevelledBadger.FlashFrostVale.Globals;
using DishevelledBadger.FlashFrostVale.SharedData;
using DishevelledBadger.FlashFrostVale.Networking;

namespace DishevelledBadger.FlashFrostVale.Player
{
    // Represents a player's colony in the game, managing resources, production, and buildings.
    // This is a NetworkBehaviour, with authority primarily on the server.
    public class Colony : NetworkBehaviour
    {
        // Server-side dictionaries for resource management.
        private Dictionary<string, float> _serverStorage = new Dictionary<string, float>();
        private Dictionary<string, float> _serverProduction = new Dictionary<string, float>();

        // --- Server-Side Logic Fields ---
        private List<Building> _serverLogicalStructures; // Server's list of this colony's Building instances.

        [Header("Colony Setup (Assign in NetworkGamePlayerFFV Prefab)")] // Inspector settings for new colonies
        [Tooltip("Data for the main colony building (e.g., Town Hall).")]
        public BuildingTypeData mainBuildingTypeData; // ScriptableObject or data for the starting building.
        public string desiredHexTypeForStart = "Meadow"; // Preferred terrain for initial placement.
        float initialFood = 100;  // Starting resources.
        float initialWood = 100;
        float initialMud = 0;
        float initialStone = 0;

        // --- Client-Side References (For local player's UI/control, NOT networked state) ---
        public HexGridLayout clientMapReference; // Optional: reference to map visuals for client interaction.
        public CameraController clientCamera;     // Optional: reference to local player's camera.

        // Read-only server-side access to the list of buildings this colony owns.
        public List<Building> Structures => _serverLogicalStructures;

        // --- Initialization ---
        public override void OnStartServer() // Called on the server when this NetworkBehaviour starts.
        {
            base.OnStartServer();
            // Initialize server-side collections. Full resource init often handled by NetworkManager or player spawn logic.
            _serverLogicalStructures = new List<Building>();
            if (_serverStorage == null) _serverStorage = new Dictionary<string, float>();
            if (_serverProduction == null) _serverProduction = new Dictionary<string, float>();
        }

        /// <summary>[SERVER] Ensures the server-side list of buildings is initialized.</summary>
        [Server] // This method only runs on the server.
        public void InitializeStructuresList()
        {
            if (_serverLogicalStructures == null) _serverLogicalStructures = new List<Building>();
        }

        /// <summary>[SERVER] Sets up the initial resources and production for this colony.</summary>
        [Server]
        public void InitializeColonyOnServer()
        {
            if (DebugManager.DebugModeEnabled) Debug.Log($"Colony [Server NetID: {this.netId}]: Initializing resources.");
            // Set initial storage.
            _serverStorage["food"] = initialFood;
            _serverStorage["wood"] = initialWood;
            _serverStorage["mud"] = initialMud;
            _serverStorage["stone"] = initialStone;
            // Set initial production (usually starts at 0 until buildings add to it).
            _serverProduction["food"] = 0;
            _serverProduction["wood"] = 0;
            _serverProduction["mud"] = 0;
            _serverProduction["stone"] = 0;
        }

        /// <summary>[SERVER] Places the initial colony building on the server's logical map.</summary>
        /// <param name="startHexLogic">The server's logical hex tile for placement.</param>
        /// <param name="buildingLogicPrefab">The prefab/template for the logical building instance.</param>
        [Server]
        public void PlaceInitialColonyBuilding(TriangleHex startHexLogic, Building buildingLogicPrefab)
        {
            if (startHexLogic == null || buildingLogicPrefab == null) 
            {
                if (DebugManager.DebugModeEnabled) Debug.Log($"Colony PlaceInitialColonyBuilding : startHexLogic or buildingLogicPrefab is null, returning");
                return;
            }
            if (startHexLogic.Occupant != null) 
            {
                if (DebugManager.DebugModeEnabled) Debug.Log($"Colony PlaceInitialColonyBuilding : startHexLogic {startHexLogic.IndexCoordinates} is already occupied");
                return; 
            }

            // Instantiate server-side logical building (as Building is a MonoBehaviour).
            Building newBuildingInstance = Instantiate(buildingLogicPrefab);
            // Copy properties from prefab/data to the instance.
            newBuildingInstance.buildingName = buildingLogicPrefab.buildingName;
            newBuildingInstance.food = buildingLogicPrefab.food; newBuildingInstance.foodCost = buildingLogicPrefab.foodCost;
            newBuildingInstance.wood = buildingLogicPrefab.wood; newBuildingInstance.woodCost = buildingLogicPrefab.woodCost;
            newBuildingInstance.mud = buildingLogicPrefab.mud; newBuildingInstance.mudCost = buildingLogicPrefab.mudCost;
            newBuildingInstance.stone = buildingLogicPrefab.stone; newBuildingInstance.stoneCost = buildingLogicPrefab.stoneCost;

            startHexLogic.Occupant = newBuildingInstance; // Assign to server's logical hex.
            InitializeStructuresList(); // Ensure list is ready.
            if (!_serverLogicalStructures.Contains(newBuildingInstance)) _serverLogicalStructures.Add(newBuildingInstance);

            // Update server-side production based on the new building.
            _serverProduction["food"] += newBuildingInstance.food; 
           _serverProduction["wood"] += newBuildingInstance.wood;
            _serverProduction["mud"] += newBuildingInstance.mud;
            _serverProduction["stone"] += newBuildingInstance.stone;

            // Update synced map data so clients see the new building.
            if (MapManager.Instance != null)
            {
                int tileIndex = MapManager.Instance.SyncedHexTiles.FindIndex(td => td.coordinates == startHexLogic.IndexCoordinates);
                if (tileIndex != -1)
                {
                    HexTileData data = MapManager.Instance.SyncedHexTiles[tileIndex];
                    int buildingTypeId = (mainBuildingTypeData != null) ? mainBuildingTypeData.buildingId : 0;
                    data.occupantBuildingTypeId = buildingTypeId;
                    data.occupantOwnerNetId = this.netId; // Owner is this Colony's player.
                    MapManager.Instance.SyncedHexTiles[tileIndex] = data; // Triggers SyncList update.
                }
                else
                {
                    if (DebugManager.DebugModeEnabled) Debug.Log($"Colony PlaceInitialColonyBuilding : target tile {startHexLogic.IndexCoordinates} could not be found in SyncedHexTiles");
                }
            }
            if (DebugManager.DebugModeEnabled) Debug.Log($"Colony [Server NetID: {this.netId}]: Placed initial building '{buildingLogicPrefab.buildingName}'.");
        }

        // --- Placeholder methods for future Command/Server-driven logic ---
        // These currently have basic [Server] guards and log warnings.
        public void AddBuilding(TriangleHex h, Building b)
        {
            if (!NetworkServer.active) { return; }
            if (DebugManager.DebugModeEnabled) Debug.LogWarning("Colony.AddBuilding: Placeholder server logic. Use Command.");
        }

        public void RemoveBuilding(TriangleHex h)
        {
            if (!NetworkServer.active) { return; }
            if (DebugManager.DebugModeEnabled) Debug.LogWarning("Colony.RemoveBuilding: Placeholder server logic. Use Command.");
        }

        public void Produce() // To be called by a server-side tick manager.
        {
            if (!NetworkServer.active) { return; }
            if (DebugManager.DebugModeEnabled) Debug.LogWarning("Colony.Produce: Placeholder server logic. Implement server tick.");
        }
    }
}