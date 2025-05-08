using System;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using DishevelledBadger.FlashFrostVale.Server.Generator;
using DishevelledBadger.FlashFrostVale.Player;

//Hexre collider
namespace DishevelledBadger.FlashFrostVale.Server
{
    public class HexGridLayout : NetworkBehaviour
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
        public InfoPanel infoPanel; //This needs to change, info panels should be managed by the player
        public GameObject buildingTypes;

        [Header("Hex base")]
        public GameObject hex;

        [Header("Generator Object")]
        public MapGenerator generator;

        private readonly float sqrt3 = Mathf.Sqrt(3);
        public List<TriangleHex> hexes = new List<TriangleHex>();
        private List<GameObject> backgroundHexes = new List<GameObject>();
        private TriangleHex currentSelected;

        public TriangleHex CurrentSelected
        {
            get => currentSelected;
        }

        public MapGenerator Generator
        {
            get => generator;
            set => generator = value;
        }

        public void DisplayBoard()
        {
            long t = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            generator.hexes = hexes;
            LayoutGrid();
            //Debug.Log("Grid layed out in " + (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond - t) + "ms");
            Generator.Procedural_Map_Generate();
            //Debug.Log("Generated in " + (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond - t) + "ms");
            infoPanel.Hide();
            buildingTypes.transform.gameObject.SetActive(false);
        }

        public void LayoutGrid()
        {
            DestroyGrid();
            //Debug.Log($"Displaying grid {gridSize.x}, {gridSize.y}");

            for (int y = 0; y < gridSize.y; y++)
            {
                for (int x = 0; x < gridSize.x; x++)
                {
                    TriangleHex tile = Instantiate(
                            hex,
                            GetPositionForHexFromCoordinate(new Vector2Int(x, y)),
                            transform.rotation
                        )
                        .GetComponent<TriangleHex>();
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

            GlobalConstants.mapTopEdge = GetPositionForHexFromCoordinate(hexes[0].IndexCoordinates).z;
            GlobalConstants.mapLeftEdge = GetPositionForHexFromCoordinate(hexes[0].IndexCoordinates).x;
            GlobalConstants.mapBottomEdge = GetPositionForHexFromCoordinate(
                hexes[hexes.Count - 1].IndexCoordinates
            ).z;
            GlobalConstants.mapRightEdge = GetPositionForHexFromCoordinate(
                hexes[hexes.Count - 1].IndexCoordinates
            ).x;
            /*
            Debug.Log(
                "Hex list edges top, right, bottom, left: "
                    + hexes[0].IndexCoordinates.y
                    + " "
                    + hexes[0].IndexCoordinates.x
                    + " "
                    + hexes[hexes.Count - 1].IndexCoordinates.y
                    + " "
                    + hexes[hexes.Count - 1].IndexCoordinates.x
                    + "\n"
                    + "Map edges top, right, bottom, left: "
                    + GlobalConstants.mapTopEdge
                    + " "
                    + GlobalConstants.mapRightEdge
                    + " "
                    + GlobalConstants.mapBottomEdge
                    + " "
                    + GlobalConstants.mapLeftEdge
                    + "\n"
                    + "Hex size scale x, y: "
                    + hexes[0].transform.localScale.x
                    + " "
                    + hexes[0].transform.localScale.z
                    + "\n"
            );
            */
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
            //Debug.Log("Destroying grid...");

            foreach (TriangleHex child in hexes)
            {
                Destroy(child.gameObject);
            }
            hexes.Clear();
        }

        public Vector3 GetPositionForHexFromCoordinate(Vector2Int coordinate)
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
}
