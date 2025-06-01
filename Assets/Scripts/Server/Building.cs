using UnityEngine; // Unity Engine functionalities
using DishevelledBadger.FlashFrostVale.Globals;

namespace DishevelledBadger.FlashFrostVale.Server // Server-side building representation
{
    // Defines properties and basic behavior for a game building.
    public class Building : MonoBehaviour
    {
        [Header("Name")]
        public string buildingName; // Building's display name.

        [Header("Production")] // Per-turn resource generation
        public float food;
        public float wood;
        public float mud;
        public float stone;

        [Header("Cost")] // Resources needed to build
        public float foodCost;
        public float woodCost;
        public float mudCost;
        public float stoneCost;

        [Header("Description")]
        [TextArea(3, 5)] // Inspector UI: multi-line text field
        public string desc; // In-game description.

        /// <summary>Sets the color of a specific part of the building, likely for player ownership.</summary>
        /// <param name="colorMaterial">Material to apply to the "PlayerColor" child's renderer.</param>
        public void SetColor(Material colorMaterial)
        {
            // Find child "PlayerColor" and set its first material.
            Transform playerColorTransform = transform.Find("PlayerColor");
            if (playerColorTransform != null)
            {
                MeshRenderer renderer = playerColorTransform.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    if (renderer.materials.Length > 0)
                    {
                        Material[] mats = renderer.materials; // Modifying materials array requires getting then setting
                        mats[0] = colorMaterial;
                        renderer.materials = mats;
                    }
                    else renderer.material = colorMaterial; // Fallback if no materials array (less common)
                }
                if (DebugManager.DebugModeEnabled) Debug.Log($"Building SetColor : Missing MeshRenderer on PlayerColor child");
            }
            if (DebugManager.DebugModeEnabled) Debug.Log($"Building SetColor : PlayerColor child not found");
        }
    }
}