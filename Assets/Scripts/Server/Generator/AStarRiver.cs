using System.Collections.Generic;
using UnityEngine;

namespace DishevelledBadger.FlashFrostVale.Server.Generator
{
    public class AStarRiver : AbstractGenerator
    {
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

            List<TriangleHex> path = GreedyFindPath(
                hexes.Find(h => h.IndexCoordinates == startPos),
                hexes.Find(h => h.IndexCoordinates == endPos)
            );

            /*
            List<TriangleHex> path = RecursiveFindPath(
                endPos,
                new List<TriangleHex> { hexes.Find(h => h.IndexCoordinates == startPos) }
            );
            */
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
}
