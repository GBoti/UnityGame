using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Tarn : AbstractGenerator
{
    override public void Generate(List<TriangleHex> hexes, Vector2Int gridSize)
    {
        int tarnBaseX = UnityEngine.Random.Range(0, gridSize.x - 1);
        int tarnBaseY = UnityEngine.Random.Range(0, gridSize.y - 1);

        TriangleHex tarnBase = hexes.Find(
            h => h.IndexCoordinates == new Vector2Int(tarnBaseX, tarnBaseY)
        );

        List<TriangleHex> tarn = new List<TriangleHex>();

        foreach (TriangleHex h in hexes)
        {
            float dist = GetDistanceBetweenHexes(tarnBase, h);
            float radius = 2;
            if (dist <= radius)
            {
                if (!tarn.Contains(h))
                {
                    tarn.Add(h);
                }
            }
        }

        foreach (TriangleHex t in tarn)
        {
            if (t.Height != 0.1f)
            {
                t.Height = -1.0f;
            }
            foreach (TriangleHex n in t.Neighbours.Values)
            {
                if (n.Height != -1.0f && n.Height != 0.1f)
                {
                    n.Height = 8.0f;
                    foreach (TriangleHex nn in n.Neighbours.Values)
                    {
                        if (nn.Height < 7 && nn.Height != -1.0f && nn.Height != 0.1f)
                        {
                            nn.Height = 7.0f;
                        }
                        foreach (TriangleHex nnn in nn.Neighbours.Values)
                        {
                            if (nnn.Height < 5 && nnn.Height != -1.0f && nnn.Height != 0.1f)
                            {
                                nnn.Height = 5;
                            }
                        }
                    }
                }
            }
        }
    }
}
