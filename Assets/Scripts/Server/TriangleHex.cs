using System.Collections.Generic;
using UnityEngine;
using DishevelledBadger.FlashFrostVale.SharedData;
using DishevelledBadger.FlashFrostVale.Globals;
using Mirror;

namespace DishevelledBadger.FlashFrostVale.Server
{
    // Defines the 6 directions for hexagonal neighbours.
    public enum side
    {
        left,
        topleft,
        topright,
        right,
        bottomright,
        bottomleft
    }

    // Represents a single hexagonal tile in the game world, handling both server logic and client visuals.
    public class TriangleHex : MonoBehaviour
    {
        // --- Common Fields (Server & Client) ---
        private Vector2Int _indexCoordinates; // Grid position of this hex.
        private float _height;                // Height of the terrain on this hex

        // --- Server-Side Logical Fields ---
        private Dictionary<side, TriangleHex> _logicalNeighbours; // Adjacent hexes (server-side).
        private string _logicalTerrainTypeString;                 // E.g., "Forest", "Water" (server-side).
        private Dictionary<string, float> _logicalResources;    // Available resources on this hex (server-side).
        private Building _logicalOccupantBuildingInstance;      // Building instance on this hex (server-side).
        private Vector3 _previousLocalScaleServer;            // Stores initial scale for server-side height adjustments.

        // --- Client-Side Visual Fields ---
        [Header("Client Visuals - Assign in Prefab if this script is on Visual Hex")]
        [Tooltip("MeshRenderer of the child 'BackgroundHex' object used for selection highlighting.")]
        [SerializeField] public MeshRenderer backgroundHexRenderer; // Renderer for selection highlight (client-side).
        private Material _originalBackgroundMaterial;             // Original material to revert selection (client-side).
        private bool _isVisuallySelectedLocally = false;        // Tracks if this hex is selected by the local client.

        // --- Properties ---
        public Vector2Int IndexCoordinates // Public accessor for grid position.
        {
            get => _indexCoordinates;
            private set => _indexCoordinates = value; // Settable internally or during initialization.
        }

        public float Height // Public accessor for game world elevation.
        {
            get => _height;
            set => _height = value;
        }

        // Server-Side Specific Properties
        public string Terrain // Logical terrain type (Server-only write).
        {
            get => _logicalTerrainTypeString;
            set { if (Mirror.NetworkServer.active) _logicalTerrainTypeString = value; } // Only server can set.
        }
        public Dictionary<side, TriangleHex> Neighbours => _logicalNeighbours; // Read-only access to server-side neighbours.
        public Dictionary<string, float> Resources { get => _logicalResources; set { if (Mirror.NetworkServer.active) _logicalResources = value; } } // Server-side resources (Server-only write).

        public Building Occupant // Logical building occupant (Server-only write with cleanup).
        {
            get => _logicalOccupantBuildingInstance;
            set
            {
                if (!Mirror.NetworkServer.active) // Prevent client modification.
                {
                    if (DebugManager.DebugModeEnabled && Application.isPlaying) Debug.LogWarning($"TriangleHex.Occupant setter called on client for hex {IndexCoordinates}. This is server-side logic.");
                    return;
                }
                if (_logicalOccupantBuildingInstance != null) // If an old building exists...
                {
                    Destroy(_logicalOccupantBuildingInstance.gameObject); // ...destroy it.
                }
                _logicalOccupantBuildingInstance = value; // Set the new building.
                if (_logicalOccupantBuildingInstance != null) // If a new building is assigned...
                {
                    _logicalOccupantBuildingInstance.transform.SetParent(this.transform); // Parent it to this hex.
                    _logicalOccupantBuildingInstance.transform.localPosition = new Vector3(0, _height, 0); // Position it.
                }
            }
        }

        // --- Server-Side Initialization ---
        // Initializes a logical hex instance on the server.
        public void InitiateHex(Vector2Int iC)
        {
            this.IndexCoordinates = iC;
            this._logicalNeighbours = new Dictionary<side, TriangleHex>();
            this._logicalResources = new Dictionary<string, float>();
            this.Occupant = null;         // Start with no occupant.
            this.Terrain = "ground";      // Default terrain.
            this._previousLocalScaleServer = transform.localScale;
        }

        // --- Client-Side Initialization ---
        // Initializes a visual hex instance on the client based on server data.
        public void InitializeClientVisual(HexTileData data, Material visualMaterialFromPrefab)
        {
            this.IndexCoordinates = data.coordinates;
            this.Height = data.height;
            this.name = $"VisualHex_{data.coordinates.x}_{data.coordinates.y}";

            if (backgroundHexRenderer != null)
            {
                _originalBackgroundMaterial = backgroundHexRenderer.sharedMaterial; // Store original material.
            }
            else if (DebugManager.DebugModeEnabled) // Fallback to find renderer if not assigned.
            {
                Transform bgHexTransform = transform.Find("BackgroundHex");
                if (bgHexTransform != null)
                {
                    backgroundHexRenderer = bgHexTransform.GetComponent<MeshRenderer>();
                    if (backgroundHexRenderer != null) _originalBackgroundMaterial = backgroundHexRenderer.sharedMaterial;
                    else Debug.LogWarning($"TriangleHex Visual {name}: Child 'BackgroundHex' found, but no MeshRenderer.");
                }
                else Debug.LogWarning($"TriangleHex Visual {name}: backgroundHexRenderer not assigned and 'BackgroundHex' child not found.");
            }
        }

        /// <summary>Highlights this hex visual for local selection.</summary>
        public void SelectVisual(Material selectionMaterial)
        {
            if (backgroundHexRenderer == null)
            {
                if (DebugManager.DebugModeEnabled) Debug.LogWarning($"TriangleHex Visual {name}: Cannot SelectVisual, backgroundHexRenderer is null.");
                return;
            }
            if (_originalBackgroundMaterial == null) _originalBackgroundMaterial = backgroundHexRenderer.sharedMaterial; // Ensure original is stored.
            if (selectionMaterial != null) backgroundHexRenderer.material = selectionMaterial; // Apply selection.
            else if (DebugManager.DebugModeEnabled) Debug.LogWarning($"TriangleHex Visual {name}: SelectVisual called with null selectionMaterial.");
            _isVisuallySelectedLocally = true;
        }

        /// <summary>Reverts this hex visual to its original appearance.</summary>
        public void DeselectVisual()
        {
            if (backgroundHexRenderer == null || _originalBackgroundMaterial == null) return; // Nothing to revert or no renderer.
            backgroundHexRenderer.material = _originalBackgroundMaterial; // Revert material.
            _isVisuallySelectedLocally = false;
        }

        // --- Server-Side Logic Methods ---
        [Server] // This method only runs on the server.
        public void SetMaterial(Material m) // Sets material for server's visual representation (if any).
        {
            for (int i = 0; i < transform.childCount - 1; i++) // Iterate children (excluding potential background).
            {
                MeshRenderer mr = transform.GetChild(i).GetComponent<MeshRenderer>();
                if (mr != null) mr.material = m;
            }
        }

        [Server] // This method only runs on the server.
        public void SetHeight(float h) // Adjusts hex height and visual scale on the server.
        {
            this.Height = h;
            if (_previousLocalScaleServer == Vector3.zero) _previousLocalScaleServer = transform.localScale; // Cache original scale.
            transform.localScale = _previousLocalScaleServer; // Reset scale before applying height.
            transform.localScale += new Vector3(0, h * 100, 0); // Apply height scaling (100 seems to be a multiplier).
        }

        [Server] // This method only runs on the server.
        public void SetBackgroundMaterial(Material mat) // Sets background material on server.
        {
            if (transform.childCount > 0)
            {
                Transform child = transform.GetChild(transform.childCount - 1); // Assume last child is background.
                MeshRenderer mr = child.GetComponent<MeshRenderer>();
                if (mr != null) mr.material = mat;
            }
        }

        [Server] // This method only runs on the server.
        public Material GetMaterial() // Gets main material from the server's first child renderer.
        {
            if (transform.childCount > 0)
            {
                MeshRenderer mr = transform.GetChild(0).GetComponent<MeshRenderer>();
                if (mr != null) return mr.material;
            }
            return null;
        }

        [Server] // This method only runs on the server.
        public void AddNeighbour(TriangleHex nb, side s) // Adds a logical neighbour on the server.
        {
            if (_logicalNeighbours == null) _logicalNeighbours = new Dictionary<side, TriangleHex>();
            _logicalNeighbours[s] = nb;
        }

        [Server] // This method only runs on the server.
        public void GenerateResources() // Generates resources for this hex based on its terrain type.
        {
            if (_logicalResources == null) _logicalResources = new Dictionary<string, float>();

            // Initialize all resources to 0.
            _logicalResources["food"] = 0.0f;
            _logicalResources["wood"] = 0.0f;
            _logicalResources["mud"] = 0.0f;
            _logicalResources["stone"] = 0.0f;

            // Populate resources based on terrain.
            switch (_logicalTerrainTypeString)
            {
                case "Water":
                    _logicalResources["food"] = Random.Range(1, 3); _logicalResources["wood"] = Random.Range(0, 1);
                    _logicalResources["mud"] = Random.Range(1, 2); _logicalResources["stone"] = Random.Range(0, 1);
                    break;
                case "Sand":
                    _logicalResources["mud"] = Random.Range(0, 1); _logicalResources["stone"] = Random.Range(0, 1);
                    break;
                case "Meadow":
                    _logicalResources["food"] = Random.Range(1, 3); _logicalResources["wood"] = Random.Range(0, 1);
                    break;
                case "Forest":
                    _logicalResources["food"] = Random.Range(0, 1); _logicalResources["wood"] = Random.Range(2, 4);
                    break;
                case "Mountain":
                    _logicalResources["stone"] = Random.Range(1, 3);
                    break;
                case "Snowy Peak": // No resources by default.
                    break;
                default:
                    if (DebugManager.DebugModeEnabled) Debug.LogWarning($"TriangleHex {IndexCoordinates}: GenerateResources called with unhandled terrain type: '{_logicalTerrainTypeString}'");
                    break;
            }
        }
    }
}