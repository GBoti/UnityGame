using System.Collections.Generic;
using UnityEngine;

namespace DishevelledBadger.FlashFrostVale.Server.Generator
{
    // Main class responsible for procedurally generating the game map.
    public class MapGenerator : MonoBehaviour
    {
        [Header("Perlin noise offset scale")]
        [SerializeField]
        public float scaler = 0.2f; // Scales the distance between two sampling points on the Perlin noise

        [Header("Map parameters")]
        [SerializeField]
        public Vector2Int gridSize; // Dimensions (width, height) of the hex grid.
        public List<TriangleHex> hexes; // List of all logical hex tiles on the map.

        [Header("Generator parameters")]
        [SerializeField]
        NewDict featuresDict; // Serializable dictionary-like structure to configure map features (e.g., rivers, mountains).
                              // Custom dictionary to help with editor access for testing

        Dictionary<AbstractGenerator, int> features; // Runtime dictionary of feature generators and their counts.

        // Main method to generate the entire map procedurally.
        public void Procedural_Map_Generate()
        {
            // 1. Initialize base terrain heights using Perlin noise.
            float perlinNoiseOffsetX = Random.Range(0, 1000); // Random offset for unique map each time.
            float perlinNoiseOffsetY = Random.Range(0, 1000);
            foreach (TriangleHex h in hexes)
            {
                Vector2 pos = h.IndexCoordinates;
                // Apply Perlin noise, scaled, to set initial height.
                h.Height = Mathf.PerlinNoise(
                                (pos.x + perlinNoiseOffsetX) * scaler,
                                (pos.y + perlinNoiseOffsetY) * scaler
                            ) * 10f; // Multiplier to scale noise output to desired height range.
                if (h.Height < 0) h.Height = 0f;
                if (h.Height > 10) h.Height = 10f;
            }

            // 2. Apply specific map features (mountains, rivers, etc.) over the base terrain.
            if (featuresDict != null) features = featuresDict.ToDictionary(); // Convert Inspector config to runtime dictionary.
            if (features != null)
            {
                foreach (AbstractGenerator f in features.Keys) // Iterate through each feature type (e.g., Tarn, MountainRange).
                {
                    for (int i = 0; i < features[f]; i++) // Run each feature generator the specified number of times.
                    {
                        f.Generate(hexes, gridSize); // Call the feature's generation logic.
                    }
                }
            }

            // 3. Finalize hex properties: set terrain type, generate resources, apply final height visuals.
            foreach (TriangleHex h in hexes)
            {
                // Assign terrain type based on final height.
                switch (h.Height)
                {
                    case -1.0f: // Tarns
                        h.Terrain = "Water";
                        h.Height = 7.0f; // Adjust height for visuals after terrain assignment.
                        break;
                    case 0.1f: // Rivers
                        h.Terrain = "Water";
                        break;
                    case 0.5f: // Beaches
                        h.Terrain = "Sand";
                        break;
                    case < 4: // Lowlands
                        h.Terrain = "Meadow";
                        break;
                    case < 7: // Forest
                        h.Terrain = "Forest";
                        break;
                    case < 9: // Mountains
                        h.Terrain = "Mountain";
                        break;
                    default: // Highest elevations
                        h.Terrain = "Snowy Peak";
                        break;
                }
                h.GenerateResources(); // Populate resources based on the final terrain type.
                h.SetHeight(h.Height); // Apply final height.
            }
        }
    }
}