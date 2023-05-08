using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class TestButton : MonoBehaviour
{
    [SerializeField] private HexGridLayout grid;

    void Start()
    {
        Button button = GetComponent<Button>();
        button.onClick.AddListener(() =>
        {
            long t = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            grid.LayoutGrid();
            Debug.Log("Grid layed out in " + (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond - t) + "ms");
            grid.Procedural_Map_Generate();
            Debug.Log("Generated in " + (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond - t) + "ms");
        });
    }
}
