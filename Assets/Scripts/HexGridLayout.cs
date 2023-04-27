using System;
using System.Collections;
using System.Collections.Generic;
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

    public Material ground;
    public Material water;
    public Material backGround;
    public Material selected;
    public Material neighbour;

    public GameObject hex;

    private readonly float sqrt3 = Mathf.Sqrt(3);
    private List<TriangleHex> hexes = new List<TriangleHex>();
    private List<GameObject> backgroundHexes = new List<GameObject>();

    private void OnEnable()
    {
        LayoutGrid();
        GenerateMap();
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
                tile.transform.localScale = new Vector3(size*20, size*12, size*20);
                tile.transform.localRotation *= Quaternion.Euler(0f, 0f, 0f);
                tile.Terrain = "ground";
                tile.InitiateHex(new Vector2Int(x,y), ground, selected, neighbour, backGround, ground, water);
                // Current hex will go in the current = y * gridSize.x + x; slot in hexes
                int current = y * gridSize.x + x;
                // Need to add the already existing hexes, but need to check if they exist
                // (In the case of hex 0,0 there will be no other existing hexes)
                // In case of a non-existent hex, set the value to null
                // Already existing hexes are: Left, Top-left, Top-right, since
                // the grid is built from the top-left to the right then down.
                // the left is easy allways current-1, except when x == 0, then it's null
                
                if ( x != 0 ){
                    foreach(TriangleHex h in hexes){
                        if(h.IndexCoordinates.x == x - 1 && h.IndexCoordinates.y == y){
                            tile.AddNeighbour(h, side.right);
                            h.AddNeighbour(tile, side.left);
                        }
                    }
                }
                if (!(y == 0 || (x == 0 && y % 2 != 0))){
                    foreach(TriangleHex h in hexes){
                        if(y % 2 == 0){
                            if(h.IndexCoordinates.x == x && h.IndexCoordinates.y == y - 1){
                                tile.AddNeighbour(h, side.bottomright);
                                h.AddNeighbour(tile, side.topleft);
                            }
                        }
                        if(y % 2 != 0){
                            if(h.IndexCoordinates.x == x - 1 && h.IndexCoordinates.y == y - 1){
                                tile.AddNeighbour(h, side.bottomright);
                                h.AddNeighbour(tile, side.topleft);
                            }
                        }
                    }
                }
                if (!(y == 0 || (x == ((y + 1) * gridSize.x) - 1 && y % 2 == 0))){
                    foreach(TriangleHex h in hexes){
                        if(y % 2 == 0){
                            if(h.IndexCoordinates.x == x + 1 && h.IndexCoordinates.y == y - 1){
                                tile.AddNeighbour(h, side.bottomleft);
                                h.AddNeighbour(tile, side.topright);
                            }
                        }
                        if(y % 2 != 0){
                            if(h.IndexCoordinates.x == x && h.IndexCoordinates.y == y - 1){
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
        /*for(int i = 0; i < hexes.Count; i++){
            GenerateTerrain(hexes[i]);
            GenerateTerrain(hexes[hexes.Count - 1 - i]);
        }*/
    }

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
    /*
    public void GenerateTerrain(TriangleHex h){
        int groundChance = 50;
        int waterChance = 50;
        int groundNeighbours = 0;
        int waterNeighbours = 0;
        foreach (TriangleHex n in h.Neighbours){
            if(n.Terrain == "ground"){
                groundNeighbours++;
            }
            if(n.Terrain == "water"){
                waterNeighbours++;
            }
        }

        if(groundNeighbours/h.Neighbours.Count*100 > 80){
            waterChance += 10;
            groundChance -= 10;
        }
        if(waterNeighbours >= 3){
            waterChance -= 40;
            groundChance += 40;
        }
        if(waterNeighbours == 6){
            waterChance = 100;
            groundChance = 0;
        }

        int num = UnityEngine.Random.Range(1,100);
        if(num <= groundChance){
            h.SetMaterial(ground);
            h.Terrain = "ground";
            h.Basic = ground;
        } else if (num <= groundChance + waterChance){
            h.SetMaterial(water);
            h.Terrain = "water";
            h.Basic = water;
        }
    }
    */

    public void HighlightHex(){
        foreach (TriangleHex h in hexes)
        {
            if(h.IndexCoordinates == selectedHex){
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
