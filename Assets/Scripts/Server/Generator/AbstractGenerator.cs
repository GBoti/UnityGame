using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DishevelledBadger.FlashFrostVale.Server.Generator
{
    // Abstract base class for all map feature generators, providing common utilities like pathfinding.
    public abstract class AbstractGenerator : MonoBehaviour, IGeneratorInterface
    {
        // Pathfinding cost parameters: influence of height difference and direct distance.
        public float heightWeight; // How much height difference between hexes affects path cost.
        public float pathWeight;   // How much distance to the target affects path cost.
        public float gridSize;     // Max grid dimension, used as a loose upper bound for path length in recursion.

        // Abstract method to be implemented by concrete generator subclasses (e.g., River, MountainRange).
        public abstract void Generate(List<TriangleHex> hexes, Vector2Int gridSize);

        // Helper struct to store a neighboring hex and the cost to move to it during pathfinding.
        public struct PathfindingNeighbour
        {
            public TriangleHex neighbouringHex; // The actual hex tile.
            public float cost;                  // Calculated cost to reach this hex.

            // Constructor for PathfindingNeighbour.
            public PathfindingNeighbour(float c, TriangleHex h) // 'c' for cost, 'h' for hex.
            {
                neighbouringHex = h;
                cost = c;
            }
        }

        // Recursive pathfinding algorithm (depth-first search with cost-based ordering).
        // Tries to find a path from the last hex in 'path' to 'end' coordinates.
        public List<TriangleHex> RecursiveFindPath(Vector2Int end, List<TriangleHex> path)
        {
            // Safety break: prevent excessively long paths/infinite recursion.
            if (path.Count > gridSize * 4) // Path length limit.
            {
                return null; // Path too long, abandon.
            }

            List<PathfindingNeighbour> neighbours = new List<PathfindingNeighbour>();
            TriangleHex currentLastHex = path[path.Count - 1]; // Current end of the path.

            // Evaluate neighbors of the current last hex in the path.
            foreach (TriangleHex n in currentLastHex.Neighbours.Values)
            {
                if (n.IndexCoordinates == end) // Target found.
                {
                    path.Add(n);
                    return path; // Return completed path.
                }
                if (path.Contains(n)) continue; // Skip if already in path (avoid cycles).

                // Calculate cost: considers height difference and distance to target.
                float costToNeighbour =
                    ((n.Height - currentLastHex.Height) * heightWeight) // Height difference cost.
                    + (MathF.Sqrt( // Euclidean distance cost to the final 'end' coordinate.
                           MathF.Pow((end - n.IndexCoordinates).x, 2)
                           + MathF.Pow((end - n.IndexCoordinates).y, 2)
                       ) * pathWeight);
                neighbours.Add(new PathfindingNeighbour(costToNeighbour, n));
            }

            neighbours = neighbours.OrderBy(n => n.cost).ToList(); // Sort neighbors by cost (cheapest first).

            // Recursively explore paths through sorted neighbors.
            foreach (PathfindingNeighbour p in neighbours)
            {
                path.Add(p.neighbouringHex); // Tentatively add neighbor to path.
                List<TriangleHex> foundPath = RecursiveFindPath(end, path); // Recurse.
                if (foundPath != null) return foundPath; // Path found, propagate it up.
                path.RemoveAt(path.Count - 1); // Backtrack: remove neighbor if path not found through it.
            }
            return null; // No path found from this point.
        }

        // Iterative greedy pathfinding algorithm.
        // Finds a path from 'start' hex to 'end' hex by always choosing the "best" local neighbor.
        public List<TriangleHex> GreedyFindPath(TriangleHex start, TriangleHex end)
        {
            if (start == null || end == null) return null; // Guard against null inputs.

            List<TriangleHex> path = new List<TriangleHex> { start };
            TriangleHex current = start;

            int safetyBreak = 0; // Prevent infinite loops if end is unreachable.
            int maxIterations = (int)(this.gridSize * this.gridSize * 4); // Generous limit.

            while (current != end && safetyBreak < maxIterations)
            {
                TriangleHex bestNextHex = start;
                float bestCost = float.MaxValue; // Initialize with a very high cost.

                foreach (TriangleHex n in current.Neighbours.Values)
                {
                    if (path.Contains(n)) continue; // Skip if already in path.

                    // Calculate cost similar to recursive method.
                    float costToNeighbour =
                        ((n.Height - current.Height) * heightWeight)
                        + (MathF.Sqrt(
                               MathF.Pow((end.IndexCoordinates - n.IndexCoordinates).x, 2)
                               + MathF.Pow((end.IndexCoordinates - n.IndexCoordinates).y, 2)
                           ) * pathWeight);

                    if (costToNeighbour < bestCost) // If this neighbor is better than previous best...
                    {
                        bestNextHex = n;
                        bestCost = costToNeighbour;
                    }
                }

                if (bestNextHex == null) return path; // No valid next step found (stuck).

                current = bestNextHex;
                path.Add(bestNextHex);
                safetyBreak++;
            }
            if (current != end) return null; // Did not reach the end within safety limit
            return path;
        }

        // Calculates hex distance (number of steps) between two hexes using a greedy approach.
        public float GetDistanceBetweenHexes(TriangleHex start, TriangleHex end)
        {
            if (start == null || end == null) return -1f; // Invalid input.

            TriangleHex current = start;
            float hexDistance = 0;
            int safetyBreak = 0; // Prevent infinite loops.
            int maxIterations = (int)(this.gridSize * this.gridSize * 2);

            while (current != end && safetyBreak < maxIterations)
            {
                TriangleHex closestNextHex = start;
                float leastCartesianDist = float.MaxValue;

                if (current.Neighbours == null || current.Neighbours.Count == 0) return hexDistance; // Stuck

                // Find neighbor closest to the 'end' hex in Cartesian distance.
                foreach (TriangleHex n in current.Neighbours.Values)
                {
                    float cartesianDistToTarget = MathF.Sqrt(
                                            MathF.Pow((end.IndexCoordinates - n.IndexCoordinates).x, 2)
                                            + MathF.Pow((end.IndexCoordinates - n.IndexCoordinates).y, 2)
                                        );
                    if (cartesianDistToTarget < leastCartesianDist)
                    {
                        closestNextHex = n;
                        leastCartesianDist = cartesianDistToTarget;
                    }
                }

                if (closestNextHex == null) return hexDistance; // No valid next step (should not happen if neighbors exist).

                current = closestNextHex;
                hexDistance += 1f; // Increment hex step count.
                safetyBreak++;
            }
            if (current != end) return -1f; // Did not reach end (e.g. stuck or max iterations)
            return hexDistance;
        }
    }
}