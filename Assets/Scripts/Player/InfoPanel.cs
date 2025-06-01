using TMPro;
using UnityEngine;
using DishevelledBadger.FlashFrostVale.Globals;
using DishevelledBadger.FlashFrostVale.SharedData;
using DishevelledBadger.FlashFrostVale.Networking;

namespace DishevelledBadger.FlashFrostVale.Player
{
    // Manages a UI panel that displays information about a selected hex tile.
    public class InfoPanel : MonoBehaviour
    {
        [Header("UI References - Assign in Inspector")] // Group UI elements in Inspector
        [SerializeField] private ResourcePanel foodPanel;  // Panel to display food resource info.
        [SerializeField] private ResourcePanel woodPanel;  // Panel for wood.
        [SerializeField] private ResourcePanel mudPanel;   // Panel for mud.
        [SerializeField] private ResourcePanel stonePanel; // Panel for stone.
        [SerializeField] private TextMeshProUGUI terrainTypeText; // Text to show terrain type.
        [SerializeField] private BuildButton buildButton; // Button to open build options.
        [SerializeField] private BuildingInfo buildingSpecificInfoPanel; // Panel for detailed building info.
        [SerializeField] private RemoveBuildingButton removeButton; // Button to remove a building.

        void Awake()
        {
            Hide(); // Start hidden.
            // Null checks for essential UI components during development.
            if (terrainTypeText == null && DebugManager.DebugModeEnabled) Debug.LogError("InfoPanel: terrainTypeText not assigned!");
            if (buildingSpecificInfoPanel == null && DebugManager.DebugModeEnabled) Debug.LogError("InfoPanel: buildingSpecificInfoPanel not assigned!");
        }

        /// <summary>Displays info for the given hex tile data.</summary>
        public void ShowHexData(HexTileData tileData)
        {
            if (DebugManager.DebugModeEnabled) Debug.Log($"InfoPanel: Showing data for hex {tileData.coordinates}");

            // Reset visibility of conditional UI elements.
            if (buildButton != null) buildButton.gameObject.SetActive(false);
            if (buildingSpecificInfoPanel != null) buildingSpecificInfoPanel.gameObject.SetActive(false);
            if (removeButton != null) removeButton.gameObject.SetActive(false);

            // Display terrain type.
            if (terrainTypeText != null && GlobalHelper.terrainTypes != null && tileData.typeId >= 0 && tileData.typeId < GlobalHelper.terrainTypes.Length)
            {
                terrainTypeText.text = GlobalHelper.terrainTypes[tileData.typeId];
            }
            else
            {
                if (DebugManager.DebugModeEnabled) Debug.Log($"InfoPanel ShowHexData : terrainTypeText might be null (terrainTypeText:{terrainTypeText}), " +
                                                             $"GlobalHelper.terrainTypes might be null (GlobalHelper.terrainTypes:{GlobalHelper.terrainTypes}, " +
                                                             $"tileData.typeId might be out of bounds (tileData.typeId:{tileData.typeId}))");
            }

            // Handle building information.
            if (tileData.occupantBuildingTypeId != -1) // If a building exists on the tile.
            {
                if (buildingSpecificInfoPanel != null && MapManager.Instance != null && MapManager.Instance.clientBuildingDatabase != null)
                {
                    // Get building type data from the client-side database.
                    BuildingTypeData buildingType = MapManager.Instance.clientBuildingDatabase.GetBuildingDataById(tileData.occupantBuildingTypeId);
                    if (buildingType != null)
                    {
                        buildingSpecificInfoPanel.DisplayBuildingTypeInfo(buildingType); // Show building details.
                        buildingSpecificInfoPanel.gameObject.SetActive(true);
                    }
                    else
                    {
                        if (DebugManager.DebugModeEnabled) Debug.Log($"InfoPanle ShowHexData : can't find buildingType it is null");
                    }
                }
                if (removeButton != null) removeButton.gameObject.SetActive(true); // Show remove button.
            }
            else // Tile is empty.
            {
                if (buildButton != null) buildButton.gameObject.SetActive(true); // Show build button.
            }

            transform.gameObject.SetActive(true); // Make the main info panel visible.
        }

        /// <summary>Hides the entire info panel.</summary>
        public void Hide()
        {
            transform.gameObject.SetActive(false);
            if (DebugManager.DebugModeEnabled && gameObject.activeSelf) Debug.Log("InfoPanel: Hide called but still active (check logic).");
        }
    }
}