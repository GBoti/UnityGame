using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriangleHex : MonoBehaviour
{
    private Vector2Int indexCoordinates;
    private Dictionary<side, TriangleHex> neighbours;
    private Material basic;
    private Material selected;
    private Material backGround;
    private string terrain;
    private Dictionary<string, float> resources;
    //private HexStates potentialStates;
    //private Dictionary<rule, Material> ruleMaterialMap;
    //private Dictionary<Material, rule> materialRuleMap;
    private float height;
    private List<GameObject> occupants;

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

    /*
    public HexStates PotentialStates
    {
        get => potentialStates;
    }
    */

    public float Height
    {
        set => height = value;
        get => height;
    }

    public Dictionary<side, TriangleHex> Neighbours
    {
        get => neighbours;
    }

    public Dictionary<string, float> Resources
    {
        get => resources;
        set => resources = value;
    }

    public void InitiateHex(Vector2Int iC, Material b, Material s, Material bG, Material g, Material w)
    {
        indexCoordinates = iC;
        neighbours = new Dictionary<side, TriangleHex>();
        basic = b;
        selected = s;
        backGround = bG;
        resources = new Dictionary<string, float>();

        /*
        ruleMaterialMap = new Dictionary<rule, Material>();
        ruleMaterialMap[rule.water] = w;
        ruleMaterialMap[rule.ground] = g;

        materialRuleMap = new Dictionary<Material, rule>();
        materialRuleMap[w] = rule.water;
        materialRuleMap[g] = rule.ground;

        potentialStates = new HexStates();
        */
    }

    public void Clicked()
    {
        SetBackgroundMaterial(selected);
    }

    public void Declicked()
    {
        SetBackgroundMaterial(backGround);
    }

    public void SetMaterial(Material m)
    {
        basic = m;
        for (int i = 0; i < transform.childCount - 1; i++)
        {
            transform.GetChild(i).GetComponent<MeshRenderer>().material = m;
        }
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

    public void GenerateResources()
    {
        resources["food"] = 0.0f;
        resources["wood"] = 0.0f;
        resources["mud"] = 0.0f;
        resources["stone"] = 0.0f;

        switch (terrain)
        {
            case "Water":
                resources["food"] = UnityEngine.Random.Range(1, 3);
                resources["wood"] = UnityEngine.Random.Range(0, 1);
                resources["mud"] = UnityEngine.Random.Range(1, 2);
                resources["stone"] = UnityEngine.Random.Range(0, 1);
                break;
            case "Sand":
                resources["food"] = 0.0f;
                resources["wood"] = 0.0f;
                resources["mud"] = UnityEngine.Random.Range(0, 1);
                resources["stone"] = UnityEngine.Random.Range(0, 1);
                break;
            case "Meadow":
                resources["food"] = UnityEngine.Random.Range(1, 3);
                resources["wood"] = UnityEngine.Random.Range(0, 1);
                resources["mud"] = 0.0f;
                resources["stone"] = 0.0f;
                break;
            case "Forest":
                resources["food"] = UnityEngine.Random.Range(0, 1);
                resources["wood"] = UnityEngine.Random.Range(2, 4);
                resources["mud"] = 0.0f;
                resources["stone"] = 0.0f;
                break;
            case "Mountain":
                resources["food"] = 0.0f;
                resources["wood"] = 0.0f;
                resources["mud"] = 0.0f;
                resources["stone"] = UnityEngine.Random.Range(1, 3);
                break;
        }
    }

    /*
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
    */



    /*
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
    */
}
