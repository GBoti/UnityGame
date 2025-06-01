using UnityEngine;

[CreateAssetMenu(fileName = "NewBuildingType", menuName = "FlashFrostVale/Building Type Data")]
public class BuildingTypeData : ScriptableObject
{
    public int buildingId; // Unique ID for this building type (e.g., MainBase = 0, Gatherer = 1)
    public string buildingName = "Unnamed Building";

    [Header("Resource Costs")]
    public float foodCost;
    public float woodCost;
    public float mudCost;
    public float stoneCost;

    [Header("Resource Production (Per Tick/Interval - can be negative for upkeep)")]
    public float foodProduction;
    public float woodProduction;
    public float mudProduction;
    public float stoneProduction;

    [Header("Visuals & Logic Prefab")]
    // This is the prefab that contains the Building.cs script AND the 3D model.
    public GameObject buildingVisualAndLogicPrefab;

    [TextArea(3, 5)]
    public string description;
}
