using TMPro;
using UnityEngine;
using DishevelledBadger.FlashFrostVale.Server;

namespace DishevelledBadger.FlashFrostVale.Player
{
    public class BuildingInfoPanel : MonoBehaviour
    {
        [Header("Information")]
        public Building building;

        [Header("Components")]
        public TextMeshProUGUI nameText;
        public ResourcePanel foodPanel;
        public ResourcePanel woodPanel;
        public ResourcePanel mudPanel;
        public ResourcePanel stonePanel;
        public ResourcePanel foodCostPanel;
        public ResourcePanel woodCostPanel;
        public ResourcePanel mudCostPanel;
        public ResourcePanel stoneCostPanel;
        public TextMeshProUGUI descText;

        private void Start()
        {
            nameText.text = building.name;
            foodPanel.SetValue(building.food, null);
            woodPanel.SetValue(building.wood, null);
            mudPanel.SetValue(building.mud, null);
            stonePanel.SetValue(building.stone, null);
            foodCostPanel.SetValue(building.foodCost, null);
            woodCostPanel.SetValue(building.woodCost, null);
            mudCostPanel.SetValue(building.mudCost, null);
            stoneCostPanel.SetValue(building.stoneCost, null);
            descText.text = building.desc;
        }
    }
}