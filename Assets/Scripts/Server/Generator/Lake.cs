using System.Collections.Generic;
using UnityEngine;

namespace DishevelledBadger.FlashFrostVale.Server.Generator
{
    // Generates a lake feature on the map by modifying hex heights.
    public class Lake : AbstractGenerator
    {
        // Overrides the base generator method to implement lake creation.
        override public void Generate(List<TriangleHex> hexes, Vector2Int gridSize)
        {
            // 1. Select a random base hex for the lake's center.
            int lakeBaseX = UnityEngine.Random.Range(0, gridSize.x - 1);
            int lakeBaseY = UnityEngine.Random.Range(0, gridSize.y - 1);
            TriangleHex lakeBase = hexes.Find(h => h.IndexCoordinates == new Vector2Int(lakeBaseX, lakeBaseY));

            if (lakeBase == null) return; // Exit if base hex not found.

            // 2. Identify hexes within a random radius to form the lake area.
            List<TriangleHex> lakeHexes = new List<TriangleHex>();
            float radius = UnityEngine.Random.Range(3, 5); // Lake size varies.
            foreach (TriangleHex h in hexes)
            {
                float dist = GetDistanceBetweenHexes(lakeBase, h);
                if (dist <= radius)
                {
                    if (!lakeHexes.Contains(h)) // Avoid duplicates.
                    {
                        lakeHexes.Add(h);
                    }
                }
            }

            // 3. Modify heights: Set lake hexes to water level and adjust shorelines.
            foreach (TriangleHex h in lakeHexes)
            {
                h.Height = 0.1f; // Set lake hexes to water/river level.

                // Adjust shoreline neighbors.
                foreach (TriangleHex n in h.Neighbours.Values)
                {
                    // If neighbor is not water and is relatively low, make it a gentle shore (e.g., sand).
                    if (n.Height != 0.1f && n.Height < 7)
                    {
                        n.Height = 0.5f; // Shoreline height.
                    }
                    // If neighbor is very high (mountain), slightly reduce its height near the lake.
                    if (n.Height >= 9)
                    {
                        n.Height = 8; // Creates a slightly lower bank against mountains.
                    }
                }
            }
        }
    }
}