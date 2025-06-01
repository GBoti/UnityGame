using System;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using DishevelledBadger.FlashFrostVale.Server.Generator;
using DishevelledBadger.FlashFrostVale.Globals;
using DishevelledBadger.FlashFrostVale.Networking;
using DishevelledBadger.FlashFrostVale.SharedData;

namespace DishevelledBadger.FlashFrostVale.Server
{
    // Manages the server-side logical grid of hexagonal tiles.
    public class HexGridLayout : NetworkBehaviour
    {
        [Header("Grid Settings")]
        public Vector2Int gridSize; // Dimensions of the hex grid (columns, rows).

        [Header("Tile Settings")]
        public float size = 1f; // Size of individual hex tiles.

        [Header("Pathfinding weights")]
        public float heightWeight; // Influence of height differences in pathfinding (used by MapGenerator).
        public float pathWeight;   // Influence of heuristics in pathfinding (used by MapGenerator).

        [Header("Hex Prefab (Logical Server Tile)")]
        [Tooltip("Prefab for the server-side logical hex tile. Must have TriangleHex component.")]
        public GameObject hexPrefab; // Prefab for server-side logical hex tiles.

        [Header("Generator Object")]
        [Tooltip("Reference to the MapGenerator component/GameObject.")]
        public MapGenerator mapGenerator; // Reference to the map generation logic.

        private readonly float sqrt3 = Mathf.Sqrt(3); // Cached square root of 3 for hex calculations.
        public List<TriangleHex> serverLogicalHexes = new List<TriangleHex>(); // Stores all server-side logical hex instances.

        public MapGenerator MapGeneratorInstance // Public accessor for the map generator.
        {
            get => mapGenerator;
            set => mapGenerator = value;
        }

        /// <summary>[SERVER] Creates server's internal logical grid. Instantiates non-networked TriangleHex objects.</summary>
        [Server] // Ensures this method only runs on the server.
        private void CreateServerLogicGrid()
        {
            DestroyServerLogicGrid(); // Clear any existing logical grid.
            if (DebugManager.DebugModeEnabled) Debug.Log($"HexGridLayout [SERVER-LOGIC]: Creating server logic grid {gridSize.x}, {gridSize.y}");

            if (hexPrefab == null) // Check if prefab is assigned.
            {
                if (DebugManager.DebugModeEnabled) Debug.LogError("HexGridLayout [SERVER-LOGIC]: hexPrefab is not assigned!");
                return;
            }

            for (int y = 0; y < gridSize.y; y++) // Iterate through rows.
            {
                for (int x = 0; x < gridSize.x; x++) // Iterate through columns.
                {
                    // Instantiate the logical hex tile at its calculated position.
                    GameObject tileGO = Instantiate(
                        hexPrefab,
                        GetPositionForHexFromCoordinate(new Vector2Int(x, y)),
                        Quaternion.identity
                    );

                    tileGO.transform.SetParent(gameObject.transform); // Organize under this GameObject.

                    TriangleHex tileLogic = tileGO.GetComponent<TriangleHex>(); // Get the core logic component.
                    if (tileLogic == null)
                    {
                        if (DebugManager.DebugModeEnabled) Debug.LogError($"HexGridLayout [SERVER-LOGIC]: hexPrefab '{hexPrefab.name}' missing TriangleHex component!");
                        Destroy(tileGO); // Clean up if component is missing.
                        continue;
                    }

                    // Apply scale (mostly for potential server-side debug visuals).
                    tileLogic.transform.localScale = new Vector3(size * 20, size * 12, size * 20);
                    tileLogic.Terrain = "ground"; // Set default terrain.
                    tileLogic.InitiateHex(new Vector2Int(x, y)); // Initialize the hex logic.

                    // --- Neighbor linking logic ---
                    // The grid is built from left to right and top to bottom,
                    // so if we add the left and top neighbours to a tile and the other way around too all hexes will have their neighbours
                    // Left neighbor
                    if (x != 0)
                    {
                        TriangleHex neighbor = serverLogicalHexes.Find(h => h.IndexCoordinates.x == x - 1 && h.IndexCoordinates.y == y);
                        if (neighbor != null)
                        {
                            tileLogic.AddNeighbour(neighbor, side.left);
                            neighbor.AddNeighbour(tileLogic, side.right);
                        }
                    }

                    // Top neighbours
                    if (y != 0)
                    {
                        // Odd rows as seen on screen (since we start from zero the first row is actually the 0th)
                        if (y % 2 == 0)
                        {
                            TriangleHex neighbourTopLeft = serverLogicalHexes.Find(h => h.IndexCoordinates.x == x && h.IndexCoordinates.y == y -1);
                            if(neighbourTopLeft != null)
                            {
                                tileLogic.AddNeighbour(neighbourTopLeft, side.topleft);
                                neighbourTopLeft.AddNeighbour(tileLogic, side.bottomright);
                            }
                            TriangleHex neighbourTopRight = serverLogicalHexes.Find(h => h.IndexCoordinates.x == x + 1 && h.IndexCoordinates.y == y - 1);
                            if(neighbourTopRight != null)
                            {
                                tileLogic.AddNeighbour(neighbourTopRight, side.topright);
                                neighbourTopRight.AddNeighbour(tileLogic, side.bottomleft);
                            }
                        } 
                        else // Even rows as seen on screen
                        {
                            TriangleHex neighbourTopLeft = serverLogicalHexes.Find(h => h.IndexCoordinates.x == x - 1 && h.IndexCoordinates.y == y - 1);
                            if (neighbourTopLeft != null)
                            {
                                tileLogic.AddNeighbour(neighbourTopLeft, side.topleft);
                                neighbourTopLeft.AddNeighbour(tileLogic, side.bottomright);
                            }
                            TriangleHex neighbourTopRight = serverLogicalHexes.Find(h => h.IndexCoordinates.x == x && h.IndexCoordinates.y == y - 1);
                            if (neighbourTopRight != null)
                            {
                                tileLogic.AddNeighbour(neighbourTopRight, side.topright);
                                neighbourTopRight.AddNeighbour(tileLogic, side.bottomleft);
                            }
                        }
                    }
                    
                    serverLogicalHexes.Add(tileLogic); // Add successfully created logical tile to the list.
                }
            }

            // Set global map boundary constants if tiles were created.
            if (serverLogicalHexes.Count > 0)
            {
                GlobalConstants.mapTopEdge = GetPositionForHexFromCoordinate(serverLogicalHexes[0].IndexCoordinates).z;
                GlobalConstants.mapLeftEdge = GetPositionForHexFromCoordinate(serverLogicalHexes[0].IndexCoordinates).x;
                GlobalConstants.mapBottomEdge = GetPositionForHexFromCoordinate(serverLogicalHexes[serverLogicalHexes.Count - 1].IndexCoordinates).z;
                GlobalConstants.mapRightEdge = GetPositionForHexFromCoordinate(serverLogicalHexes[serverLogicalHexes.Count - 1].IndexCoordinates).x;
            }
            if (DebugManager.DebugModeEnabled) Debug.Log($"HexGridLayout [SERVER-LOGIC]: Finished creating {serverLogicalHexes.Count} logical hex instances.");
        }

        /// <summary>[SERVER] Generates map's logical structure and data. Returns list of HexTileData for clients.</summary>
        [Server]
        public List<HexTileData> GenerateInitialMapLogicAndData()
        {
            if (!NetworkServer.active) // Server-only check.
            {
                if (DebugManager.DebugModeEnabled) Debug.LogWarning("HexGridLayout: GenerateInitialMapLogicAndData called, but not on active server.");
                return null;
            }
            // MapManager.Instance is used for type conversion, ensure it exists.
            if (MapManager.Instance == null)
            {
                if (DebugManager.DebugModeEnabled) Debug.LogError("HexGridLayout [SERVER]: MapManager.Instance is null.");
                return null;
            }

            if (DebugManager.DebugModeEnabled) Debug.Log("HexGridLayout [SERVER]: Starting generation of server-side logical grid.");
            CreateServerLogicGrid(); // Create the base logical grid.

            if (this.MapGeneratorInstance != null) // If a map generator is assigned...
            {
                this.MapGeneratorInstance.hexes = this.serverLogicalHexes; // ...pass it the grid...
                this.MapGeneratorInstance.gridSize = this.gridSize;       // ...and its dimensions...
                this.MapGeneratorInstance.Procedural_Map_Generate();      // ...then run generation.
            }
            else
            {
                if (DebugManager.DebugModeEnabled) Debug.LogError("HexGridLayout [SERVER]: MapGeneratorInstance not assigned!");
                return null;
            }

            List<HexTileData> generatedTiles = new List<HexTileData>(); // Prepare data for clients.
            foreach (TriangleHex serverTileLogic in this.serverLogicalHexes) // Convert logical tiles to data DTOs.
            {
                if (serverTileLogic == null) continue;
                HexTileData data = new HexTileData(
                    serverTileLogic.IndexCoordinates,
                    MapManager.Instance.GetHexTypeIdFromServerTerrain(serverTileLogic.Terrain), // Get client-side type ID.
                    serverTileLogic.Height
                );
                generatedTiles.Add(data);
            }

            if (DebugManager.DebugModeEnabled) Debug.Log($"HexGridLayout [SERVER]: Finished generation. Generated {generatedTiles.Count} tiles for server logic.");
            return generatedTiles; // Return data for client map setup.
        }

        /// <summary>[SERVER] Destroys all server-side logical hex GameObjects.</summary>
        [Server]
        public void DestroyServerLogicGrid()
        {
            if (DebugManager.DebugModeEnabled) Debug.Log("HexGridLayout [SERVER-LOGIC]: Destroying server logic grid...");
            foreach (TriangleHex childLogic in serverLogicalHexes) // Iterate through existing logical tiles.
            {
                if (childLogic != null && childLogic.gameObject != null)
                {
                    Destroy(childLogic.gameObject); // Destroy their GameObjects.
                }
            }
            serverLogicalHexes.Clear(); // Clear the list.
        }

        /// <summary>Calculates world position for a hex from its grid coordinate (even-r offset).</summary>
        public Vector3 GetPositionForHexFromCoordinate(Vector2Int coordinate)
        {
            int column = coordinate.x;
            int row = coordinate.y;

            float width, height_geom, xPosition, zPosition;
            bool shouldOffset = (row % 2) == 0; // Even rows are offset horizontally.
            float currentHexSize = this.size;

            width = sqrt3 * currentHexSize;        // Geometric width of a hex.
            height_geom = 2f * currentHexSize;   // Geometric height of a hex.

            float horizontalDistance = width;
            float verticalDistance = height_geom * (3f / 4f); // Vertical distance between hex centers.
            float offset = shouldOffset ? width / 2f : 0f;  // Horizontal offset for even rows.

            xPosition = (column * horizontalDistance) + offset;
            zPosition = row * verticalDistance;

            return new Vector3(xPosition, 0, -zPosition); // Y is 0 (flat plane), Z is negated for typical top-down view.
        }
    }
}