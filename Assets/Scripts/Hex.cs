using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hex : MonoBehaviour
{
    private Vector2Int indexCoordinates;

    private List<Hex> neighbours;

    private Material basic;
    private Material selected;
    private Material neighbour;
    private Material backGround;
    private bool showNeighbours;
    private string terrain;

    public Vector2Int IndexCoordinates{
        set => indexCoordinates = value;
        get => indexCoordinates;
    }

    public string Terrain{
        set => terrain = value;
        get => terrain;
    }

    public List<Hex> Neighbours{
        set => neighbours = value;
        get => neighbours;
    }

    public Material Basic{
        set => basic = value;
        get => basic;
    }
    public void InitiateHex(Vector2Int iC, Material b, Material s, Material n, Material bG){
        indexCoordinates = iC;
        neighbours = new List<Hex>();
        showNeighbours = true;
        basic = b;
        selected = s;
        neighbour = n;
        backGround = bG;
    }
/*
    void Update(){
        if(Input.GetMouseButtonDown(0)){
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if(Physics.Raycast(ray, out hit)){
                if(hit.transform.name == "Hex"){
                    
                }
            }
        }
    }
*/
    public void ToggleHighlight(){
        if(showNeighbours){
            SetBackgroundMaterial(selected);
            foreach(Hex n in neighbours){
                if (n != null){
                    n.SetBackgroundMaterial(neighbour);
                }
            }
            showNeighbours = false;
        } else if (!showNeighbours){
            SetBackgroundMaterial(backGround);
            foreach(Hex n in neighbours){
                if (n != null){
                    n.SetBackgroundMaterial(backGround);
                }
            }
            showNeighbours = true;
        }
    }

    public void SetMaterial(Material mat){
        gameObject.GetComponent<MeshRenderer>().material = mat;
    }

    public void SetBackgroundMaterial(Material mat){
        transform.GetChild(0).gameObject.GetComponent<MeshRenderer>().material = mat;
    }

    public Material GetMaterial(){
        return gameObject.GetComponent<MeshRenderer>().material;
    }

    public void AddNeighbour(Hex nb){
        neighbours.Add(nb);
    }
}
