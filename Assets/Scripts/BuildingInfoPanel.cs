using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

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
        foodPanel.SetValue(building.food);
        woodPanel.SetValue(building.wood);
        mudPanel.SetValue(building.mud);
        stonePanel.SetValue(building.stone);
        foodCostPanel.SetValue(building.foodCost);
        woodCostPanel.SetValue(building.woodCost);
        mudCostPanel.SetValue(building.mudCost);
        stoneCostPanel.SetValue(building.stoneCost);
        descText.text = building.desc;
    }
}
