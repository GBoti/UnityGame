using DishevelledBadger.FlashFrostVale.Server;
using System;
using UnityEngine;

namespace DishevelledBadger.FlashFrostVale.Globals
{
    // Static class providing global utility methods and data for the game.
    public static class GlobalHelper
    {
        /// <summary>
        /// Calculates the "distance" in hex steps between two hexes using a greedy algorithm.
        /// It iteratively moves to the neighbor closest to the 'end' hex based on Cartesian distance of their coordinates.
        /// </summary>
        /// <param name="start">The starting TriangleHex.</param>
        /// <param name="end">The target TriangleHex.</param>
        /// <returns>The number of hex steps taken to reach the end hex.</returns>
        public static float GetDistanceBetweenHexes(TriangleHex start, TriangleHex end)
        {
            if (start == null || end == null) return -1f; // Handle null inputs.
            if (start == end) return 0f; // If start and end are the same, distance is 0.

            TriangleHex current = start; // Initialize current position.
            float hexDistance = 0;       // Initialize step counter.
            int safetyBreak = 0;         // Safety counter to prevent potential infinite loops.
            int maxIterations = 1000;    // Arbitrary limit for safety.

            // Loop until current hex is the end hex or safety break is hit.
            while (current != end && safetyBreak < maxIterations)
            {
                TriangleHex closest = null; // Stores the best neighbor in current iteration.
                float leastDist = -1;       // Stores the smallest Cartesian distance found so far.

                if (current.Neighbours == null || current.Neighbours.Count == 0) return -1f; // Stuck, no neighbours

                // Iterate through all neighbors of the current hex.
                foreach (TriangleHex n in current.Neighbours.Values)
                {
                    if (n == null) continue; // Skip null neighbors.

                    // Calculate Cartesian distance from this neighbor 'n' to the 'end' hex.
                    float dist = MathF.Sqrt(
                        MathF.Pow((end.IndexCoordinates - n.IndexCoordinates).x, 2)
                        + MathF.Pow((end.IndexCoordinates - n.IndexCoordinates).y, 2)
                    );

                    // If this is the first neighbor checked or its distance is less than the current least.
                    if (leastDist < 0 || dist < leastDist)
                    {
                        leastDist = dist; // Update least distance.
                        closest = n;      // Mark this neighbor as the current closest.
                    }
                }

                if (closest == null) return -1f; // Should not happen if neighbours exist and are not null.

                current = closest;    // Move to the chosen closest neighbor.
                hexDistance += 1f;    // Increment hex step count.
                safetyBreak++;        // Increment safety counter.
            }
            if (current != end) return -1f; // Did not reach the end (e.g. safety break or got stuck).
            return hexDistance;       // Return total hex steps.
        }

        /// <summary>
        /// Performs component-wise division of two Vector3 instances (a / b).
        /// </summary>
        /// <param name="a">The dividend vector.</param>
        /// <param name="b">The divisor vector (components should ideally not be zero).</param>
        /// <returns>A new Vector3 where each component is a.component / b.component.</returns>
        public static Vector3 DivideVector3ByComponents(Vector3 a, Vector3 b)
        {
            if(b.x != 0 && b.y != 0 && b.z != 0)
                return new Vector3(a.x / b.x, a.y / b.y, a.z / b.z);
            return Vector3.negativeInfinity;
        }

        // Readonly array of terrain type names indexed by a terrain type ID.
        public static readonly string[] terrainTypes = {
            "Water",
            "Sand",
            "Meadow",
            "Forest",
            "Mountain",
            "Mountain Peak"
        };
    }
}