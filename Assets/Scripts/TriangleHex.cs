using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriangleHex : MonoBehaviour
{
    private Vector2Int indexCoordinates;

    //private List<TriangleHex> neighbours;
    private Dictionary<side, TriangleHex> neighbours;

    private Material basic;
    private Material selected;
    private Material neighbour;
    private Material backGround;
    private bool showNeighbours;
    private string terrain;
    private HexStates potentialStates;
    private Dictionary<rule, Material> ruleMaterialMap;
    private Dictionary<Material, rule> materialRuleMap;
    private float height;

    public Vector2Int IndexCoordinates
    {
        set => indexCoordinates = value;
        get => indexCoordinates;
    }

    public string Terrain
    {
        set => terrain = value;
        get => terrain;
    }

    public Material Basic
    {
        set => basic = value;
        get => basic;
    }

    public HexStates PotentialStates
    {
        get => potentialStates;
    }

    public float Height
    {
        set => height = value;
        get => height;
    }

    public Dictionary<side, TriangleHex> Neighbours
    {
        get => neighbours;
    }

    public void InitiateHex(Vector2Int iC, Material b, Material s, Material n, Material bG, Material g, Material w)
    {
        indexCoordinates = iC;
        neighbours = new Dictionary<side, TriangleHex>();
        showNeighbours = true;
        basic = b;
        selected = s;
        neighbour = n;
        backGround = bG;

        ruleMaterialMap = new Dictionary<rule, Material>();
        ruleMaterialMap[rule.water] = w;
        ruleMaterialMap[rule.ground] = g;

        materialRuleMap = new Dictionary<Material, rule>();
        materialRuleMap[w] = rule.water;
        materialRuleMap[g] = rule.ground;

        //Debug.Log("Keys: " + w + ", " + g);

        potentialStates = new HexStates();
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

        public void SetTerrainHeight(float h)
        {
            transform.GetChild(0).localPosition.Set(transform.GetChild(0).localPosition.x, transform.GetChild(0).localPosition.y + h, transform.GetChild(0).localPosition.z);
        }
    */

    public void ToggleHighlight()
    {
        if (showNeighbours)
        {
            SetBackgroundMaterial(selected);
            foreach (KeyValuePair<side, TriangleHex> n in neighbours)
            {
                n.Value.SetBackgroundMaterial(neighbour);
            }
            showNeighbours = false;
        }
        else if (!showNeighbours)
        {
            SetBackgroundMaterial(backGround);
            foreach (KeyValuePair<side, TriangleHex> n in neighbours)
            {
                n.Value.SetBackgroundMaterial(backGround);
            }
            showNeighbours = true;
        }
    }

    public void SetMaterial(Material m)
    {
        basic = m;
        for (int i = 0; i < transform.childCount - 1; i++)
        {
            transform.GetChild(i).GetComponent<MeshRenderer>().material = m;
        }
    }

    public void SetMaterialByState(State state)
    {
        bool isWater = true;
        for (int i = 0; i < state.rules.Length; i++)
        {
            transform.GetChild(i + 1).GetComponent<MeshRenderer>().material = ruleMaterialMap[state.rules[i]];
            if (state.rules[i] == rule.ground)
            {
                isWater = false;
            }
        }
        transform.GetChild(0).GetComponent<MeshRenderer>().material = isWater ? ruleMaterialMap[rule.water] : ruleMaterialMap[rule.ground];
    }

    public void SetBackgroundMaterial(Material mat)
    {
        transform.GetChild(7).gameObject.GetComponent<MeshRenderer>().material = mat;
    }

    public Material GetMaterial()
    {
        return transform.GetChild(0).GetComponent<MeshRenderer>().material;
    }

    public void AddNeighbour(TriangleHex nb, side s)
    {
        neighbours[s] = nb;
    }

    public void Collapse()
    {
        if (potentialStates.Count != 1)
        {
            potentialStates.Collapse();
        }
        SetMaterialByState(potentialStates.States[0]);
        foreach (KeyValuePair<side, TriangleHex> kvp in neighbours)
        {
            if (kvp.Value.PotentialStates.Count > 1)
            {
                int s = (int)kvp.Key;
                if (s >= 3)
                {
                    s -= 3;
                }
                else
                {
                    s += 3;
                }
                s++;
                Material m = transform.GetChild(s).GetComponent<MeshRenderer>().sharedMaterial;
                Debug.Log("Material: " + m + ", rule: " + materialRuleMap[m] + " Sender: " + kvp.Key + " Reciever: " + (side)(s - 1) + " Sender coords: " + indexCoordinates);
                kvp.Value.Reduce(kvp.Key, materialRuleMap[m]);
            }
        }
    }

    public void Reduce(side s, rule r)
    {
        potentialStates.Reduce(s, r);
        Debug.Log("Side: " + s + " ,rule: " + r);
        if (potentialStates.Count == 1)
        {
            Collapse();
        }
    }
}
