using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public interface IGeneratorInterface
{
    public void Generate(List<TriangleHex> hexes, Vector2Int gridSize);

    public float GetDistanceBetweenHexes(TriangleHex h1, TriangleHex h2);
}
