using System.Collections.Generic;
using UnityEngine;

namespace DishevelledBadger.FlashFrostVale.Server.Generator
{
    // Generates a river feature on the map by pathfinding and modifying hex heights.
    public class River : AbstractGenerator
    {
        // Overrides the base generator method to implement river creation.
        override public void Generate(List<TriangleHex> hexes, Vector2Int gridSize)
        {
            Vector2Int startPosCoord; // Using 'Coord' suffix to distinguish from TriangleHex instance.
            Vector2Int endPosCoord;

            // 1. Determine random start and end points for the river, typically on opposite edges.
            if (UnityEngine.Random.Range(0, 100) % 2 == 0) // 50/50 chance for horizontal or vertical orientation.
            {
                // Horizontal river (starts on left/right edge, ends on opposite).
                int riverY = UnityEngine.Random.Range(0, gridSize.y); // Random Y position on an edge.
                startPosCoord = new Vector2Int(0, riverY);            // Start on left edge.
                endPosCoord = new Vector2Int(gridSize.x - 1, gridSize.y - 1 - riverY); // End on right edge (mirrored Y).
            }
            else
            {
                // Vertical river (starts on top/bottom edge, ends on opposite).
                int riverX = UnityEngine.Random.Range(0, gridSize.x); // Random X position on an edge.
                startPosCoord = new Vector2Int(riverX, 0);            // Start on top edge.
                endPosCoord = new Vector2Int(gridSize.x - 1 - riverX, gridSize.y - 1); // End on bottom edge (mirrored X).
            }

            // Find the actual TriangleHex instances for the start and end coordinates.
            TriangleHex startHex = hexes.Find(h => h.IndexCoordinates == startPosCoord);
            TriangleHex endHex = hexes.Find(h => h.IndexCoordinates == endPosCoord);

            if (startHex == null || endHex == null) return; // Exit if start or end hex not found.

            // 2. Find a path between the start and end points for the river.
            // Striving for local optimum
            List<TriangleHex> path = GreedyFindPath(startHex, endHex);

            if (path == null || path.Count == 0) return; // Exit if no path found.

            // 3. Modify hex heights along the path to create the riverbed and banks.
            foreach (TriangleHex h in path)
            {
                h.Height = 0.1f; // Set river hexes to a specific water/river level.

                // Adjust shoreline neighbors.
                foreach (TriangleHex n in h.Neighbours.Values)
                {
                    // If neighbor is not already river and is relatively low, make it a gentle bank/shore.
                    if (n.Height != 0.1f && n.Height < 7)
                    {
                        n.Height = 0.5f; // Shoreline height.
                    }
                    // If neighbor is very high (mountain), slightly reduce its height near the river.
                    if (n.Height >= 9)
                    {
                        n.Height = 8; // Creates a slightly lower bank against mountains.
                    }
                }
            }
        }
    }
}