using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

//Hexre collider

public class HexGridLayout : MonoBehaviour
{
    [Header("Grid Settings")]
    public Vector2Int gridSize;

    [Header("Tile Settings")]
    public float size = 1f;

    [Header("Selected hex")]
    public Vector2Int selectedHex;

    [Header("Pathfinding weights")]
    public float heightWeight;
    public float pathWeight;

    public Material ground;
    public Material water;
    public Material backGround;
    public Material selected;
    public Material neighbour;

    public GameObject hex;

    private readonly float sqrt3 = Mathf.Sqrt(3);
    private List<TriangleHex> hexes = new List<TriangleHex>();
    private List<GameObject> backgroundHexes = new List<GameObject>();

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

    private void OnEnable()
    {
        LayoutGrid();
        Procedural_Map_Generate();
    }

    public void LayoutGrid()
    {
        DestroyGrid();
        Debug.Log($"Displaying grid {gridSize.x}, {gridSize.y}");

        for (int y = 0; y < gridSize.y; y++)
        {
            for (int x = 0; x < gridSize.x; x++)
            {
                TriangleHex tile = Instantiate(hex, GetPositionForHexFromCoordinate(new Vector2Int(x, y)), transform.rotation).GetComponent<TriangleHex>();
                tile.transform.SetParent(gameObject.transform);
                tile.transform.localScale = new Vector3(size * 20, size * 12, size * 20);
                tile.transform.localRotation *= Quaternion.Euler(0f, 0f, 0f);
                tile.Terrain = "ground";
                tile.InitiateHex(new Vector2Int(x, y), ground, selected, neighbour, backGround, ground, water);
                // Current hex will go in the current = y * gridSize.x + x; slot in hexes
                int current = y * gridSize.x + x;
                // Need to add the already existing hexes, but need to check if they exist
                // (In the case of hex 0,0 there will be no other existing hexes)
                // In case of a non-existent hex, set the value to null
                // Already existing hexes are: Left, Top-left, Top-right, since
                // the grid is built from the top-left to the right then down.
                // the left is easy allways current-1, except when x == 0, then it's null

                if (x != 0)
                {
                    foreach (TriangleHex h in hexes)
                    {
                        if (h.IndexCoordinates.x == x - 1 && h.IndexCoordinates.y == y)
                        {
                            tile.AddNeighbour(h, side.right);
                            h.AddNeighbour(tile, side.left);
                        }
                    }
                }
                if (!(y == 0 || (x == 0 && y % 2 != 0)))
                {
                    foreach (TriangleHex h in hexes)
                    {
                        if (y % 2 == 0)
                        {
                            if (h.IndexCoordinates.x == x && h.IndexCoordinates.y == y - 1)
                            {
                                tile.AddNeighbour(h, side.bottomright);
                                h.AddNeighbour(tile, side.topleft);
                            }
                        }
                        if (y % 2 != 0)
                        {
                            if (h.IndexCoordinates.x == x - 1 && h.IndexCoordinates.y == y - 1)
                            {
                                tile.AddNeighbour(h, side.bottomright);
                                h.AddNeighbour(tile, side.topleft);
                            }
                        }
                    }
                }
                if (!(y == 0 || (x == ((y + 1) * gridSize.x) - 1 && y % 2 == 0)))
                {
                    foreach (TriangleHex h in hexes)
                    {
                        if (y % 2 == 0)
                        {
                            if (h.IndexCoordinates.x == x + 1 && h.IndexCoordinates.y == y - 1)
                            {
                                tile.AddNeighbour(h, side.bottomleft);
                                h.AddNeighbour(tile, side.topright);
                            }
                        }
                        if (y % 2 != 0)
                        {
                            if (h.IndexCoordinates.x == x && h.IndexCoordinates.y == y - 1)
                            {
                                tile.AddNeighbour(h, side.bottomleft);
                                h.AddNeighbour(tile, side.topright);
                            }
                        }
                    }
                }

                // The other neighbours will be added as we build the grid.

                hexes.Add(tile);
            }
        }
    }

    //Procedural generation
    //PerlinNoise -> height
    //Pathfinder egyik oldalról másikra height alapján
    //Utána height és szomszéd alapján -> hex material

    public void Procedural_Map_Generate()
    {
        float perlinNoiseOffsetX = UnityEngine.Random.Range(0, 100);
        float perlinNoiseOffsetY = UnityEngine.Random.Range(0, 100);
        foreach (TriangleHex h in hexes)
        {
            Vector2 pos = h.IndexCoordinates;
            float scaler = 0.15f;
            h.Height = (int)(Mathf.PerlinNoise((pos.x + perlinNoiseOffsetX) * scaler, (pos.y + perlinNoiseOffsetY) * scaler) * 10);
            //Debug.Log("Coords: " + pos.x + "," + pos.y + ", height: " + h.Height);
        }
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

        List<TriangleHex> path = RecursiveFindPath(endPos, new List<TriangleHex> { hexes.Find(h => h.IndexCoordinates == startPos) });

        foreach (TriangleHex h in path)
        {
            h.Height = -1;
            //If needed, surrounding hexes heigth modification
        }

        foreach (TriangleHex h in hexes)
        {
            //Debug.Log("Coords: " + h.IndexCoordinates.x + "," + h.IndexCoordinates.y + ", height: " + h.Height);
            if (h.Height == -1)
            {
                h.SetMaterial(water);
            }
            else
            {
                h.SetMaterial(ground);
            }
        }
    }

    public List<TriangleHex> RecursiveFindPath(Vector2Int end, List<TriangleHex> path)
    {
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
            float currentValue = ((n.Height - path[path.Count - 1].Height) * heightWeight) + (Mathf.Sqrt(Mathf.Pow((end - n.IndexCoordinates).x, 2) + Mathf.Pow((end - n.IndexCoordinates).y, 2)) * pathWeight);
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

    public List<TriangleHex> FindPath(Vector2Int start, Vector2Int end)
    {
        Debug.Log("Start: " + start + ", end: " + end);
        List<TriangleHex> path = new List<TriangleHex>();
        path.Add(hexes.Find(h => h.IndexCoordinates == start));
        bool done = false;
        while (!done)
        {
            TriangleHex best = null;
            float bestValue = float.MaxValue;

            foreach (TriangleHex n in path[path.Count - 1].Neighbours.Values)
            {
                if (!path.Contains(n))
                {
                    float currentValue = ((n.Height - path[path.Count - 1].Height) * heightWeight) + (Mathf.Sqrt(Mathf.Pow((end - n.IndexCoordinates).x, 2) + Mathf.Pow((end - n.IndexCoordinates).y, 2)) * pathWeight);
                    if (currentValue < bestValue)
                    {
                        best = n;
                        bestValue = currentValue;
                    }
                }
            }

            Debug.Log("The best one: " + best.IndexCoordinates);
            path.Add(best);
            best.SetMaterial(selected);

            if (best.IndexCoordinates == end)
            {
                done = true;
            }
        }
        return path;
        //Rekurzívan hogy elkerüljük a saját farokba harapást
    }

    /*
    public void GenerateMap(){
        hexes[hexes.Count/2].Collapse();
        while(true){
            TriangleHex fewest = null;
            bool finished = true;
            foreach(TriangleHex h in hexes){
                if(fewest == null && h.PotentialStates.Count > 1){
                    fewest = h;
                    finished = false;
                }
                else if (h.PotentialStates.Count > 1 && h.PotentialStates.Count < fewest.PotentialStates.Count){
                    fewest = h;
                    finished = false;
                }
            }
            if(finished){
                break;
            }
            fewest.Collapse();
        }
        Debug.Log("Generated map");
    }
    */

    public void HighlightHex()
    {
        foreach (TriangleHex h in hexes)
        {
            if (h.IndexCoordinates == selectedHex)
            {
                h.ToggleHighlight();
            }
        }
    }

    public void DestroyGrid()
    {
        Debug.Log("Destroying grid...");

        foreach (TriangleHex child in hexes)
        {
            Destroy(child.gameObject);
        }
        hexes.Clear();
    }

    private Vector3 GetPositionForHexFromCoordinate(Vector2Int coordinate)
    {
        int column = coordinate.x;
        int row = coordinate.y;

        float width;
        float height;
        float xPosition = 0;
        float yPosition = 0;
        bool shouldOffset;
        float horizontalDistance;
        float verticalDistance;
        float offset;
        float hexSize = size;

        shouldOffset = (row % 2) == 0;
        width = sqrt3 * hexSize;
        height = 2f * hexSize;

        horizontalDistance = width;
        verticalDistance = height * (3f / 4f);

        offset = shouldOffset ? width / 2 : 0;

        xPosition = column * horizontalDistance + offset;
        yPosition = row * verticalDistance;

        return new Vector3(xPosition, 0, -yPosition);
    }
}
