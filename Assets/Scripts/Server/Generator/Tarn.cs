using System.Collections.Generic;
using UnityEngine;

namespace DishevelledBadger.FlashFrostVale.Server.Generator
{
    // Generates a "tarn" (mountain lake) feature on the map by modifying hex heights.
    public class Tarn : AbstractGenerator
    {
        // Overrides the base generator method to implement tarn creation.
        override public void Generate(List<TriangleHex> hexes, Vector2Int gridSize)
        {
            // 1. Select a random base hex for the tarn's center.
            int tarnBaseX = UnityEngine.Random.Range(0, gridSize.x - 1);
            int tarnBaseY = UnityEngine.Random.Range(0, gridSize.y - 1);
            TriangleHex tarnBase = hexes.Find(h => h.IndexCoordinates == new Vector2Int(tarnBaseX, tarnBaseY));

            if (tarnBase == null) return; // Exit if base hex not found (should not happen in a full grid)

            // 2. Identify all hexes within a defined radius to form the tarn area.
            List<TriangleHex> tarnHexes = new List<TriangleHex>();
            float radius = 1; // Defines the size of the tarn depression.
            foreach (TriangleHex h in hexes)
            {
                float dist = GetDistanceBetweenHexes(tarnBase, h);
                if (dist <= radius)
                {
                        tarnHexes.Add(h);
                }
            }

            // 3. Modify heights: Create the depression and surrounding ridges.
            foreach (TriangleHex t in tarnHexes)
            {
                // Set tarn hexes to water level, unless they are a special height (e.g., river at 0.1f).
                if (t.Height != 0.1f) // Preserve specific pre-existing features like rivers.
                {
                    t.Height = -1.0f; // Tarn water level.
                }

                // Create a raised rim around the tarn.
                foreach (TriangleHex n in t.Neighbours.Values) // First ring of neighbours.
                {
                    // If neighbour is not already water/river, raise it.
                    if (n.Height != -1.0f && n.Height != 0.1f)
                    {
                        n.Height = 8.0f; // Mountainous rim.

                        foreach (TriangleHex nn in n.Neighbours.Values) // Second ring of neighbours.
                        {
                            // If further neighbour is not water/river and below a certain threshold, raise it slightly less.
                            if (nn.Height < 7 && nn.Height != -1.0f && nn.Height != 0.1f)
                            {
                                nn.Height = 7.0f; // Outer slope.

                                foreach (TriangleHex nnn in nn.Neighbours.Values) // Third ring.
                                {
                                    // Further slope down for the third ring.
                                    if (nnn.Height < 5 && nnn.Height != -1.0f && nnn.Height != 0.1f)
                                    {
                                        nnn.Height = 5.0f; // Gentle outer slope.
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}