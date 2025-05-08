using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DishevelledBadger.FlashFrostVale.Server.Generator
{
    public abstract class AbstractGenerator : MonoBehaviour, IGeneratorInterface
    {
        public float heightWeight;
        public float pathWeight;
        public float gridSize;

        public abstract void Generate(List<TriangleHex> hexes, Vector2Int gridSize);

        public struct PathfindingNeighbour
        {
            public TriangleHex neighbouringHex;
            public float cost;

            public PathfindingNeighbour(float f, TriangleHex h)
            {
                neighbouringHex = h;
                cost = f;
            }
        }

        public List<TriangleHex> RecursiveFindPath(Vector2Int end, List<TriangleHex> path)
        {
            if (path.Count > gridSize * 4)
            {
                return path;
            }

            List<PathfindingNeighbour> neighbours = new List<PathfindingNeighbour>();

            foreach (TriangleHex n in path[path.Count - 1].Neighbours.Values)
            {
                if (n.IndexCoordinates == end)
                {
                    path.Add(n);
                    return path;
                }
                if (path.Contains(n))
                {
                    continue;
                }
                float currentValue =
                    ((n.Height - path[path.Count - 1].Height) * heightWeight)
                    + (
                        MathF.Sqrt(
                            MathF.Pow((end - n.IndexCoordinates).x, 2)
                                + MathF.Pow((end - n.IndexCoordinates).y, 2)
                        ) * pathWeight
                    );
                neighbours.Add(new PathfindingNeighbour(currentValue, n));
            }

            neighbours = neighbours.OrderBy(n => n.cost).ToList();

            foreach (PathfindingNeighbour p in neighbours)
            {
                path.Add(p.neighbouringHex);
                List<TriangleHex> currentPath = RecursiveFindPath(end, path);
                if (currentPath != null)
                {
                    return currentPath;
                }
                path.RemoveAt(path.Count - 1);
            }
            return null;
        }

        public List<TriangleHex> GreedyFindPath(TriangleHex start, TriangleHex end)
        {
            List<TriangleHex> path = new List<TriangleHex> { start };
            TriangleHex current = start;

            while (current != end)
            {
                TriangleHex best = start;
                float bestDist = 0;

                foreach (TriangleHex n in current.Neighbours.Values)
                {
                    if (path.Contains(n))
                    {
                        continue;
                    }
                    if (bestDist == 0)
                    {
                        best = n;
                        bestDist =
                            (n.Height - current.Height) * heightWeight
                            + MathF.Sqrt(
                                MathF.Pow((end.IndexCoordinates - n.IndexCoordinates).x, 2)
                                    + MathF.Pow((end.IndexCoordinates - n.IndexCoordinates).y, 2)
                            ) * pathWeight;
                    }
                    else
                    {
                        float dist =
                            (n.Height - current.Height) * heightWeight
                            + MathF.Sqrt(
                                MathF.Pow((end.IndexCoordinates - n.IndexCoordinates).x, 2)
                                    + MathF.Pow((end.IndexCoordinates - n.IndexCoordinates).y, 2)
                            ) * pathWeight;
                        if (dist < bestDist)
                        {
                            best = n;
                            bestDist = dist;
                        }
                    }
                }
                current = best;
                path.Add(best);
            }
            return path;
        }

        public float GetDistanceBetweenHexes(TriangleHex start, TriangleHex end)
        {
            TriangleHex current = start;
            float hexDistance = 0;

            while (current != end)
            {
                TriangleHex closest = start;
                //TriangleHex closest = new TriangleHex();
                float leastDist = -1;
                foreach (TriangleHex n in current.Neighbours.Values)
                {
                    if (leastDist < 0)
                    {
                        closest = n;
                        leastDist = MathF.Sqrt(
                            MathF.Pow((end.IndexCoordinates - n.IndexCoordinates).x, 2)
                                + MathF.Pow((end.IndexCoordinates - n.IndexCoordinates).y, 2)
                        );
                    }
                    else
                    {
                        float dist = MathF.Sqrt(
                            MathF.Pow((end.IndexCoordinates - n.IndexCoordinates).x, 2)
                                + MathF.Pow((end.IndexCoordinates - n.IndexCoordinates).y, 2)
                        );
                        if (dist < leastDist)
                        {
                            closest = n;
                            leastDist = dist;
                        }
                    }
                }
                current = closest;
                hexDistance += 1f;
            }
            return hexDistance;
        }
    }
}
