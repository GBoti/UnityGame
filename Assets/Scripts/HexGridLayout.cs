using System;
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

    [Header("Pathfinding weights")]
    public float heightWeight;
    public float pathWeight;

    [Header("Materials")]

    public Material ground;
    public Material water;
    public Material backGround;
    public Material selected;
    public Material neighbour;
    public Material forest;
    public Material sand;
    public Material mountain;
    public Material mountainPeak;

    [Header("Info panel")]
    public InfoPanel infoPanel;
    public GameObject buildingTypes;

    [Header("Hex base")]
    public GameObject hex;
    [Header("Main building prefab")]
    public Building mainBuilding;
    public Colony colony;
    public new CameraController camera;

    private readonly float sqrt3 = Mathf.Sqrt(3);
    private List<TriangleHex> hexes = new List<TriangleHex>();
    private List<GameObject> backgroundHexes = new List<GameObject>();
    private TriangleHex currentSelected;

    public TriangleHex CurrentSelected
    {
        get => currentSelected;
    }

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

    public void DisplayBoard()
    {
        long t = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
        LayoutGrid();
        Debug.Log("Grid layed out in " + (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond - t) + "ms");
        Procedural_Map_Generate();
        Debug.Log("Generated in " + (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond - t) + "ms");
        infoPanel.Hide();
        buildingTypes.transform.gameObject.SetActive(false);
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
                tile.InitiateHex(new Vector2Int(x, y), ground, selected, backGround, ground, water);
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
            foreach (TriangleHex n in h.Neighbours.Values)
            {
                if (n.Height != -1 && n.Height < 7)
                {
                    n.Height = -0.5f;
                }
                if (n.Height >= 9)
                {
                    n.Height = 8;
                }
            }
        }

        foreach (TriangleHex h in hexes)
        {
            switch (h.Height)
            {
                case -1:
                    h.SetMaterial(water);
                    h.Terrain = "Water";
                    break;
                case -0.5f:
                    h.SetMaterial(sand);
                    h.Terrain = "Sand";
                    break;
                case < 3:
                    h.SetMaterial(ground);
                    h.Terrain = "Meadow";
                    break;
                case < 7:
                    h.SetMaterial(forest);
                    h.Terrain = "Forest";
                    break;
                case < 9:
                    h.SetMaterial(mountain);
                    h.Terrain = "Mountain";
                    break;
                default:
                    h.SetMaterial(mountainPeak);
                    h.Terrain = "Snowy Peak";
                    break;
            }
            h.GenerateResources();
        }

        List<TriangleHex> meadows = hexes.FindAll(h => h.Terrain == "Meadow");
        int index = UnityEngine.Random.Range(0, meadows.Count - 1);
        colony.AddBuilding(meadows[index], mainBuilding);
        camera.transform.position = GetPositionForHexFromCoordinate(meadows[index].IndexCoordinates);
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

    public void ManageSelected(TriangleHex clicked)
    {
        infoPanel.Hide();
        buildingTypes.transform.gameObject.SetActive(false);
        if (currentSelected != null)
        {
            currentSelected.Declicked();
        }
        if (currentSelected == clicked)
        {
            currentSelected = null;
        }
        else
        {
            currentSelected = clicked;
            clicked.Clicked();
            infoPanel.Show(clicked);
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

    /*
    TODO: On holding alt display resources on hexes
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.LeftAlt))
        {

        }
        if (Input.GetKeyUp(KeyCode.LeftAlt))
        {

        }
    }
    */
}
