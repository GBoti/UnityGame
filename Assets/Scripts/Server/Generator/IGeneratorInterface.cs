using System.Collections.Generic;
using UnityEngine;

namespace DishevelledBadger.FlashFrostVale.Server.Generator
{
    // Defines the contract for map feature generator classes.
    public interface IGeneratorInterface
    {
        /// <summary>
        /// Method to be implemented by classes that generate specific map features.
        /// </summary>
        /// <param name="hexes">The list of all hex tiles on the map to be modified.</param>
        /// <param name="gridSize">The dimensions of the hex grid.</param>
        public void Generate(List<TriangleHex> hexes, Vector2Int gridSize);
    }
}