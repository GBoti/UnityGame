using TMPro;
using UnityEngine;
using DishevelledBadger.FlashFrostVale.Server;
using DishevelledBadger.FlashFrostVale.Globals;

namespace DishevelledBadger.FlashFrostVale.Player // UI elements related to player view
{
    // Manages a UI panel that displays detailed information about a specific building type or instance.
    public class BuildingInfoPanel : MonoBehaviour
    {
        [Header("UI Components (Assign in Inspector)")] // UI elements to populate with building data.
        public TextMeshProUGUI nameText;      // Displays building name.
        public ResourcePanel foodPanel;     // Displays food production.
        public ResourcePanel woodPanel;     // Displays wood production.
        public ResourcePanel mudPanel;      // Displays mud production.
        public ResourcePanel stonePanel;    // Displays stone production.
        public ResourcePanel foodCostPanel; // Displays food cost.
        public ResourcePanel woodCostPanel; // Displays wood cost.
        public ResourcePanel mudCostPanel;  // Displays mud cost.
        public ResourcePanel stoneCostPanel;// Displays stone cost.
        public TextMeshProUGUI descText;      // Displays building description.

        private void Awake()
        {
            // Null checks for all critical UI components to ensure they are assigned in the Inspector.
            if (nameText == null && DebugManager.DebugModeEnabled) Debug.LogError("BuildingInfoPanel: nameText not assigned!");
            if (foodPanel == null && DebugManager.DebugModeEnabled) Debug.LogError("BuildingInfoPanel: foodPanel not assigned!");
            if (woodPanel == null && DebugManager.DebugModeEnabled) Debug.LogError("BuildingInfoPanel: woodPanel not assigned!");
            if (mudPanel == null && DebugManager.DebugModeEnabled) Debug.LogError("BuildingInfoPanel: mudPanel not assigned!");
            if (stonePanel == null && DebugManager.DebugModeEnabled) Debug.LogError("BuildingInfoPanel: stonePanel not assigned!");
            if (foodCostPanel == null && DebugManager.DebugModeEnabled) Debug.LogError("BuildingInfoPanel: foodCostPanel not assigned!");
            if (woodCostPanel == null && DebugManager.DebugModeEnabled) Debug.LogError("BuildingInfoPanel: woodCostPanel not assigned!");
            if (mudCostPanel == null && DebugManager.DebugModeEnabled) Debug.LogError("BuildingInfoPanel: mudCostPanel not assigned!");
            if (stoneCostPanel == null && DebugManager.DebugModeEnabled) Debug.LogError("BuildingInfoPanel: stoneCostPanel not assigned!");
            if (descText == null && DebugManager.DebugModeEnabled) Debug.LogError("BuildingInfoPanel: descText not assigned!");
        }

        /// <summary>Displays information from a BuildingTypeData ScriptableObject.</summary>
        /// <param name="buildingData">The static data definition of the building type.</param>
        public void DisplayBuildingTypeInfo(BuildingTypeData buildingData)
        {
            if (buildingData == null) // Guard against null data.
            {
                if (DebugManager.DebugModeEnabled) Debug.LogError("BuildingInfoPanel: DisplayBuildingTypeInfo called with null data.");
                gameObject.SetActive(false); // Hide panel if no data.
                return;
            }

            // Populate UI elements with data from BuildingTypeData.
            if (nameText != null) nameText.text = buildingData.buildingName;

            // Production.
            if (foodPanel != null) foodPanel.SetValue(buildingData.foodProduction, null);
            if (woodPanel != null) woodPanel.SetValue(buildingData.woodProduction, null);
            if (mudPanel != null) mudPanel.SetValue(buildingData.mudProduction, null);
            if (stonePanel != null) stonePanel.SetValue(buildingData.stoneProduction, null);

            // Costs.
            if (foodCostPanel != null) foodCostPanel.SetValue(buildingData.foodCost, null);
            if (woodCostPanel != null) woodCostPanel.SetValue(buildingData.woodCost, null);
            if (mudCostPanel != null) mudCostPanel.SetValue(buildingData.mudCost, null);
            if (stoneCostPanel != null) stoneCostPanel.SetValue(buildingData.stoneCost, null);

            if (descText != null) descText.text = buildingData.description;

            gameObject.SetActive(true); // Show the populated panel.
        }

        /// <summary>Displays information from a live Building MonoBehaviour instance.</summary>
        /// <param name="liveBuildingInstance">The active Building component instance.</param>
        public void DisplayLiveBuildingInfo(Building liveBuildingInstance)
        {
            if (liveBuildingInstance == null) // Guard against null instance.
            {
                if (DebugManager.DebugModeEnabled) Debug.LogError("BuildingInfoPanel: DisplayLiveBuildingInfo called with null instance.");
                gameObject.SetActive(false); // Hide panel if no instance.
                return;
            }

            // Populate UI elements with data from the live Building instance.
            if (nameText != null) nameText.text = liveBuildingInstance.buildingName;

            // Production from the live instance.
            if (foodPanel != null) foodPanel.SetValue(liveBuildingInstance.food, null);
            if (woodPanel != null) woodPanel.SetValue(liveBuildingInstance.wood, null);
            if (mudPanel != null) mudPanel.SetValue(liveBuildingInstance.mud, null);
            if (stonePanel != null) stonePanel.SetValue(liveBuildingInstance.stone, null);

            // Costs from the live instance.
            if (foodCostPanel != null) foodCostPanel.SetValue(liveBuildingInstance.foodCost, null);
            if (woodCostPanel != null) woodCostPanel.SetValue(liveBuildingInstance.woodCost, null);
            if (mudCostPanel != null) mudCostPanel.SetValue(liveBuildingInstance.mudCost, null);
            if (stoneCostPanel != null) stoneCostPanel.SetValue(liveBuildingInstance.stoneCost, null);

            if (descText != null) descText.text = liveBuildingInstance.desc;

            gameObject.SetActive(true); // Show the populated panel.
        }

        /// <summary>Hides the building info panel.</summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
