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
            grid.GenerateMap();
            Debug.Log(DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond - t);
        });
    }
}
