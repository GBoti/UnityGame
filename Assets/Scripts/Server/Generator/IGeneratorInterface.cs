using System.Collections.Generic;
using UnityEngine;

namespace DishevelledBadger.FlashFrostVale.Server.Generator
{
    public interface IGeneratorInterface
    {
        public void Generate(List<TriangleHex> hexes, Vector2Int gridSize);

        public float GetDistanceBetweenHexes(TriangleHex h1, TriangleHex h2);
    }
}
