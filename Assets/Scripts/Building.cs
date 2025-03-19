using System.Collections.Generic;
using UnityEngine;

public class Building : MonoBehaviour
{
    [Header("Name")]
    public string buildingName;

    [Header("Production")]
    public float food;
    public float wood;
    public float mud;
    public float stone;

    [Header("Cost")]
    public float foodCost;
    public float woodCost;
    public float mudCost;
    public float stoneCost;

    [Header("Description")]
    public string desc;
    //[Header("Influence Radius")]
    //public int influenceRadius;
}
