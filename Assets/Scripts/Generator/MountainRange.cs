using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MountainRange : AbstractGenerator
{
    override public void Generate(List<TriangleHex> hexes, Vector2Int gridSize)
    {
        TriangleHex startPos;
        TriangleHex endPos;

        List<TriangleHex> mountains = new List<TriangleHex>();

        foreach (TriangleHex h in hexes)
        {
            if (h.Height >= 7)
            {
                mountains.Add(h);
            }
        }

        int index = UnityEngine.Random.Range(0, mountains.Count / 2);

        startPos = mountains[index];
        endPos = mountains[mountains.Count - index];

        List<TriangleHex> ridge = RecursiveFindPath(
            endPos.IndexCoordinates,
            new List<TriangleHex> { startPos }
        );

        foreach (TriangleHex r in ridge)
        {
            if (r.Height != 0.1f)
            {
                r.Height = 10;
            }
            foreach (TriangleHex n in r.Neighbours.Values)
            {
                if (n.Height < 9.0f && n.Height != 0.1f)
                {
                    n.Height = 8;
                }
                foreach (TriangleHex nn in n.Neighbours.Values)
                {
                    if (nn.Height < 8.0f && nn.Height != 0.1f)
                    {
                        nn.Height = 7;
                    }
                    foreach (TriangleHex nnn in nn.Neighbours.Values)
                    {
                        if(nnn.Height < 7 && nnn.Height != 0.1f)
                        {
                            nnn.Height = 5;
                        }
                    }
                }
            }
        }
    }
}
