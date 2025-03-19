using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Lake : AbstractGenerator
{
    public Lake(float hw, float pw)
    {
        heightWeight = hw;
        pathWeight = pw;
    }

    override public void Generate(List<TriangleHex> hexes, Vector2Int gridSize)
    {
        int lakeBaseX = UnityEngine.Random.Range(0, gridSize.x - 1);
        int lakeBaseY = UnityEngine.Random.Range(0, gridSize.y - 1);

        TriangleHex lakeBase = hexes.Find(
            h => h.IndexCoordinates == new Vector2Int(lakeBaseX, lakeBaseY)
        );

        List<TriangleHex> lake = new List<TriangleHex>();

        foreach (TriangleHex h in hexes)
        {
            float dist = GetDistanceBetweenHexes(lakeBase, h);
            float radius = UnityEngine.Random.Range(3, 5);
            if (dist <= radius)
            {
                if (!lake.Contains(h))
                {
                    lake.Add(h);
                }
            }
        }

        foreach (TriangleHex h in lake)
        {
            h.Height = 0.1f;
            foreach (TriangleHex n in h.Neighbours.Values)
            {
                if (n.Height != 0.1f && n.Height < 7)
                {
                    n.Height = 0.5f;
                }
                if (n.Height >= 9)
                {
                    n.Height = 8;
                }
            }
        }
    }
}
