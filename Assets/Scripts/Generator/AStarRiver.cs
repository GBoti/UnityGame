using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AStarRiver : AbstractGenerator
{
    public AStarRiver(float hw, float pw)
    {
        heightWeight = hw;
        pathWeight = pw;
    }

    override public void Generate(List<TriangleHex> hexes, Vector2Int gridSize)
    {
        Vector2Int startPos;
        Vector2Int endPos;
        if (UnityEngine.Random.Range(0, 100) % 2 == 0)
        {
            int riverPos = UnityEngine.Random.Range(0, gridSize.y - 1);
            startPos = new Vector2Int(0, riverPos);
            endPos = new Vector2Int(gridSize.x - 1, gridSize.y - 1 - riverPos);
        }
        else
        {
            int riverPos = UnityEngine.Random.Range(0, gridSize.x);
            startPos = new Vector2Int(riverPos, 0);
            endPos = new Vector2Int(gridSize.x - 1 - riverPos, gridSize.y - 1);
        }

        List<TriangleHex> path = RecursiveFindPath(
            endPos,
            new List<TriangleHex> { hexes.Find(h => h.IndexCoordinates == startPos) }
        );

        foreach (TriangleHex h in path)
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
