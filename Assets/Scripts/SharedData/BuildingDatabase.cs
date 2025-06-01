using UnityEngine;
using System.Collections.Generic;
using System.Linq;
// Allows creating this as an asset from Assets/Create menu (FlashFrostVale/Building Database)
[CreateAssetMenu(fileName = "BuildingDatabase", menuName = "FlashFrostVale/Building Database")]
// ScriptableObject to hold a database of building types.
public class BuildingDatabase : ScriptableObject
{
    // List of all building type data, assignable in the Inspector.
    public List<BuildingTypeData> allBuildingTypes;

    /// <summary>Finds BuildingTypeData by its ID.</summary>
    /// <param name="id">The building's unique ID.</param>
    /// <returns>BuildingTypeData or null if not found.</returns>
    public BuildingTypeData GetBuildingDataById(int id)
    {
        if (allBuildingTypes == null) return null; // Guard clause for uninitialized list
        // LINQ: Find first building 'b' where b is not null and its ID matches.
        return allBuildingTypes.FirstOrDefault(b => b != null && b.buildingId == id);
    }

    /// <summary>Finds BuildingTypeData by its name (case-insensitive).</summary>
    /// <param name="nameKey">The building's name key.</param>
    /// <returns>BuildingTypeData or null if not found.</returns>
    public BuildingTypeData GetBuildingDataByName(string nameKey)
    {
        if (allBuildingTypes == null) return null; // Guard clause
        // LINQ: Find first building 'b' where b is not null and its name matches (ignoring case).
        return allBuildingTypes.FirstOrDefault(b => b != null && b.buildingName.Equals(nameKey, System.StringComparison.OrdinalIgnoreCase));
    }
}