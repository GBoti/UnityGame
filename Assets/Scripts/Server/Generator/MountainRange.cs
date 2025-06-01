using System.Collections.Generic;
using UnityEngine;

namespace DishevelledBadger.FlashFrostVale.Server.Generator
{
    // Generates a mountain range feature by identifying a path between existing high points and elevating it further.
    public class MountainRange : AbstractGenerator
    {
        // Overrides the base generator method to implement mountain range creation.
        override public void Generate(List<TriangleHex> hexes, Vector2Int gridSize)
        {
            TriangleHex startPos;
            TriangleHex endPos;

            heightWeight = -heightWeight;
            pathWeight = 2 * pathWeight;

            // 1. Identify all hexes already at or above a certain height.
            List<TriangleHex> existingHighGround = new List<TriangleHex>();
            foreach (TriangleHex h in hexes)
            {
                if (h.Height >= 7) // Threshold for existing "mountainous" terrain.
                {
                    existingHighGround.Add(h);
                }
            }

            if (existingHighGround.Count < 2) return; // Need at least two points to form a range.

            // 2. Select start and end points from the existing high ground.
            int index = UnityEngine.Random.Range(0, existingHighGround.Count / 2);
            startPos = existingHighGround[index];
            // Ensure endPos index is valid, picking from the latter part of the list.
            int endIndex = Mathf.Clamp(existingHighGround.Count - 1 - index, index + 1, existingHighGround.Count - 1);
            endPos = existingHighGround[endIndex];


            // 3. Find a path between these points to form the main ridge.
            // With GreedyFindPath the generation only strives for local optimum
            List<TriangleHex> ridge = GreedyFindPath(startPos, endPos);

            if (ridge == null) return; // Pathfinding failed or returned no path.

            // 4. Elevate the ridge hexes and their surrounding neighbors to form the mountain range.
            foreach (TriangleHex r in ridge)
            {
                // Elevate the main ridge line, preserving special heights (e.g., rivers at 0.1f).
                if (r.Height != 0.1f)
                {
                    r.Height = 10; // Peak height for the ridge.
                }

                // Elevate the first ring of neighbors.
                foreach (TriangleHex n in r.Neighbours.Values)
                {
                    if (n.Height < 9.0f && n.Height != 0.1f) // Don't lower existing higher terrain or overwrite rivers.
                    {
                        n.Height = 8; // Slope down from the peak.
                    }

                    // Elevate the second ring of neighbors.
                    foreach (TriangleHex nn in n.Neighbours.Values)
                    {
                        if (nn.Height < 8.0f && nn.Height != 0.1f)
                        {
                            nn.Height = 7; // Further slope.
                        }

                        // Elevate the third ring of neighbors.
                        foreach (TriangleHex nnn in nn.Neighbours.Values)
                        {
                            if (nnn.Height < 7.0f && nnn.Height != 0.1f)
                            {
                                nnn.Height = 5; // Gentle outer slope.
                            }
                        }
                    }
                }
            }
        }
    }
}