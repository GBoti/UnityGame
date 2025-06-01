using TMPro;
using UnityEngine;
using DishevelledBadger.FlashFrostVale.Globals;

namespace DishevelledBadger.FlashFrostVale.Player
{
    // Manages a UI panel to display basic information about a building type (name, production, description).
    public class BuildingInfo : MonoBehaviour
    {
        [Header("Components")] // UI elements to populate with building data, assign in Inspector.
        public TextMeshProUGUI nameText;  // Displays building name.
        public ResourcePanel foodPanel; // Displays food production.
        public ResourcePanel woodPanel; // Displays wood production.
        public ResourcePanel mudPanel;  // Displays mud production.
        public ResourcePanel stonePanel;// Displays stone production.
        public TextMeshProUGUI descText;  // Displays building description.

        /// <summary>
        /// Populates the UI elements with data from the provided BuildingTypeData.
        /// </summary>
        /// <param name="buildingData">The static data definition of the building type.</param>
        public void DisplayBuildingTypeInfo(BuildingTypeData buildingData)
        {
            if (buildingData == null) // Guard against null input data.
            {
                // Log error if debug mode is enabled.
                if (DebugManager.DebugModeEnabled) Debug.LogError("BuildingInfo: DisplayBuildingTypeInfo called with null buildingData.");
                gameObject.SetActive(false); // Hide this panel if no data is provided.
                return;
            }

            // Set building name.
            if (nameText != null) nameText.text = buildingData.buildingName;

            // Set production values using ResourcePanel components.
            // Assumes ResourcePanel.SetValue updates its display; 'null' for production rate means show flat value.
            if (foodPanel != null) foodPanel.SetValue(buildingData.foodProduction, null);
            if (woodPanel != null) woodPanel.SetValue(buildingData.woodProduction, null);
            if (mudPanel != null) mudPanel.SetValue(buildingData.mudProduction, null);
            if (stonePanel != null) stonePanel.SetValue(buildingData.stoneProduction, null);

            // Set building description.
            if (descText != null) descText.text = buildingData.description;

            gameObject.SetActive(true); // Make the panel visible after populating.
        }
    }
}