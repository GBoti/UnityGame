using System.Collections;
using System.Collections.Generic;
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
            grid.LayoutGrid();
        });
    }
}
